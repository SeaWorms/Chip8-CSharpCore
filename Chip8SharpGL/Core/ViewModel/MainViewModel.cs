using Chip8SharpGL.BaseClass;
using System;
using System.Text;
using SharpGL;
using SharpGL.WPF;
using System.Drawing;
using SharpGL.SceneGraph.Assets;
using System.Drawing.Imaging;
using System.Threading;
using Chip8SharpGL.Core.Model;
using Microsoft.Win32;

namespace Chip8SharpGL.Core.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        readonly SynchronizationContext _synchronizationContext = SynchronizationContext.Current;
        Chip8 controller;
        Thread threadChip;

        public double Width { get; set; }
        public double Height { get; set; }

        OpenGL openGL;
        OpenGLControl OpenGLControl;
        Texture texture = new Texture();

        string path = @"..\..\\Resource\Img\Sprite-Gradient.png";
        Bitmap ImageOutput;

        private string _lastFileName;

        byte[] _Memory;
        byte[] _V;
        ushort _I;
        ushort _PC;
        string _OutTextMemory;
        string _Log;

        public byte[] Memory
        {
            get { return _Memory; }
            set
            {
                _Memory = value;
                OnPropertyChanged();
            }
        }

        public byte[] V
        {
            get { return _V; }
            set
            {
                _V = value;
                OnPropertyChanged();
            }
        }

        public ushort I
        {
            get { return _I; }
            set
            {
                _I = value;
                OnPropertyChanged();
            }
        }

        public ushort PC
        {
            get { return _PC; }
            set
            {
                _PC = value;
                OnPropertyChanged();
            }
        }

        public string OutTextMemory
        {
            get { return _OutTextMemory; }
            set
            {
                _OutTextMemory = value;
                OnPropertyChanged();
            }
        }

        public string Log
        {
            get { return _Log; }
            set
            {
                _Log = value;
                OnPropertyChanged();
            }
        }

        public MainViewModel()
        {
            ImageOutput = new Bitmap(path);

            controller = new Chip8();
            controller.OutputImage += InputBufferImage;
            controller.OutputRegisters += InputRegisters;
            controller.Log += (x) => { Log = AppendLine(Log, x); };
            controller.EndOfCycle += EndCycle;

            threadChip = CreateThread();
        }

        public void OpenROM()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Chip files (*.ch8)|*.ch8|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                controller.Initialize();
                controller.OpenROM(openFileDialog.FileName);
                _lastFileName = openFileDialog.FileName;
                Log = string.Empty;
                Log = "Log";
                Log = AppendLine(Log, $"Open file - { openFileDialog.SafeFileName}");
            }
        }

        private Thread CreateThread()
        {
            Thread thread = new Thread(() => controller.Run());
            thread.IsBackground = true;
            thread.Start();
            return thread;
        }

        public void StartRun()
        {
            controller.StartRun();
        }

        public void NextTact()
        {
            controller.Cycle();
        }

        public void PauseRun()
        {
            controller.PauseRun();
        }

        public void SetOpenGl(OpenGLControl openGL)
        {
            this.openGL = openGL.OpenGL;
            this.OpenGLControl = openGL;
        }

        public void SetClockFrequency(int Hertz)
        {
            controller.SetHertz(Hertz);
        }

        public void OpenGlInitialized(object sender, OpenGLRoutedEventArgs args)
        {
            openGL.ClearColor(0, 0, 0, 0);
            openGL.Enable(OpenGL.GL_TEXTURE_2D);
            openGL.Disable(OpenGL.GL_DEPTH_TEST);
            openGL.Disable(OpenGL.GL_BLEND);
            openGL.Disable(OpenGL.GL_SMOOTH);
            openGL.Disable(OpenGL.GL_POINT_SMOOTH);
            openGL.Disable(OpenGL.GL_LINE_SMOOTH);
            openGL.Disable(OpenGL.GL_POLYGON_SMOOTH);
        }

        public void OpenGlDraw(object sender, OpenGLRoutedEventArgs args)
        {
            openGL.MatrixMode(OpenGL.GL_PROJECTION);
            openGL.Ortho2D(0, OpenGLControl.Width, OpenGLControl.Height, 0);
            openGL.Disable(OpenGL.GL_DEPTH_TEST);

            openGL.LoadIdentity();
            if (ImageOutput != null)
            {
                texture.Create(openGL, new Bitmap(ImageOutput));
                texture.Bind(openGL);
            }

            openGL.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MIN_FILTER, OpenGL.GL_NEAREST);
            openGL.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MAG_FILTER, OpenGL.GL_NEAREST);

            openGL.Begin(OpenGL.GL_QUADS);

            openGL.TexCoord(0.0f, 1.0f); openGL.Vertex(-1.0f, -1.0f, 1.0f);
            openGL.TexCoord(1.0f, 1.0f); openGL.Vertex(1.0f, -1.0f, 1.0f);
            openGL.TexCoord(1.0f, 0.0f); openGL.Vertex(1.0f, 1.0f, 1.0f);
            openGL.TexCoord(0.0f, 0.0f); openGL.Vertex(-1.0f, 1.0f, 1.0f);

            openGL.End();
            openGL.Flush();
        }

        public void OpenGlResized(object sender, OpenGLRoutedEventArgs args)
        {
            openGL.MatrixMode(OpenGL.GL_PROJECTION);
            openGL.LoadIdentity();
            openGL.Perspective(60.0f, (double)OpenGLControl.Width / (double)OpenGLControl.Height, 0.01, 10000.0);
            openGL.LookAt(0, 0, -500, 0, 0, 0, 0, 1, 0);
            openGL.MatrixMode(OpenGL.GL_MODELVIEW);
        }

        private void InputRegisters(byte[] V, ushort I, ushort PC, byte[] Memory)
        {
            _synchronizationContext.Post((x) => { this.V = V; this.I = I; this.PC = PC; this.Memory = Memory; }, null);
        }

        private void InputBufferImage(byte[,] ImageData)
        {

            Bitmap bitmap = new Bitmap(Chip8.Width, Chip8.Height, PixelFormat.Format32bppArgb);

            for (int x = 0; x < ImageData.GetLength(0); x++)
            {
                for (int y = 0; y < ImageData.GetLength(1); y++)
                {
                    if (ImageData[x, y] == 0x1u)
                    {
                        bitmap.SetPixel(x, y, Color.FromArgb(255, 255, 255));
                    }
                    else
                    {
                        bitmap.SetPixel(x, y, Color.FromArgb(0, 0, 0));
                    }
                }
            }

            ImageOutput = bitmap;
        }

        private void EndCycle()
        {
            CreateOutMemoryText(PC, Memory);
        }

        private void CreateOutMemoryText(ushort PC, byte[] Memory)
        {
            int StartMemory = PC - 6;

            StringBuilder OutputBuilder = new StringBuilder();
            OutputBuilder.AppendLine($"|Address| PC | Value |");
            OutputBuilder.AppendLine($"|=======|====|=======|");

            string counter;

            if (Memory == null)
                return;

            for (int i = 0; i < 10; i++)
            {
                if ((StartMemory >= 0) && (StartMemory <= (Memory.Length - 1)))
                {
                    if (i == 3)
                    {
                        counter = @" -> ";
                    }
                    else
                    {
                        counter = new string(' ', 4);
                    }
                    OutputBuilder.AppendLine($"| 0x{ StartMemory.ToString("X4") }|{ counter }| 0x{ GetMemoryCode(Memory[StartMemory], Memory[StartMemory + 1]) }|");
                }
                else
                {
                    OutputBuilder.AppendLine($"| 0x0000|    | 0x0000|");
                }
                StartMemory += 2;
            }

            OutputBuilder.AppendLine($"|=======|====|=======|");

            OutTextMemory = OutputBuilder.ToString();
        }

        private string GetMemoryCode(byte High, byte Low) => ((High << 8) | Low).ToString("X4");

        private string AppendLine(string BaseString, string NewLine) => BaseString + Environment.NewLine + NewLine;

    }
}
