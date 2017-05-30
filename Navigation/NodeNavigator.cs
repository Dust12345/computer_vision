using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using GLab.Core;
using Emgu.CV;

namespace Frame.VrAibo.Navigation
{
    class NodeNavigator
    {
        public Vector2 CurrentRobotPosition { get; private set; }
        public double CurrentRobotRotation { get; private set; }
        public MovementHistory CurrentMovementHistory { get; private set; }

        private Node _headNode;
        private Node _lastNode;


        public NodeNavigator()
        {
            CurrentRobotPosition = new Vector2(0, 0);
            CurrentRobotRotation = 0;

            CurrentMovementHistory = new MovementHistory();
            _headNode = new Node(null, null, true);
            _lastNode = _headNode;
        }

        public Vector2 getCurrentHeading()
        {
            return VctOp.calcMovementVector(CurrentRobotRotation, new Vector2(0, 1));
        }

        private bool isPointOnMovementHistory(Vector2 endPosOfThisHistory,Vector2 posOfDetectedPath,ref float rotation ,double distThreshold,List<MovementStep> movementSteps)
        {
            for(int i= movementSteps.Count - 1; i > -1; i--)
            {
                Vector2 vct = VctOp.calcMovementVector(rotation,new Vector2(0, -movementSteps[i].Movement));
                endPosOfThisHistory.X = endPosOfThisHistory.X + vct.X;
                endPosOfThisHistory.Y = endPosOfThisHistory.Y + vct.Y;

                rotation = rotation - movementSteps[i].Rotation;
                rotation = rotation % 360;

                //check if that poinz is close to the refernce point
                double dist = VctOp.calcDistance(posOfDetectedPath, endPosOfThisHistory);

                Logger.Instance.LogInfo(endPosOfThisHistory+" end pos");


                if (dist < distThreshold)
                {
                    return true;
                }
            }

            return false;
        }

        public bool isKnowPath(Vector2 posOfDetectedPath, double distThreshold)
        {
            //check if that point is close to a point in the current movement history
            MovementHistory mh = CurrentMovementHistory;

            float rotation = (float)CurrentRobotRotation;
            Vector2 robotPos = new Vector2(CurrentRobotPosition.X, CurrentRobotPosition.Y);

            bool isOnPath= isPointOnMovementHistory(robotPos, posOfDetectedPath,ref rotation, distThreshold, mh.getAsList());

            if (isOnPath)
            {
                return true;
            }else
            {
                if (_lastNode.IsRootNode)
                {
                    return false;
                }

                Node n = _lastNode.Parent;

                while (true)
                {
                    
                    if(isPointOnMovementHistory(robotPos, posOfDetectedPath, ref rotation, distThreshold, n.MovementHistory.getAsList())){
                        return true;
                    }

                    if (n.IsRootNode)
                    {
                        return false;
                    }
                }
            }

            return false;
        }

        public void addMovement(float movementDistance, float rotation)
        {
            // Add new step to the current history
            CurrentMovementHistory.push(new MovementStep(movementDistance, rotation));

            //add the rotation to the overall rotation
            CurrentRobotRotation += rotation;
            CurrentRobotRotation = CurrentRobotRotation % 360;

            Vector2 unrotatedMovementVector = new Vector2(0, movementDistance);

            //rotate the vector
            Vector2 rotatedVector = VctOp.calcMovementVector(CurrentRobotRotation, unrotatedMovementVector);
            CurrentRobotPosition = CurrentRobotPosition + rotatedVector;
        }

        /// <summary>
        /// Creates a new node at the current position
        /// This copies the current movement history into the node memory,
        /// creating a new segment
        /// </summary>
        public void createNewNodeAtCurrentPosition(bool hasLeftTurn = false, bool hasRigthTurn = false,bool hasFront = true)
        {
            Node newNode = new Node(CurrentMovementHistory, _lastNode, hasLeftTurn, hasRigthTurn,hasFront);
            _lastNode.Children.Add(newNode);

            CurrentMovementHistory.clear();
            _lastNode = newNode;
        }

        /// <summary>
        /// Returns the parent of the currently active branch, effectively the last saved Node
        /// Does not discard current branch
        /// </summary>
        /// <returns></returns>
        public Node getCurrentNodeParent()
        {
            return _lastNode.Parent;
        }

        /// <summary>
        /// Discards the currently active branch of the tree, 
        /// removing all movement changes that happened since the node was saved
        /// </summary>
        public void discardCurrentBranch()
        {
            _lastNode.MovementHistory.clear();
            _lastNode = _lastNode.Parent;
        }
    }
}
