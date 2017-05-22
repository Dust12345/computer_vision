using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Frame.VrAibo.NodeNavigator
{
    class NodeNavigator
    {
        public Vector2 CurrentRobotPosition { get; private set; }
        public double CurrentRobotRotation { get; private set; }

        private MovementHistory _currentMovementHistory;
        private Node _headNode;
        private Node _lastNode;


        public NodeNavigator()
        {
            CurrentRobotPosition = new Vector2(0, 0);
            CurrentRobotRotation = 0;

            _currentMovementHistory = new MovementHistory();
            _headNode = new Node(null, null, true);
            _lastNode = _headNode;
        }

        public Vector2 getCurrentHeading()
        {
            return VctOp.calcMovementVector(CurrentRobotRotation, new Vector2(0, 1));
        }

        public void addMovement(float movementDistance, double rotation)
        {
            // Add new step to the current history
            _currentMovementHistory.add(new MovementStep(movementDistance, rotation));

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
        public void createNewNodeAtCurrentPosition()
        {
            Node newNode = new Node(_currentMovementHistory);
            _currentMovementHistory.clear();
            _lastNode.Children.Add(newNode);
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
