using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Frame.VrAibo.Navigation
{
    class Node
    {
        public static Node RootNode { get; private set; }

        public bool HasLeftTurn { get; private set; }
        public bool HasRigthTurn { get; private set; }
        public bool HasFront { get; private set; }

        public Vector2 Coordinate { get; private set; }
        public MovementHistory MovementHistory { get; private set; }
        public List<Node> Children { get; private set; }
        public Node Parent { get; private set; }
        public int TraversalCount { get; private set; }

        public bool IsRootNode { get; private set; }

        public Node(MovementHistory movementHistory = null, Node parent = null, bool setAsRoot = false, bool hasLeftTurn = false,bool hasRigthTurn = false,bool hasFront = true)
        {
            if(setAsRoot)
            {
                if(RootNode != null)
                {
                    throw new InvalidOperationException("A root node is already defined!");
                }

                IsRootNode = true;
                RootNode = this;
            }
            else
            {
                // If it's not a root node, then it must have a parent
                if(parent == null)
                {
                    throw new ArgumentException("Parent must be defined for non-root nodes");
                }

                // Similarily, any node that isn't root must have a movement history that leads to it
                if(movementHistory == null)
                {
                    throw new ArgumentException("Movement history must be defined for non-root nodes");
                }
            }

            HasLeftTurn = hasLeftTurn;
            HasRigthTurn = hasRigthTurn;
            HasFront = hasFront;

            MovementHistory = movementHistory;
            Parent = parent;
        }
    }
}
