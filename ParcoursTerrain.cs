#region

using System;
using System.Drawing.Imaging;
using Emgu.CV;
using Emgu.CV.Structure;
using GLab.FractalTerrain.Modules.Generators;
using GLab.FractalTerrain.Modules.Modifiers;
using GLab.FractalTerrain.Structures;
using GLab.FractalTerrain.Utilities;
using GLab.VirtualAibo;
using Microsoft.Xna.Framework;
using MathHelper = GLab.FractalTerrain.Utilities.MathHelper;

#endregion

namespace Frame.VrAibo
{
    /// <summary>
    ///   Terrain parcours
    /// </summary>
    public class ParcoursTerrain : Parcours
    {
        private readonly Image<Gray, byte> _heightmap;
        private readonly TerrainMesh _terrainMesh;

        public ParcoursTerrain()
            : base(0, 0, 0, false, 0, 0)
        {
            _heightmap = new Image<Gray, byte>(256, 256);
            GenerateHeightmap(_heightmap);

            Volume heightmapVolume = VolumeConverter.ConvertRasterImageToVolume(_heightmap);
            _terrainMesh = new TerrainMesh
            {
                HeightmapVolume = heightmapVolume,
                CellScale = 4,
                HeightScale = 256,
                EnableTexturing = true
            };
            _terrainMesh.Generate();

            Renderables.Add(_terrainMesh);
        }

        /// <summary>
        ///   Draws the heightmap to the given raster image.
        /// </summary>
        /// <param name = "image"></param>
        private void GenerateHeightmap(Image<Gray, byte> image)
        {
            Amplification amp = new Amplification { Intensity = 0.5 };
            amp[0] = new RidgedMultifractalNoise { Lacunarity = 2.127821, H = 0.7321, Octaves = 9, Offset = 0.8, Frequency = 0.005 };

            Vector2 center = new Vector2(_heightmap.Width / 2f, _heightmap.Height / 2f);

            for (int x = 0; x < _heightmap.Width; x++)
            {
                for (int y = 0; y < _heightmap.Height; y++)
                {
                    Vector2 position = new Vector2(x, y);
                    float length = (center - position).Length();

                    if (length > 64)
                    {
                        double density = amp.Density(x, y, 0);

                        if (length < 80)
                        {
                            density *= (length - 64.0) / (80.0 - 64.0);
                        }

                        byte heightValue = (byte)MathHelper.Clamp(Math.Abs(density) * 255.0, 0.0, 255.0);
                        image[y, x] = new Gray(heightValue);
                    }
                }
            }
        }
      
    }
}