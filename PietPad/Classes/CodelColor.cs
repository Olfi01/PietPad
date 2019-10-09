using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace PietPad.Classes
{
    public class CodelColor
    {
        public ColorHue? Hue { get; }
        public ColorLightness? Lightness { get; }
        public Brush Brush { get; }
        protected CodelColor(ColorHue? hue, ColorLightness? lightness, string colorcode)
        {
            Hue = hue;
            Lightness = lightness;
            Brush = (SolidColorBrush)new BrushConverter().ConvertFrom(colorcode);
        }

        public static readonly CodelColor LightRed = new CodelColor(ColorHue.Red, ColorLightness.Light, "#FFC0C0");
        public static readonly CodelColor Red = new CodelColor(ColorHue.Red, ColorLightness.Normal, "#FF0000");
        public static readonly CodelColor DarkRed = new CodelColor(ColorHue.Red, ColorLightness.Dark, "#C00000");
        public static readonly CodelColor LightYellow = new CodelColor(ColorHue.Yellow, ColorLightness.Light, "#FFFFC0");
        public static readonly CodelColor Yellow = new CodelColor(ColorHue.Yellow, ColorLightness.Normal, "#FFFF00");
        public static readonly CodelColor DarkYellow = new CodelColor(ColorHue.Yellow, ColorLightness.Dark, "#C0C000");
        public static readonly CodelColor LightGreen = new CodelColor(ColorHue.Green, ColorLightness.Light, "#C0FFC0");
        public static readonly CodelColor Green = new CodelColor(ColorHue.Green, ColorLightness.Normal, "#00FF00");
        public static readonly CodelColor DarkGreen = new CodelColor(ColorHue.Green, ColorLightness.Dark, "#00C000");
        public static readonly CodelColor LightCyan = new CodelColor(ColorHue.Cyan, ColorLightness.Light, "#C0FFFF");
        public static readonly CodelColor Cyan = new CodelColor(ColorHue.Cyan, ColorLightness.Normal, "#00FFFF");
        public static readonly CodelColor DarkCyan = new CodelColor(ColorHue.Cyan, ColorLightness.Dark, "#00C0C0");
        public static readonly CodelColor LightBlue = new CodelColor(ColorHue.Blue, ColorLightness.Light, "#C0C0FF");
        public static readonly CodelColor Blue = new CodelColor(ColorHue.Blue, ColorLightness.Normal, "#0000FF");
        public static readonly CodelColor DarkBlue = new CodelColor(ColorHue.Blue, ColorLightness.Dark, "#0000C0");
        public static readonly CodelColor LightMagenta = new CodelColor(ColorHue.Magenta, ColorLightness.Light, "#FFC0FF");
        public static readonly CodelColor Magenta = new CodelColor(ColorHue.Magenta, ColorLightness.Normal, "#FF00FF");
        public static readonly CodelColor DarkMagenta = new CodelColor(ColorHue.Magenta, ColorLightness.Dark, "#C000C0");

        public static readonly CodelColor White = new CodelColor(null, null, "#FFFFFF");
        public static readonly CodelColor Black = new CodelColor(null, null, "#000000");

        private static readonly Operation[,] ops = new Operation[6, 3] 
        {
            { Operation.nop, Operation.push, Operation.pop },
            { Operation.add, Operation.subtract, Operation.multiply },
            { Operation.divide, Operation.mod, Operation.not },
            { Operation.greater, Operation.pointer, Operation.@switch },
            { Operation.duplicate, Operation.roll, Operation.in_number },
            { Operation.in_char, Operation.out_number, Operation.out_char }
        };
        public static Operation CalculateOperation(CodelColor from, CodelColor to)
        {
            if (from == White || to == White) return Operation.nop;
            int hueChange = to.Hue.Value - from.Hue.Value;
            if (hueChange < 0) hueChange += 6;
            else if (hueChange > 5) hueChange -= 6;
            int lightnessChange = to.Lightness.Value - from.Lightness.Value;
            if (lightnessChange < 0) lightnessChange += 3;
            else if (lightnessChange > 2) lightnessChange -= 3;
            return ops[hueChange, lightnessChange];
        }
    }

    public enum ColorHue
    {
        Red,
        Yellow,
        Green,
        Cyan,
        Blue,
        Magenta
    }

    public enum ColorLightness
    {
        Light,
        Normal,
        Dark
    }
}
