using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chip8SharpGL.Core.Model
{
    public struct CommandCode
    {
        ushort OperationCode;
        public CommandCode(byte High, byte Low) => OperationCode = (ushort)((High << 8) | Low);
        public CommandCode(ushort Code) => OperationCode = Code;

        public ushort GetCommandCode => OperationCode;
        public byte GetX000 => (byte)((OperationCode & 0xF000u) >> 12);
        public byte Get0X00 => (byte)((OperationCode & 0x0F00u) >> 8);
        public byte Get00X0 => (byte)((OperationCode & 0x00F0u) >> 4);
        public byte Get000X => (byte)(OperationCode & 0x000Fu);
        public ushort Get0XXX => (ushort)(OperationCode & 0x0FFFu);
        public byte Get00XX => (byte)(OperationCode & 0x00FFu);
        public byte Get0XX0 => (byte)((OperationCode & 0x0FF0u) >> 4);
    }

    class Chip8
    {
        public delegate void OutputImageHandler(byte[,] Image);
        public event OutputImageHandler OutputImage;

        public delegate void OutputRegistersHandler(byte[] V, ushort I, ushort PC, byte[] Memory);
        public event OutputRegistersHandler OutputRegisters;

        public delegate void EndOfCycleHandler();
        public event EndOfCycleHandler EndOfCycle;

        public delegate void LogHandler(string Message);
        public event LogHandler Log;

        public static int Width { get { return _Width; } }
        public static int Height { get { return _Height; } }

        const int _Width = 64;
        const int _Height = 32;

        const int StartProgrammAdress = 0x200;
        const int StartFontAdress = 0x50;

        Random randomGenerator = new Random();

        //Memory
        byte[] Memory = new byte[4096];

        Stack<ushort> CommandStack = new Stack<ushort>();

        //Registers
        byte[] V = new byte[16];
        ushort Index = 0;
        ushort ProgrammCounter = StartProgrammAdress;

        //Timer
        byte Timer = 0;

        //Graphics and Sound
        byte Sound = 0;
        byte[,] VideoBuffer = new byte[_Width, _Height];

        public byte[,] ImageOutput { get { return VideoBuffer; } }

        //Keyboard
        byte InputKey = 0;
        bool KeyPress = false;

        //Fonts
        byte[] Font = new byte[] {
            0xF0, 0x90, 0x90, 0x90, 0xF0,
            0x20, 0x60, 0x20, 0x20, 0x70,
            0xF0, 0x10, 0xF0, 0x80, 0xF0,
            0xF0, 0x10, 0xF0, 0x10, 0xF0,
            0x90, 0x90, 0xF0, 0x10, 0x10,
            0xF0, 0x80, 0xF0, 0x10, 0xF0,
            0xF0, 0x80, 0xF0, 0x90, 0xF0,
            0xF0, 0x10, 0x20, 0x40, 0x40,
            0xF0, 0x90, 0xF0, 0x90, 0xF0,
            0xF0, 0x90, 0xF0, 0x10, 0xF0,
            0xF0, 0x90, 0xF0, 0x90, 0x90,
            0xE0, 0x90, 0xE0, 0x90, 0xE0,
            0xF0, 0x80, 0x80, 0x80, 0xF0,
            0xE0, 0x90, 0x90, 0x90, 0xE0,
            0xF0, 0x80, 0xF0, 0x80, 0xF0,
            0xF0, 0x80, 0xF0, 0x80, 0x80
        };

        //Debagging
        private bool checkRun = false;

        //Clock frequency
        private int Hertz = 500;
        private double TimeOfOneTick = 120;

        public Chip8()
        {
            Initialize();
        }

        public void Initialize()
        {
            Index = 0;
            ProgrammCounter = StartProgrammAdress;

            CommandStack.Clear();
            Array.Clear(V, 0, V.Length);
            Array.Clear(Memory, 0, Memory.Length);
            Array.Clear(VideoBuffer, 0, VideoBuffer.Length);
            //Load Fonts
            for (int i = 0; i < Font.Length; i++)
                Memory[StartFontAdress + i] = Font[i];
        }

        public void Run()
        {
            DateTime lastCycleTime = new DateTime(0);

            while (true)
            {
                if (checkRun == false)
                    continue;

                var deltaTime = DateTime.Now - lastCycleTime;

                if (deltaTime.Milliseconds < TimeOfOneTick)
                    continue;

                Cycle();

                lastCycleTime = DateTime.Now;
            }
        }

        public void Cycle()
        {
            CommandDecode();
            DecrementTimer();
            IncrementProgrammCounter();
            SetKeyPress(false);

            //output
            OutputImage(VideoBuffer);
            OutputRegisters(V, Index, ProgrammCounter, Memory);

            //End Cycle
            EndOfCycle();
        }

        private void Error(string Message)
        {
            checkRun = false;
            Log(Message);
        }

        public void SetHertz(int Hertz)
        {
            if (Hertz > 0)
            {
                this.Hertz = Hertz;
                TimeOfOneTick = 60_000 / this.Hertz;
            }
        }

        public void LoadROM(byte[] RomByte)
        {
            for (int i = 0; i < RomByte.Length; i++)
                Memory[StartProgrammAdress + i] = RomByte[i];
        }

        public void OpenROM(in string path)
        {
            byte[] ROM = System.IO.File.ReadAllBytes(path);

            if ((ROM.Length + StartProgrammAdress) < Memory.Length)
                for (int i = 0; i < ROM.Length; i++)
                    Memory[StartProgrammAdress + i] = ROM[i];

        }

        public void SetKey(byte x) => InputKey = x;

        public void SetKeyPress(bool x) => KeyPress = x;

        public void PauseRun() => checkRun = false;
        public void StartRun() => checkRun = true;

        void IncrementProgrammCounter() => ProgrammCounter += 2;
        void DecrementProgrammCounter() => ProgrammCounter -= 2;

        void DecrementTimer()
        {
            if (Timer > 0)
                Timer -= 1;
        }

        void CommandDecode()
        {
            if ((ProgrammCounter + 1) > Memory.Length)
            {
                Error($"Out of bounds of an array. PC = {ProgrammCounter.ToString("X4")}");
                return;
            }

            CommandCode command = new CommandCode(Memory[ProgrammCounter], Memory[ProgrammCounter + 1]);

            switch (command.GetX000)
            {
                case (byte)0x0u:

                    switch (command.Get000X)
                    {
                        case (byte)0x0u:
                            Command_00E0();
                            break;
                        case (byte)0xEu:
                            Command_00EE();
                            break;
                        default:
                            Error($"Unknown command {command.GetCommandCode.ToString("X4")}");
                            break;
                    }

                    break;

                case (byte)0x1u:
                    Command_1NNN(command);
                    break;

                case (byte)0x2u:
                    Command_2NNN(command);
                    break;

                case (byte)0x3u:
                    Command_3XKK(command);
                    break;

                case (byte)0x4u:
                    Command_4XKK(command);
                    break;

                case (byte)0x5u:
                    Command_5XY0(command);
                    break;

                case (byte)0x6u:
                    Command_6XKK(command);
                    break;

                case (byte)0x7u:
                    Command_7XKK(command);
                    break;

                case (byte)0x8u:

                    switch (command.Get000X)
                    {
                        case (byte)0x0u:
                            Command_8XY0(command);
                            break;

                        case (byte)0x1u:
                            Command_8XY1(command);
                            break;

                        case (byte)0x2u:
                            Command_8XY2(command);
                            break;

                        case (byte)0x3u:
                            Command_8XY3(command);
                            break;

                        case (byte)0x4u:
                            Command_8XY4(command);
                            break;

                        case (byte)0x5u:
                            Command_8XY5(command);
                            break;

                        case (byte)0x6u:
                            Command_8XY6(command);
                            break;

                        case (byte)0x7u:
                            Command_8XY7(command);
                            break;

                        case (byte)0xEu:
                            Command_8XYE(command);
                            break;

                        default:
                            Error($"Unknown command {command.GetCommandCode.ToString("X4")}");
                            break;
                    }

                    break;

                case (byte)0x9u:
                    Command_9XY0(command);
                    break;

                case (byte)0xAu:
                    Command_ANNN(command);
                    break;

                case (byte)0xBu:
                    Command_BNNN(command);
                    break;

                case (byte)0xCu:
                    Command_CXKK(command);
                    break;

                case (byte)0xDu:
                    Command_DXYN(command);
                    break;

                case (byte)0xEu:

                    switch (command.Get00XX)
                    {
                        case (byte)0x9Eu:
                            Command_EX9E(command);
                            break;

                        case (byte)0xA1u:
                            Command_EXA1(command);
                            break;

                        default:
                            Error($"Unknown command {command.GetCommandCode.ToString("X4")}");
                            break;
                    }

                    break;

                case (byte)0xFu:

                    switch (command.Get00XX)
                    {
                        case (byte)0x07u:
                            Command_FX07(command);
                            break;

                        case (byte)0x0Au:
                            Command_FX0A(command);
                            break;

                        case (byte)0x15u:
                            Command_FX15(command);
                            break;

                        case (byte)0x18u:
                            Command_FX18(command);
                            break;

                        case (byte)0x1Eu:
                            Command_FX1E(command);
                            break;

                        case (byte)0x29u:
                            Command_FX29(command);
                            break;

                        case (byte)0x33u:
                            Command_FX33(command);
                            break;

                        case (byte)0x55u:
                            Command_FX55(command);
                            break;

                        case (byte)0x65u:
                            Command_FX65(command);
                            break;

                        default:
                            Error($"Unknown command {command.GetCommandCode.ToString("X4")}");
                            break;
                    }

                    break;

                default:
                    Error($"Unknown command {command.GetCommandCode.ToString("X4")}");
                    break;
            }
        }

        void Command_00E0() => Array.Clear(VideoBuffer, 0, VideoBuffer.Length);

        void Command_00EE() => ProgrammCounter = CommandStack.Pop();

        void Command_1NNN(in CommandCode code) => ProgrammCounter = code.Get0XXX;

        void Command_2NNN(in CommandCode code)
        {
            CommandStack.Push(ProgrammCounter);
            ProgrammCounter = code.Get0XXX;
        }

        void Command_3XKK(in CommandCode code)
        {
            if (V[code.Get0X00] == code.Get00XX)
                IncrementProgrammCounter();
        }

        void Command_4XKK(in CommandCode code)
        {
            if (V[code.Get0X00] != code.Get00XX)
                IncrementProgrammCounter();
        }

        void Command_5XY0(in CommandCode code)
        {
            if (V[code.Get0X00] == V[code.Get00X0])
                IncrementProgrammCounter();
        }

        void Command_6XKK(in CommandCode code) => V[code.Get0X00] = code.Get00XX;

        void Command_7XKK(in CommandCode code) => V[code.Get0X00] += code.Get00XX;

        void Command_8XY0(in CommandCode code) => V[code.Get0X00] = V[code.Get00X0];

        void Command_8XY1(in CommandCode code) => V[code.Get0X00] |= V[code.Get00X0];

        void Command_8XY2(in CommandCode code) => V[code.Get0X00] &= V[code.Get00X0];

        void Command_8XY3(in CommandCode code) => V[code.Get0X00] ^= V[code.Get00X0];

        void Command_8XY4(in CommandCode code)
        {
            if (V[code.Get0X00] + V[code.Get00X0] > 255)
            {
                V[0xF] = 1;
            }
            else
            {
                V[0xF] = 0;
            }

            V[code.Get0X00] += V[code.Get00X0];
        }

        void Command_8XY5(in CommandCode code)
        {
            if (V[code.Get0X00] > V[code.Get00X0])
            {
                V[0xF] = 1;
            }
            else
            {
                V[0xF] = 0;
            }
            V[code.Get0X00] -= V[code.Get00X0];
        }

        void Command_8XY6(in CommandCode code)
        {
            V[0xF] = (byte)(V[code.Get0X00] & 0x1u);
            V[code.Get0X00] >>= 1;
        }

        void Command_8XY7(in CommandCode code)
        {
            if (V[code.Get00X0] > V[code.Get0X00])
            {
                V[0xF] = 1;
            }
            else
            {
                V[0xF] = 0;
            }
            V[code.Get0X00] = (byte)(V[code.Get00X0] - V[code.Get0X00]);
        }

        void Command_8XYE(in CommandCode code)
        {
            V[0xF] = (byte)((V[code.Get0X00] & 0x80u) >> 7);
            V[code.Get0X00] <<= 1;
        }

        void Command_9XY0(in CommandCode code)
        {
            if (V[code.Get0X00] != V[code.Get00X0])
                IncrementProgrammCounter();
        }

        void Command_ANNN(in CommandCode code) => Index = code.Get0XXX;

        void Command_BNNN(in CommandCode code) => ProgrammCounter = (ushort)(V[0x0] + code.Get0XXX);

        void Command_CXKK(in CommandCode code) => V[code.Get0X00] = (byte)(randomGenerator.Next(0, 255) & code.Get00XX);

        void Command_DXYN(in CommandCode code)
        {
            byte x = V[code.Get0X00];
            byte y = V[code.Get00X0];
            byte height = code.Get000X;
            byte collision = 0;

            for (int dy = 0; dy < height; dy++)
            {
                var bits = Memory[Index + dy];
                int yPos = y + dy;

                for (int dx = 0; dx < 8; dx++)
                {
                    int xPos = x + dx;

                    if ((xPos >= _Width) || (yPos >= _Height))
                        continue;

                    if ((bits & 0x80) != 0)
                    {
                        if (VideoBuffer[xPos, yPos] == 0x1)
                            collision = 1;

                        VideoBuffer[xPos, yPos] ^= 0x1;
                    }
                    bits <<= 1;
                }
            }

            V[0xF] = collision;

        }

        void Command_EX9E(in CommandCode code)
        {
            if (V[code.Get0X00] == InputKey)
                IncrementProgrammCounter();
        }

        void Command_EXA1(in CommandCode code)
        {
            if (V[code.Get0X00] != InputKey)
                IncrementProgrammCounter();
        }

        void Command_FX07(in CommandCode code) => V[code.Get0X00] = Timer;

        void Command_FX0A(in CommandCode code)
        {
            if (KeyPress == true)
            {
                V[code.Get0X00] = InputKey;
            }
            else
            {
                DecrementProgrammCounter();
            }
        }

        void Command_FX15(in CommandCode code) => Timer = V[code.Get0X00];

        void Command_FX18(in CommandCode code) => Sound = V[code.Get0X00];

        void Command_FX1E(in CommandCode code) => Index += V[code.Get0X00];

        void Command_FX29(in CommandCode code) => Index = (ushort)(StartFontAdress + (5 * V[code.Get0X00]));

        void Command_FX33(in CommandCode code)
        {
            byte value = V[code.Get0X00];
            for (int i = 2; i >= 0; i--)
            {
                Memory[Index + i] = (byte)(value % 10);
                value /= 10;
            }
        }

        void Command_FX55(in CommandCode code)
        {
            for (int i = 0; i <= code.Get0X00; i++)
                Memory[Index + i] = V[i];
        }

        void Command_FX65(in CommandCode code)
        {
            for (int i = 0; i <= code.Get0X00; i++)
                V[i] = Memory[Index + i];
        }
    }
}
