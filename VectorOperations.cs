using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Frame.VrAibo
{
    public static class VctOp
    {

        
        public static double DegToRad = Math.PI / 180;

        public static Vector2 calcMovementVector(double angle, Vector2 vector)
        {
            double rad = DegToRad * angle;

            Vector2 rotatedVector = new Vector2(0, 0);
            double x = vector.X * Math.Cos(rad) - vector.Y * Math.Sin(rad);
            double y = vector.X * Math.Sin(rad) + vector.Y * Math.Cos(rad);
            rotatedVector.X = (float)x;
            rotatedVector.Y = (float)y;
            return rotatedVector;
        }

        public static double calcDistance(Vector2 v1, Vector2 v2)
        {
            return Math.Sqrt(Math.Pow((v1.X - v2.X), 2) + Math.Pow((v1.Y - v2.Y), 2));
        }

        public static Vector2 getVectorTotarget(Vector2 pos, Vector2 target)
        {
            Vector2 vct = new Vector2(target.X - pos.X, target.Y - pos.Y);
            return vct;
        }

        public static double calcAngleBeteenVectors(Vector2 vec1, Vector2 vec2)
        {
            //calc dot product
            //float dot = vec1.x * vec2.y + vec2.x* vec1.y;
            //float absDot = (vec1.x * vec2.y) + std::abs(vec2.x* vec1.y);

            //a = atan2d(x1*y2-y1*x2,x1*x2+y1*y2); https://de.mathworks.com/matlabcentral/answers/180131-how-can-i-find-the-angle-between-two-vectors-including-directional-information?requestedDomain=www.mathworks.com
            double rad = Math.Atan2(vec1.X * vec2.Y - vec1.Y * vec2.X, vec1.X * vec2.X + vec1.Y * vec2.Y);
            double angle = rad * (180 / 3.14);
            return angle;

        }

        public static bool isClockwise(Vector2 v1, Vector2 v2)
        {
            if (v1.Y * v2.X > v1.X * v2.Y)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

    }
}
