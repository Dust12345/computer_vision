#region

using GLab.Rendering;
using GLab.Rendering.Primitives;
using Microsoft.Xna.Framework;

#endregion

namespace Frame.VrAibo
{
    /// <summary>
    ///   Final stripe containing the treasure.
    /// </summary>
    public class StripeTreasure : Stripe
    {
        public StripeTreasure(bool addPlane = true)
            : base(-4, addPlane)
        {
            GenerateTreasure();
        }

        private void GenerateTreasure()
        {
            // Create treasure
            ConeFactory coneFactory = new ConeFactory(XnaRenderer.Instance);
            GeometricPrimitive treasure = coneFactory.CreateCone(16);
            treasure.Color = Color.Gold;
            AddGeometricPrimitve(treasure);
        }

        public override void GenerateObstacles()
        {
            // No more obstacles!
        }
    }
}