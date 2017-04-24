#region

using System.Collections.Generic;
using GLab.Rendering;
using GLab.Rendering.Primitives;
using GLab.VirtualAibo;
using Microsoft.Xna.Framework;

#endregion

namespace Frame.VrAibo
{
    public abstract class Stripe : Parcours
    {
        public Stripe(int pos, bool addPlane)
            : base(0, pos, 0, addPlane, 200, 40)
        {
            InitStripe();
        }

        private void InitStripe()
        {
            GenerateSeparationLine();
            GenerateObstacles();
        }

        private void GenerateSeparationLine()
        {
            // Create separation line
            PathFactory pf = new PathFactory(XnaRenderer.Instance);
            List<WayPoint> waypoints = new List<WayPoint> { new WayPoint(new Vector2(-100, -20)), new WayPoint(new Vector2(100, -20)) };
            GeometricPrimitive path = pf.CreatePath(waypoints);
            path.Color = Color.Red;

            AddGeometricPrimitve(path);
        }

        public abstract void GenerateObstacles();
    }
}