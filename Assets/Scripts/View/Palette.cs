using UnityEngine;

namespace Queenless
{
    public static class Palette
    {
        public static readonly Color Background = Hex(0x140e0a);
        public static readonly Color Floor = Hex(0x2a1c12);
        public static readonly Color FloorAlt = Hex(0x342215);
        public static readonly Color Wall = Hex(0x3d2918);
        public static readonly Color WallHighlight = Hex(0x8d5a32);
        public static readonly Color Body = Hex(0xb6844f);
        public static readonly Color Honey = Hex(0xffc857);
        public static readonly Color Core = Hex(0xfff3c4);
        public static readonly Color Silence = Hex(0x6b4c9a);
        public static readonly Color Gold = Hex(0xe2a31b);
        public static readonly Color Teal = Hex(0x2bbbad);
        public static readonly Color Exit = Hex(0xfff6d6);
        public static readonly Color Pad = Hex(0xf2d2ff);
        public static readonly Color Mite = Hex(0xd64545);
        public static readonly Color Ink = Hex(0x1a120c);
        public static readonly Color Wax = Hex(0x3d2918);

        public static Color Hex(int rgb)
        {
            return new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);
        }

        public static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
