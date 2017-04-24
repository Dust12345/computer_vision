#region

using GLab.Rendering;
using GLab.Rendering.Primitives;
using Microsoft.Xna.Framework;

#endregion

namespace Frame.VrAibo
{
    /// <summary>
    ///   Stripe #0
    /// </summary>
    public class Stripe00 : Stripe
    {
        public Stripe00(bool addPlane = true) : base(0, addPlane)
        {
        }

        public override void GenerateObstacles()
        {
            PlatonicSolidFactory psf = new PlatonicSolidFactory(XnaRenderer.Instance);
            SphereFactory sf = new SphereFactory(XnaRenderer.Instance);

            // Spheres
            GeometricPrimitive sphere1 = sf.CreateSphere(1.0f, 3,
                                                         Color.FromNonPremultiplied(new Vector4(1.0f, 0.0f, 0.2f, 1.0f)),
                                                         Matrix.Identity);
            sphere1.WorldMatrix *= Matrix.CreateTranslation(-17f, 1f, -1f);
            GeometricPrimitive sphere2 = sf.CreateSphere(1.0f, 3,
                                                         Color.FromNonPremultiplied(new Vector4(1.0f, 0.0f, 0.2f, 1.0f)),
                                                         Matrix.Identity);
            sphere2.WorldMatrix *= Matrix.CreateTranslation(9f, 1f, 1f);

            AddGeometricPrimitve(sphere1);
            AddGeometricPrimitve(sphere2);

            // Center wall
            GeometricPrimitive wall1 = psf.CreateGeometricPrimitive(PlatonicSolid.Hexahedron, Color.Purple);
            wall1.WorldMatrix *= Matrix.CreateScale(12f, 2f, 0.4f);
            wall1.WorldMatrix *= Matrix.CreateTranslation(0f, 1f, 0f);

            // Left wall
            GeometricPrimitive wall2 = psf.CreateGeometricPrimitive(PlatonicSolid.Hexahedron,
                                                                    Color.FromNonPremultiplied(new Vector4(0.6f, 0.2f,
                                                                                                           0.7f, 1.0f)));
            wall2.WorldMatrix *= Matrix.CreateScale(20f, 2f, 0.4f);
            wall2.WorldMatrix *= Matrix.CreateRotationY(MathHelper.ToRadians(35));
            wall2.WorldMatrix *= Matrix.CreateTranslation(-20f, 1.0f, 0.0f);

            // Right wall
            GeometricPrimitive wall3 = psf.CreateGeometricPrimitive(PlatonicSolid.Hexahedron,
                                                                    Color.FromNonPremultiplied(new Vector4(0.6f, 0.2f,
                                                                                                           0.7f, 1.0f)));
            wall3.WorldMatrix *= Matrix.CreateScale(20f, 2f, 0.4f);
            wall3.WorldMatrix *= Matrix.CreateRotationY(MathHelper.ToRadians(-35));
            wall3.WorldMatrix *= Matrix.CreateTranslation(20f, 1.0f, 0.0f);

            AddGeometricPrimitve(wall1);
            AddGeometricPrimitve(wall2);
            AddGeometricPrimitve(wall3);
        }
    }
}