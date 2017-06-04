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

        bool disableSideMovement = false;

        Hsv colorfDetectedObject = new Hsv();
        bool objectDetected = false;

        int frontHeadPos = 0;

        MovementConsenter movementConsenter;

        ObstacleManager om;
        NodeNavigator nodeNavigator;


        bool leftIsOldPath = false;
        bool rigthIsOld = false;
        

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

        private int lineStartFront = -1;
        private int lineEndFront = -1;

        private FrmImage processedDB;

        int picsTaken = 0;

        private bool justReturnedFromTrackBack = false;

        private FrmImage stuff;
        private FrmImage distanceDB;

        int pathMinThreshold = 80;


        Image<Gray, short> disp;

        bool moveBack = false;

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

            //om.addObstacal(new Hsv(19, 0, 0), new Vector2(0, 0));
            //om.addObstacal(new Hsv(0, 0, 0), new Vector2(0, 0));

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


       /* public bool isNotObject(Image<Rgb, byte> img, Rgb pathColor, int x, int y)
        {

            Logger.Instance.LogInfo("method called");
            //scan upwards, if the path ends before the horizon is reached it is a path
            int horizonThreshold = (img.Height / 2) - 10;

            for (int i = y; i > horizonThreshold; i--)
            {
                Logger.Instance.LogInfo("Did an iteration");

                if (pathColor.Blue == img[i, x].Blue && pathColor.Red == img[i, x].Red && pathColor.Green == img[i, x].Green)
                {
                    img[i, x] = new Rgb(0, 255, 0);
                }
                else
                {
                    Logger.Instance.LogInfo("returned here");
                    processedDB.SetImage(img);
                    return true;
                }
            }

            Logger.Instance.LogInfo("returned in false");
            processedDB.SetImage(img);
            return false;
        }*/

     

        private void moveBasedOnImage()
        {
            Logger.Instance.LogInfo("----------------------------------");

            //save the values from the last interation for comparison         

            int sideLookRange = 1;
            int knowPathThreshold = 5;
            bool frontOK = false;

            int lookAheadDistance = 10;

            double nodeCheckTheshold = 4;


            int scanHeigth = GLab.VirtualAibo.VrAibo.SurfaceHeight - 10;

            ImageOperations.scanForPath(front, scanHeigth, out lineStartFront, out lineEndFront,pathMinThreshold);

           int center = (lineEndFront - ((lineEndFront - lineStartFront) / 2));          

            Rgb referenceColor = front[scanHeigth, center];

           // isNotObject(front, referenceColor, center, scanHeigth);


            //check if the return values suggest that ther is a path
            if (lineStartFront != -1 && lineEndFront != -1 &&  lineStartFront != 255)
            {
                

                LineSegment2D ls = new LineSegment2D(new System.Drawing.Point(lineStartFront, scanHeigth), new System.Drawing.Point(lineEndFront, scanHeigth));

                frontOK = ImageOperations.checkIfvalidPath(lineStartFront, lineEndFront, scanHeigth, front, lookAheadDistance);

                front.Draw(ls, new Rgb(0, 0, 255), 2);
            }

            scanHeigth = GLab.VirtualAibo.VrAibo.SurfaceHeight - 40;
            int lineStartLeft = -1;
            int lineEndLeft = -1;

            bool LeftOK = false;

            bool pathFound = ImageOperations.scanForPath(left, scanHeigth, out lineStartLeft, out lineEndLeft,pathMinThreshold);

            //check if the return values suggest that ther is a path
            if (pathFound && lineStartLeft != -1 && lineEndLeft != -1 && ImageOperations.isSidePath(lineStartLeft, lineEndLeft, left.Width))
            {
                LeftOK = ImageOperations.checkIfvalidPath(lineStartLeft, lineEndLeft, scanHeigth, left, 20);

                LineSegment2D ls = new LineSegment2D(new System.Drawing.Point(lineStartLeft, scanHeigth), new System.Drawing.Point(lineEndLeft, scanHeigth));
                left.Draw(ls, new Rgb(0, 0, 255), 2);
            }

            int lineStartRigth = -1;
            int lineEndRigth = -1;

            bool rigthOK = false;
            pathFound = ImageOperations.scanForPath(rigth, scanHeigth, out lineStartRigth, out lineEndRigth,pathMinThreshold);

            Logger.Instance.LogInfo("R: " + lineStartRigth + " to " + lineEndRigth);


            //check if the return values suggest that ther is a path
            if (pathFound && lineStartRigth != -1 && lineEndRigth != -1 && ImageOperations.isSidePath(lineStartRigth, lineEndRigth, rigth.Width))
            {
                rigthOK = ImageOperations.checkIfvalidPath(lineStartRigth, lineEndRigth, scanHeigth, rigth, 20);
                LineSegment2D ls = new LineSegment2D(new System.Drawing.Point(lineStartRigth, scanHeigth), new System.Drawing.Point(lineEndRigth, scanHeigth));
                rigth.Draw(ls, new Rgb(0, 0, 255), 2);
            }

            if (disableSideMovement)
            {
                rigthOK = false;
                LeftOK = false;
            }


            if (leftIsOldPath)
            {
                if (!LeftOK)
                {
                    leftIsOldPath = false;
                }
                else
                {
                    LeftOK = false;
                }
            }

            if (rigthIsOld)
            {
                if (!rigthOK)
                {
                    rigthIsOld = false;
                }
                else
                {
                    rigthOK = false;
                }
            }

            //check if we are very close to a node
            bool closeToNode = nodeNavigator.isCloseToNode(nodeCheckTheshold);

            if (closeToNode)
            {
                //LeftOK = false;
                //rigthOK = false;
            }

            //check we returned to a previusly visited intersection
            if (justReturnedFromTrackBack&&false)
            {
                //handleReturnFromMoveBack();             
            }
            else
            {
                //not directly on an old intersection             

                //check of the front is clear
                if (frontOK)
                {
                    //check if there are no side paths or if we ignore them because we just turned
                    if ((!LeftOK && !rigthOK))
                    {
                        //just move front
                        //only way to move it forward
                        int diffX = (GLab.VirtualAibo.VrAibo.SurfaceWidth / 2) - (lineEndFront - ((lineEndFront - lineStartFront) / 2));
                        float phi = Alpha * diffX;
                        movementConsenter.pathDetectionRequest(AiboSpeed, phi);
                      
                    }
                    else
                    {
                        //a possible intersection has beed detected                      
                        //check which way to intersection is going
                        if (LeftOK && rigthOK)
                        {
                            nodeNavigator.createNewNodeAtCurrentPosition(false, true, true);

                            //intersections goes both ways         
                            //move left first, on hard turns like this never move on the turn
                            movementConsenter.pathDetectionRequest(0, 90);
                            leftIsOldPath = true;
                            rigthIsOld = true;
                          

                        }
                        else if (LeftOK)
                        {

                            int diffX = (GLab.VirtualAibo.VrAibo.SurfaceWidth / 2) - (lineEndLeft - ((lineEndLeft - lineStartLeft) / 2));
                            float phi = Alpha * diffX;

                            nodeNavigator.createNewNodeAtCurrentPosition(false, false, true);
                            //intersection only goes to the left
                            movementConsenter.pathDetectionRequest(0, 90 + phi);
                            leftIsOldPath = true;
                            rigthIsOld = true;
                        }
                        else
                        {

                            int diffX = (GLab.VirtualAibo.VrAibo.SurfaceWidth / 2) - (lineEndRigth - ((lineEndRigth - lineStartRigth) / 2));
                            float phi = Alpha * diffX;

                            nodeNavigator.createNewNodeAtCurrentPosition(false, false, true);
                            //intersection goes to the rigth
                            movementConsenter.pathDetectionRequest(0, -90+phi);
                            leftIsOldPath = true;
                            rigthIsOld = true;
                        }
                    }

                }
                else
                {
                    //Logger.Instance.LogInfo("FRONT NOT OK");
                    //movementConsenter.RequestReturnToLastNode();

                    //front not ok

                    Logger.Instance.LogInfo("Front is blocked");
                    //chech whether this realy is a dead end
                    //there is the possibility that a T shaped intersection or a simple turn is view as a dead end,
                    //because the end of the path is reached, while the side views to not enoug of the side paths to detect them

                    int sideScanHeigth = GLab.VirtualAibo.VrAibo.SurfaceHeight - 20;

                    int leftStart = -1;
                    int rigthEnd = -1;

                    bool leftScanResult = ImageOperations.scanForSidePathLeft(left, sideScanHeigth, 10, out leftStart);
                    bool rigthScanResult = ImageOperations.scanForSidePathRigth(rigth, sideScanHeigth, 10, out rigthEnd);


                    if (closeToNode)
                    {
                        //leftScanResult = false;
                        //rigthScanResult = false;
                    }
                    else
                    {
                        Logger.Instance.LogInfo("TO CLOSE TO NODE");
                    }


                    if (disableSideMovement)
                    {
                        leftScanResult = false;
                        rigthScanResult = false;
                    }

                    /* posOfPathLeft = new Vector2(0, sideLookRange);
                     rotLeft = nodeNavigator.CurrentRobotRotation + 90;
                    rotLeft = rotLeft % 360;

                    posOfPathLeft = VctOp.calcMovementVector(rotLeft, posOfPathLeft);

                    posOfPathLeft.X = +nodeNavigator.CurrentRobotPosition.X;
                    posOfPathLeft.Y = +nodeNavigator.CurrentRobotPosition.Y;


                    l = nodeNavigator.isKnowPath(posOfPathLeft, knowPathThreshold);

                    if (l)
                    {
                        leftScanResult = false;
                    }

                    posOfPathRigth = new Vector2(0, sideLookRange);
                    rotRigth = nodeNavigator.CurrentRobotRotation - 90;
                    rotRigth = rotRigth % 360;

                    posOfPathRigth = VctOp.calcMovementVector(rotRigth, posOfPathRigth);

                    posOfPathRigth.X = +nodeNavigator.CurrentRobotPosition.X;
                    posOfPathRigth.Y = +nodeNavigator.CurrentRobotPosition.Y;


                    r = nodeNavigator.isKnowPath(posOfPathRigth, knowPathThreshold);
                    if (r)
                    {
                        rigthScanResult = false;
                    }*/


                    if (leftScanResult && !rigthScanResult)
                    {

                        int diffX = (GLab.VirtualAibo.VrAibo.SurfaceWidth / 2) - (left.Width - 1 - (((left.Width - 1) - leftStart) / 2));
                        float phi = Alpha * diffX;

                        //check if the path we saw is an old one
                        //simpe left turn
                        nodeNavigator.createNewNodeAtCurrentPosition(false, false, false);
                        movementConsenter.pathDetectionRequest(0, 90 + phi);

                        leftIsOldPath = true;
                        rigthIsOld = true;
                        return;
                    }
                    else if (!leftScanResult && rigthScanResult)
                    {

                        int diffX = (GLab.VirtualAibo.VrAibo.SurfaceWidth / 2) - (rigthEnd - ((rigthEnd - (rigth.Width-1)) / 2));
                        float phi = Alpha * diffX;

                        nodeNavigator.createNewNodeAtCurrentPosition(false, false, false);
                        //simple rigth turn
                        movementConsenter.pathDetectionRequest(0, -90+phi);
                        leftIsOldPath = true;
                        rigthIsOld = true;
                        return;
                    }
                    else if (leftScanResult &&!rigthScanResult)
                    {
                        leftIsOldPath = true;
                        rigthIsOld = true;

                        nodeNavigator.createNewNodeAtCurrentPosition(false, true, false);
                        movementConsenter.pathDetectionRequest(0, 90);
                       
                        //T intersection
                    }
                    else
                    {
                        //dead end reached              
                        //if (!movementConsenter.isReturning())
                        {
                            movementConsenter.RequestReturnToLastNode();
                            moveBack = true;

                        }
                       
                    }

                    //mc.RequestRotation(180);
                    //moveBack = true;
                    return;
                }

            }

            return;

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

            //mask the objects

            List<Hsv> colorsToMask = om.getColorsOfCloseObstacals(nodeNavigator.CurrentRobotPosition, 5);

            ImageOperations.maskMultipleColors(front, out maskFront, colorsToMask, hueRange, strctSize);
            ImageOperations.maskMultipleColors(left, out maskLeft, colorsToMask, hueRange, strctSize);
            ImageOperations.maskMultipleColors(rigth, out maskRight, colorsToMask, hueRange, strctSize);


            //check the fron image
            bool frontStatus = ImageOperations.objectInside(maskFront, maskFront.Height / 2);
            bool leftStatus = ImageOperations.objectInside(maskLeft, maskLeft.Height / 2);
            bool rigthStatus = ImageOperations.objectInside(maskRight, maskRight.Height / 2);


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
                        movementConsenter.objectDetectionRequest(0, -90);
                    }
                    else
                    {
                        Logger.Instance.LogInfo("Turn L");
                        //rigth is free
                        //_vrAibo.Turn(+90);
                        movementConsenter.objectDetectionRequest(0, 90);
                    }
                }
                else
                {
                    //_vrAibo.Walk(AiboSpeed);
                    movementConsenter.objectDetectionRequest(AiboSpeed, 0);
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
                        movementConsenter.objectDetectionRequest(AiboSpeed, 0);
                    }
                    else
                    {
                        //left ok
                        //_vrAibo.Turn(+90);
                        movementConsenter.objectDetectionRequest(0, 90);

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
                        movementConsenter.objectDetectionRequest(AiboSpeed, 0);

                    }
                    else
                    {
                        //Logger.Instance.LogInfo("R is free");
                        //rigth ok
                        //_vrAibo.Turn(-90);
                        movementConsenter.objectDetectionRequest(0, -90);

                    }
                }
            }

            frontWindow.SetImage(maskFront);
            leftWindow.SetImage(maskLeft);
            rigthWindow.SetImage(maskRight);

            //Logger.Instance.LogInfo("-------------------------");

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

            List<Hsv> colorsToMask = om.getColorsOfCloseObstacals(nodeNavigator.CurrentRobotPosition, 8);

            ImageOperations.maskMultipleColors(front, out maskFront, colorsToMask, hueRange, strctSize);
            ImageOperations.maskMultipleColors(left, out maskLeft, colorsToMask, hueRange, strctSize);
            ImageOperations.maskMultipleColors(rigth, out maskRight, colorsToMask, hueRange, strctSize);

            int objectEnd = 0;
            bool objectToLeft;

            hsvEvalReturn frontStatus = evalMask2(maskFront, scanHeigth, out objectEnd, out objectToLeft);

            int objectEndR = 0;
            bool objectToLeftR;

            //hsvEvalReturn rStatus = evalMask2(maskRight, scanHeigth, out objectEndR, out objectToLeftR);


            //hsvEvalReturn frontStatus = evalMask(maskFront, scanHeigth, out objectEnd, out objectToLeft);

            Logger.Instance.LogInfo("Object status " + frontStatus);

            if (frontStatus == hsvEvalReturn.No_Object)
            {
               
               // return;
            }
            else if (frontStatus == hsvEvalReturn.Object_no_border)
            {
                //with no border
               // movementConsenter.objectDetectionRequest()
                movementConsenter.objectDetectionRequest(0, 20);
                //_vrAibo.Turn(20);
            }
            else
            {
                //object with border
                if (objectToLeft)
                {
                    //object to the left
                    int diffX = (GLab.VirtualAibo.VrAibo.SurfaceWidth / 5) - objectEnd;
                    float phi = Alpha * diffX;

                    if (phi > 15 || phi < -15)
                    {
                        movementConsenter.objectDetectionRequest(0, phi);
                    }
                    else
                    {
                        movementConsenter.objectDetectionRequest(AiboSpeed, phi);
                    }

                    frontHeadPos = 15;

                    Logger.Instance.LogInfo("Phi " + phi);

                    //_vrAibo.Turn(phi);
                 

                }
                else
                {

                    Logger.Instance.LogInfo("object is on the R");

                    Logger.Instance.LogInfo("abject end " + objectEnd);

                    //object to the rigth
                    int diffX = (GLab.VirtualAibo.VrAibo.SurfaceWidth - (GLab.VirtualAibo.VrAibo.SurfaceWidth / 5)) - objectEnd;
                    float phi = Alpha * diffX;

                    Logger.Instance.LogInfo("diffx " + diffX);

                    Logger.Instance.LogInfo("Phi " + phi);

                    if (phi > 15 || phi < -15)
                    {
                        movementConsenter.objectDetectionRequest(0, phi);
                    }
                    else
                    {
                        movementConsenter.objectDetectionRequest(AiboSpeed, phi);
                    }


                    frontHeadPos = -15;

                    //_vrAibo.Turn(phi);
                    
                }

            }

            frontWindow.SetImage(maskFront);
            leftWindow.SetImage(maskLeft);
            rigthWindow.SetImage(maskRight);


        }

        private hsvEvalReturn evalMask2(Image<Gray, byte> mask, int scanHeigth, out int objectEnd, out bool objectToLeft)
        {
            List<Segment> objectSegments = new List<Segment>();
            bool onObject = 0 < mask.Data[scanHeigth, 0,0];           

            int newSegemntStart = 0;

            for (int i = 0; i < mask.Width; i++)
            {
               
                //get the distance
                double pixelColor = mask.Data[scanHeigth, i,0];
                bool isCurrentPixelObject = 0 < pixelColor;
                //check if the current pixel still the same as the segment we are currently tracking
                if (isCurrentPixelObject == onObject)
                {
                    //in this case do nothing
                }
                else
                {
                    if (onObject)
                    {
                        //change detected
                        objectSegments.Add(new Segment(newSegemntStart, i - 1, onObject));                      
                    }
                    newSegemntStart = i;
                    onObject = isCurrentPixelObject;
                }
            }

            if (onObject)
            {
                //dont forget to add the last segment
                objectSegments.Add(new Segment(newSegemntStart, mask.Width, onObject));
            }


         
                if (objectSegments.Count == 0)
                {
                    Logger.Instance.LogInfo("No object detected");
                    objectEnd = 0;
                    objectToLeft = false;
                    return hsvEvalReturn.No_Object;
                }

            //eval the segmens
            if (objectSegments.Count == 1)
            {
                Logger.Instance.LogInfo("Only one object detected");
                if (objectSegments[0].start == 0 && objectSegments[0].end== mask.Width)
                {
                    objectEnd = 0;
                    objectToLeft = true;
                    return hsvEvalReturn.Object_no_border;
                }
                else
                {
                    

                    int distToLeft = objectSegments[0].start;
                    int distToRigth = mask.Width - objectSegments[0].end;

                    if (distToLeft < distToRigth)
                    {
                        objectEnd = objectSegments[0].end;
                        objectToLeft = true;
                        return hsvEvalReturn.Object_with_border;
                    }
                    else
                    {
                        objectEnd = objectSegments[0].start;
                        objectToLeft = false;
                        return hsvEvalReturn.Object_with_border;
                    }
                }
            }
            else
            {
                //more than one object

                Logger.Instance.LogInfo("More than one object");

                //check which gap between segments is the closest to the center of the image
                int imageCenter = mask.Width / 2;
                int closestStart = -1;
                int closestEnd = -1;
                int closestDist = -1;

                for (int i = 1; i < objectSegments.Count; i++)
                {
                    int center = (objectSegments[i].start - ((objectSegments[i].start - objectSegments[i - 1].end) / 2));

                    //calc distance to image center
                    int dist = Math.Abs(center - imageCenter);

                    if (closestDist == -1)
                    {
                        //first gap we see
                        closestDist = dist;
                        closestStart = objectSegments[i - 1].end;
                        closestEnd = objectSegments[i].start;

                    }
                    else
                    {
                        if (dist < closestDist)
                        {
                            closestDist = dist;
                            closestStart = objectSegments[i - 1].end;
                            closestEnd = objectSegments[i].start;
                        }
                    }
                }

                objectEnd = closestEnd;
                objectToLeft = false;

                Logger.Instance.LogInfo("Object to the rigth, with the end at " + objectEnd);

                return hsvEvalReturn.Object_with_border;



            }
          
        }

        private hsvEvalReturn evalMask(Image<Gray, byte> mask, int scanHeigth, out int objectEnd, out bool objectToLeft)
        {
            //check if we start on free space or an object
            bool startOnObject = mask[scanHeigth, 0].Intensity > 0;

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

            int avrgDist = ImageOperations.getSegments(disp, center, distanceDB, out objectSegments, distThres, heigth);

            int segmentMergeThreshold = 10;

            //Logger.Instance.LogInfo("s " + objectSegments.Count);

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

            if (movementConsenter.busy())
            {
                movementConsenter.update();
                //turn = 0;
                //return true;
            }
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
                    _vrAibo.HeadYaw = frontHeadPos;
                    rigth = center.Copy();
                    picsTaken = 0;
                    //Logger.Instance.LogInfo("All Pics taken");
                    if (!moveBack)
                    {
                        moveBasedOnImage();
                    }
                   
                    //doStereoVisionStuff(front, disp);

                 

                    //List<Hsv> colorsToMask = om.getColorsOfCloseObstacals(nodeNavigator.CurrentRobotPosition, 10);


                    //movementConsenter.pathDetectionRequest(AiboSpeed, 0);
                    //moveAroundObject();

                    //moveAroundObject2();

                    leftWindow.SetImage(left);
                    rigthWindow.SetImage(rigth);

                    frontWindow.SetImage(front);

                    float executedMovement;
                    float executedRotation;

                   bool returnDone = movementConsenter.execute(out executedMovement, out executedRotation);

                   if (returnDone)
                   {
                       leftIsOldPath = true;
                       rigthIsOld = true;
                       moveBack = false;
                   }
                   
                    //nodeNavigator.addMovement(executedMovement, executedRotation);
                }

            _frmEyeCenter.SetImage(channelRed);

            turn = 0; // Replace this with your calculated turn value

            // Free your resources!
            leftEye.Dispose();
            rightEye.Dispose();
            return true;
        }
    }
}
