using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PietPad.Classes.Interpreter;

namespace PietPad.Classes
{
    public class ColorBlock
    {
        public List<Codel> Codels { get; } = new List<Codel>();
        public int Size => Codels.Count;

        public Codel FindFurthestCodel(DirectionPointer dp, CodelChooser cc)
        {
            int xFactor = 0, yFactor = 0;
            switch (dp)
            {
                case DirectionPointer.Right:
                    xFactor = 1;
                    break;
                case DirectionPointer.Down:
                    yFactor = 1;
                    break;
                case DirectionPointer.Left:
                    xFactor = -1;
                    break;
                case DirectionPointer.Up:
                    yFactor = -1;
                    break;
            }

            var edge = Codels.GroupBy(x => x.Position.x * xFactor + x.Position.y * yFactor).OrderBy(x => x.Key).Last();

            xFactor = 0; yFactor = 0;
            switch (dp)
            {
                case DirectionPointer.Right:
                    switch (cc)
                    {
                        case CodelChooser.Left:
                            yFactor = -1;
                            break;
                        case CodelChooser.Right:
                            yFactor = 1;
                            break;
                    }
                    break;
                case DirectionPointer.Down:
                    switch (cc)
                    {
                        case CodelChooser.Left:
                            xFactor = 1;
                            break;
                        case CodelChooser.Right:
                            xFactor = -1;
                            break;
                    }
                    break;
                case DirectionPointer.Left:
                    switch (cc)
                    {
                        case CodelChooser.Left:
                            yFactor = 1;
                            break;
                        case CodelChooser.Right:
                            yFactor = -1;
                            break;
                    }
                    break;
                case DirectionPointer.Up:
                    switch (cc)
                    {
                        case CodelChooser.Left:
                            xFactor = -1;
                            break;
                        case CodelChooser.Right:
                            xFactor = 1;
                            break;
                    }
                    break;
            }

            var codel = edge.OrderBy(x => x.Position.x * xFactor + x.Position.y * yFactor).Last();
            return codel;
        }
    }
}
