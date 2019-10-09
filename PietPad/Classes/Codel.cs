using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PietPad.Classes
{
    public class Codel
    {
        public CodelColor Color { get; set; } = CodelColor.White;
        public (int x, int y) Position { get; set; }

        public Codel(int x, int y)
        {
            Position = (x, y);
        }
    }
}
