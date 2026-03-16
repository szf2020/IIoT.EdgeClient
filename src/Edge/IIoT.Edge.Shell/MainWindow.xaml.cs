using System.Windows;
using System.Windows.Input;
using IIoT.Edge.Shell.ViewModels;

namespace IIoT.Edge.Shell
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainWindowViewModel viewModel)
        {
            InitializeComponent();
            this.DataContext = viewModel;
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
    }
}