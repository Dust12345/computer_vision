using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Frame.VrAibo.NodeNavigator
{
    class NodeNavigator
    {  
        public Node StartNode { get; private set; }

        private Vector2 robotPosition;
        private double robotRotation;
        

        public NodeNavigator()
        {
            robotPosition = new Vector2(0, 0);
            robotRotation = 0;
        }

        public double getRotation()
        {
            return robotRotation;
        }

        public Vector2 getCurrentHeading()
        {
            return VctOp.calcMovementVector(robotRotation, new Vector2(0, 1));
        }

        public void addMovement(float movementDistance, double rotation)
        {
            //add the rotation to the overall rotation
            robotRotation += rotation;
            robotRotation = robotRotation % 360;

            Vector2 unrotatedMovementVector = new Vector2(0, movementDistance);

            //rotate the vector
            Vector2 rotatedVector = VctOp.calcMovementVector(robotRotation, unrotatedMovementVector);

            //add the rotated vector to the old position
            robotPosition.X += rotatedVector.X;
            robotPosition.Y += rotatedVector.Y;
        }
        

        // simple stopgap implementation to make other stuff work
        public Vector2 getCurrentPosition()
        {
            return robotPosition;
        }
    }
}
