#region

using GLab.Rendering;
using GLab.Rendering.Primitives;
using Microsoft.Xna.Framework;

#endregion

namespace Frame.VrAibo
{
    /// <summary>
    ///   Stripe #3
    /// </summary>
    public class Stripe03 : Stripe
    {
        public Stripe03(bool addPlane = true)
            : base(-3, addPlane)
        {
        }

        public override void GenerateObstacles()
        {
            Matrix rotation = Matrix.CreateRotationY(MathHelper.ToRadians(10));
            Matrix translation = Matrix.CreateTranslation(-30, 1.0f, 0.0f);
            Matrix scale = Matrix.CreateScale(1.0f, 2.0f, 1.0f);

            PlatonicSolidFactory psf = new PlatonicSolidFactory(XnaRenderer.Instance);

            for (int i = 0; i < 36; i++)
            {
                // Mind the gap
                if (i == 22)
                    continue;

                GeometricPrimitive barrier = psf.CreateGeometricPrimitive(PlatonicSolid.Hexahedron, Color.Red);
                barrier.WorldMatrix *= scale;
                barrier.WorldMatrix *= Matrix.CreateTranslation(i*2, 0f, 0.0f)*translation;
                barrier.WorldMatrix *= rotation;

                AddGeometricPrimitve(barrier);
            }
        }
    }
}