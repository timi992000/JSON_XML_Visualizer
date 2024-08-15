using ControlzEx.Theming;
using MahApps.Metro.Controls;
using System.Windows;

namespace JSON_XML_Visualizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            DataContext = new MainWindowViewModel();
            ThemeManager.Current.ChangeTheme(Application.Current, "Dark.Purple");
            InitializeComponent();
        }
    }
}