using System.Windows;
using Pack_Track.ViewModels;

namespace Pack_Track.Views
{
    public partial class MissingEquipmentDialog : Window
    {
        public MissingEquipmentDialog(List<EquipmentCardViewModel> missingItems)
        {
            InitializeComponent();

            var viewModel = new MissingEquipmentViewModel(missingItems);
            DataContext = viewModel;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
