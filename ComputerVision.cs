#region

using System.Collections.Generic;
using System.Drawing;
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
    /// <summary>
    ///   Central class for the Computer Vision lab.
    /// </summary>
    internal class ComputerVision : IPluginClient
    {
        private const float AiboSpeed = 0.2f;
        private FrmImage _frmImage;
        private FrmVrAiboRemote _frmVrAiboRemote;
        private GLab.VirtualAibo.VrAibo _vrAibo;

        public ComputerVision()
        {
            Name = "Computer Vision Lab";
        }

        public bool GoAibo { get; set; }

        public override void Setup()
        {
            /*
             * DEFAULT PARCOURS LAB TASK
             *
             *
             * Coordinate System
             *      
             *      |         50        100
             *   ---+---------+---------+------>  x-axis
             *      |         :                  
             *      | Parcours: Parcours
             *      |    00   :    01   
             *      |         :                  
             *   50 +.........:..........
             *      |         :                  
             *      | Parcours: Parcours
             *      |    03   :    02   
             *      |         :                  
             *      |         :                  
             *   100+
             *      |
             *      v
             *   z-axis
             */

            // Task 1: FOLLOW PARCOURS

            // Create list of parcour elements
            /*
             * Task 1 (a): DEFAULT PARCOURS LAB TASK WITHOUT TERRAIN
             */
            List<Parcours> parcours = new List<Parcours>
                                          {
                                              // Creates a new parcour element.
                                              new Parcours00(0, 0),
                                              // Creates a new parcour element and rotates it 90° to the left.
                                              new Parcours01(1, 0, -90),
                                              // Creates a new parcour element and rotates it 180°.
                                              new Parcours02(1, 1, 180),
                                              // Creates a new parcour element and rotates it 90° to the right.
                                              new Parcours03(0, 1, 90)
                                          };

            /*
             * Task 1 (b): DEFAULT PARCOURS LAB TASK WITH TERRAIN
             */
            //List<Parcours> parcours = new List<Parcours>
            //                              {
            //                                  new ParcoursTerrain(),
            //                                  new Parcours00(0, 0, 0, false),
            //                                  new Parcours01(1, 0, -90, false),
            //                                  new Parcours02(1, 1, 180, false),
            //                                  new Parcours03(0, 1, 90, false)
            //                              };

            // Creates a new Virtual Aibo
            _vrAibo = new GLab.VirtualAibo.VrAibo(parcours) { Position = new Vector2(-25, 50) };
            _vrAibo.Turn(-45.0f);        // looking into Parcours00
            // Set other positions and orientations for Virtual Aibo
            //_vrAibo.Position = new Vector2(50, -25);
            //_vrAibo.Turn(-135.0f);       // looking into Parcours01
            //_vrAibo.Position = new Vector2(125, 50);  
            //_vrAibo.Turn(135.0f);       // looking into Parcours02
            //_vrAibo.Position = new Vector2(50, 125);  
            //_vrAibo.Turn(45.0f);        // looking into Parcours03

            // END of Definitions for Task 1


            // Task 2: FIND TREASURE
            /*
             * Task 2 (a): TREASURE LAB TASK WITHOUT TERRAIN
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
             * Task 2 (b): TREASURE LAB TASK WITH TERRAIN
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

            // Creates a new Virtual Aibo at position (0,20) facing north
            //_vrAibo = new GLab.VirtualAibo.VrAibo(stripes) { Position = new Vector2(0, 20) };

            // END of Definitions for Task 2


            // Use center camera only, omit left and right camera 
            _vrAibo.ShowLeftCamera(false);
            _vrAibo.ShowRightCamera(false);
            _vrAibo.ShowCenterCamera(true);

            // Create a remote control window an link to Aibo instance
            _frmVrAiboRemote = new FrmVrAiboRemote(_vrAibo, _timer);

            // Hook functionality to blue green and red button.
            _frmVrAiboRemote.HookMeBlue += delegate { GoAibo = !GoAibo; };
            _frmVrAiboRemote.HookMeGreen += ImageProcessing;
            //_frmVrAiboRemote.HookMeRed += delegate { };

            // Create a new window for the processed center image
            // Unprocessed left/right/center/birdview windows are created and updated automatically
            _frmImage = new FrmImage("Processing-Center",
                                         GLab.VirtualAibo.VrAibo.SurfaceWidth,
                                         GLab.VirtualAibo.VrAibo.SurfaceHeight);

            Logger.Instance.ClearLog();
            Logger.Instance.LogInfo("Use Aibo remote to walk around.");
            Logger.Instance.LogInfo("FOR TASK 1: Walk to the line and press the BLUE or GREEN button to start line tracking in RUN or STEP-Mode.");
            Logger.Instance.LogInfo("FOR TASK 2: Press the BLUE or GREEN button to start in RUN or STEP-Mode.");
            // Start the looped execution of Run()
            Start();
        }

        public override void Run()
        {
            if (GoAibo)
                ImageProcessing();
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
        }

        /// <summary>
        ///   Let's do some image processing and move aibo
        /// </summary>
        private void ImageProcessing()
        {
            float turn;

            if (TrackLine(out turn))
                {
                _vrAibo.Turn(turn);
                _vrAibo.Walk(AiboSpeed);
                _vrAibo.HeadPitch = -10f;
            }
        }

        /// <summary>
        ///   This method is called whenever a frame was rendered and is ready for processing.
        ///   Find the path and assign the turn parameter accordingly.
        /// </summary>
        /// <param name = "turn">Out parameter. Degrees Aibo needs to turn to center the line.</param>
        /// <returns>Return value indicates whether the line was found or not.</returns>
        private bool TrackLine(out float turn)
        {
            // Get image from center eye
            Image<Rgb, byte> centerEye = new Image<Rgb, byte>((Bitmap)_vrAibo.GetBitmapCenterEye());

            // ...and create a new Image...
            Image<Rgb, byte> centerProcessing = centerEye.Copy();

            float disparity = _vrAibo.Disparity;  // Half the distance between left and right camera
            float cameraHeight = _vrAibo.CameraHeight;
            // See  VirtualAibo.cs  for more information

            /////////////////////
            // YOUR CODE HERE! //
            /////////////////////

            turn = 0; // Replace this with your calculated turn value

            Gray cannyThreshold = new Gray(10);
            Gray cannyThresholdLinking = new Gray(10);

            Image<Bgra, byte> cvCenter = new Image<Bgra, byte>((Bitmap) _vrAibo.GetBitmapCenterEye());
            Image<Gray, byte> cvGray = cvCenter.Convert<Gray, byte>();
            Image<Gray, byte> cvCanny = cvGray.Canny(10, 10);

            _frmImage.SetImage(cvCanny);

            // Free your resources!
            centerEye.Dispose();
            centerProcessing.Dispose();
            cvCenter.Dispose();
            cvGray.Dispose();
            cvCanny.Dispose();
            return true;
        }
    }
}