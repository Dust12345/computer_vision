using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GLab.StereoVision;

namespace Frame.VrAibo.MovementConsenter
{
    class MovementConsenter
    {
        private GLab.VirtualAibo.VrAibo _robot;

        public MovementConsenter(GLab.VirtualAibo.VrAibo robot)
        {
            _robot = robot;
        }

        public void RequestMovement(float amount)
        {
            _robot.Turn(amount);
        }
        
        public void RequestRotation(float amount)
        {
            _robot.Walk(amount);
        }   
    }
}
