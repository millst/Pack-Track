using System.Windows;

namespace Pack_Track.Views
{
    public partial class AssetNumberDialog : Window
    {
        public string AssetNumbers { get; set; } = string.Empty;

        public AssetNumberDialog()
        {
            InitializeComponent();
            DataContext = this;
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

        private void GenerateSequence_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AssetSequenceDialog();
            if (dialog.ShowDialog() == true)
            {
                AssetNumbers = dialog.GeneratedNumbers;
                AssetNumbersTextBox.Text = AssetNumbers;
            }
        }
    }
}