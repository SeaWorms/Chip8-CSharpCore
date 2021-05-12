using Chip8SharpGL.Core.ViewModel;
using SharpGL.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Chip8SharpGL
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        protected MainViewModel ViewModel => (MainViewModel)Resources["viewModel"];

        public MainWindow()
        {
            InitializeComponent();
            ViewModel.SetOpenGl((OpenGLControl)this.FindName("OutputWindow"));
        }

        private void OpenGLControl_OpenGLDraw(object sender, SharpGL.WPF.OpenGLRoutedEventArgs args)
        {
            ViewModel.OpenGlDraw(sender, args);
        }

        private void OpenGLControl_OpenGLInitialized(object sender, SharpGL.WPF.OpenGLRoutedEventArgs args)
        {
            ViewModel.OpenGlInitialized(sender, args);
        }

        private void OutputWindow_Resized(object sender, OpenGLRoutedEventArgs args)
        {
            ViewModel.OpenGlResized(sender, args);
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.OpenROM();
        }
        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.StartRun();
        }
        private void NextStepButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.NextTact();
        }
        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.PauseRun();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void HertzTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(Hertz.Text))
                ViewModel.SetClockFrequency(int.Parse(Hertz.Text));
        }

        private void InputButtonHandrel(object sender, RoutedEventArgs e)
        {
            ViewModel.ButtonInputHandel(((Button)sender).Tag.ToString());
        }

    }
}
