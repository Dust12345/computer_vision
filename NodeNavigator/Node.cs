using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Frame.VrAibo.NodeNavigator
{
    class Node
    {
        public Tuple<float, float> Coordinate { get; private set; }
        public List<MovementStep> MovementHistory { get; private set; }
        public List<Node> Children { get; private set; }
        public Node Parent { get; private set; }
        public int TraversalCount { get; private set; }

        public Node()
        {

        }
    }
}
