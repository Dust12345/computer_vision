using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Frame.VrAibo.NodeNavigator
{
    class MovementHistory
    {
        private List<MovementStep> _movementHistory;

        public MovementHistory()
        {
            _movementHistory = new List<MovementStep>();
        }

        public void add(MovementStep step)
        {
            _movementHistory.Add(step);
        }

        public void clear()
        {
            _movementHistory.Clear();
        }
    }
}
