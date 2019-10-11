using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PietPad
{
    /// <summary>
    /// Interaction logic for ExportDialog.xaml
    /// </summary>
    public partial class ImportExportDialog : Window
    {
        private readonly bool isExport;
        public string FilePathResult = null;
        public int PixelsPerCodel = -1;
        public bool Successful { get => FilePathResult != null && PixelsPerCodel != -1 && PixelsPerCodel > 0 && PixelsPerCodel <= 1000; }
        public ImportExportDialog(bool isExport, string initialPath = "")
        {
            InitializeComponent();

            this.isExport = isExport;
            if (this.isExport)
            {
                importExportButton.Content = "Export";
                Title = "Export";
            }
            else
            {
                importExportButton.Content = "Import";
                Title = "Import";
            }
            filePathTextBox.Text = initialPath;
        }

        private void ChooseFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (isExport)
            {
                var sfd = new SaveFileDialog
                {
                    Title = "Export file",
                    DefaultExt = ".png",
                    Filter = "Portable Network Graphics (*.png)|*.png|All Files (*.*)|*.*",
                    ValidateNames = true,
                    InitialDirectory = filePathTextBox.Text
                };
                if (sfd.ShowDialog() == true)
                {
                    filePathTextBox.Text = sfd.FileName;
                }
            }
            else
            {
                var ofd = new OpenFileDialog
                {
                    Title = "Import file",
                    DefaultExt = ".png",
                    Filter = "Portable Network Graphics (*.png)|*.png|All Files (*.*)|*.*",
                    ValidateNames = true,
                    CheckFileExists = true,
                    InitialDirectory = filePathTextBox.Text
                };
                if (ofd.ShowDialog() == true)
                {
                    filePathTextBox.Text = ofd.FileName;
                }
            }
            importExportButton.Focus();
        }

        private void ImportExportButton_Click(object sender, RoutedEventArgs e)
        {
            FilePathResult = filePathTextBox.Text;
            if (ppcCounter.Value.HasValue) PixelsPerCodel = ppcCounter.Value.Value;
            DialogResult = Successful;
        }
    }
}
