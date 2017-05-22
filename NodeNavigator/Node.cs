using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Frame.VrAibo.NodeNavigator
{
    class Node
    {
        public Vector2 Coordinate { get; private set; }
        public MovementHistory MovementHistory { get; private set; }
        public List<Node> Children { get; private set; }
        public Node Parent { get; private set; }
        public int TraversalCount { get; private set; }

        public Node(MovementHistory movementHistory, Node parent = null)
        {
            MovementHistory = movementHistory;
            Parent = parent;
        }

        /// <summary>
        /// Returns true if node is root
        /// </summary>
        /// <returns></returns>
        public bool isRoot()
        {
            return Parent == null;
        }
    }
}
