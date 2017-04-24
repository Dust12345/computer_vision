#region

using GLab.Rendering;
using GLab.Rendering.Primitives;
using Microsoft.Xna.Framework;

#endregion

namespace Frame.VrAibo
{
    /// <summary>
    ///   Stripe #2
    /// </summary>
    public class Stripe02 : Stripe
    {
        public Stripe02(bool addPlane = true)
            : base(-2, addPlane)
        {
        }

        public override void GenerateObstacles()
        {
            PlatonicSolidFactory psf = new PlatonicSolidFactory(XnaRenderer.Instance);
            SphereFactory sf = new SphereFactory(XnaRenderer.Instance);

            // Spheres
            GeometricPrimitive sphere1 = sf.CreateSphere(1.0f, 3,
                                                         Color.FromNonPremultiplied(new Vector4(0.0f, 0.8f, 0.6f, 1.0f)),
                                                         Matrix.Identity);
            sphere1.WorldMatrix *= Matrix.CreateTranslation(-4f, 1f, 3f);

            AddGeometricPrimitve(sphere1);

            GeometricPrimitive wallLeft = psf.CreateGeometricPrimitive(PlatonicSolid.Hexahedron,
                                                                       Color.FromNonPremultiplied(new Vector4(0.0f, 1.0f,
                                                                                                              1.0f, 1.0f)));
            wallLeft.WorldMatrix *= Matrix.CreateScale(8.0f, 2f, 0.4f);
            wallLeft.WorldMatrix *= Matrix.CreateTranslation(-10f, 1f, 0.0f);

            GeometricPrimitive wallRight = psf.CreateGeometricPrimitive(PlatonicSolid.Hexahedron,
                                                                        Color.FromNonPremultiplied(new Vector4(0.0f,
                                                                                                               1.0f,
                                                                                                               1.0f,
                                                                                                               1.0f)));
            wallRight.WorldMatrix *= Matrix.CreateScale(8.0f, 2f, 0.4f);
            wallRight.WorldMatrix *= Matrix.CreateTranslation(2.0f, 1f, 0.0f);

            GeometricPrimitive wallBack = psf.CreateGeometricPrimitive(PlatonicSolid.Hexahedron,
                                                                       Color.FromNonPremultiplied(new Vector4(0.0f, 0.8f,
                                                                                                              0.8f, 1.0f)));
            wallBack.WorldMatrix *= Matrix.CreateScale(12.0f, 2f, 0.4f);
            wallBack.WorldMatrix *= Matrix.CreateTranslation(-2.0f, 1f, -3.0f);

            AddGeometricPrimitve(wallLeft);
            AddGeometricPrimitve(wallRight);
            AddGeometricPrimitve(wallBack);
        }
    }
}