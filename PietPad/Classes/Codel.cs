using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PietPad.Classes
{
    public class Codel
    {
        public static readonly CodelColor DefaultColor = CodelColor.White;
        public CodelColor Color { get; set; } = DefaultColor;
        public (int x, int y) Position { get; set; }

        public Codel(int x, int y)
        {
            Position = (x, y);
        }
    }
}
