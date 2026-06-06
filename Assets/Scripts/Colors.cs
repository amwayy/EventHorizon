using UnityEngine;

namespace DefaultNamespace
{
    public enum ColorType
    {
        Red,
        Green,
        Blue,
        Magenta,
        Violet,   // blue masked with 0.2 opacity red
        White,
    }
    
    public static class Colors
    {
        public static Color GetJigsawColor(ColorType colorType)
        {
            return colorType switch
            {
                ColorType.Red => new Color(236 / 255f, 137 / 255f, 137 / 255f),
                ColorType.Green => new Color(137 / 255f, 236 / 255f, 137 / 255f),
                ColorType.Blue => new Color(137 / 255f, 137 / 255f, 236 / 255f),
                ColorType.Magenta => new Color(236 / 255f, 137 / 255f, 236 / 255f),
                ColorType.Violet => new Color(178 / 255f, 112 / 255f, 194 / 255f),
                _ => Color.white,
            };
        }
        
        public static Color GetBarrierColor(ColorType colorType)
        {
            return colorType switch
            {
                ColorType.Red => new Color(230 / 255f, 0 / 255f, 0 / 255f),
                ColorType.Green => new Color(0 / 255f, 230 / 255f, 0 / 255f),
                ColorType.Blue => new Color(0 / 255f, 0 / 255f, 230 / 255f),
                ColorType.Magenta => new Color(230 / 255f, 0 / 255f, 230 / 255f),
                ColorType.Violet => new Color(158 / 255f, 0 / 255f, 175 / 255f),
                _ => Color.white,
            };
        }
    }
}