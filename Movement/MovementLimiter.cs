using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Frame.VrAibo.Navigation;

namespace Frame.VrAibo.Movement
{
    class MovementLimiter
    {
        public static float MaxMovementPerFrame = 0.5f;
        public static float MaxRotationPerFrame = 1.0f;

        private GLab.VirtualAibo.VrAibo _robot;
        private MovementStep _step;

        public bool Done { get; private set; }

        public MovementLimiter(GLab.VirtualAibo.VrAibo robot, MovementStep step)
        {
            _robot = robot;
            _step = step;
            checkAndUpdateDone();   // movement step may contain no movement nor rotation
        }

        private bool checkAndUpdateDone()
        {
            if (_step.Movement == 0.0f &&
                _step.Rotation == 0.0f)
            {
                Done = true;
            }

            return Done;
        }

        public bool execute()
        {
            // Clamp rotation and movement to max speed
            float movement = Math.Min(MaxMovementPerFrame, Math.Abs(_step.Movement));
            float rotation = Math.Min(MaxRotationPerFrame, Math.Abs(_step.Rotation));

            // Get sign of movement step
            int signMovement = _step.Movement < 0 ? -1 : 1;
            int signRotation = _step.Rotation < 0 ? -1 : 1;

            // Execute movement and rotation
            _robot.Walk(movement * signMovement);
            _robot.Turn(rotation * signRotation);

            _step = new MovementStep(
                _step.Movement - (movement * signMovement),
                _step.Rotation - (rotation * signRotation));

            return checkAndUpdateDone();
        }
    }
}
