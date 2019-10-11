using PietPad.Classes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PietPad
{
    public static class Extensions
    {
        public static string GetName(this Operation operation)
        {
            switch (operation)
            {
                case Operation.nop:
                    return string.Empty;
                case Operation.in_char:
                    return "in(char)";
                case Operation.out_char:
                    return "out(char)";
                case Operation.in_number:
                    return "in(num)";
                case Operation.out_number:
                    return "out(num)";
                case Operation.@switch:
                    return "switch";
                default:
                    return operation.ToString();
            }
        }

        public static CodelColor GetCodelColor(this Color color)
        {
            var h = color.GetHue();
            var b = color.GetBrightness();
            var s = color.GetSaturation();
            ColorHue hue;
            ColorLightness lightness;
            if (h < 30)
            {
                if (s < 0.05) return b < 0.50 ? CodelColor.Black : CodelColor.White;
                hue = ColorHue.Red;
            }
            else if (h < 90) hue = ColorHue.Yellow;
            else if (h < 150) hue = ColorHue.Green;
            else if (h < 210) hue = ColorHue.Cyan;
            else if (h < 270) hue = ColorHue.Blue;
            else hue = ColorHue.Magenta;
            if (b < 0.4375) lightness = ColorLightness.Dark;
            else if (b < 0.5625) lightness = ColorLightness.Normal;
            else lightness = ColorLightness.Light;
            return CodelColor.GetColor(hue, lightness);
        }
    }
}
