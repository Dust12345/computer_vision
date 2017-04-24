#region

using GLab.Rendering;
using GLab.Rendering.Primitives;
using Microsoft.Xna.Framework;

#endregion

namespace Frame.VrAibo
{
    /// <summary>
    ///   Stripe #1
    /// </summary>
    public class Stripe01 : Stripe
    {
        public Stripe01(bool addPlane = true)
            : base(-1, addPlane)
        {
        }

        public override void GenerateObstacles()
        {
            PlatonicSolidFactory psf = new PlatonicSolidFactory(XnaRenderer.Instance);

            GeometricPrimitive pillarLeft = psf.CreateGeometricPrimitive(PlatonicSolid.Hexahedron, Color.Red);
            pillarLeft.WorldMatrix *= Matrix.CreateScale(8.0f, 2f, 0.4f);
            pillarLeft.WorldMatrix *= Matrix.CreateTranslation(-10f, 1f, 0f);

            GeometricPrimitive pillarRight = psf.CreateGeometricPrimitive(PlatonicSolid.Hexahedron, Color.Red);
            pillarRight.WorldMatrix *= Matrix.CreateScale(8.0f, 2f, 0.4f);
            pillarRight.WorldMatrix *= Matrix.CreateTranslation(2.0f, 1f, 0f);

            GeometricPrimitive upperBalk = psf.CreateGeometricPrimitive(PlatonicSolid.Hexahedron, Color.Red);
            upperBalk.WorldMatrix *= Matrix.CreateScale(20.0f, 1f, 0.4f);
            upperBalk.WorldMatrix *= Matrix.CreateTranslation(-4.0f, 2.5f, 0f);

            Matrix globalRotation = Matrix.CreateRotationY(MathHelper.ToRadians(-15));
            pillarLeft.WorldMatrix *= globalRotation;
            pillarRight.WorldMatrix *= globalRotation;
            upperBalk.WorldMatrix *= globalRotation;

            AddGeometricPrimitve(pillarLeft);
            AddGeometricPrimitve(pillarRight);
            AddGeometricPrimitve(upperBalk);
        }
    }
}