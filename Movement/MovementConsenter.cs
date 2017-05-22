using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GLab.StereoVision;
using GLab.Core;
using Frame.VrAibo.Node;

namespace Frame.VrAibo.Movement
{
    class MovementConsenter
    {
        private GLab.VirtualAibo.VrAibo _robot;
       
        //temporary
        public double astimatedDistanceToObject;

        private bool pathRequstedMovement = false;
        private float movementFromPath = 0;
        private float turnFromPath = 0;

        private bool objectDetectionRequstedMovement = false;
        private float moveFromObjectDetection = 0;
        private float turnFromObjectDetection = 0;

        private double angleDiffThreshold = 15;

        private bool returnToLastNode = false;

        private NodeNavigator _navigator;


        public MovementConsenter(GLab.VirtualAibo.VrAibo robot, NodeNavigator navigator)
        {
            astimatedDistanceToObject = -1;
            _robot = robot;
            _navigator = navigator;
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

        public void pathDetectionReques(float movement, double rotation)
        {
          
              pathRequstedMovement = true;
              movementFromPath = movement;
              turnFromPath = (float)rotation;
        }

        public void objectDetectionReques(float movement, double rotation)
        {
              objectDetectionRequstedMovement = true;
              moveFromObjectDetection = movement;
              turnFromObjectDetection = (float)rotation;
        }

        public void RequestRotation(float amount)
        {
            _robot.Turn(amount);
        }

        private void handleSimplePathMovement(out float executedMovement, out double executedRotation)
        {
            if (astimatedDistanceToObject == -1)
            {
                //no objecz detected, just move
                _robot.Turn(turnFromPath);
                _robot.Walk(movementFromPath);              
                executedMovement = movementFromPath;
                executedRotation = turnFromPath;
                return;
            }
            else
            {
                //obkect was detected
                if (astimatedDistanceToObject < movementFromPath)
                {
                    //do nothing
                    executedMovement = 0;
                    executedRotation = 0;
                }
                else
                {
                    astimatedDistanceToObject -= movementFromPath;
                    _robot.Turn(turnFromPath);
                    _robot.Walk(movementFromPath);
                    executedMovement = movementFromPath;
                    executedRotation = turnFromPath;
                    return;
                }
            }
        }

        private void handleObjectDetectionMovement(out float executedMovement, out double executedRotation)
        {
            Logger.Instance.LogInfo("handle just object");
            _robot.Turn(turnFromObjectDetection);
            _robot.Walk(moveFromObjectDetection);

            executedMovement = moveFromObjectDetection;
            executedRotation = turnFromObjectDetection;
        }

        private void handleReturnToLastNode(out float executedMovement, out double executedRotation)
        {
            Logger.Instance.LogInfo("Handle returning to last node");

            // Get node history
            MovementHistory history = _navigator.CurrentMovementHistory;

            MovementStep step = history.pop(); // TODO: check if popped in source history

            executedMovement = step.Movement;
            executedRotation = step.Rotation;
        }

        private void handleBothRequest(out float executedMovement, out double executedRotation)
        {
            Logger.Instance.LogInfo("handle both");

         
            //check if both movement request point in the same direction, if so we can just move as the path algo suggests

            double angleDiff = Math.Abs(turnFromPath - turnFromObjectDetection);

            if (angleDiff < angleDiffThreshold)
            {
                _robot.Turn(turnFromPath);
                _robot.Walk(movementFromPath);
                executedMovement = movementFromPath;
                executedRotation = turnFromPath;
                return;
            }
            else
            {
                _robot.Turn(turnFromObjectDetection);
                _robot.Walk(moveFromObjectDetection);
                executedMovement = moveFromObjectDetection;
                executedRotation = turnFromObjectDetection;
                return;
            }

#if false 
            //obkect was detected
            if (astimatedDistanceToObject < movementFromPath)
            {
                Logger.Instance.LogInfo("went in here");
                //we are to close to an object
                //move via the object detection
                _robot.Turn(turnFromObjectDetection);
                _robot.Walk(moveFromObjectDetection);
                executedMovement = moveFromObjectDetection;
                executedRotation = turnFromObjectDetection;
              
            }
            else
            {
                astimatedDistanceToObject -= movementFromPath;

                _robot.Turn(turnFromPath);
                _robot.Walk(movementFromPath);


                executedMovement = movementFromPath;
                executedRotation = turnFromPath;
            }
#endif
        }

        public void execute(out float executedMovement,out double executedRotation)
        {
            Logger.Instance.LogInfo("Exectute called");

            //check the simple cases, where only one requested movement
            if(returnToLastNode)
            {
                handleReturnToLastNode(out executedMovement, out executedRotation);
            }
            else if (pathRequstedMovement && !objectDetectionRequstedMovement)
            {
                handleSimplePathMovement(out executedMovement,out executedRotation);
            }
            else if (!pathRequstedMovement && objectDetectionRequstedMovement)
            {
                handleObjectDetectionMovement(out executedMovement, out executedRotation);
            }
            else if (pathRequstedMovement && objectDetectionRequstedMovement)
            {
                //Logger.Instance.LogInfo("SHOULD HANDLE BOTH");
                //executedMovement = 0;
                //executedRotation = 0;
                handleBothRequest(out executedMovement, out executedRotation);
            }
            else
            {
                Logger.Instance.LogInfo("NO INFO");
                executedMovement = 0;
                executedRotation = 0;
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
