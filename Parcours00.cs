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
    ///   Parcours #0
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
    public class Parcours00 : Parcours
    {
        private readonly GeometricPrimitive _path, _cube;

        public Parcours00(int x, int y, float rotation = 0, bool addPlane = true, float width = 100f, float height = 100f)
            : base(x, y, rotation, addPlane, width, height)
        {
            // Create main path (with a gap!)
            PathFactory pf = new PathFactory(XnaRenderer.Instance);
            List<WayPoint> waypoints = new List<WayPoint>
                                           {
                                               new WayPoint(new Vector2(0, 50)),
                                               new WayPoint(new Vector2(0, 0),2),
                                               new WayPoint(new Vector2(25, 0),4)
                                           };
            _path = pf.CreatePath(waypoints);
            _path.Color = Color.Red;
            AddGeometricPrimitve(_path);
            List<WayPoint> waypoints2 = new List<WayPoint> { new WayPoint(new Vector2(35, 0)), new WayPoint(new Vector2(50, 0)) };
            _path = pf.CreatePath(waypoints2);
            _path.Color = Color.Red;
            AddGeometricPrimitve(_path);

            // Add additional path
            List<WayPoint> waypoints3 = new List<WayPoint>
                                            {
                                                new WayPoint(new Vector2(0, 0)),
                                                new WayPoint(new Vector2(0, -30),3),
                                                new WayPoint(new Vector2(10, -30),1.5f),
                                                new WayPoint(new Vector2(10, -20),2),
                                                new WayPoint(new Vector2(0, -20))
                                            };
            _path = pf.CreatePath(waypoints3);
            _path.Color = Color.Red;
            AddGeometricPrimitve(_path);

            // Add some spheres
            SphereFactory psf = new SphereFactory(XnaRenderer.Instance);
            PlatonicSolidFactory psfBaum = new PlatonicSolidFactory(XnaRenderer.Instance);

            // Use a fixed seed value for the placement
            Random rnd = new Random(42);

            for (int i = 0; i < 8; i++)
            {
                GeometricPrimitive tetrahedron = psf.CreateSphere(2.0f, 3);
                tetrahedron.Color = Color.Red;
                tetrahedron.WorldMatrix *= Matrix.CreateTranslation(0.0f, 2.0f, 0.0f);
                tetrahedron.WorldMatrix *=
                Matrix.CreateTranslation(new Vector3(rnd.Next(-40, 40), 0f, rnd.Next(-40, 40)));
                AddGeometricPrimitve(tetrahedron);
            }
            GeometricPrimitive sphere1 = psf.CreateSphere(4.0f, 3);
            sphere1.Color = Color.Orange;
            sphere1.WorldMatrix *= Matrix.CreateTranslation(-4.0f, 4.0f, -4.0f);
            AddGeometricPrimitve(sphere1);

            GeometricPrimitive baum1 = psfBaum.CreateGeometricPrimitive(PlatonicSolid.Tetrahedron);
            baum1.Color = Color.DarkGreen;
            baum1.WorldMatrix *= Matrix.CreateScale(new Vector3(5, 7, 5));
            baum1.WorldMatrix *= Matrix.CreateTranslation(30.0f, 0.25f, -5.0f);
            AddGeometricPrimitve(baum1);

            // Add an obstacle
            PlatonicSolidFactory psf2 = new PlatonicSolidFactory(XnaRenderer.Instance);
            _cube = psf2.CreateGeometricPrimitive(PlatonicSolid.Hexahedron, Color.Bisque);
            _cube.WorldMatrix *= Matrix.CreateTranslation(0, 0.5f, 0);
            _cube.WorldMatrix *= Matrix.CreateScale(8, 4, 1);
            _cube.WorldMatrix *= Matrix.CreateTranslation(0, 0, 17);
            AddGeometricPrimitve(_cube);

            PlatonicSolidFactory psf3 = new PlatonicSolidFactory(XnaRenderer.Instance);
            _cube = psf3.CreateGeometricPrimitive(PlatonicSolid.Hexahedron, Color.Bisque);
            _cube.WorldMatrix *= Matrix.CreateTranslation(0, 0.5f, 0);
            _cube.WorldMatrix *= Matrix.CreateScale(40, 4, 1);
            _cube.WorldMatrix *= Matrix.CreateTranslation(25, 0, 17);
            AddGeometricPrimitve(_cube);

            PlatonicSolidFactory psf5 = new PlatonicSolidFactory(XnaRenderer.Instance);
            _cube = psf5.CreateGeometricPrimitive(PlatonicSolid.Hexahedron, Color.Bisque);
            _cube.WorldMatrix *= Matrix.CreateTranslation(0, 0.5f, 0);
            _cube.WorldMatrix *= Matrix.CreateScale(20, 4, 1);
            _cube.WorldMatrix *= Matrix.CreateRotationY(80.0f);
            _cube.WorldMatrix *= Matrix.CreateTranslation(40, 0, 14);


            AddGeometricPrimitve(_cube);

            PlatonicSolidFactory psf4 = new PlatonicSolidFactory(XnaRenderer.Instance);
            _cube = psf4.CreateGeometricPrimitive(PlatonicSolid.Hexahedron, Color.Bisque);
            _cube.WorldMatrix *= Matrix.CreateTranslation(0, 0.5f, 0);
            _cube.WorldMatrix *= Matrix.CreateScale(8, 4, 1);
            _cube.WorldMatrix *= Matrix.CreateTranslation(-5, 0, 17);
            AddGeometricPrimitve(_cube);

        }
    }
}