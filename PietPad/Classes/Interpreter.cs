using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PietPad.Classes
{
    public class Interpreter
    {
        public ObservableStack<int> Stack { get; } = new ObservableStack<int>();
        public DirectionPointer DP { get; private set; }
        public CodelChooser CC { get; private set; }
        public bool IsDebug { get; }
        public int StepDelay { get; }
        /// <summary>
        /// The encoding that is assumed for the input and output stream
        /// </summary>
        public static readonly Encoding Encoding = Encoding.Unicode;

        /// <summary>
        /// Will be fired on each new operation if debug is active
        /// </summary>
        public event EventHandler<Operation> OperationSelected;
        /// <summary>
        /// Will be fired when an operation can't be executed if debug is active
        /// </summary>
        public event EventHandler<Operation> OperationFailed;
        /// <summary>
        /// Will be fired when the direction of the DP changes if debug is active
        /// </summary>
        public event EventHandler<DirectionPointer> DpChanged;
        /// <summary>
        /// Will be fired when the direction of the CC changes if debug is active
        /// </summary>
        public event EventHandler<CodelChooser> CcChanged;
        /// <summary>
        /// Will be fired when current color block changes during interpretation, if debug is active
        /// </summary>
        public event EventHandler<ColorBlock> EnterColorBlock;
        /// <summary>
        /// Current state of interpretation in debug mode
        /// </summary>
        public ExecutionState State { get; private set; }

        /// <summary>
        /// Initializes a new interpreter for a Piet program
        /// </summary>
        /// <param name="debug">Whether the interpreter should work in debug mode</param>
        /// <param name="stepDelay">The delay between operations. Will be ignored if <paramref name="debug"/> is false.</param>
        public Interpreter(bool debug = false, int stepDelay = 1000)
        {
            IsDebug = debug;
            StepDelay = stepDelay;
        }

        /// <summary>
        /// Pauses the interpretation (only available in debug mode). Will be ignored if no interpretation is in progress or the interpretation is already paused.
        /// </summary>
        public void PauseInterpretation()
        {
            if (!IsDebug) throw new InvalidOperationException("Interpreter is not debuggable!");
            if (State != ExecutionState.Running) return;
            State = ExecutionState.Paused;
        }

        /// <summary>
        /// Resumes the interpretation (only available in debug mode). Will be ignored if no interpretation is in progress or it wasn't paused to begin with.
        /// </summary>
        public void ResumeInterpretation()
        {
            if (!IsDebug) throw new InvalidOperationException("Interpreter is not debuggable!");
            if (State != ExecutionState.Paused) return;
            State = ExecutionState.Running;
        }
        
        /// <summary>
        /// Lets the current interpretation proceed by one step (only available in debug mode). Will be ignoed if no interpretation is in progress or it was not paused before.
        /// </summary>
        public void Step()
        {
            if (!IsDebug) throw new InvalidOperationException("Interpreter is not debuggable!");
            if (State != ExecutionState.Paused) return;
            State = ExecutionState.OneMoreStep;
        }

        /// <summary>
        /// Cancels the current interpretation (only available in debug mode).
        /// </summary>
        public void CancelInterpretation()
        {
            if (!IsDebug) throw new InvalidOperationException("Interpreter is not debuggable!");
            State = ExecutionState.Idle;
        }

        public void Interpret(Codel[,] image, Stream stdout, Stream stdin)
        {
            if (State != ExecutionState.Idle) throw new InvalidOperationException("Can't start two interpretations at once!");
            State = ExecutionState.Running;
            using (StreamWriter stdoutWriter = new StreamWriter(stdout, Encoding))
            {
                stdoutWriter.AutoFlush = true;
                using (StreamReader stdinReader = new StreamReader(stdin, Encoding))
                {
                    DP = DirectionPointer.Right;
                    if (IsDebug) DpChanged?.Invoke(this, DP);
                    CC = CodelChooser.Left;
                    if (IsDebug) CcChanged?.Invoke(this, CC);
                    Stack.Clear();
                    List<ColorBlock> colorBlocks = new List<ColorBlock>();
                    for (int x = 0; x < image.GetLength(0); x++)
                    {
                        for (int y = 0; y < image.GetLength(1); y++)
                        {
                            if (image[x, y].Position.x != x || image[x, y].Position.y != y) throw new ArgumentException("Position noted in Codel class not equal to position noted in grid!");

                            bool nextTo(Codel c, int xPos, int yPos)
                            {
                                return (Math.Abs(c.Position.x - xPos) == 1 && c.Position.y == yPos) || (Math.Abs(c.Position.y - yPos) == 1 && c.Position.x == xPos);
                            }
                            bool hasAdjacentCodel(ColorBlock b)
                            {
                                return b.Codels.Any(c => nextTo(c, x, y) && c.Color == image[x, y].Color);
                            }
                            if (!colorBlocks.Any(hasAdjacentCodel))
                            {
                                var colorBlock = new ColorBlock(image[x, y]);
                                colorBlocks.Add(colorBlock);
                            }
                            else
                            {
                                if (colorBlocks.Count(hasAdjacentCodel) > 1)
                                {
                                    var block = new ColorBlock(image[x, y]);
                                    colorBlocks.ForEach(b => { if (hasAdjacentCodel(b)) b.Codels.ForEach(c => block.Codels.Add(c)); });
                                    colorBlocks.RemoveAll(hasAdjacentCodel);
                                    colorBlocks.Add(block);
                                }
                                else colorBlocks.Find(hasAdjacentCodel).Codels.Add(image[x, y]);
                            }
                        }
                    }

                    Codel currentCodel;
                    (int x, int y) currentPosition = (0, 0);
                    ColorBlock currentColorBlock, previousColorBlock;
                    Codel nextCodel;
                    List<(int x, int y)> whiteTrace = new List<(int x, int y)>();
                    if (IsDebug) EnterColorBlock?.Invoke(this, colorBlocks.Find(x => x.Codels.Contains(image[0, 0])));
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
                            if (whiteTrace.Any(x => x.x == currentPosition.x && x.y == currentPosition.y))
                            {
                                State = ExecutionState.Idle;
                                return;
                            }
                            continue;
                        }
                        whiteTrace.Clear();
                        if (IsDebug) Thread.Sleep(StepDelay);
                        while (IsDebug && State == ExecutionState.Paused) { }
                        if (State == ExecutionState.OneMoreStep) State = ExecutionState.Paused;
                        else if (State == ExecutionState.Idle) return;
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

                        if (nextCodel == null)
                        {
                            State = ExecutionState.Idle;
                            return;
                        }
                        previousColorBlock = currentColorBlock;
                        currentColorBlock = colorBlocks.Find(x => x.Codels.Contains(nextCodel));
                        if (IsDebug && nextCodel.Color != CodelColor.White) EnterColorBlock?.Invoke(this, currentColorBlock);
                        var operation = CodelColor.CalculateOperation(furthestCodel.Color, nextCodel.Color);
                        if (IsDebug) OperationSelected?.Invoke(this, operation);

                        // check if operation is impossible and needs to be skipped
                        bool skip;
                        switch (operation)
                        {
                            case Operation.add:
                            case Operation.greater:
                            case Operation.subtract:
                            case Operation.multiply:
                            case Operation.roll:
                                skip = Stack.Count < 2;
                                break;
                            case Operation.divide:
                            case Operation.mod:
                                skip = Stack.Count < 2 || Stack.Peek() == 0;
                                break;
                            case Operation.duplicate:
                            case Operation.pointer:
                            case Operation.out_number:
                            case Operation.pop:
                            case Operation.not:
                            case Operation.@switch:
                            case Operation.out_char:
                                skip = Stack.Count < 1;
                                break;
                            default:
                                skip = false;
                                break;
                        }

                        // execute operation
                        if (!skip)
                        {
                            int top, sectop, b;
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
                                    b = stdinReader.Read();
                                    if (b != -1) Stack.Push(b);
                                    else OperationFailed?.Invoke(this, operation);
                                    break;
                                case Operation.push:
                                    Stack.Push(previousColorBlock.Size);
                                    break;
                                case Operation.subtract:
                                    top = Stack.Pop();
                                    sectop = Stack.Pop();
                                    Stack.Push(sectop - top);
                                    break;
                                case Operation.mod:
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
                                        if (IsDebug) OperationFailed?.Invoke(this, operation);
                                        break;
                                    }
                                    Roll(sectop, top);
                                    break;
                                case Operation.out_number:
                                    OutputNumber(stdoutWriter, Stack.Pop());
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
                                    ReadNumber(stdinReader);
                                    break;
                                case Operation.out_char:
                                    stdoutWriter.Write((char)Stack.Pop());
                                    break;
                            }
                        }
                        else if (IsDebug) OperationFailed?.Invoke(this, operation);
                    }
                }
            }
        }

        private void ReadNumber(StreamReader streamReader)
        {
            string number = "";
            if (streamReader.Peek() == '-') number += (char)streamReader.Read();
            while (char.IsDigit((char)streamReader.Peek()))
            {
                number += (char)streamReader.Read();
            }
            Stack.Push(int.Parse(number));
        }

        private void OutputNumber(StreamWriter streamWriter, int number)
        {
            streamWriter.Write(number.ToString());
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
            if (IsDebug) CcChanged?.Invoke(this, CC);
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
            if (IsDebug) DpChanged?.Invoke(this, DP);
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
            if (IsDebug) DpChanged?.Invoke(this, DP);
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

        public enum ExecutionState
        {
            Idle,
            Running,
            Paused,
            OneMoreStep
        }
    }
}
