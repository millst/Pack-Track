// Views/ActorEditDialog.xaml.cs
using System.Windows;
using Pack_Track.Models;

namespace Pack_Track.Views
{
    public partial class ActorEditDialog : Window
    {
        public Actor Actor { get; set; }

        public ActorEditDialog(Actor actor)
        {
            InitializeComponent();
            Actor = actor;
            DataContext = Actor;
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