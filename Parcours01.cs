#region

using System.Collections.Generic;
using GLab.Rendering;
using GLab.Rendering.Primitives;
using GLab.VirtualAibo;
using Microsoft.Xna.Framework;

#endregion

using System;

namespace Frame.VrAibo
{
    /// <summary>
    ///   Parcours #1
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
    public class Parcours01 : Parcours
    {
        private readonly GeometricPrimitive _cube;
        private readonly GeometricPrimitive _path;

        public Parcours01(int x, int y, float rotation = 0, bool addPlane = true, float width = 100f, float height = 100f)
            : base(x, y, rotation, addPlane, width, height)
        {
            // Create a path
            PathFactory pf = new PathFactory(XnaRenderer.Instance);
            List<WayPoint> waypoints = new List<WayPoint>
                                           {
                                               new WayPoint(new Vector2(0, 50)),
                                               new WayPoint(new Vector2(0, 0),2),
                                               new WayPoint(new Vector2(50, 0),4)
                                           };
            _path = pf.CreatePath(waypoints);
            _path.Color = Color.GreenYellow;
            AddGeometricPrimitve(_path);
            // Add additional path
            List<WayPoint> waypoints2 = new List<WayPoint>
                                            {
                                                new WayPoint(new Vector2(20, -10),2),
                                                new WayPoint(new Vector2(20, 10),3)
                                            };
            _path = pf.CreatePath(waypoints2);
            _path.Color = Color.GreenYellow;
            AddGeometricPrimitve(_path);

            // Add a basic cube
            PlatonicSolidFactory psf = new PlatonicSolidFactory(XnaRenderer.Instance);
            _cube = psf.CreateGeometricPrimitive(PlatonicSolid.Hexahedron, Color.Firebrick);
            _cube.WorldMatrix *= Matrix.CreateTranslation(0, 0.5f, 0);
            _cube.WorldMatrix *= Matrix.CreateScale(4, 4, 4);
            _cube.WorldMatrix *= Matrix.CreateTranslation(10, 0, -10);
            AddGeometricPrimitve(_cube);
            // Add an obstacle
            PlatonicSolidFactory psf2 = new PlatonicSolidFactory(XnaRenderer.Instance);
            _cube = psf2.CreateGeometricPrimitive(PlatonicSolid.Hexahedron, Color.Bisque);
            _cube.WorldMatrix *= Matrix.CreateTranslation(0, 0.5f, 0);
            _cube.WorldMatrix *= Matrix.CreateScale(6, 4, 1);
            _cube.WorldMatrix *= Matrix.CreateRotationY(3.14f/4.0f);
            _cube.WorldMatrix *= Matrix.CreateTranslation(-2.5f, 0, 17);
            AddGeometricPrimitve(_cube);
            _cube = psf2.CreateGeometricPrimitive(PlatonicSolid.Hexahedron, Color.Bisque);
            _cube.WorldMatrix *= Matrix.CreateTranslation(0, 0.5f, 0);
            _cube.WorldMatrix *= Matrix.CreateScale(6, 4, 1);
            _cube.WorldMatrix *= Matrix.CreateRotationY(-3.14f / 4.0f);
            _cube.WorldMatrix *= Matrix.CreateTranslation(2.5f, 0, 17);
            AddGeometricPrimitve(_cube);
        }
    }
}