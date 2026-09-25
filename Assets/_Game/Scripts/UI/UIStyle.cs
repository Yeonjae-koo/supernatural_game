using UnityEngine;
namespace BeforeJericho.UI
{
    /// <summary>Colour tokens from the HTML prototype (:root variables).</summary>
    public static class UIStyle
    {
        public static readonly Color Ink = Hex(0x0b0c0d), Char = Hex(0x151719), Line = Hex(0x2c3033);
        public static readonly Color Bone = Hex(0xd9d8d2), Ash = Hex(0x8e918e), Dim = Hex(0x595d5d), Body = Hex(0xb7b9b5);
        public static readonly Color Accent = Hex(0xb8433a), Cold = Hex(0x8aa3a6), Ember = Hex(0xc8733a), Blood = Hex(0x7d241e);
        public static Color Hex(int rgb, float a = 1) => new(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, a);
        public static Color Alpha(Color c, float a) { c.a = a; return c; }
    }
}
