// Views/RunEditDialog.xaml.cs
using System.Windows;
using Pack_Track.Models;

namespace Pack_Track.Views
{
    public partial class RunEditDialog : Window
    {
        public Run Run { get; set; }

        public RunEditDialog(Run run)
        {
            InitializeComponent();
            Run = run;
            DataContext = Run;
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