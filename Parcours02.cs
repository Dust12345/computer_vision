#region

using System;
using System.Collections.Generic;
using GLab.Rendering;
using GLab.Rendering.Primitives;
using GLab.VirtualAibo;
using Microsoft.Xna.Framework;

#endregion

namespace Frame.VrAibo
{
    /// <summary>
    ///   Parcours #2
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
    public class Parcours02 : Parcours
    {
        private readonly GeometricPrimitive _path, _cube;

        public Parcours02(int x, int y, float rotation = 0, bool addPlane = true, float width = 100f, float height = 100f)
            : base(x, y, rotation, addPlane, width, height)
        {
            // Create a path
            PathFactory pf = new PathFactory(XnaRenderer.Instance);
            List<WayPoint> waypoints = new List<WayPoint>
                                           {
                                               new WayPoint(new Vector2(0, 50)),
                                               new WayPoint(new Vector2(0, 10),2),
                                               new WayPoint(new Vector2(3, 3),2),
                                               new WayPoint(new Vector2(10, 0)),
                                               new WayPoint(new Vector2(50, 0))
                                           };
            _path = pf.CreatePath(waypoints);
            _path.Color = Color.Blue;
            AddGeometricPrimitve(_path);
            // Add additional path
            List<WayPoint> waypoints2 = new List<WayPoint>
                                            {
                                                new WayPoint(new Vector2(15, -10)),
                                                new WayPoint(new Vector2(15, 0))
                                            };
            _path = pf.CreatePath(waypoints2);
            _path.Color = Color.Blue;
            AddGeometricPrimitve(_path);
            // Add additional path
            List<WayPoint> waypoints3 = new List<WayPoint>
                                            {
                                                new WayPoint(new Vector2(30, 0),3),
                                                new WayPoint(new Vector2(30, 10))
                                            };
            _path = pf.CreatePath(waypoints3);
            _path.Color = Color.Blue;
            AddGeometricPrimitve(_path);

            // Add some randomly placed and scaled "trees"
            PlatonicSolidFactory psf = new PlatonicSolidFactory(XnaRenderer.Instance);

            // Use a fixed seed value for the placement
            Random rnd = new Random(42*2);
            for (int i = 0; i < 8; i++)
            {
                GeometricPrimitive tetrahedron = psf.CreateGeometricPrimitive(PlatonicSolid.Tetrahedron);
                tetrahedron.Color = Color.DarkGreen;
                tetrahedron.WorldMatrix *= Matrix.CreateTranslation(0.0f, 0.25f, 0.0f);

                float tetra_width = rnd.Next(4, 8);
                float tetra_height = rnd.Next(6, 16);

                tetrahedron.WorldMatrix *= Matrix.CreateScale(new Vector3(tetra_width, tetra_height, tetra_width));
                tetrahedron.WorldMatrix *=
                Matrix.CreateTranslation(new Vector3(rnd.Next(-40, 40), 0f, rnd.Next(-40, 40)));

                //AddGeometricPrimitve(tetrahedron);
            }
            // Add an obstacle
            PlatonicSolidFactory psf2 = new PlatonicSolidFactory(XnaRenderer.Instance);
            _cube = psf2.CreateGeometricPrimitive(PlatonicSolid.Hexahedron, Color.Bisque);
            _cube.WorldMatrix *= Matrix.CreateTranslation(0, 0.5f, 0);
            _cube.WorldMatrix *= Matrix.CreateScale(8, 4, 1);
            _cube.WorldMatrix *= Matrix.CreateTranslation(0, 0, 15);
            //AddGeometricPrimitve(_cube);
            _cube = psf2.CreateGeometricPrimitive(PlatonicSolid.Hexahedron, Color.Bisque);
            _cube.WorldMatrix *= Matrix.CreateTranslation(0, 0.5f, 0);
            _cube.WorldMatrix *= Matrix.CreateScale(8, 4, 1);
            _cube.WorldMatrix *= Matrix.CreateRotationY(3.14f / 2.0f);
            _cube.WorldMatrix *= Matrix.CreateTranslation(-3.5f, 0, 19);
            //AddGeometricPrimitve(_cube);
            _cube = psf2.CreateGeometricPrimitive(PlatonicSolid.Hexahedron, Color.Bisque);
            _cube.WorldMatrix *= Matrix.CreateTranslation(0, 0.5f, 0);
            _cube.WorldMatrix *= Matrix.CreateScale(8, 4, 1);
            _cube.WorldMatrix *= Matrix.CreateRotationY(3.14f / 2.0f);
            _cube.WorldMatrix *= Matrix.CreateTranslation(3.5f, 0, 19);
            //AddGeometricPrimitve(_cube);
        }
    }
}