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
            _path.Color = Color.Red;
            AddGeometricPrimitve(_path);
            // Add additional path
            List<WayPoint> waypoints2 = new List<WayPoint>
                                            {
                                                new WayPoint(new Vector2(15, -10)),
                                                new WayPoint(new Vector2(15, 0))
                                            };
            _path = pf.CreatePath(waypoints2);
            _path.Color = Color.Red;
            AddGeometricPrimitve(_path);
            // Add additional path
            List<WayPoint> waypoints3 = new List<WayPoint>
                                            {
                                                new WayPoint(new Vector2(30, 0),3),
                                                new WayPoint(new Vector2(30, 10))
                                            };
            _path = pf.CreatePath(waypoints3);
            _path.Color = Color.Red;
            AddGeometricPrimitve(_path);

            // Add some randomly placed and scaled "trees"
            PlatonicSolidFactory psf = new PlatonicSolidFactory(XnaRenderer.Instance);
        

            GeometricPrimitive t = psf.CreateGeometricPrimitive(PlatonicSolid.Hexahedron);
            t.Color = Color.DarkGreen;
            t.WorldMatrix *= Matrix.CreateTranslation(0.0f, 0.25f, 0.0f);
            float w = 6;
            float h = 7;
            t.WorldMatrix *= Matrix.CreateScale(new Vector3(w, h, w));
            t.WorldMatrix *=
            Matrix.CreateTranslation(new Vector3(-8.0f,0f, -5.0f));
            //AddGeometricPrimitve(t);


            GeometricPrimitive t1 = psf.CreateGeometricPrimitive(PlatonicSolid.Hexahedron);
            t1.Color = Color.DarkGreen;
            t1.WorldMatrix *= Matrix.CreateTranslation(0.0f, 0.25f, 0.0f);          
            t1.WorldMatrix *= Matrix.CreateScale(new Vector3(w, h, w));
            t1.WorldMatrix *=
            Matrix.CreateTranslation(new Vector3(30.0f, 0f, 3.0f));
            AddGeometricPrimitve(t1);

            GeometricPrimitive t2 = psf.CreateGeometricPrimitive(PlatonicSolid.Tetrahedron);
            t2.Color = Color.DarkGreen;
            t2.WorldMatrix *= Matrix.CreateTranslation(0.0f, 0.25f, 0.0f);
            t2.WorldMatrix *= Matrix.CreateScale(new Vector3(w, h, w));
            t2.WorldMatrix *=
            Matrix.CreateTranslation(new Vector3(32.0f, 0f, 8.0f));
            AddGeometricPrimitve(t2);

            // Add an obstacle
            PlatonicSolidFactory psf2 = new PlatonicSolidFactory(XnaRenderer.Instance);
            _cube = psf2.CreateGeometricPrimitive(PlatonicSolid.Hexahedron, Color.Bisque);
            _cube.WorldMatrix *= Matrix.CreateTranslation(0, 0.5f, 0);
            _cube.WorldMatrix *= Matrix.CreateScale(4, 4, 1);
            _cube.WorldMatrix *= Matrix.CreateTranslation(3, 0, 20);
            AddGeometricPrimitve(_cube);
            _cube = psf2.CreateGeometricPrimitive(PlatonicSolid.Hexahedron, Color.Bisque);
            _cube.WorldMatrix *= Matrix.CreateTranslation(0, 0.5f, 0);
            _cube.WorldMatrix *= Matrix.CreateScale(4, 4, 1);
//            _cube.WorldMatrix *= Matrix.CreateRotationY(3.14f / 2.0f);
            _cube.WorldMatrix *= Matrix.CreateTranslation(-3, 0, 20);
            AddGeometricPrimitve(_cube);

            SphereFactory sf = new SphereFactory(XnaRenderer.Instance);

            GeometricPrimitive tetrahedron = sf.CreateSphere(2.0f, 3);
            tetrahedron.Color = Color.Red;
            tetrahedron.WorldMatrix *= Matrix.CreateTranslation(0, 0.5f, 0);
            tetrahedron.WorldMatrix *= Matrix.CreateTranslation(0f, 1, 35);
            AddGeometricPrimitve(tetrahedron);

        }
    }
}