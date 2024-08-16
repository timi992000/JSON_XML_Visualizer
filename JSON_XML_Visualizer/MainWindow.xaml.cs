using ControlzEx.Theming;
using JSON_XML_Visualizer.Entities;
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

        private void __SelectedNodeChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is XTreeNode xNode && this.DataContext is MainWindowViewModel vm)
                vm.SelectedNode = xNode;
        }
    }
}