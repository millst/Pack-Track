// Views/SceneEditDialog.xaml.cs
using System.Windows;
using Pack_Track.Models;

namespace Pack_Track.Views
{
    public partial class SceneEditDialog : Window
    {
        public Scene Scene { get; set; }

        public SceneEditDialog(Scene scene)
        {
            InitializeComponent();
            Scene = scene;
            DataContext = Scene;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}