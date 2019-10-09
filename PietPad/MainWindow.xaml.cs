using PietPad.Classes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace PietPad
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var i = new Interpreter();
            var image = new Codel[3, 2] { { new Codel(0, 0), new Codel(0, 1) }, { new Codel(1, 0), new Codel(1, 1) }, { new Codel(2, 0), new Codel(2, 1) } };
            image[0, 0].Color = CodelColor.LightRed;
            image[1, 0].Color = CodelColor.Red;
            image[2, 0].Color = CodelColor.DarkMagenta;
            var stdout = new MemoryStream();
            var stdin = new MemoryStream();
            i.Interpret(image, stdout, stdin);
            using (var sr = new StreamReader(stdout))
            {
                stdout.Seek(0, SeekOrigin.Begin);
                MessageBox.Show(sr.ReadToEnd());
            }
        }
    }
}
