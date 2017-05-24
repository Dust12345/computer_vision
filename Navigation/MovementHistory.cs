﻿using System;
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

        /// <summary>
        /// Inserts a new movement step to the history
        /// </summary>
        /// <param name="step">The step to be inserted</param>
        public void push(MovementStep step)
        {
            _movementHistory.Push(step);
        }

        /// <summary>
        /// Removes and returns the movement step that was last inserted
        /// </summary>
        public MovementStep pop()
        {
            return _movementHistory.Pop();
        }

        /// <summary>
        /// Clears the entire movement history
        /// </summary>
        public void clear()
        {
            _movementHistory.Clear();
        }
    }
}