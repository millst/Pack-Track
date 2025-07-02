// ViewModels/MissingEquipmentViewModel.cs
using Pack_Track.ViewModels;
using System.Collections.ObjectModel;

namespace Pack_Track.ViewModels
{
    public class MissingEquipmentViewModel : BaseViewModel
    {
        public MissingEquipmentViewModel(List<EquipmentCardViewModel> missingItems)
        {
            MissingItems = new ObservableCollection<EquipmentCardViewModel>(missingItems);

            TotalCost = MissingItems.Sum(item => CalculateItemCost(item));
        }

        public ObservableCollection<EquipmentCardViewModel> MissingItems { get; }
        public decimal TotalCost { get; }

        private decimal CalculateItemCost(EquipmentCardViewModel item)
        {
            // For now, return a placeholder cost
            // In a full implementation, this would calculate based on allocated products
            return 100m; // Placeholder cost per missing item
        }
    }
}