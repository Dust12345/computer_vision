using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Frame.VrAibo.Navigation
{
    class MovementHistory
    {
        private Stack<MovementStep> _movementHistory;

        public MovementHistory()
        {
            _movementHistory = new Stack<MovementStep>();
        }

        public void push(MovementStep step)
        {
            _movementHistory.Push(step);
        }

        public MovementStep pop()
        {
            return _movementHistory.Pop();
        }

        public void clear()
        {
            _movementHistory.Clear();
        }
    }
}
