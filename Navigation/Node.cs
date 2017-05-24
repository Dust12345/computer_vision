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

        public Vector2 Coordinate { get; private set; }
        public MovementHistory MovementHistory { get; private set; }
        public List<Node> Children { get; private set; }
        public Node Parent { get; private set; }
        public int TraversalCount { get; private set; }

        private bool _isRootNode;

        public Node(MovementHistory movementHistory = null, Node parent = null, bool setAsRoot = false)
        {
            MovementHistory = movementHistory;
            Parent = parent;

            if(setAsRoot)
            {
                if(RootNode != null)
                {
                    throw new InvalidOperationException("A root node is already defined!");
                }

                _isRootNode = true;
                RootNode = this;
            }
        }

        /// <summary>
        /// Returns true if node is root
        /// </summary>
        /// <returns></returns>
        public bool isRootNode()
        {
            return _isRootNode;
        }
    }
}
