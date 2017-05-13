#region

using System.Collections.Generic;
using GLab.Rendering;
using GLab.Rendering.Primitives;
using GLab.VirtualAibo;
using Microsoft.Xna.Framework;

#endregion

namespace Frame.VrAibo
{
    /// <summary>
    ///   Parcours #3
    /// </summary>
    /*
     * Coordinate System
     *      |         |
     *      |        50        
     * -----+---------+----->  x-axis
     *      |         |                    Allowed area:  left or above of shown rectangle                 
     *      |  actual | other Parcour
     *      | Parcours|                    Forbidden area: right or below of shown rectangle
     *      |         |                  
     * --50 +---------+
     *      | other Parcour                          
     *      v
     *   z-axis
     *   
     */
    public class Parcours03 : Parcours
    {
        private readonly GeometricPrimitive _cylinder, _cube;
        private readonly GeometricPrimitive _path;

        public Parcours03(int x, int y, float rotation = 0, bool addPlane = true, float width = 100f, float height = 100f)
            : base(x, y, rotation, addPlane, width, height)
        {
            // Create a path
            PathFactory pf = new PathFactory(XnaRenderer.Instance);
            List<WayPoint> waypoints = new List<WayPoint>
                                           {
                                               new WayPoint(new Vector2(0, 50)),
                                               new WayPoint(new Vector2(0, 0),4),
                                               new WayPoint(new Vector2(50, 0),2)
                                           };
            _path = pf.CreatePath(waypoints);
            _path.Color = Color.Gold;
            AddGeometricPrimitve(_path);
            // Add additional path
            List<WayPoint> waypoints2 = new List<WayPoint>
                                            {
                                                new WayPoint(new Vector2(10, -15),2), 
                                                new WayPoint(new Vector2(30, 15),0.25f)
                                            };
            _path = pf.CreatePath(waypoints2);
            _path.Color = Color.Gold;
            AddGeometricPrimitve(_path);

            // Add a basic cylinder
            CylinderFactory cf = new CylinderFactory(XnaRenderer.Instance);
            _cylinder = cf.CreateCylinder(20, Color.Gainsboro);
            _cylinder.WorldMatrix *= Matrix.CreateTranslation(0, 0.5f, 0);
            _cylinder.WorldMatrix *= Matrix.CreateScale(5, 5, 5);
            _cylinder.WorldMatrix *= Matrix.CreateTranslation(10, 0, 10);
            AddGeometricPrimitve(_cylinder);
            // Add an obstacle
            PlatonicSolidFactory psf2 = new PlatonicSolidFactory(XnaRenderer.Instance);
            _cube = psf2.CreateGeometricPrimitive(PlatonicSolid.Hexahedron, Color.Bisque);
            _cube.WorldMatrix *= Matrix.CreateTranslation(0, 0.5f, 0);
            _cube.WorldMatrix *= Matrix.CreateScale(6, 4, 1);
            _cube.WorldMatrix *= Matrix.CreateRotationY(-3.14f / 4.0f);
            _cube.WorldMatrix *= Matrix.CreateTranslation(-2.5f, 0, 17);
            AddGeometricPrimitve(_cube);
            _cube = psf2.CreateGeometricPrimitive(PlatonicSolid.Hexahedron, Color.Bisque);
            _cube.WorldMatrix *= Matrix.CreateTranslation(0, 0.5f, 0);
            _cube.WorldMatrix *= Matrix.CreateScale(6, 4, 1);
            _cube.WorldMatrix *= Matrix.CreateRotationY(3.14f / 4.0f);
            _cube.WorldMatrix *= Matrix.CreateTranslation(2.5f, 0, 17);
            AddGeometricPrimitve(_cube);

        }
    }
}