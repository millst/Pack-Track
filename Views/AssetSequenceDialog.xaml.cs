using System.Windows;
using System.Text;

namespace Pack_Track.Views
{
    public partial class AssetSequenceDialog : Window
    {
        public string GeneratedNumbers { get; private set; } = string.Empty;

        public AssetSequenceDialog()
        {
            InitializeComponent();
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var prefix = PrefixTextBox.Text.Trim();
                var startNumber = int.Parse(StartNumberTextBox.Text);
                var endNumber = int.Parse(EndNumberTextBox.Text);

                if (endNumber < startNumber)
                {
                    MessageBox.Show("End number must be greater than or equal to start number.", "Invalid Range");
                    return;
                }

                var sb = new StringBuilder();
                for (int i = startNumber; i <= endNumber; i++)
                {
                    if (sb.Length > 0) sb.AppendLine();
                    sb.Append($"{prefix}{i}");
                }

                GeneratedNumbers = sb.ToString();
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating sequence: {ex.Message}", "Error");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}