using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace aedt_launcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static readonly RoutedUICommand QuitCommand = new RoutedUICommand("Quit", "QuitCommand", typeof(MainWindow));

        aedt_launcher.FileClass icepak = new FileClass();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string[] args = System.Environment.GetCommandLineArgs();

            this.AllowDrop = true;

            this.textBox_Filename.IsReadOnly = true;
            this.textBox_Type.IsReadOnly = true;
            this.textBox_Version.IsReadOnly = true;

            this.button_Extract.IsEnabled = false;
            this.button_Launch.IsEnabled = false;

            if (args.Length > 1)
            {
                checkFile(args[1..]);
            }
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            string[]? files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files == null) return;

            checkFile(files);
        }

        private void Window_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void button_Extract_Click(object sender, RoutedEventArgs e)
        {
            icepak.ExtractSAversion();
            this.textBox_Type.Text = icepak.FileType;
            this.textBox_Version.Text = icepak.FileVersion;

            if (icepak.FileType == "Error")
            {
                this.button_Extract.IsEnabled = false;
                this.button_Launch.IsEnabled = false;
            }
        }

        private void button_Launch_Click(object sender, RoutedEventArgs e)
        {
            icepak.launchApplication();
        }

        private void app_Exit()
        {
            Application.Current.Shutdown();
        }

        private void checkFile(string[] files)
        {
            if (files != null)
            {
                foreach (var path in files)
                {
                    this.button_Extract.IsEnabled = false;
                    this.button_Launch.IsEnabled = false;
                    icepak.CheckFile(path);
                    this.textBox_Filename.Text = icepak.FileName;
                    this.textBox_Type.Text = icepak.FileType;
                    this.textBox_Version.Text = icepak.FileVersion;

                    if (this.textBox_Type.Text != "None")
                    {
                        this.button_Launch.IsEnabled = true;
                        if (this.textBox_Type.Text == "ICEPAK Packfile")
                        {
                            this.button_Extract.IsEnabled = true;
                        }
                    }
                }
            }
        }

        private void titleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void clsButton_Click(object sender, RoutedEventArgs e)
        {
            this.app_Exit();
        }

        private void QuitCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            this.app_Exit();
        }

        private void minButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
    }
}
