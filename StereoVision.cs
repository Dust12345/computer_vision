#region

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using Emgu.CV;
using Emgu.CV.Structure;
using GLab.Core;
using GLab.Core.Forms;
using GLab.Core.PluginInterfaces;
using GLab.VirtualAibo;
using GLab.VirtualAibo.Forms;
using Microsoft.Xna.Framework;

#endregion

namespace Frame.VrAibo
{

    public class MovePair
    {
        public MovePair()
        {
            left = false;
            rigth = false;
            froont = false;
            isIntersection = false;
            movement = 0;
            turn = 0;
        }

        public float movement;
        public float turn;
        public bool left;
        public bool rigth;
        public bool froont;
        public bool isIntersection;
    }

    internal class StereoVision : IPluginClient
    {
        private const float AiboSpeed = 0.3f;
        private FrmImage _frmImage;
        private FrmImage _referenceImage;
        private FrmVrAiboRemote _frmVrAiboRemote;
        private GLab.VirtualAibo.VrAibo _vrAibo;
        private GLab.StereoVision.StereoVision _stereoVision;
        private double lastDepth = 0;

        //private StateMachine sm;

        float distMovedTillLastTurn = 0;

        private bool moveBack = false;

        List<MovePair> movePairs;

        //-----------------

        private const int ScanLineStartY = GLab.VirtualAibo.VrAibo.SurfaceHeight - 50;

        // Compute alpha value (horizontal pixel expansion)
        private const float Alpha = GLab.VirtualAibo.VrAibo.FovY / GLab.VirtualAibo.VrAibo.SurfaceWidth;

        private FrmImage _frmEyeCenter;

        private FrmImage frontWindow;
        private FrmImage leftWindow;
        private FrmImage rigthWindow;

        private Image<Rgb, byte> front;
        private Image<Rgb, byte> left;
        private Image<Rgb, byte> rigth;

        private int lastFrontStart = -1;
        private int lastFrontEnd = -1;
        private int lineStartFront = -1;
        private int lineEndFront = -1;



        int picsTaken = 0;

        private bool justReturnedFromTrackBack = false;

        private FrmImage stuff;

        int pathMinThreshold = 60;

        //--------------


        public StereoVision()
        {
            Name = "Stereo Vision Lab";
        }

        public bool GoAibo { get; set; }

        public override void Setup()
        {

            //sm = new StateMachine();

            movePairs = new List<MovePair>();

            /*
             * DEFAULT PARCOURS LAB TASK WITH TERRAIN
             */
            List<Parcours> parcours = new List<Parcours>
                                          {
                                              new ParcoursTerrain(),
                                              new Parcours00(0, 0, 0, false),
                                              new Parcours01(1, 0, -90, false),
                                              new Parcours02(1, 1, 180, false),
                                              new Parcours03(0, 1, 90, false)
                                          };

            // Creates a new Virtual Aibo
            //_vrAibo = new GLab.VirtualAibo.VrAibo(parcours) { Position = new Vector2(0.4f, 35) };


            _vrAibo = new GLab.VirtualAibo.VrAibo(parcours) { Position = new Vector2(0.047f, -12.66f) };

            //{X:0,04798115 Y:-12,66051}


            
          //  _vrAibo.Position.X =  0.4;
           //  _vrAibo.Position.Y =  34.7;
            

            /*
             * TREASURE LAB TASK
             */
            //List<Parcours> stripes = new List<Parcours>
            //                             {
            //                                 new Stripe00(),
            //                                 new Stripe01(),
            //                                 new Stripe02(),
            //                                 new Stripe03(),
            //                                 new StripeTreasure()
            //                             };

            /*
             * TREASURE LAB TASK WITH TERRAIN
             */
            //List<Parcours> stripes = new List<Parcours>
            //                             {
            //                                 new ParcoursTerrain(),
            //                                 new Stripe00(false),
            //                                 new Stripe01(false),
            //                                 new Stripe02(false),
            //                                 new Stripe03(false),
            //                                 new StripeTreasure(false)
            //                             };

            // Creates a new Virtual Aibo
            //_vrAibo = new GLab.VirtualAibo.VrAibo(stripes) { Position = new Vector2(0, 20) };

            // Use left and right cameras, omit center camera
            _vrAibo.ShowLeftCamera(true);
            _vrAibo.ShowRightCamera(true);
            _vrAibo.ShowCenterCamera(false);

            // Create a remote control window an link to Aibo instance
            _frmVrAiboRemote = new FrmVrAiboRemote(_vrAibo, _timer);

            // Hook functionality to blue green and red button.
            _frmVrAiboRemote.HookMeBlue += delegate { GoAibo = !GoAibo; };
            _frmVrAiboRemote.HookMeGreen += ImageProcessing;
            // _frmVrAiboRemote.HookMeRed += delegate { };

            // Create a new window for the processed center image
            // Unprocessed left/right/center/birdview windows are created and updated automatically
            _frmImage = new FrmImage("Disparities",
                                         GLab.VirtualAibo.VrAibo.SurfaceWidth,
                                         GLab.VirtualAibo.VrAibo.SurfaceHeight);




            frontWindow = new FrmImage("Front",
                                     GLab.VirtualAibo.VrAibo.SurfaceWidth,
                                     GLab.VirtualAibo.VrAibo.SurfaceHeight);
            leftWindow = new FrmImage("Left",
                                       GLab.VirtualAibo.VrAibo.SurfaceWidth,
                                       GLab.VirtualAibo.VrAibo.SurfaceHeight);
            rigthWindow = new FrmImage("Rigth",
                                       GLab.VirtualAibo.VrAibo.SurfaceWidth,
                                       GLab.VirtualAibo.VrAibo.SurfaceHeight);

            // Create a new window for the reference image, which has the same dimensions
            // as the resulting image after applying the sgm algorithm
            // Note that in the current SGM Implementation, this is *not* the same as the center image
            _referenceImage = new FrmImage("Reference Image",
                                         GLab.VirtualAibo.VrAibo.SurfaceWidth,
                                         GLab.VirtualAibo.VrAibo.SurfaceHeight);


            Logger.Instance.LogInfo("Use Aibo remote to walk around.");
            Logger.Instance.LogInfo("Walk onto the red line and press the BLUE button to start line tracking.");

            // Setup stereo vision facilities
            _stereoVision = new GLab.StereoVision.StereoVision(
                                            GLab.VirtualAibo.VrAibo.SurfaceWidth,
                                            GLab.VirtualAibo.VrAibo.SurfaceHeight,
                                            _vrAibo.Disparity * 200
                                            );




            //----------------

            // Create a new window for the processed center image
            // Unprocessed left/right/center/birdview windows are created and updated automatically
            _frmEyeCenter = new FrmImage("Processing Center", GLab.VirtualAibo.VrAibo.SurfaceWidth,
                                           GLab.VirtualAibo.VrAibo.SurfaceHeight, DisplayMode.Original);

            // Create a new window for the processed center image
            // Unprocessed left/right/center/birdview windows are created and updated automatically
            stuff = new FrmImage("Stuff", GLab.VirtualAibo.VrAibo.SurfaceWidth,
                                           GLab.VirtualAibo.VrAibo.SurfaceHeight, DisplayMode.Original);

            //-----



            // Start the looped execution of Run()
            _vrAibo.Update();
            ImageProcessing();
            Start();
        }

        public override void Run()
        {
            if (GoAibo)
            {
                ImageProcessing();
            }
            _vrAibo.Update();
        }

        public override void Teardown()
        {
            // Stop the execution of Run()
            Stop();

            // Clean up after yourself!
            _vrAibo.Dispose();
            _frmImage.Dispose();
            _frmVrAiboRemote.Dispose();
            _frmImage.Dispose();
            _referenceImage.Dispose();
        }

        /// <summary>
        ///   Let's do some image processing and move aibo
        /// </summary>
        private void ImageProcessing()
        {
            float turn;

            if (TrackLine(out turn))
            {
                //_vrAibo.Turn(turn);
                //_vrAibo.Walk(AiboSpeed);
            }
        }

        private int scanHorizontal(Image<Rgb, byte> img, System.Drawing.Point poo,Rgb pathColor,int dir)
        {

            int x = poo.X;
            int y = poo.Y;

            //scan starting from the point of interest into the given dir until the color of that pixel does not longer match the path color
            while (true)
            {             
                //check if we ran of the image
                if (x < 0 || x >= img.Width)
                {
                    return x;
                }
                else
                {
                    //check the color
                    if (pathColor.Blue == img[y, x].Blue && pathColor.Red == img[y, x].Red && pathColor.Green == img[y, x].Green)
                    {
                        //still the same color, keep going
                        x = x + dir;
                    }
                    else
                    {
                        //end of the path reached
                        return x -dir;
                    }
                }
            }            
        }

        private bool scanAhead(Image<Rgb, byte> img, System.Drawing.Point poo, Image<Rgb, byte> dbImage, Rgb pathColor,ref System.Drawing.Point pointOfIntersection,out int dir)
        {
            int scanInterval = 15;

            stuff.SetImage(img);

            dir = 0;

            int y = poo.Y;
            int x = poo.X;
            int stepsTillLastHorizontalScan = 0;
            int pathEnd = 0;

            pathColor = img[poo];

            //scan verticaly until the path ends
            while (true)
            {
                //check if we ran of the image
                if (y < 0)
                {
                    //ran of the image
                    break;
                }
                else
                {                   
                   
                    //check if the color is still the same
                    if (pathColor.Blue == img[y, x].Blue && pathColor.Red == img[y, x].Red && pathColor.Green == img[y, x].Green)
                    {
                        //still on the path
                        //check if we need to start a horizontal scan
                        if (stepsTillLastHorizontalScan == scanInterval)
                        {
                            //start an horizontal scan
                            System.Drawing.Point origin = new System.Drawing.Point(x, y);
                            int pathEndLeft = scanHorizontal(img, origin, pathColor, -1);
                            int pathEndRigth = scanHorizontal(img, origin, pathColor, 1);

                            LineSegment2D ls2 = new LineSegment2D(new System.Drawing.Point(pathEndLeft, y), new System.Drawing.Point(pathEndRigth, y));
                          

                            //check if an intersection
                            if (pathEndLeft == 0 || pathEndRigth == img.Width)
                            {

                                pointOfIntersection.X = origin.X;
                                pointOfIntersection.Y = origin.Y;

                                //chck which way the intersection
                                if (pathEndLeft == 0)
                                {
                                    dir = -1;
                                }
                                else
                                {
                                    dir = 1;
                                }

                                dbImage.Draw(ls2, new Rgb(0, 255, 0), 2);
                                return true;

                            }
                            else
                            {
                                dbImage.Draw(ls2, new Rgb(0, 0, 255), 2);
                            }


                            //reset the counter
                            stepsTillLastHorizontalScan = 0;
                        }
                        else
                        {
                            stepsTillLastHorizontalScan++;
                        }
                        y--;
                    }
                    else
                    {                       
                        //path endet
                        pathEnd = y;
                        break;
                    }

                }
            }
            LineSegment2D ls3 = new LineSegment2D(new System.Drawing.Point(poo.X, poo.Y), new System.Drawing.Point(poo.X, pathEnd));
            dbImage.Draw(ls3, new Rgb(0, 0, 255), 2);
            return false;
        }

        private bool isSidePath(int lineStart,int lineEnd,int imgWidth)
        {
            //first case applies if the path is wider than the image
            if (lineStart == 0 && lineEnd == imgWidth)
            {
                return true;
            }
            else if (lineStart > 0 && lineEnd <= imgWidth)
            {
                return true;
            }

            return false;
        }

        private bool scanForPath(Image<Rgb, byte> img, int heigth, out int lineStart, out int lineEnd)
        {

            lineStart = 0;
            lineEnd = 256;

            // ...and find the line
            for (int x = 0; x < img.Width; ++x)
            {

                //color to check the path
                Rgb pixel = img[heigth, x];
                int puixelOfSameColor = 0;
                lineStart = x;

                //look ahead to see if this is the color of tzhe path
                for (int j = x; j < img.Width; j++)
                {
                    if (pixel.Blue == img[heigth, j].Blue && pixel.Red == img[heigth, j].Red && pixel.Green == img[heigth, j].Green)
                    {
                        puixelOfSameColor++;
                        lineEnd = j;
                    }
                    else
                    {
                        lineEnd = j - 1;
                        break;
                    }
                }



                //cherck if this was the path
                if (puixelOfSameColor >= pathMinThreshold)
                {                   
                    return true;
                }

            }

            return false;
        }


        

        private bool checkIfvalidPath(int start, int end, int heigth, Image<Rgb, byte> img,int scanDist)
        {
            int center = (end - ((end - start) / 2));

            Rgb referenceColor = img[heigth, center];

            for (int i = 0; i < scanDist; i++)
            {
                if (referenceColor.Blue == img[heigth-i, center].Blue && referenceColor.Red == img[heigth-i, center].Red && referenceColor.Green == img[heigth-i, center].Green)
                  {

                  }
                else
                {
                    LineSegment2D ls = new LineSegment2D(new System.Drawing.Point(center, heigth), new System.Drawing.Point(center, heigth-i));

                    img.Draw(ls, new Rgb(0, 255, 0), 2);

                    return false;
                }
            }

            LineSegment2D ls1 = new LineSegment2D(new System.Drawing.Point(center, heigth), new System.Drawing.Point(center, heigth - scanDist));

            img.Draw(ls1, new Rgb(0, 0, 255), 2);
            return true;
        }

        private bool scanForSidePathRigth(Image<Rgb, byte> img, int scanHeigth, int minSegmentLength)
        {

            int sameColorPixels = 0;

            Rgb pathColor = img[scanHeigth, 0];


            //the path we are intereted in should start at the rigth side of the image, if the path would lie in the center of the image the normal side image eval algo would have detected that path
            for (int i = 0; i < img.Width; i++)
            {
                //check whether the pixel still is of the color we are looking for
                if (pathColor.Blue == img[scanHeigth, i].Blue && pathColor.Red == img[scanHeigth, i].Red && pathColor.Green == img[scanHeigth, i].Green)
                {
                    sameColorPixels++;
                }
                else
                {
                    //a different color was scanned, check if we saw enough pixel
                    if (sameColorPixels >= minSegmentLength)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

            }

            //realisticly this should never be reached
            return false;
        }


        private bool scanForSidePathLeft(Image<Rgb, byte> img, int scanHeigth, int minSegmentLength)
        {

            int sameColorPixels = 0;

            Rgb pathColor = img[scanHeigth, img.Width-1];


            //the path we are intereted in should start at the rigth side of the image, if the path would lie in the center of the image the normal side image eval algo would have detected that path
            for (int i = img.Width-1; i >0 ; i--)
            {
                //check whether the pixel still is of the color we are looking for
                if (pathColor.Blue == img[scanHeigth, i].Blue && pathColor.Red == img[scanHeigth, i].Red && pathColor.Green == img[scanHeigth, i].Green)
                {
                    sameColorPixels++;
                }
                else
                {
                    //a different color was scanned, check if we saw enough pixel
                    if (sameColorPixels >= minSegmentLength)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

            }

                //realisticly this should never be reached
                return false;
        }


        private void moveBasedOnImage()
        {
            Logger.Instance.LogInfo("----------------------------------");

            //save the values from the last interation for comparison
            lastFrontStart = lineStartFront;
            lastFrontEnd = lineEndFront;

            int minMoveDistanceTillNextTurn = 10;

            bool frontOK = false;

            int lookAheadDistance = 10;

        
            int scanHeigth = GLab.VirtualAibo.VrAibo.SurfaceHeight - 10;           

            scanForPath(front, scanHeigth, out lineStartFront, out lineEndFront);

            //check if the return values suggest that ther is a path
            if (lineStartFront != -1 && lineEndFront != -1)
            {
                LineSegment2D ls = new LineSegment2D(new System.Drawing.Point(lineStartFront, scanHeigth), new System.Drawing.Point(lineEndFront, scanHeigth));

                frontOK = checkIfvalidPath(lineStartFront, lineEndFront, scanHeigth, front, lookAheadDistance);              

                front.Draw(ls, new Rgb(0, 0, 255), 2);
            }

            scanHeigth = GLab.VirtualAibo.VrAibo.SurfaceHeight - 40;
            int lineStartLeft = -1;
            int lineEndLeft = -1;

            bool LeftOK = false;

            bool pathFound = scanForPath(left, scanHeigth, out lineStartLeft, out lineEndLeft);

            //check if the return values suggest that ther is a path
            if (pathFound && lineStartLeft != -1 && lineEndLeft != -1 && isSidePath(lineStartLeft, lineEndLeft, left.Width))
            {
                LeftOK = checkIfvalidPath(lineStartLeft, lineEndLeft, scanHeigth, left, 20);

                LineSegment2D ls = new LineSegment2D(new System.Drawing.Point(lineStartLeft, scanHeigth), new System.Drawing.Point(lineEndLeft, scanHeigth));
                left.Draw(ls, new Rgb(0, 0, 255), 2);
            }


            int lineStartRigth = -1;
            int lineEndRigth = -1;

            bool rigthOK = false;


            pathFound = scanForPath(rigth, scanHeigth, out lineStartRigth, out lineEndRigth);


          
            //check if the return values suggest that ther is a path
            if (pathFound && lineStartRigth != -1 && lineEndRigth != -1 && isSidePath(lineStartRigth, lineEndRigth, rigth.Width))
            {
                rigthOK = checkIfvalidPath(lineStartRigth, lineEndRigth, scanHeigth, rigth, 20);

                

                LineSegment2D ls = new LineSegment2D(new System.Drawing.Point(lineStartRigth, scanHeigth), new System.Drawing.Point(lineEndRigth, scanHeigth));
                
                rigth.Draw(ls, new Rgb(0, 0, 255), 2);
            }


            //check we returned to a previusly visited intersection
            if (justReturnedFromTrackBack)
            {

                Logger.Instance.LogInfo("Just returned");

                justReturnedFromTrackBack = false;
                //check wether the intersection has a path we did not visit yet
                if (movePairs.Count == 1)
                {

                    MovePair mp = movePairs[0];

                    distMovedTillLastTurn = 0;
                    if (movePairs[0].left)
                    {
                        _vrAibo.Turn(90);
                        _vrAibo.Walk(AiboSpeed);
                        distMovedTillLastTurn += AiboSpeed;

                        mp.left = false;
                        mp.movement = AiboSpeed;
                        mp.turn = 90;
                        movePairs.Add(mp);
                    }
                    else if (movePairs[0].froont)
                    {

                        _vrAibo.Walk(AiboSpeed);
                        distMovedTillLastTurn += AiboSpeed;

                        mp.froont = false;
                        mp.movement = AiboSpeed;
                        movePairs.Add(mp);
                    }
                    else
                    {
                        _vrAibo.Turn(-90);
                        _vrAibo.Walk(AiboSpeed);
                        distMovedTillLastTurn += AiboSpeed;

                        mp.rigth = false;
                        mp.movement = AiboSpeed;
                        mp.turn = -90;
                        movePairs.Add(mp);
                    }

                }
                else
                {
                    //all paths of the intersection were visited, turn around and move back
                    _vrAibo.Turn(180);
                    _vrAibo.Walk(AiboSpeed);
                }
            }
            else
            {
                //not directly on an old intersection             

                //check of the front is clear
                if (frontOK)
                {
                    Logger.Instance.LogInfo("Front ok");
                    Logger.Instance.LogInfo("Status R: "+rigthOK);
                    Logger.Instance.LogInfo("Min Status: " + (distMovedTillLastTurn < minMoveDistanceTillNextTurn));
                    Logger.Instance.LogInfo("Moved: " + distMovedTillLastTurn + " must move: " + minMoveDistanceTillNextTurn);
                    


                    //check if there are no side paths or if we ignore them because we just turned
                    if ((!LeftOK && !rigthOK) || distMovedTillLastTurn < minMoveDistanceTillNextTurn)
                    {

                        Logger.Instance.LogInfo("move forwards");
                        //just move front
                        //only way to move it forward
                        int diffX = (GLab.VirtualAibo.VrAibo.SurfaceWidth / 2) - (lineEndFront - ((lineEndFront - lineStartFront) / 2));
                        float phi = Alpha * diffX;

                        MovePair mp = new MovePair();
                        mp.movement = AiboSpeed;

                        int sDiff = Math.Abs(lastFrontStart- lineStartFront);
                        int eDiff = Math.Abs(lastFrontEnd- lineEndFront);


                       /* if (sDiff > 30)
                        {
                            Logger.Instance.LogInfo("hard turn triggred");
                            phi = phi * 4;
                        }*/

                        if (phi != 0.0f)
                        {
                            //Logger.Instance.LogInfo("Turning by " + phi + " degree");
                            _vrAibo.Turn(phi / 2);
                            mp.turn = phi / 2;
                        }

                        _vrAibo.Walk(AiboSpeed);
                        movePairs.Add(mp);
                        distMovedTillLastTurn += AiboSpeed;
                    }
                    else
                    {
                        //a possible intersection has beed detected

                        Logger.Instance.LogInfo("new intersection found");
                        distMovedTillLastTurn = 0;
                        movePairs.Clear();


                      MovePair mp = new MovePair();
                        mp.isIntersection = true;
                        mp.froont = true;
                        //check which way to intersection is going
                        if (LeftOK && rigthOK)
                        {
                            //intersections goes both ways
                            mp.left = true;
                            mp.rigth = true;
                            //if both way, go left first
                            _vrAibo.Turn(90);
                            _vrAibo.Walk(AiboSpeed);
                            distMovedTillLastTurn += AiboSpeed;

                            mp.left = false;
                            mp.movement = AiboSpeed;
                            mp.turn = 90;
                            movePairs.Add(mp);

                        }else if (LeftOK)
                        {
                            _vrAibo.Turn(90);
                            _vrAibo.Walk(AiboSpeed);
                            distMovedTillLastTurn += AiboSpeed;

                            mp.left = false;
                            mp.movement = AiboSpeed;
                            mp.turn = 90;
                            movePairs.Add(mp);
                        }
                        else
                        {

                           
                            _vrAibo.Turn(-90);
                            _vrAibo.Walk(AiboSpeed);
                            distMovedTillLastTurn += AiboSpeed;

                            mp.rigth = false;
                            mp.movement = AiboSpeed;
                            mp.turn = -90;
                            movePairs.Add(mp);
                        }


                    }

                }
                else
                {
                    

                    //front not ok
                    Logger.Instance.LogInfo("Front not ok");
                    Logger.Instance.LogInfo("line start "+ lineStartFront);
                    Logger.Instance.LogInfo("line end " + lineEndFront);


                    //chech whether this realy is a dead end
                    //there is the possibility that a T shaped intersection or a simple turn is view as a dead end,
                    //because the end of the path is reached, while the side views to not enoug of the side paths to detect them

                    int sideScanHeigth = GLab.VirtualAibo.VrAibo.SurfaceHeight - 20;


                    bool leftScanResult = scanForSidePathLeft(left, sideScanHeigth, 10);
                    bool rigthScanResult = scanForSidePathRigth(rigth, sideScanHeigth, 10);

                    if (leftScanResult && !rigthScanResult)
                    {
                        //simpe left turn
                        _vrAibo.Walk(AiboSpeed);
                        _vrAibo.Turn(90);
                        distMovedTillLastTurn = 0;
                        return;
                    }
                    else if (!leftScanResult && rigthScanResult)
                    {
                        //simple rigth turn
                        _vrAibo.Walk(AiboSpeed);
                        _vrAibo.Turn(-90);
                        distMovedTillLastTurn = 0;
                        return;
                    }
                    else{
                        distMovedTillLastTurn = 0;
                        //T intersection
                    }

                  

                    _vrAibo.Turn(180);
                    moveBack = true;
                    return;                  
                }

            }

            return;

        }


        private void moveBackViaRecordedPath()
        {
            //get the last element
            MovePair mp = movePairs[movePairs.Count - 1];
            
            //apply the turn in reverse and move

            if (movePairs.Count - 1 == 0)
            {            

                _vrAibo.Walk(mp.movement);
                _vrAibo.Turn(mp.turn);
            }
            else
            {
                _vrAibo.Walk(mp.movement);
                _vrAibo.Turn(mp.turn * -1);
            }

         

           

            if (movePairs.Count > 1)
            {
                movePairs.RemoveAt(movePairs.Count - 1);
            }

          
        
        }

        private bool TrackLine(out float turn)
        {
            // Get images from vraibo
            Image<Gray, byte> leftEye = new Image<Gray, byte>((Bitmap)_vrAibo.GetBitmapLeftEye());
            Image<Gray, byte> rightEye = new Image<Gray, byte>((Bitmap)_vrAibo.GetBitmapRightEye());


            // Let the stereo vision class compute a disparity map
            _stereoVision.ComputeDisparityMap(ref leftEye, ref rightEye);

           /* StereoSGBM sgbm = new StereoSGBM(0, 16, 0, 0, 0, -1, 0, 7, 50, 1, StereoSGBM.Mode.SGBM);

            Image<Gray, short> disp = new Image<Gray, short>(leftEye.Size);

              sgbm.FindStereoCorrespondence(leftEye, rightEye, disp);

              _frmImage.SetImage(disp);*/

            // Display the resulting disparity map. Darker values mean less disparity which means the object is farer away
            _frmImage.SetImage(_stereoVision.DisparityMapDisplayable);

            // This is a calculation to get a point just below the image center. This point is where depth perception is shown
            int probe_x = _stereoVision.RefImage.Width / 2;
            int probe_y = (int)(_stereoVision.RefImage.Height * 0.6);

            // Get and display the reference image. Every pixel here corresponds to a pixel in the disparity map
            // If you want to detect objects for which you want to have the distance, use *this* image only.
            // Also, we draw a white cross on the point in the image from where the depth is taken
            Image<Gray, byte> tmpRefImg = _stereoVision.RefImage;
            tmpRefImg.Draw(new Cross2DF(new PointF(probe_x, probe_y), 8, 8), new Gray(255), 1);
            _referenceImage.SetImage(tmpRefImg);

            // This prints the current depth on the point to the log facility, if there is enough change
            // to the depth value
            double depth = _stereoVision.GetDepth(probe_x, probe_y);
            if (Math.Abs(depth - lastDepth) > 0.1)
                //Logger.Instance.LogInfo(depth.ToString(CultureInfo.InvariantCulture));
                //Logger.Instance.LogInfo("distance: " + depth);
            lastDepth = depth;

            //---------------------------


            // Get bitmap from center eye camera...
            Bitmap centerEye = (Bitmap)_vrAibo.GetBitmapCenterEye();
            // ...and create a new RasterImage...
            Image<Rgb, byte> center = new Image<Rgb, byte>(centerEye);

            // Get red-channel
            Image<Rgb, byte> channelRed = new Image<Rgb, byte>(center.Width, center.Height);
            // set "channel of interest" (coi) to red
            CvInvoke.cvSetImageCOI(center.Ptr, 1);
            CvInvoke.cvSetImageCOI(channelRed.Ptr, 1);
            CvInvoke.cvCopy(center.Ptr, channelRed.Ptr, IntPtr.Zero);
            // reset coi
            CvInvoke.cvSetImageCOI(center.Ptr, 0);
            CvInvoke.cvSetImageCOI(channelRed.Ptr, 0);

          
            if (moveBack)
            {

                if (movePairs.Count == 0)
                {

                }else

                if (movePairs.Count == 1)
                {
                    moveBack = false;
                    moveBackViaRecordedPath();
                    justReturnedFromTrackBack = true;

                    if (!movePairs[0].left && !movePairs[0].rigth && !movePairs[0].froont)
                    {

                        
                        movePairs.RemoveAt(0);
                    }

                }
                else
                {
                    moveBackViaRecordedPath();
                }
                
                
            }
            else
            {
                if (picsTaken == 0)
                {
                    front = center.Copy();
                    //turn 90 left
                    _vrAibo.HeadYaw = 90;
                    picsTaken++;
                }
                else if (picsTaken == 1)
                {
                    //turn 90 left
                    _vrAibo.HeadYaw = -90;
                    left = center.Copy();
                    picsTaken++;
                }
                else if (picsTaken == 2)
                {
                    //turn 90 left
                    _vrAibo.HeadYaw = 0;
                    rigth = center.Copy();
                    picsTaken = 0;
                    //Logger.Instance.LogInfo("All Pics taken");
                    moveBasedOnImage();

                    Logger.Instance.LogInfo("At pos "+_vrAibo.Position);
                    frontWindow.SetImage(front);
                    leftWindow.SetImage(left);
                    rigthWindow.SetImage(rigth);
                }       
            
            }


            _frmEyeCenter.SetImage(channelRed);


            //------------------------

            turn = 0; // Replace this with your calculated turn value

            // Free your resources!
            leftEye.Dispose();
            rightEye.Dispose();
            return true;
        }
    }
}
