using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GLab.StereoVision;
using GLab.Core;
using Frame.VrAibo.Navigation;

using Microsoft.Xna.Framework;

namespace Frame.VrAibo.Movement
{

    enum stateOfDetObject { Left, Rigth, Center};

    class MovementConsenter
    {
        private GLab.VirtualAibo.VrAibo _robot;

        //temporary
        public double astimatedDistanceToObject;

        private bool pathRequstedMovement = false;
        private float movementFromPath = 0;
        private float turnFromPath = 0;
        private bool pathWasFrontRequest = false;


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
        private Vector2 posOfEstimation = new Vector2(0, 0);

        private double targetThreshold = 1;

        private stateOfDetObject objState;

        private double distThresholdForNodes = 5;
        private double turnDistThreshold = 0.5;

        private int pathSeenInRow =0;
        private int targetDeleteThreshold = 3;

      


        

        public MovementConsenter(GLab.VirtualAibo.VrAibo robot, NodeNavigator navigator)
        {
            astimatedDistanceToObject = -1;
            _robot = robot;
            _navigator = navigator;

            // TODO dummy assignment to initialize field
            _currentLimiter = new MovementLimiter(robot, new MovementStep(0.0f, 0.0f));
        }

      

        public bool RequestReturnToLastNode()
        {
            if (!validTarget)
            {
                returnToLastNode = true;
                firstMovementOnReturn = true;
                didLastRot = false;
                return true;
            }
            else
            {
                return false;
            }

           
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
     
        public void pathDetectionRequest(float movement, double rotation,bool requestByFront)
        {
            pathRequstedMovement = true;
            movementFromPath = movement;
            turnFromPath = (float)rotation;
            pathWasFrontRequest = requestByFront;
        }

        public void requestNewNode(bool left, bool rigth, bool front)
        {
           nodeRequestL = left; 
            nodeRequestR = rigth;
            nodeRequestF = front;
            requesteNewNode = true;
        }

        public void objectDetectionRequest(float movement, double rotation, stateOfDetObject objectState)
        {
            objectDetectionRequstedMovement = true;
            moveFromObjectDetection = movement;
            turnFromObjectDetection = (float)rotation;
            objState = objectState;
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
            if (requesteNewNode)
            {
                //check if the node is already know

                bool nodeKnow = _navigator.isCloseToNode(distThresholdForNodes);

                Node node = _navigator.nodeReachedbyCycle(distThresholdForNodes);

                if (node != null)
                {
                    //node is already know
                    //for now we simple assume that it is the last node we detected
                   

                    //check the distabce

                    double dist = VctOp.calcDistance(_navigator.CurrentRobotPosition, node.PosOfNode);
                    if (dist > turnDistThreshold)
                    {
                        //if the dist is to great we move in first, because of we turn from to far away we migth not see the other paths
                        Vector2 dirVct = VctOp.getVectorTotarget(_navigator.CurrentRobotPosition, node.PosOfNode);
                        Vector2 currentHeading = _navigator.getCurrentHeading();                      
                        double angle = VctOp.calcAngleBeteenVectors(currentHeading, dirVct);

                        HandleMovement((float)dist, (float)angle);

                        executedMovement = (float)dist;
                        executedRotation = (float)angle;
                        return;



                    }

                    //calc the diference in rotation between us and the heading of the node
                    double rotDiff = node.NodeHeading - _navigator.CurrentRobotRotation;

                    //now decide which way we sould turn next at this node
                    if (node.HasLeftTurn)
                    {
                        node.HasLeftTurn = false;
                        HandleMovement(0, (float)(rotDiff+90.0));

                        executedMovement = 0;
                        executedRotation = (float)(rotDiff + 90.0);
                        return;

                    }
                    else if (node.HasRigthTurn)
                    {
                        node.HasRigthTurn = false;
                        HandleMovement(0, (float)(rotDiff - 90.0));

                        executedMovement = 0;
                        executedRotation = (float)(rotDiff - 90.0);
                        return;
                    }
                    else if (node.HasFront)
                    {
                        node.HasFront = false;
                        HandleMovement(0, (float)(rotDiff));

                        executedMovement = 0;
                        executedRotation = (float)(rotDiff);
                        return;
                    }
                    else
                    {
                        //dead end
                        Logger.Instance.LogInfo("KNOWN DEAD END, WHAT SHOULD WE DO ?");
                        executedMovement = 0;
                        executedRotation = 0;
                        return;
                    }
                }
                else
                {
                    //its a new node
                    _navigator.createNewNodeAtCurrentPosition(nodeRequestL, nodeRequestR, nodeRequestF);
                    HandleMovement(movementFromPath, turnFromPath);

                    executedMovement = movementFromPath;
                    executedRotation = turnFromPath;
                    return;
                }
            }
            else
            {
                HandleMovement(movementFromPath, turnFromPath);

                executedMovement = movementFromPath;
                executedRotation = turnFromPath;
                return;
            }    
         

  
        }

        private void handleObjectDetectionMovement(out float executedMovement, out float executedRotation)
        {
            Logger.Instance.LogInfo("handle just object");
            //since we always try to move aroun an object on the left side, we cann follow the path detection if is also points left
         
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

          

            //in case the path movement wants to turn the same way the as the object detection we favor the path
            bool perfromPath = false;

            if (objState != stateOfDetObject.Center)
            {
                if (objState == stateOfDetObject.Left)
                {

                    //object to the left
                    if (turnFromObjectDetection < 0 && turnFromPath < 0)
                    {
                        //both point away from the object
                        if (turnFromObjectDetection < turnFromPath)
                        {
                            //in case the object turn request a greater movement we follow it, to avoid a collision
                            perfromPath = false;
                        }else{
                            perfromPath = true;

                        }

                    }
                    else if (turnFromObjectDetection > 0)
                    {
                        //object turn request a turn towards the object, follow the path unless it request a larger turn towards the object
                        if (turnFromPath < turnFromObjectDetection)
                        {
                            perfromPath = true;
                        }
                        else
                        {
                            perfromPath = false;
                        }

                    }
                    else
                    {
                        //object detection points away from the object and path towards it, follow the object detection
                        perfromPath = false;
                    }
                }
                else
                {

                    Logger.Instance.LogInfo("OBJECT TO THE R");

                  //object to the rigth, do the same thing as for left
                    //object to the left
                    if (turnFromObjectDetection > 0 && turnFromPath > 0)
                    {

                        Logger.Instance.LogInfo("BOTH POINT AWAY");

                        Logger.Instance.LogInfo("Object: " + turnFromObjectDetection + " P: " + turnFromPath);

                        //both point away from the object
                        if (turnFromObjectDetection > turnFromPath)
                        {
                            //in case the object turn request a greater movement we follow it, to avoid a collision
                            perfromPath = false;
                        }
                        else
                        {
                            perfromPath = true;

                        }

                    }
                    else if (turnFromObjectDetection < 0)
                    {
                        Logger.Instance.LogInfo("Turn to object");

                        Logger.Instance.LogInfo("Object: " + turnFromObjectDetection + " P: " + turnFromPath);

                        if (turnFromPath > turnFromObjectDetection)
                        {
                            perfromPath = true;
                        }
                        else
                        {
                            perfromPath = false;
                        }

                    }
                    else
                    {
                        //object detection points away from the object and path towards it, follow the object detection
                        perfromPath = false;
                    }
                }
            }
            else
            {
                //in case the object is in the center we follow the object detection
                perfromPath = false;
            }

            //set a target were we expect the path to be after we are clear of the object, if we havnt done so aleady
            if (!validTarget)
            {
                //we asume the path continiues about 15 units infront of us
                Vector2 vct = new Vector2(0, 30);
                vct = VctOp.calcMovementVector(_navigator.CurrentRobotRotation, vct);
                vct.X += _navigator.CurrentRobotPosition.X;
                vct.Y += _navigator.CurrentRobotPosition.Y;

                estimatedTarget = vct;
                validTarget = true;

                posOfEstimation = _navigator.CurrentRobotPosition;

            }

            if (perfromPath)
            {
                Logger.Instance.LogInfo("PERFORM PATH MOVEMENT");
                HandleMovement(movementFromPath, turnFromPath);
                executedMovement = movementFromPath;
                executedRotation = turnFromPath;
            }
            else
            {
                Logger.Instance.LogInfo("PERFORM OBJECT MOVEMENT");
                Logger.Instance.LogInfo("Valid target = "+validTarget);
                //only attempt to set a target of a conflict exists

              


                HandleMovement(moveFromObjectDetection, turnFromObjectDetection);
                executedMovement = moveFromObjectDetection;
                executedRotation = turnFromObjectDetection;
            }

            //move according to the object detection
          /*  HandleMovement(moveFromObjectDetection, turnFromObjectDetection);
            executedMovement = moveFromObjectDetection;
            executedRotation = turnFromObjectDetection;*/
            return;


       /*     double angleDiff = Math.Abs(turnFromPath - turnFromObjectDetection);

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
            }*/

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

        public bool execute(out float executedMovement, out float executedRotation, out bool moveBackAccepted)
        {
            //in case we are searching for a target, we only accept path movement request if they were issiued by the front

            if (validTarget && !pathWasFrontRequest)
            {
                Logger.Instance.LogInfo("WAS DELETED BY THIS");
                pathRequstedMovement = false;
            }

            if (validTarget && !pathRequstedMovement)
            {
                pathSeenInRow = 0;
            }

            //check the simple cases, where only one requested movement
            if (returnToLastNode&& !validTarget)
            {
                Logger.Instance.LogInfo("RETURN TO LAST NODE");

                bool returnDone = handleReturnToLastNode(out executedMovement, out executedRotation);
                _navigator.trackReverseMovement(executedMovement, executedRotation);
                clearVars();

                if (returnDone)
                {
                    moveBackAccepted = false;
                }
                else
                {
                    moveBackAccepted = true;
                }

               
                return returnDone;
            }
            else if (pathRequstedMovement && !objectDetectionRequstedMovement)
            {
                //since we found a path there is no need for a target, if we had one to beginn with
                if (validTarget)
                {
                    pathSeenInRow++;
                    if (pathSeenInRow >= targetDeleteThreshold)
                    {
                        Logger.Instance.LogInfo("VALID PATH WAS FOUND");
                        validTarget = false;
                    }
                    
                }

                handleSimplePathMovement(out executedMovement, out executedRotation);
                _navigator.addMovement(executedMovement, executedRotation);
                clearVars();
                moveBackAccepted = false;
                return false;
            }
            else if (!pathRequstedMovement && objectDetectionRequstedMovement)
            {
                handleObjectDetectionMovement(out executedMovement, out executedRotation);
                _navigator.addMovement(executedMovement, executedRotation);
                clearVars();
                moveBackAccepted = false;
                return false;
            }
            else if (pathRequstedMovement && objectDetectionRequstedMovement)
            {
                
                //executedMovement = 0;
                //executedRotation = 0;
                handleBothRequest(out executedMovement, out executedRotation);
                _navigator.addMovement(executedMovement, executedRotation);
                clearVars();
                moveBackAccepted = false;
                return false;
            }
            else
            {
                //disable this for now
               /* executedMovement = 0;
                executedRotation = 0;

                return false;*/

                Logger.Instance.LogInfo("Handle blocked path");

                //check if we need a new estimated target
                double dist = VctOp.calcDistance(_navigator.CurrentRobotPosition, estimatedTarget);

                if (dist < targetThreshold)
                {
                    //calc a new target
                    estimatedTarget = posOfEstimation;
                }              

                Vector2 dirVct = VctOp.getVectorTotarget(_navigator.CurrentRobotPosition, estimatedTarget);             
                Vector2 currentHeading = _navigator.getCurrentHeading();

                double angle = Math.Abs(VctOp.calcAngleBeteenVectors(currentHeading, dirVct));
                double angle2 = VctOp.calcAngleBeteenVectors(currentHeading, dirVct);
                bool clockwise = VctOp.isClockwise(currentHeading, dirVct);

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
                moveBackAccepted = false;
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
            pathWasFrontRequest = false;
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
