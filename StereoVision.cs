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
using Frame.VrAibo.Navigation;
using Frame.VrAibo.Movement;

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

    public struct Segment
    {
        public int start;
        public int end;

        public bool isObject;

        public Segment(int start, int end, bool isObject)
        {
            this.start = start;
            this.end = end;
            this.isObject = isObject;
        }
    }

    enum hsvEvalReturn { No_Object, Object_with_border, Object_no_border };

    internal class StereoVision : IPluginClient
    {
        private const float AiboSpeed = 0.3f;
        private FrmImage _frmImage;
        private FrmImage _referenceImage;
        private FrmVrAiboRemote _frmVrAiboRemote;
        private GLab.VirtualAibo.VrAibo _vrAibo;
        private GLab.StereoVision.StereoVision _stereoVision;
        private double lastDepth = 0;



        Hsv colorfDetectedObject = new Hsv();
        bool objectDetected = false;

        MovementConsenter movementConsenter;

        ObstacleManager om;
        NodeNavigator nodeNavigator;

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

        private FrmImage processedDB;

        int picsTaken = 0;

        private bool justReturnedFromTrackBack = false;

        private FrmImage stuff;
        private FrmImage distanceDB;

        int pathMinThreshold = 60;


        Image<Gray, short> disp;

        //--------------


        public StereoVision()
        {
            Name = "Stereo Vision Lab";
        }

        public bool GoAibo { get; set; }

        public override void Setup()
        {



            om = new ObstacleManager();
            nodeNavigator = new NodeNavigator();

            //add some colors for testing stuff

        //    om.addObstacal(new Hsv(19, 0, 0), new Vector2(0, 0));
            //om.addObstacal(new Hsv(0, 0, 0), new Vector2(0, 0));


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
            _vrAibo = new GLab.VirtualAibo.VrAibo(parcours) { Position = new Vector2(0.2f, 31.25f) };

            //_vrAibo = new GLab.VirtualAibo.VrAibo(parcours) { Position = new Vector2(0.047f, -12.66f) };


            //{X:0,04798115 Y:-12,66051}



            //  _vrAibo.Position.X =  0.4;
            //  _vrAibo.Position.Y =  34.7;

            // _vrAibo = new GLab.VirtualAibo.VrAibo(parcours) { Position = new Vector2(0.047f, 50.34f) };

            // _vrAibo = new GLab.VirtualAibo.VrAibo(parcours) { Position = new Vector2(0.047f, 30.73f) };

            //{X:0,047 Y:50,34013}

            movementConsenter = new MovementConsenter(_vrAibo, nodeNavigator);


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

            processedDB = new FrmImage("ProcessDB",
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


            disp = new Image<Gray, short>(256, 256);

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

            distanceDB = new FrmImage("Distance Output", GLab.VirtualAibo.VrAibo.SurfaceWidth,
                                           GLab.VirtualAibo.VrAibo.SurfaceHeight, DisplayMode.Original);


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

        private int scanHorizontal(Image<Rgb, byte> img, System.Drawing.Point poo, Rgb pathColor, int dir)
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
                        return x - dir;
                    }
                }
            }
        }

        private bool scanAhead(Image<Rgb, byte> img, System.Drawing.Point poo, Image<Rgb, byte> dbImage, Rgb pathColor, ref System.Drawing.Point pointOfIntersection, out int dir)
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

        private bool isSidePath(int lineStart, int lineEnd, int imgWidth)
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




        private bool checkIfvalidPath(int start, int end, int heigth, Image<Rgb, byte> img, int scanDist)
        {
            int center = (end - ((end - start) / 2));

            Rgb referenceColor = img[heigth, center];

            for (int i = 0; i < scanDist; i++)
            {
                if (referenceColor.Blue == img[heigth - i, center].Blue && referenceColor.Red == img[heigth - i, center].Red && referenceColor.Green == img[heigth - i, center].Green)
                {

                }
                else
                {
                    LineSegment2D ls = new LineSegment2D(new System.Drawing.Point(center, heigth), new System.Drawing.Point(center, heigth - i));

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

            Rgb pathColor = img[scanHeigth, img.Width - 1];


            //the path we are intereted in should start at the rigth side of the image, if the path would lie in the center of the image the normal side image eval algo would have detected that path
            for (int i = img.Width - 1; i > 0; i--)
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

                        movementConsenter.pathDetectionReques(AiboSpeed, 90);

                        distMovedTillLastTurn += AiboSpeed;

                        mp.left = false;
                        mp.movement = AiboSpeed;
                        mp.turn = 90;
                        movePairs.Add(mp);
                    }
                    else if (movePairs[0].froont)
                    {


                        movementConsenter.pathDetectionReques(AiboSpeed, 0);


                        distMovedTillLastTurn += AiboSpeed;

                        mp.froont = false;
                        mp.movement = AiboSpeed;
                        movePairs.Add(mp);
                    }
                    else
                    {

                        movementConsenter.pathDetectionReques(AiboSpeed, -90);

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
                    //mc.RequestRotation(180);
                    //mc.RequestMovement(AiboSpeed);
                }
            }
            else
            {
                //not directly on an old intersection             

                //check of the front is clear
                if (frontOK)
                {
                    // Logger.Instance.LogInfo("Front ok");
                    //Logger.Instance.LogInfo("Status R: "+rigthOK);
                    //Logger.Instance.LogInfo("Min Status: " + (distMovedTillLastTurn < minMoveDistanceTillNextTurn));
                    //Logger.Instance.LogInfo("Moved: " + distMovedTillLastTurn + " must move: " + minMoveDistanceTillNextTurn);



                    //check if there are no side paths or if we ignore them because we just turned
                    if ((!LeftOK && !rigthOK) || distMovedTillLastTurn < minMoveDistanceTillNextTurn)
                    {


                        //just move front
                        //only way to move it forward
                        int diffX = (GLab.VirtualAibo.VrAibo.SurfaceWidth / 2) - (lineEndFront - ((lineEndFront - lineStartFront) / 2));
                        float phi = Alpha * diffX;

                        MovePair mp = new MovePair();
                        mp.movement = AiboSpeed;

                        int sDiff = Math.Abs(lastFrontStart - lineStartFront);
                        int eDiff = Math.Abs(lastFrontEnd - lineEndFront);

                        if (phi != 0.0f)
                        {
                            // mc.turnFromPath = phi / 2;
                            //mc.RequestRotation(phi / 2);
                            mp.turn = phi / 2;
                        }
                        else
                        {
                            phi = 0;
                        }


                        movementConsenter.pathDetectionReques(AiboSpeed, phi);
                        movePairs.Add(mp);
                        distMovedTillLastTurn += AiboSpeed;
                    }
                    else
                    {
                        //a possible intersection has beed detected

                        // Logger.Instance.LogInfo("new intersection found");
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

                            movementConsenter.pathDetectionReques(AiboSpeed, 90);
                            distMovedTillLastTurn += AiboSpeed;

                            mp.left = false;
                            mp.movement = AiboSpeed;
                            mp.turn = 90;
                            movePairs.Add(mp);

                        }
                        else if (LeftOK)
                        {
                            movementConsenter.pathDetectionReques(AiboSpeed, 90);
                            distMovedTillLastTurn += AiboSpeed;

                            mp.left = false;
                            mp.movement = AiboSpeed;
                            mp.turn = 90;
                            movePairs.Add(mp);
                        }
                        else
                        {


                            movementConsenter.pathDetectionReques(AiboSpeed, -90);
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
                    Logger.Instance.LogInfo("FRONT NOT OK");

                    //front not ok


                    //chech whether this realy is a dead end
                    //there is the possibility that a T shaped intersection or a simple turn is view as a dead end,
                    //because the end of the path is reached, while the side views to not enoug of the side paths to detect them

                    int sideScanHeigth = GLab.VirtualAibo.VrAibo.SurfaceHeight - 20;


                    bool leftScanResult = scanForSidePathLeft(left, sideScanHeigth, 10);
                    bool rigthScanResult = scanForSidePathRigth(rigth, sideScanHeigth, 10);

                    if (leftScanResult && !rigthScanResult)
                    {
                        //simpe left turn

                        movementConsenter.pathDetectionReques(AiboSpeed, 90);
                        distMovedTillLastTurn = 0;
                        return;
                    }
                    else if (!leftScanResult && rigthScanResult)
                    {
                        //simple rigth turn
                        movementConsenter.pathDetectionReques(AiboSpeed, -90);
                        distMovedTillLastTurn = 0;
                        return;
                    }
                    else
                    {
                        distMovedTillLastTurn = 0;
                        //T intersection
                    }

                    //mc.RequestRotation(180);
                    //moveBack = true;
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
                movementConsenter.RequestMovement(mp.movement);
                movementConsenter.RequestRotation(mp.turn);
            }
            else
            {
                movementConsenter.RequestMovement(mp.movement);
                movementConsenter.RequestRotation(mp.turn * -1);
            }

            if (movePairs.Count > 1)
            {
                movePairs.RemoveAt(movePairs.Count - 1);
            }

        }

        private double getDistance(Image<Gray, byte> disp, Image<Gray, byte> center, FrmImage dbImage)
        {
            Image<Gray, byte> distImg = center.Copy();

            int heigth = 256 / 2;
            byte distThres = 255 - 30;

            double avrgDist = 0;
            int valuesAdded = 0;

            int sideSpacing = 80;

            for (int i = sideSpacing; i < distImg.Width - sideSpacing; i++)
            {
                //get the distance
                //byte distance = disp.Data[heigth, i, 0];

                double distance = _stereoVision.GetDepth(i, heigth);

                avrgDist += distance;
                valuesAdded++;


                //Logger.Instance.LogInfo("d: "+distance);
                if (distance <= distThres)
                {
                    distImg[heigth, i] = new Gray(255);
                }
                else
                {
                    distImg[heigth, i] = new Gray(0);
                }


            }

            dbImage.SetImage(distImg);
            return avrgDist / valuesAdded;
        }


        private double getDistance(Image<Gray, short> disp, Image<Rgb, byte> center, FrmImage dbImage)
        {
            Image<Rgb, byte> distImg = center.Copy();

            int heigth = 256 / 2;
            short distThres = 255 - 30;

            double avrgDist = 0;
            int valuesAdded = 0;

            int sideSpacing = 80;

            for (int i = sideSpacing; i < distImg.Width - sideSpacing; i++)
            {
                //get the distance
                //short distance = disp.Data[heigth, i, 0];

                double distance = _stereoVision.GetDepth(i, heigth);

                avrgDist += distance;
                valuesAdded++;


                //Logger.Instance.LogInfo("d: "+distance);
                if (distance <= distThres)
                {
                    distImg[heigth, i] = new Rgb(255, 0, 0);
                }
                else
                {
                    distImg[heigth, i] = new Rgb(0, 255, 0);
                }


            }

            dbImage.SetImage(distImg);
            return avrgDist / valuesAdded;
        }


        private int getSegments(Image<Gray, short> disp, Image<Rgb, byte> center, FrmImage dbImage, out List<Segment> objectSegments, short distThres, int heigth)
        {
            Image<Rgb, byte> distImg = center.Copy();


            int avrgDist = 0;
            int valuesAdded = 0;

            objectSegments = new List<Segment>();
            int newSegemntStart = 0;

            bool onObject = distThres < disp.Data[heigth, 0, 0];



            for (int i = 0; i < distImg.Width; i++)
            {
                //get the distance
                short distance = disp.Data[heigth, i, 0];




                // double distance = _stereoVision.GetDepth(i, heigth);

                avrgDist += distance;
                valuesAdded++;


                bool isCurrentPixelObject = distThres < distance;


                //check if the current pixel still the same as the segment we are currently tracking
                if (isCurrentPixelObject == onObject)
                {
                    //in this case do nothing
                }
                else
                {
                    //change detected
                    objectSegments.Add(new Segment(newSegemntStart, i - 1, onObject));
                    newSegemntStart = i;
                    onObject = isCurrentPixelObject;

                }


                //print to the db image
                if (isCurrentPixelObject)
                {
                    //distImg[heigth, i] = new Rgb(255, 0, 0);
                }
                else
                {
                    //distImg[heigth, i] = new Rgb(0, 255, 0);
                }


                //Logger.Instance.LogInfo("d: "+distance);
                /*   if (distance <= distThres)
                   {

                       if (!trackingFreeSegment)
                       {
                           //we found a new segment
                           newSegemntStart = i;
                           trackingFreeSegment = true;
                       }

                       distImg[heigth, i] = new Rgb(255, 0, 0);
                   }
                   else
                   {
                       distImg[heigth, i] = new Rgb(0, 255, 0);
                       if (trackingFreeSegment)
                       {
                           objectSegments.Add(new Segment(newSegemntStart, i));
                           trackingFreeSegment = false;
                       }

                   }*/


            }

            //dont forget to add the last segment
            objectSegments.Add(new Segment(newSegemntStart, distImg.Width, onObject));

            dbImage.SetImage(distImg);
            return avrgDist / valuesAdded;
        }

        private void maskMultipleColors(Image<Rgb, byte> src, out Image<Gray, byte> dest, List<Hsv> colorsToMask, int hueRange, int strucSize)
        {
            Image<Hsv, byte> hsvFront = new Image<Hsv, byte>(src.Size);
            CvInvoke.cvCvtColor(src, hsvFront, Emgu.CV.CvEnum.COLOR_CONVERSION.RGB2HSV);

            if (colorsToMask.Count > 0)
            {
                getMask(src, colorsToMask[0], out dest, hueRange, strucSize);
            }
            else
            {
                //if the list is empty
                dest = new Image<Gray, byte>(src.Width, src.Height, new Gray(0));

            }

            for (int i = 1; i < colorsToMask.Count; i++)
            {
                Image<Gray, byte> mask2;
                getMask(src, colorsToMask[i], out mask2, hueRange, strucSize);

                leftWindow.SetImage(mask2);

                //add the two mask together
                dest = dest.AddWeighted(mask2, 1, 1, 0);
            }
        }

        private void getMask(Image<Hsv, byte> img, Hsv colorToFilter, out Image<Gray, byte> mask, int hueRange, int strucSize)
        {
            mask = img.InRange(new Hsv(colorToFilter.Hue - hueRange / 2, 0, 0), new Hsv(colorToFilter.Hue + hueRange / 2, 255, 255));

            StructuringElementEx se = new StructuringElementEx(10, 10, 5, 5, Emgu.CV.CvEnum.CV_ELEMENT_SHAPE.CV_SHAPE_RECT);

            CvInvoke.cvErode(mask, mask, se, strucSize);
            CvInvoke.cvDilate(mask, mask, se, strucSize);

        }

        private void getMask(Image<Rgb, byte> img, Hsv colorToFilter, out Image<Gray, byte> mask, int hueRange, int strucSize)
        {
            Image<Hsv, byte> hsvFront = new Image<Hsv, byte>(img.Size);

            CvInvoke.cvCvtColor(img, hsvFront, Emgu.CV.CvEnum.COLOR_CONVERSION.RGB2HSV);

            getMask(hsvFront, colorToFilter, out mask, hueRange, strucSize);

        }

        private void moveAroundObject()
        {

            Logger.Instance.LogInfo("-------------------------");

            Vector2 target = new Vector2(0, 30);
            Vector2 robotPos = nodeNavigator.CurrentRobotPosition;

            Image<Gray, byte> maskFront = new Image<Gray, byte>(front.Size);
            Image<Gray, byte> maskLeft = new Image<Gray, byte>(left.Size);
            Image<Gray, byte> maskRight = new Image<Gray, byte>(rigth.Size);

            int hueRange = 10;
            int strctSize = 2;

            /* getMask(front, new Hsv(19, 0, 0),out maskFront,hueRange,strctSize);
             getMask(left, new Hsv(19, 0, 0),out maskLeft, hueRange, strctSize);
             getMask(rigth, new Hsv(19, 0, 0),out maskRight, hueRange, strctSize);*/

            //mask the objects

            List<Hsv> colorsToMask = om.getColorsOfCloseObstacals(nodeNavigator.CurrentRobotPosition, 5);



            maskMultipleColors(front, out maskFront, colorsToMask, hueRange, strctSize);
            maskMultipleColors(left, out maskLeft, colorsToMask, hueRange, strctSize);
            maskMultipleColors(rigth, out maskRight, colorsToMask, hueRange, strctSize);


            //check the fron image
            bool frontStatus = objectInside(maskFront, maskFront.Height / 2);
            bool leftStatus = objectInside(maskLeft, maskLeft.Height / 2);
            bool rigthStatus = objectInside(maskRight, maskRight.Height / 2);



            //get the direction vector to the target
            //Vector2 dirVct = getVectorTotarget(_vrAibo.Position, target);


            Vector2 dirVct = VctOp.getVectorTotarget(robotPos, target);

            Vector2 currentHeading = nodeNavigator.getCurrentHeading();

            double angle = Math.Abs(VctOp.calcAngleBeteenVectors(currentHeading, dirVct));
            double angle2 = VctOp.calcAngleBeteenVectors(currentHeading, dirVct);
            bool clockwise = VctOp.isClockwise(currentHeading, dirVct);

            //check the angle between the current and the direction we need to go
            Logger.Instance.LogInfo("Current heading " + currentHeading);
            Logger.Instance.LogInfo("Dir vector " + dirVct);
            //Logger.Instance.LogInfo("Angle "+angle);
            //Logger.Instance.LogInfo("curent rot " + currentRotation);

            Logger.Instance.LogInfo("Angle signed " + angle2);



            if (angle < 45)
            {
                Logger.Instance.LogInfo("Need to move front");
                //we rougly need to move to the front
                if (frontStatus)
                {
                    //object in the front img
                    if (leftStatus && rigthStatus)
                    {
                        //bboth sides are blocked
                    }
                    else if (!rigthStatus)
                    {
                        Logger.Instance.LogInfo("Turn rigth");
                        //left is free
                        //_vrAibo.Turn(-90);
                        movementConsenter.objectDetectionReques(0, -90);
                    }
                    else
                    {
                        Logger.Instance.LogInfo("Turn L");
                        //rigth is free
                        //_vrAibo.Turn(+90);
                        movementConsenter.objectDetectionReques(0, 90);
                    }
                }
                else
                {
                    //_vrAibo.Walk(AiboSpeed);
                    movementConsenter.objectDetectionReques(AiboSpeed, 0);
                }
            }
            else if (angle > 45 && angle < 135)
            {

                Logger.Instance.LogInfo("need to move to a side");
                //we need to check the side images
                if (clockwise)
                {
                    Logger.Instance.LogInfo("need to turn left");
                    //we need to move left
                    if (leftStatus)
                    {

                        //object to the left
                        //_vrAibo.Walk(AiboSpeed);
                        movementConsenter.objectDetectionReques(AiboSpeed, 0);
                    }
                    else
                    {
                        //left ok
                        //_vrAibo.Turn(+90);
                        movementConsenter.objectDetectionReques(0, 90);

                    }
                }
                else
                {

                    Logger.Instance.LogInfo("need to turn rigth");
                    //we need to move rigth
                    if (rigthStatus)
                    {
                        Logger.Instance.LogInfo("R is blocked");
                        //object to the rigth
                        //_vrAibo.Walk(AiboSpeed);
                        movementConsenter.objectDetectionReques(AiboSpeed, 0);

                    }
                    else
                    {
                        Logger.Instance.LogInfo("R is free");
                        //rigth ok
                        //_vrAibo.Turn(-90);
                        movementConsenter.objectDetectionReques(0, -90);

                    }
                }
            }

            frontWindow.SetImage(maskFront);
            leftWindow.SetImage(maskLeft);
            rigthWindow.SetImage(maskRight);

            Logger.Instance.LogInfo("-------------------------");

        }

        public void filterViaHSV(Image<Hsv, byte> img, Hsv colorToFilter, out Image<Gray, byte> mask)
        {

            //CvInvoke.cvCvtColor(img, hsvImage, Emgu.CV.CvEnum.COLOR_CONVERSION.RGB2HSV);

            //get a probe of the color we are intersted in

            Hsv hsv_min = new Hsv(colorToFilter.Hue, 0, 0);
            Hsv hsv_max = new Hsv(colorToFilter.Hue, 255, 255);


            mask = img.InRange(hsv_min, hsv_max);

            processedDB.SetImage(mask);


        }

        private bool objectInside(Image<Gray, byte> mask, int scanHeigth)
        {
            for (int i = 0; i < mask.Width; i++)
            {
                bool isOnObject = mask[scanHeigth, i].Intensity > 0;
                if (isOnObject)
                {
                    return true;
                }
            }
            return false;
        }

        private hsvEvalReturn evalMask(Image<Gray, byte> mask, int scanHeigth, out int objectEnd, out bool objectToLeft)
        {
            //check if we start on free space or an object
            bool startOnObject = mask[scanHeigth, 0].Intensity > 0;

           // Logger.Instance.LogInfo("Start on object: "+ startOnObject);


            for (int i = 0; i < mask.Width; i++)
            {
                bool isOnObject = mask[scanHeigth, i].Intensity > 0;

                //check if a change accured
                if (startOnObject != isOnObject)
                {
                    //found a change
                    objectEnd = i;

                    

                    //determine which side of the img the object was on
                    //since we started scanning from the left, this means starting in th object means the object is to the left
                    if (startOnObject)
                    {
                        objectToLeft = true;
                    }
                    else
                    {
                        objectToLeft = false;
                    }

                    return hsvEvalReturn.Object_with_border;
                }
            }

            //if we got here this means we either saw nothing but an object or no object at all
            if (startOnObject)
            {
                objectToLeft = false;
                objectEnd = -1;
                return hsvEvalReturn.Object_no_border;
            }
            else
            {
                objectToLeft = false;
                objectEnd = -1;
                return hsvEvalReturn.No_Object;
            }
        }

        private void mergeSegments(ref List<Segment> objectSegments, int distanceThreshold)
        {
            List<Segment> objectSegmentsTmp = new List<Segment>();

            for (int i = 0; i < objectSegments.Count; i++)
            {
                if (objectSegments[i].isObject)
                {
                    int start = objectSegments[i].start;
                    int end = objectSegments[i].end;
                    for (int j = i + 1; j < objectSegments.Count; j++)
                    {
                        if (objectSegments[j].isObject)
                        {
                            int dist = objectSegments[j].start - end;
                            if (dist > distanceThreshold)
                            {
                                objectSegmentsTmp.Add(new Segment(start, end, true));
                                break;
                            }
                            else
                            {
                                end = objectSegments[j].end;
                            }
                        }



                    }


                    objectSegmentsTmp.Add(new Segment(start, end, true));
                }
            }

            objectSegments = objectSegmentsTmp;
        }

        private void doStereoVisionStuff(Image<Rgb, byte> center, Image<Gray, short> disp)
        {

            double estimatedDist = 8;

            //Logger.Instance.LogInfo("At pos " + _vrAibo.Position);

            List<Segment> objectSegments = new List<Segment>();

            Image<Hsv, byte> hsvImage = new Image<Hsv, byte>(center.Size);
            CvInvoke.cvCvtColor(center, hsvImage, Emgu.CV.CvEnum.COLOR_CONVERSION.RGB2HSV);

            short distThres = 255 - 40;

            int heigth = 256 / 2;

            int avrgDist = getSegments(disp, center, distanceDB, out objectSegments, distThres, heigth);

            int segmentMergeThreshold = 10;

            Logger.Instance.LogInfo("s " + objectSegments.Count);

            mergeSegments(ref objectSegments, segmentMergeThreshold);


            Image<Rgb, byte> b = new Image<Rgb, byte>(center.Size);
            center.CopyTo(b);



            for (int i = 0; i < objectSegments.Count; i++)
            {

                for (int j = objectSegments[i].start; j < objectSegments[i].end; j++)
                {
                    b[heigth, j] = new Rgb(255, 0, 0);
                }
            }


            distanceDB.SetImage(b);

            //eval the segments
            for (int i = 0; i < objectSegments.Count; i++)
            {

                if (objectSegments[i].isObject)
                {
                    //calc the center of the object
                    int centerIndex = ((objectSegments[i].end - objectSegments[i].start) / 2) + objectSegments[i].start;
                    //get the color of that object
                    Hsv objectColor = hsvImage[heigth, centerIndex];

                    //estimate the real world position if the object
                    //first calc the angle towards the object
                    int diffX = (GLab.VirtualAibo.VrAibo.SurfaceWidth / 2) - (centerIndex);
                    float phi = Alpha * diffX;



                    Vector2 vectorToObject = new Vector2(0, (float)estimatedDist);

                    double overallRotation = (phi + nodeNavigator.CurrentRobotRotation) % 360;

                    Vector2 objectWorldPos = VctOp.calcMovementVector(overallRotation, vectorToObject);

                    objectWorldPos += nodeNavigator.CurrentRobotPosition;

                    om.addObstacal(objectColor, objectWorldPos);


                }

            }

            /*Logger.Instance.LogInfo("---------------");


            if (avrgDist > 190)
            {
                mc.astimatedDistanceToObject = 5;

                objectDetected = true;

                //get the first segment that is an object, make sure there is one
                if (objectSegments.Count > 1)
                {
                  //get the center of that segment
                    int c = (objectSegments[0].end - objectSegments[0].start) / 2 + objectSegments[0].start;
                    //get the probe
                    colorfDetectedObject = hsvImage[heigth, c];

                }

            }

            //check if on object was detected at all
            if (objectDetected)
            {
                Image<Gray, byte> mask = new Image<Gray, byte>(center.Size);

                colorfDetectedObject = hsvImage[256 / 2, 256 / 2];

                filterViaHSV(hsvImage, colorfDetectedObject, out mask);

                int objectStart = 0;
                bool objectIsToTheLeft = true;

                hsvEvalReturn r = evalMask(mask, 256 / 2, out objectStart, out objectIsToTheLeft);

                if (r == hsvEvalReturn.No_Object)
                {
                    //something is wrong if we get here...
                }
                else if (r == hsvEvalReturn.Object_no_border)
                {
                    //the entire sceen is filled with the object
                    //mc.objectDetectionRequstedMovement = true;
                    //mc.turnFromObjectDetection = 45;
                }
                else
                {
                    //object with border
                    //mc.objectDetectionRequstedMovement = true;

                    int objectEnd;
                    if (objectIsToTheLeft)
                    {
                        objectEnd = center.Width;
                    }
                    else
                    {
                        objectEnd = 0;
                    }

                    int diffX = (GLab.VirtualAibo.VrAibo.SurfaceWidth / 2) - (objectEnd - ((objectEnd - objectStart) / 2));
                    float phi = Alpha * diffX;

                    //mc.turnFromObjectDetection = phi;


                }


                stuff.SetImage(disp);
            }*/

            //Image<Gray, short> binary = disp.Copy();

            //CvInvoke.cvThreshold(disp, binary, 180, 255, Emgu.CV.CvEnum.THRESH.CV_THRESH_TOZERO_INV & Emgu.CV.CvEnum.THRESH.CV_THRESH_BINARY);

            //processedDB.SetImage(binary);

            //Logger.Instance.LogInfo("Distance to target "+avrgDist);


            //int diffX = (GLab.VirtualAibo.VrAibo.SurfaceWidth / 2) - (objectSegments[0].end - ((objectSegments[0].end - 0) / 2));
            //float phi = Alpha * diffX;
        }


        private void moveAroundObject2()
        {
            Image<Gray, byte> maskFront = new Image<Gray, byte>(front.Size);
            Image<Gray, byte> maskLeft = new Image<Gray, byte>(left.Size);
            Image<Gray, byte> maskRight = new Image<Gray, byte>(rigth.Size);

            //params
            int hueRange = 10;
            int strctSize = 5;

            int scanHeigth = front.Height / 2;           
            
            
            //mask the objects

            List<Hsv> colorsToMask = om.getColorsOfCloseObstacals(nodeNavigator.CurrentRobotPosition, 5);

            maskMultipleColors(front, out maskFront, colorsToMask, hueRange, strctSize);
            maskMultipleColors(left, out maskLeft, colorsToMask, hueRange, strctSize);
            maskMultipleColors(rigth, out maskRight, colorsToMask, hueRange, strctSize);

            int objectEnd = 0;
            bool objectToLeft;

            hsvEvalReturn frontStatus = evalMask(maskFront, scanHeigth, out objectEnd, out objectToLeft);


            //hsvEvalReturn frontStatus = evalMask(maskFront, scanHeigth, out objectEnd, out objectToLeft);

            Logger.Instance.LogInfo("Object status "+ frontStatus);

            if (frontStatus == hsvEvalReturn.No_Object)
            {
                //do nothing
                //return;
            }
            else if(frontStatus == hsvEvalReturn.Object_no_border)
            {
                //with no border
                movementConsenter.objectDetectionReques(0, 20);
                //_vrAibo.Turn(20);
            }else
            {
                //object with border
                if (objectToLeft)
                {
                    //object to the left
                    int diffX = (GLab.VirtualAibo.VrAibo.SurfaceWidth / 5) - objectEnd;
                    float phi = Alpha * diffX;

                    Logger.Instance.LogInfo("Phi " + phi);

                    //_vrAibo.Turn(phi);
                    movementConsenter.objectDetectionReques(AiboSpeed, phi);

                }
                else
                {

                    if (_vrAibo.HeadYaw == -5)
                    {

                    }else
                    {
                        _vrAibo.HeadYaw = -5;
                    }
                    
                    //object to the rigth
                    int diffX = GLab.VirtualAibo.VrAibo.SurfaceWidth - (GLab.VirtualAibo.VrAibo.SurfaceWidth / 5) - objectEnd;
                    float phi = Alpha * diffX;

                    Logger.Instance.LogInfo("Phi " + phi);

                    //_vrAibo.Turn(phi);
                    movementConsenter.objectDetectionReques(AiboSpeed, phi);
                }

            }

            frontWindow.SetImage(maskFront);
            leftWindow.SetImage(maskLeft);
            rigthWindow.SetImage(maskRight);


        }

        private bool TrackLine(out float turn)
        {
            // Get images from vraibo
            Image<Gray, byte> leftEye = new Image<Gray, byte>((Bitmap)_vrAibo.GetBitmapLeftEye());
            Image<Gray, byte> rightEye = new Image<Gray, byte>((Bitmap)_vrAibo.GetBitmapRightEye());


            // Let the stereo vision class compute a disparity map
            _stereoVision.ComputeDisparityMap(ref leftEye, ref rightEye);



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


            /* if (moveBack)
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
             else*/
            {
                if (picsTaken == 0)
                {

                    StereoSGBM sgbm = new StereoSGBM(0, 16, 0, 0, 0, -1, 0, 7, 50, 1, StereoSGBM.Mode.SGBM);

                    //disp = new Image<Gray, short>(leftEye.Size);

                    sgbm.FindStereoCorrespondence(leftEye, rightEye, disp);

                    front = center.Copy();
                    //turn 90 left
                    _vrAibo.HeadYaw = 90;
                    picsTaken++;

                    stuff.SetImage(disp);

                    //_frmImage.SetImage(disp);




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
                    _vrAibo.HeadYaw = -10;
                    rigth = center.Copy();
                    picsTaken = 0;
                    //Logger.Instance.LogInfo("All Pics taken");
                    moveBasedOnImage();
                    doStereoVisionStuff(front, disp);

                    //moveAroundObject();

                    moveAroundObject2();


                    //Logger.Instance.LogInfo("At pos " + _vrAibo.Position);

                    //Vector2 currentHeading = calcMovementVector(90, new Vector2(0, -1));

                    //Logger.Instance.LogInfo("Heading " + currentHeading);

                    /*Logger.Instance.LogInfo("At pos "+_vrAibo.Position);

                    double angle = calcAngleBeteenVectors(new Vector2(0,-1),new Vector2(3,-3));
                    Logger.Instance.LogInfo("angle: " + angle);

                    double angle2 = calcAngleBeteenVectors(new Vector2(0, -1), new Vector2(-3, 3));
                    Logger.Instance.LogInfo("angle: " + angle2);*/

                    //frontWindow.SetImage(front);
                    //leftWindow.SetImage(left);
                    //rigthWindow.SetImage(rigth);


                    //Logger.Instance.LogInfo("distance to object "+mc.astimatedDistanceToObject);
                    
                    float executedMovement;
                    double executedRotation;

                    movementConsenter.execute(out executedMovement, out executedRotation);

                    nodeNavigator.addMovement(executedMovement, executedRotation);



                }

            }




            _frmEyeCenter.SetImage(channelRed);

            //Logger.Instance.LogInfo("At pos " + _vrAibo.Position);


            //int avrgDist = getDistance(disp, center, distanceDB);

            /* Image<Gray, byte> mask = new Image<Gray, byte>(center.Size);
             Image<Hsv, byte> hsvImage = new Image<Hsv, byte>(center.Size);
             CvInvoke.cvCvtColor(center, hsvImage, Emgu.CV.CvEnum.COLOR_CONVERSION.RGB2HSV);
             Hsv colorToFilter = hsvImage[256 / 2, 256 / 2];

             filterViaHSV(hsvImage, colorToFilter,out mask);

             int objectStart =0;
             bool objectIsToTheLeft = true;

             hsvEvalReturn r = evalMask(mask, 256 / 2, out objectStart, out objectIsToTheLeft);


             Logger.Instance.LogInfo("Eval says");
             Logger.Instance.LogInfo("R value " + r);
             Logger.Instance.LogInfo("Object to the left " + objectIsToTheLeft);

             //Logger.Instance.LogInfo("At pos " + _vrAibo.Position);

             List<Segment> objectSegments = new List<Segment>();

             int avrgDist = getSegments(disp, center, distanceDB, out objectSegments);

             Image<Gray, short> binary = disp.Copy();

             CvInvoke.cvThreshold(disp, binary, 180, 255, Emgu.CV.CvEnum.THRESH.CV_THRESH_TOZERO_INV & Emgu.CV.CvEnum.THRESH.CV_THRESH_BINARY);*()

             //processedDB.SetImage(binary);


             //int diffX = (GLab.VirtualAibo.VrAibo.SurfaceWidth / 2) - (objectSegments[0].end - ((objectSegments[0].end - 0) / 2));
             //float phi = Alpha * diffX;



                 /*   if (phi != 0.0f)
                    {
                        _vrAibo.Turn(phi);
                    }*/




            //double avrgDist = getDistance(tmpRefImg, tmpRefImg, distanceDB);







            //------------------------

            turn = 0; // Replace this with your calculated turn value

            // Free your resources!
            leftEye.Dispose();
            rightEye.Dispose();
            return true;
        }
    }
}
