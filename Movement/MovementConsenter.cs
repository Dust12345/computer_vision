using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GLab.StereoVision;
using GLab.Core;
using Frame.VrAibo.Navigation;

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
        private MovementLimiter _currentLimiter;


        public MovementConsenter(GLab.VirtualAibo.VrAibo robot, NodeNavigator navigator)
        {
            astimatedDistanceToObject = -1;
            _robot = robot;
            _navigator = navigator;

            // TODO dummy assignment to initialize field
            _currentLimiter = new MovementLimiter(robot, new MovementStep(0.0f, 0.0f));
        }

        public void RequestReturnToLastNode()
        {
            returnToLastNode = true;
        }

        private bool HandleMovement(float moveAmount, float turnAmount)
        {
            if (_currentLimiter.Done)
            {
                _currentLimiter = new MovementLimiter(_robot, new MovementStep(moveAmount, turnAmount));
            }

            return _currentLimiter.Done;
        }

        public bool RequestMovement(float amount)
        {
            return HandleMovement(amount, 0.0f);
        }

        public void RequestRotation(float amount)
        {
            // TODO: conditions
            HandleMovement(0.0f, amount);
        }

        public void pathDetectionRequest(float movement, double rotation)
        {
            pathRequstedMovement = true;
            movementFromPath = movement;
            turnFromPath = (float)rotation;
        }

        public void objectDetectionRequest(float movement, double rotation)
        {
            objectDetectionRequstedMovement = true;
            moveFromObjectDetection = movement;
            turnFromObjectDetection = (float)rotation;
        }

        public bool busy()
        {
            return !_currentLimiter.Done;
        }

        private void handleSimplePathMovement(out float executedMovement, out float executedRotation)
        {
            if (astimatedDistanceToObject == -1)
            {
                //no objecz detected, just move
                HandleMovement(movementFromPath, turnFromPath);
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
                    HandleMovement(movementFromPath, turnFromPath);
                    executedMovement = movementFromPath;
                    executedRotation = turnFromPath;
                    return;
                }
            }
        }

        private void handleObjectDetectionMovement(out float executedMovement, out float executedRotation)
        {
            Logger.Instance.LogInfo("handle just object");
            HandleMovement(moveFromObjectDetection, turnFromObjectDetection);

            executedMovement = moveFromObjectDetection;
            executedRotation = turnFromObjectDetection;
        }

        private void handleReturnToLastNode(out float executedMovement, out float executedRotation)
        {
            // Get current node history
            MovementHistory history = _navigator.CurrentMovementHistory;

            if (history.Count <= 0)
            {
                returnToLastNode = false;

                // TODO: useless last execution path for empty movement history
                executedMovement = 0;
                executedRotation = 0;
                return;
            }

            Logger.Instance.LogInfo("Handle returning to last node");

            MovementStep step = history.pop();

            // Feed execute behavior with the inverse of the history
            executedMovement = -step.Movement;
            executedRotation = -step.Rotation;
        }

        private void handleBothRequest(out float executedMovement, out float executedRotation)
        {
            Logger.Instance.LogInfo("handle both");


            //check if both movement request point in the same direction, if so we can just move as the path algo suggests

            double angleDiff = Math.Abs(turnFromPath - turnFromObjectDetection);

            if (angleDiff < angleDiffThreshold)
            {
                HandleMovement(movementFromPath, turnFromPath);
                executedMovement = movementFromPath;
                executedRotation = turnFromPath;
                return;
            }
            else
            {
                HandleMovement(moveFromObjectDetection, turnFromObjectDetection);
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

        public void execute(out float executedMovement, out float executedRotation)
        {
            Logger.Instance.LogInfo("Exectute called");

            //check the simple cases, where only one requested movement
            if (returnToLastNode)
            {
                handleReturnToLastNode(out executedMovement, out executedRotation);
            }
            else if (pathRequstedMovement && !objectDetectionRequstedMovement)
            {
                handleSimplePathMovement(out executedMovement, out executedRotation);
                _navigator.addMovement(executedMovement, executedRotation);
            }
            else if (!pathRequstedMovement && objectDetectionRequstedMovement)
            {
                handleObjectDetectionMovement(out executedMovement, out executedRotation);
                _navigator.addMovement(executedMovement, executedRotation);
            }
            else if (pathRequstedMovement && objectDetectionRequstedMovement)
            {
                //Logger.Instance.LogInfo("SHOULD HANDLE BOTH");
                //executedMovement = 0;
                //executedRotation = 0;
                handleBothRequest(out executedMovement, out executedRotation);
                _navigator.addMovement(executedMovement, executedRotation);
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

        internal void update()
        {
            if (!_currentLimiter.Done)
            {
                _currentLimiter.execute();
            }
        }
    }
}
