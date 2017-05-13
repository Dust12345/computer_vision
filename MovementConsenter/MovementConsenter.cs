using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GLab.StereoVision;


using GLab.Core;

namespace Frame.VrAibo.MovementConsenter
{
    class MovementConsenter
    {
        private GLab.VirtualAibo.VrAibo _robot;
       
        //temporary
        public double astimatedDistanceToObject;

        public bool pathRequstedMovement = false;
        public float movementFromPath =0;
        public float turnFromPath =0;

        public bool objectDetectionRequstedMovement = false;
        public float moveFromObjectDetection = 0;
        public float turnFromObjectDetection = 0;

        


        public MovementConsenter(GLab.VirtualAibo.VrAibo robot)
        {
            astimatedDistanceToObject = -1;
            _robot = robot;
        }

        public void RequestMovement(float amount)
        {
            /*
            if(astimatedDistanceToObject == -1)
            {
                 _robot.Walk(amount);
                return;
            }

             //temporary
            if (astimatedDistanceToObject < amount)
            {
                //do nothing
            }
            else
            {
                astimatedDistanceToObject -= amount;
                _robot.Walk(amount);
            }*/


           
        }
        
        public void RequestRotation(float amount)
        {
            _robot.Turn(amount);
        }

        private void handleSimplePathMovement()
        {
            if (astimatedDistanceToObject == -1)
            {
                //no objecz detected, just move
                _robot.Walk(movementFromPath);
                _robot.Turn(turnFromPath);
                return;
            }
            else
            {
                //obkect was detected
                if (astimatedDistanceToObject < movementFromPath)
                {
                    //do nothing
                }
                else
                {
                    astimatedDistanceToObject -= movementFromPath;
                    _robot.Walk(movementFromPath);
                    _robot.Turn(turnFromPath);
                    return;
                }
            }
        }

        private void handleObjectDetectionMovement()
        {
            _robot.Walk(moveFromObjectDetection);
            _robot.Turn(turnFromObjectDetection);
        }

        private void handleBothRequest()
        {
            Logger.Instance.LogInfo("handle both");

         
                
            //obkect was detected
            if (astimatedDistanceToObject < movementFromPath)
            {
                Logger.Instance.LogInfo("went in here");
                //we are to close to an object
                //move via the object detection
                _robot.Walk(moveFromObjectDetection);
                _robot.Turn(turnFromObjectDetection);
            }
            else
            {
                astimatedDistanceToObject -= movementFromPath;
                _robot.Walk(movementFromPath);
                _robot.Turn(turnFromPath);
            }
        }

        public void execute()
        {
            Logger.Instance.LogInfo("Exectute called");

            //check the simple cases, where only one requested movement
            if (pathRequstedMovement && !objectDetectionRequstedMovement)
            {
                handleSimplePathMovement();
            }
            else if (!pathRequstedMovement && objectDetectionRequstedMovement)
            {
                handleObjectDetectionMovement();
            }
            else
            {
                handleBothRequest();
            }

            //reset the values
            pathRequstedMovement = false;
            objectDetectionRequstedMovement = false;
            movementFromPath = 0;
            turnFromPath = 0;
            moveFromObjectDetection = 0;
            turnFromObjectDetection = 0;
        }
    }
}
