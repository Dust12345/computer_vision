﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Frame.VrAibo.NodeNavigator
{
    class MovementStep
    {
        public float Rotation { get; private set; }
        public float Movement { get; private set; }

        public MovementStep(float movement, float rotation)
        {
            Rotation = rotation;
            Movement = movement;
        }
    }
}