using UnityEngine;

namespace Queenless
{
    public static class SpriteFactory
    {
        public static Sprite Rounded;
        public static Sprite Disc;
        public static Sprite Soft;
        public static Sprite Ring;
        public static Sprite Chevron;
        public static Sprite Crack;
        public static Sprite White;
        public static Texture2D Vignette;

        public static void Ensure()
        {
            if (Rounded != null)
                return;
            Rounded = MakeRounded(64, 12);
            Disc = MakeDisc(64, 0.46f, 0.02f);
            Soft = MakeDisc(64, 0.5f, 0.22f);
            Ring = MakeRing(64, 0.34f, 0.46f);
            Chevron = MakeChevron(64);
            Crack = MakeCrack(64);
            White = MakeWhite();
            Vignette = MakeVignette(256);
        }

        static Sprite MakeRounded(int size, int radius)
        {
            var pixels = new Color[size * size];
            float inset = 3f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = 0f;
                    float dy = 0f;
                    if (x < inset + radius) dx = (inset + radius) - x;
                    else if (x > size - 1 - inset - radius) dx = x - (size - 1 - inset - radius);
                    if (y < inset + radius) dy = (inset + radius) - y;
                    else if (y > size - 1 - inset - radius) dy = y - (size - 1 - inset - radius);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                    if (x < inset || y < inset || x >= size - inset || y >= size - inset)
                        alpha = 0f;
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            return Bake(pixels, size, 64f);
        }

        static Sprite MakeDisc(int size, float radius, float softness)
        {
            var pixels = new Color[size * size];
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / size;
                    float alpha = Mathf.Clamp01((radius - d) / Mathf.Max(0.001f, softness));
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            return Bake(pixels, size, 64f);
        }

        static Sprite MakeRing(int size, float inner, float outer)
        {
            var pixels = new Color[size * size];
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / size;
                    float outerA = Mathf.Clamp01((outer - d) / 0.03f);
                    float innerA = Mathf.Clamp01((d - inner) / 0.03f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, outerA * innerA);
                }
            }
            return Bake(pixels, size, 64f);
        }

        static Sprite MakeChevron(int size)
        {
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x / (float)(size - 1)) * 2f - 1f;
                    float ny = y / (float)(size - 1);
                    float edge = Mathf.Lerp(0.95f, 0.05f, ny);
                    float alpha = 0f;
                    if (Mathf.Abs(nx) < edge && ny > 0.15f && ny < 0.85f)
                    {
                        float band = Mathf.Abs(Mathf.Abs(nx) - edge * 0.55f);
                        alpha = Mathf.Clamp01(0.18f - band) / 0.18f;
                    }
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            return Bake(pixels, size, 64f);
        }

        static Sprite MakeCrack(int size)
        {
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color(0f, 0f, 0f, 0f);
            DrawCrack(pixels, size, 8, 30, 56, 18);
            DrawCrack(pixels, size, 18, 40, 48, 46);
            DrawCrack(pixels, size, 6, 50, 58, 34);
            return Bake(pixels, size, 64f);
        }

        static void DrawCrack(Color[] pixels, int size, int x0, int y0, int x1, int y1)
        {
            int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
            for (int i = 0; i <= steps; i++)
            {
                float t = steps == 0 ? 0f : i / (float)steps;
                int x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t) + Mathf.Sin(t * 9f) * 2f);
                int y = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t));
                Stamp(pixels, size, x, y, 0.85f);
                Stamp(pixels, size, x, y + 1, 0.45f);
            }
        }

        static void Stamp(Color[] pixels, int size, int x, int y, float alpha)
        {
            if (x < 0 || y < 0 || x >= size || y >= size)
                return;
            pixels[y * size + x] = new Color(0.08f, 0.05f, 0.03f, alpha);
        }

        static Sprite MakeWhite()
        {
            var pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;
            return Bake(pixels, 4, 4f);
        }

        static Texture2D MakeVignette(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            var pixels = new Color[size * size];
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x - c) / c;
                    float ny = (y - c) / c;
                    float d = Mathf.Max(Mathf.Abs(nx), Mathf.Abs(ny));
                    float round = Mathf.Sqrt(nx * nx + ny * ny);
                    float edge = Mathf.Clamp01((Mathf.Max(d, round * 0.72f) - 0.45f) / 0.7f);
                    pixels[y * size + x] = new Color(0f, 0f, 0f, edge * 0.72f);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        static Sprite Bake(Color[] pixels, int size, float ppu)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), ppu);
        }
    }
}
