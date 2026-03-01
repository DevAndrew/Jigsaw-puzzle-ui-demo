using UnityEngine;

namespace JigsawPrototype.Features.Puzzle.Preview
{
    public static class PreviewPlaceholderTexture
    {
        private static Texture2D s_texture;

        public static Texture2D GetOrCreate()
        {
            if (s_texture != null)
            {
                return s_texture;
            }

            s_texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            s_texture.SetPixels(new[]
            {
                new Color(0.2f, 0.2f, 0.2f, 1f),
                new Color(0.25f, 0.25f, 0.25f, 1f),
                new Color(0.25f, 0.25f, 0.25f, 1f),
                new Color(0.2f, 0.2f, 0.2f, 1f),
            });
            s_texture.Apply();
            return s_texture;
        }
    }
}
