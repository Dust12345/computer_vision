﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GLab.StereoVision;
using GLab.Core;
using Frame.VrAibo.Navigation;

using Microsoft.Xna.Framework;

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

        private bool disableLimiter = true;

        private bool firstMovementOnReturn = false;

        private double lastRotOnReturn = 0;
        private bool didLastRot = false;

        private bool requesteNewNode = false;
        private bool nodeRequestL = false;
        private bool nodeRequestR = false;
        private bool nodeRequestF = false;

        private bool validTarget = false;
        private Vector2 estimatedTarget = new Vector2(0,0);



        

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
            firstMovementOnReturn = true;
            didLastRot = false;
        }

        private void HandleReverseMovement(float moveAmount, float turnAmount)
        {
            if (disableLimiter)
            {
                _robot.Walk(moveAmount);
                _robot.Turn(turnAmount);
            }
        }

        private bool HandleMovement(float moveAmount, float turnAmount)
        {

            if (disableLimiter)
            {
                _robot.Turn(turnAmount);
                _robot.Walk(moveAmount);
                return true;
            }

            if (_currentLimiter.Done)
            {
                _currentLimiter = new MovementLimiter(_robot, new MovementStep(moveAmount, turnAmount));
            }

            return _currentLimiter.Done;
        }


        public bool isReturning()
        {
            return returnToLastNode;
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

        public void requestNewNode(bool left, bool rigth, bool front)
        {
           nodeRequestL = left; 
            nodeRequestR = rigth;
            nodeRequestF = front;
            requesteNewNode = true;
        }

        public void objectDetectionRequest(float movement, double rotation)
        {
            objectDetectionRequstedMovement = true;
            moveFromObjectDetection = movement;
            turnFromObjectDetection = (float)rotation;
        }

        public bool busy()
        {
            if (disableLimiter)
            {
                return false;
            }

            return !_currentLimiter.Done;
        }

        private void handleSimplePathMovement(out float executedMovement, out float executedRotation)
        {



            _navigator.createNewNodeAtCurrentPosition(nodeRequestL, nodeRequestR, nodeRequestF);
            HandleMovement(movementFromPath, turnFromPath);    

            Logger.Instance.LogInfo("handle path");
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

        private bool handleReturnToLastNode(out float executedMovement, out float executedRotation)
        {

            // Get current node history
            MovementHistory history = _navigator.CurrentMovementHistory;

            if (history.Count <= 0)
            {

                //we have to do a final 180 rotation
                if (!didLastRot)
                {
                    HandleMovement(0, 180);
                    executedMovement = 0;
                    executedRotation = 180;
                    didLastRot = true;
                    return false;
                }


                returnToLastNode = false;
                // reset us to the node we returned to
                Node currentNode = _navigator.getCurrentNode();


               //ceck which way to turn next

                if (currentNode.HasLeftTurn)
                {
                    currentNode.HasLeftTurn = false;
                    //try left next
                    HandleMovement(0, 90);
                    executedMovement = 0;
                    executedRotation = 90;
                    return true;
                }
                else if (currentNode.HasRigthTurn)
                {                   
                    currentNode.HasRigthTurn = false;
                    HandleMovement(0, -90);
                    executedMovement = 0;
                    executedRotation = -90;
                    return true;
                }
                else if (currentNode.HasFront)
                {                   
                    //nothing to do
                    currentNode.HasFront = false;
                    executedMovement = 0;
                    executedRotation = 0;
                    return true;
                }
                else
                {

                    //node was a dead end
                    executedMovement = 0;
                    executedRotation = 0;
                    return true;
                }
                
              
            }

            //bevore we do anything with the movement steps, we have to do a 180 deg turn
            if (firstMovementOnReturn)
            {

              /*  List<MovementStep> l = history.getAsList();

                for (int i = 0; i < l.Count; i++)
                {
                    Logger.Instance.LogInfo("Step: "+l[i].Movement+" "+l[i].Rotation);
                }*/

                    executedMovement = 0;
                executedRotation = 180;
                HandleMovement(0, 180);
                firstMovementOnReturn = false;
                return false;
            }

            Logger.Instance.LogInfo("Handle returning to last node");

            MovementStep step = history.pop();

            // Feed execute behavior with the inverse of the history
            //lastRotOnReturn = step.Rotation;

           

            executedMovement = step.Movement;
            executedRotation = -step.Rotation;
            HandleReverseMovement(step.Movement, -step.Rotation);

            return false;
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

        public bool execute(out float executedMovement, out float executedRotation)
        {
            Logger.Instance.LogInfo("Exectute called");

            //check the simple cases, where only one requested movement
            if (returnToLastNode&& !validTarget)
            {
                bool returnDone = handleReturnToLastNode(out executedMovement, out executedRotation);
                _navigator.trackReverseMovement(executedMovement, executedRotation);
                clearVars();
                return returnDone;
            }
            else if (pathRequstedMovement && !objectDetectionRequstedMovement)
            {
                handleSimplePathMovement(out executedMovement, out executedRotation);
                _navigator.addMovement(executedMovement, executedRotation);
                clearVars();
                return false;
            }
            else if (!pathRequstedMovement && objectDetectionRequstedMovement)
            {
                handleObjectDetectionMovement(out executedMovement, out executedRotation);
                _navigator.addMovement(executedMovement, executedRotation);
                clearVars();
                return false;
            }
            else if (pathRequstedMovement && objectDetectionRequstedMovement)
            {
                
                //executedMovement = 0;
                //executedRotation = 0;
                handleBothRequest(out executedMovement, out executedRotation);
                _navigator.addMovement(executedMovement, executedRotation);
                clearVars();
                return false;
            }
            else
            {
                //disable this for now
               /* executedMovement = 0;
                executedRotation = 0;

                return false;*/

                //move to the last know point
                Vector2 targetDest = new Vector2(0, 30);

                Vector2 dirVct = VctOp.getVectorTotarget(_navigator.CurrentRobotPosition, targetDest);
                Logger.Instance.LogInfo("NO INFO");

                Logger.Instance.LogInfo("Dir vct " + dirVct);
                Logger.Instance.LogInfo("R pos " + _navigator.CurrentRobotPosition);

                Vector2 currentHeading = _navigator.getCurrentHeading();

                double angle = Math.Abs(VctOp.calcAngleBeteenVectors(currentHeading, dirVct));
                double angle2 = VctOp.calcAngleBeteenVectors(currentHeading, dirVct);
                bool clockwise = VctOp.isClockwise(currentHeading, dirVct);


                Logger.Instance.LogInfo("angle " + angle2);

                angle2 = angle2 / 4;

                

                if (angle2 < 5 && angle2 > -5)
                {
                    HandleMovement(0.3f, (float)angle2);
                    executedMovement = 0.3f;
                    executedRotation = (float)angle2;

                    _navigator.addMovement(executedMovement, executedRotation);
                }
                else
                {
                    HandleMovement(0.0f, (float)angle2);
                    executedMovement = 0.0f;
                    executedRotation = (float)angle2;
                    _navigator.addMovement(executedMovement, executedRotation);
                }

                clearVars();
                return false;              
               
            }

            //reset the values
         

        }

        private void clearVars()
        {
            pathRequstedMovement = false;
            objectDetectionRequstedMovement = false;
            movementFromPath = 0;
            turnFromPath = 0;
            moveFromObjectDetection = 0;
            turnFromObjectDetection = 0;
            requesteNewNode = false;
            nodeRequestL = false;
            nodeRequestR = false;
            nodeRequestF = false;
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
