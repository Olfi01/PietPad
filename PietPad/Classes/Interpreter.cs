using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PietPad.Classes
{
    public class Interpreter
    {
        public Stack<int> Stack { get; } = new Stack<int>();
        public DirectionPointer DP { get; private set; }
        public CodelChooser CC { get; private set; }

        public void Interpret(Codel[,] image, Stream stdout, Stream stdin)
        {
            DP = DirectionPointer.Right;
            CC = CodelChooser.Left;
            List<ColorBlock> colorBlocks = new List<ColorBlock>();
            for (int x = 0; x < image.GetLength(0); x++)
            {
                for (int y = 0; y < image.GetLength(1); y++)
                {
                    if (image[x, y].Position.x != x || image[x, y].Position.y != y) throw new ArgumentException("Position noted in Codel class not equal to position noted in grid!");
                    bool hasAdjacentCodel(ColorBlock b)
                    {
                        return b.Codels.Any(c => (Math.Abs(c.Position.x - x) == 1 ^ Math.Abs(c.Position.y - y) == 1) && c.Color == image[x, y].Color);
                    }
                    if (!colorBlocks.Any(hasAdjacentCodel))
                    {
                        var colorBlock = new ColorBlock();
                        colorBlock.Codels.Add(image[x, y]);
                        colorBlocks.Add(colorBlock);
                    }
                    else
                    {
                        colorBlocks.Find(hasAdjacentCodel).Codels.Add(image[x, y]);
                    }
                }
            }

            Codel currentCodel;
            (int x, int y) currentPosition = (0, 0);
            ColorBlock currentColorBlock;
            Codel nextCodel;
            List<(int x, int y)> whiteTrace = new List<(int x, int y)>();
            while (true)
            {
                currentCodel = image[currentPosition.x, currentPosition.y];
                nextCodel = null;
                if (currentCodel.Color == CodelColor.White)
                {
                    whiteTrace.Add(currentPosition);
                    for (int i = 0; i < 4; i++)
                    {
                        int xFactor = 0, yFactor = 0;
                        switch (DP)
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
                        int xPos = currentPosition.x + xFactor;
                        int yPos = currentPosition.y + yFactor;
                        if (xPos < 0 || xPos >= image.GetLength(0) || yPos < 0 || yPos >= image.GetLength(1) || image[xPos, yPos].Color == CodelColor.Black)
                        {
                            ToggleCodelChooser();
                            TurnDirectionPointer();
                            continue;
                        }
                        currentPosition = (xPos, yPos);
                        break;
                    }
                    if (whiteTrace.Any(x => x.x == currentPosition.x && x.y == currentPosition.y)) return;
                    continue;
                }
                whiteTrace.Clear();
                currentColorBlock = colorBlocks.Find(x => x.Codels.Contains(currentCodel));

                Codel furthestCodel = null;
                for (int i = 0; i < 8; i++)
                {
                    int xFactor = 0, yFactor = 0;
                    switch (DP)
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

                    furthestCodel = currentColorBlock.FindFurthestCodel(DP, CC);
                    int xPos = furthestCodel.Position.x + xFactor;
                    int yPos = furthestCodel.Position.y + yFactor;
                    if (xPos < 0 || xPos >= image.GetLength(0) || yPos < 0 || yPos >= image.GetLength(1) || image[xPos, yPos].Color == CodelColor.Black)
                    {
                        if (i % 2 == 0) ToggleCodelChooser();
                        else TurnDirectionPointer();
                        continue;
                    }
                    nextCodel = image[xPos, yPos];
                    currentPosition = (xPos, yPos);
                    break;
                }

                if (nextCodel == null) return;
                var operation = CodelColor.CalculateOperation(furthestCodel.Color, nextCodel.Color);

                int top, sectop;
                switch (operation)
                {
                    case Operation.nop:
                        break;
                    case Operation.add:
                        top = Stack.Pop();
                        sectop = Stack.Pop();
                        Stack.Push(sectop + top);
                        break;
                    case Operation.divide:
                        if (Stack.Peek() == 0) break;
                        top = Stack.Pop();
                        sectop = Stack.Pop();
                        Stack.Push(sectop / top);
                        break;
                    case Operation.greater:
                        top = Stack.Pop();
                        sectop = Stack.Pop();
                        if (sectop > top) Stack.Push(1);
                        else Stack.Push(0);
                        break;
                    case Operation.duplicate:
                        Stack.Push(Stack.Peek());
                        break;
                    case Operation.in_char:
                        Stack.Push(stdin.ReadByte());
                        break;
                    case Operation.push:
                        Stack.Push(currentColorBlock.Size);
                        break;
                    case Operation.subtract:
                        top = Stack.Pop();
                        sectop = Stack.Pop();
                        Stack.Push(sectop - top);
                        break;
                    case Operation.mod:
                        if (Stack.Peek() == 0) break;
                        top = Stack.Pop();
                        sectop = Stack.Pop();
                        if (top > 0) Stack.Push(sectop % top);
                        else Stack.Push((sectop % Math.Abs(top)) - Math.Abs(top));
                        break;
                    case Operation.pointer:
                        TurnDirectionPointer(Stack.Pop());
                        break;
                    case Operation.roll:
                        top = Stack.Pop();
                        sectop = Stack.Pop();
                        if (sectop < 0 || sectop > Stack.Count)
                        {
                            Stack.Push(sectop);
                            Stack.Push(top);
                            break;
                        }
                        Roll(sectop, top);
                        break;
                    case Operation.out_number:
                        OutputNumber(stdout, Stack.Pop());
                        break;
                    case Operation.pop:
                        Stack.Pop();
                        break;
                    case Operation.multiply:
                        top = Stack.Pop();
                        sectop = Stack.Pop();
                        Stack.Push(sectop * top);
                        break;
                    case Operation.not:
                        top = Stack.Pop();
                        if (top == 0) Stack.Push(1);
                        else Stack.Push(0);
                        break;
                    case Operation.@switch:
                        ToggleCodelChooser(Stack.Pop());
                        break;
                    case Operation.in_number:
                        ReadNumber(stdin);
                        break;
                    case Operation.out_char:
                        stdout.WriteByte(Convert.ToByte(Stack.Pop()));
                        break;
                }
            }
        }

        private void ReadNumber(Stream stream)
        {
            int totalNumber = 0;
            do
            {
                int number = stream.ReadByte();
                if (number > 57 || number < 48)
                {
                    stream.Seek(-1, SeekOrigin.Current);
                    break;
                }
                else totalNumber = totalNumber * 10 + (number - 48);
            } while (true);
            Stack.Push(totalNumber);
        }

        private byte DigitToAscii(int digit)
        {
            if (digit > 9 || digit < 0) throw new ArgumentException("Not a digit!");
            return Convert.ToByte(48 + digit);
        }

        private void OutputNumber(Stream stream, int number)
        {
            bool minus = number < 0;
            Stack<int> digits = new Stack<int>();
            Digitize(Math.Abs(number), digits);
            if (minus) stream.WriteByte(Convert.ToByte('-'));
            while (digits.Count > 0)
            {
                stream.WriteByte(DigitToAscii(digits.Pop()));
            }
        }

        private void Digitize(int number, Stack<int> digits)
        {
            if (number < 10)
            {
                digits.Push(number);
            }
            else
            {
                digits.Push(number % 10);
                Digitize(number / 10, digits);
            }
        }

        private void Roll(int depth, int times)
        {
            if (times == 0) return;
            if (times < 0) RollBackwards(depth, Math.Abs(times));
            int[] buffer = new int[depth];
            for (int i = 0; i < depth; i++)
            {
                buffer[i] = Stack.Pop();
            }
            Stack.Push(buffer[0]);
            for (int i = depth - 1; i > 0; i--)
            {
                Stack.Push(buffer[i]);
            }
            Roll(depth, times - 1);
        }

        private void RollBackwards(int depth, int times)
        {
            if (times <= 0) return;
            int[] buffer = new int[depth];
            for (int i = 0; i < depth; i++)
            {
                buffer[i] = Stack.Pop();
            }
            for (int i = depth - 2; i >= 0; i++)
            {
                Stack.Push(buffer[i]);
            }
            Stack.Push(buffer[depth - 1]);
            RollBackwards(depth, times - 1);
        }

        private void ToggleCodelChooser()
        {
            switch (CC)
            {
                case CodelChooser.Left:
                    CC = CodelChooser.Right;
                    return;
                case CodelChooser.Right:
                    CC = CodelChooser.Left;
                    return;
            }
        }

        private void ToggleCodelChooser(int times)
        {
            times = Math.Abs(times);
            for (int i = 0; i < times % 2; i++)
            {
                ToggleCodelChooser();
            }
        }

        private void TurnDirectionPointer()
        {
            switch (DP)
            {
                case DirectionPointer.Right:
                    DP = DirectionPointer.Down;
                    return;
                case DirectionPointer.Down:
                    DP = DirectionPointer.Left;
                    return;
                case DirectionPointer.Left:
                    DP = DirectionPointer.Up;
                    return;
                case DirectionPointer.Up:
                    DP = DirectionPointer.Right;
                    return;
            }
        }

        private void TurnDirectionPointerCounterclockwise()
        {
            switch (DP)
            {
                case DirectionPointer.Right:
                    DP = DirectionPointer.Up;
                    return;
                case DirectionPointer.Down:
                    DP = DirectionPointer.Right;
                    return;
                case DirectionPointer.Left:
                    DP = DirectionPointer.Down;
                    return;
                case DirectionPointer.Up:
                    DP = DirectionPointer.Left;
                    return;
            }
        }

        private void TurnDirectionPointer(int times)
        {
            if (times == 0) return;
            if (times > 0)
            {
                for (int i = 0; i < times % 4; i++)
                {
                    TurnDirectionPointer();
                }
            }
            else if (times < 0)
            {
                for (int i = 0; i < Math.Abs(times) % 4; i++)
                {
                    TurnDirectionPointerCounterclockwise();
                }
            }
        }

        public enum DirectionPointer
        {
            Right,
            Down,
            Left,
            Up
        }

        public enum CodelChooser
        {
            Left,
            Right
        }
    }
}
