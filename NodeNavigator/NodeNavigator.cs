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
        public Vector2 RobotPosition { get; private set; }
        public double RobotRotation { get; private set; }       
        

        public NodeNavigator()
        {
            RobotPosition = new Vector2(0, 0);
            RobotRotation = 0;
        }

        public Vector2 getCurrentHeading()
        {
            return VctOp.calcMovementVector(RobotRotation, new Vector2(0, 1));
        }

        public void addMovement(float movementDistance, double rotation)
        {
            //add the rotation to the overall rotation
            RobotRotation += rotation;
            RobotRotation = RobotRotation % 360;

            Vector2 unrotatedMovementVector = new Vector2(0, movementDistance);

            //rotate the vector
            Vector2 rotatedVector = VctOp.calcMovementVector(RobotRotation, unrotatedMovementVector);

            //add the rotated vector to the old position
            RobotPosition = RobotPosition + rotatedVector;
        }
    }
}
