// ViewModels/RunSummaryViewModels.cs - Clean recreation for run tracking summaries
using Pack_Track.Models;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Pack_Track.ViewModels
{
    public class RunSummaryViewModel : BaseViewModel
    {
        private readonly Run _run;
        internal readonly Show _show; // Changed to internal so EquipmentIssueViewModel can access it
        private readonly List<Product> _allProducts;

        public RunSummaryViewModel(Run run, Show show, List<Product> allProducts)
        {
            _run = run;
            _show = show;
            _allProducts = allProducts;

            OutstandingIssues = new ObservableCollection<EquipmentIssueViewModel>();
            LoadOutstandingIssues();
        }

        public Run Run => _run;
        public ObservableCollection<EquipmentIssueViewModel> OutstandingIssues { get; }

        public bool HasOutstandingIssues => OutstandingIssues.Any();
        public int OutstandingCount => OutstandingIssues.Count;

        private void LoadOutstandingIssues()
        {
            OutstandingIssues.Clear();

            System.Diagnostics.Debug.WriteLine($"=== LoadOutstandingIssues for Run: {_run.Name} ===");
            System.Diagnostics.Debug.WriteLine($"Total AssetStatuses in show: {_show.AssetStatuses.Count}");

            // Debug: List all asset statuses
            foreach (var asset in _show.AssetStatuses)
            {
                System.Diagnostics.Debug.WriteLine($"Asset {asset.AssetNumber}: Status={asset.Status}, AssignedTo={asset.CurrentlyAssignedToActorId}");
            }

            // Get all currently checked out, damaged, or missing equipment
            var problematicAssets = _show.AssetStatuses.Where(a =>
                a.Status == EquipmentStatus.CheckedOut ||
                a.Status == EquipmentStatus.Missing ||
                a.Status == EquipmentStatus.Damaged).ToList();

            System.Diagnostics.Debug.WriteLine($"Found {problematicAssets.Count} problematic assets");

            foreach (var assetStatus in problematicAssets)
            {
                System.Diagnostics.Debug.WriteLine($"Processing asset: {assetStatus.AssetNumber} - Status: {assetStatus.Status}");

                var product = _allProducts.FirstOrDefault(p => p.Id == assetStatus.ProductId);
                var actor = _show.Cast.FirstOrDefault(a => a.Id == assetStatus.CurrentlyAssignedToActorId);

                System.Diagnostics.Debug.WriteLine($"Product found: {product?.Name ?? "NULL"}");
                System.Diagnostics.Debug.WriteLine($"Actor found: {actor?.DisplayName ?? "NULL"}");

                if (product != null)
                {
                    // Create a synthetic allocation and record for the summary
                    var allocation = new Allocation
                    {
                        ActorId = actor?.Id ?? Guid.Empty,
                        ProductId = product.Id,
                        AssetInfo = assetStatus.AssetNumber,
                        Actor = actor,
                        Product = product
                    };

                    var record = new CheckInOutRecord
                    {
                        AllocationId = allocation.Id,
                        CheckOutTime = assetStatus.LastStatusChange ?? DateTime.Now,
                        Status = assetStatus.Status switch
                        {
                            EquipmentStatus.CheckedOut => CheckInOutStatus.CheckedOut,
                            EquipmentStatus.Missing => CheckInOutStatus.Lost,
                            EquipmentStatus.Damaged => CheckInOutStatus.Damaged,
                            _ => CheckInOutStatus.CheckedOut
                        },
                        Allocation = allocation
                    };

                    var issue = new EquipmentIssueViewModel(record, allocation, actor, product, _run, this);
                    OutstandingIssues.Add(issue);
                    System.Diagnostics.Debug.WriteLine($"Added issue: {issue.Description}");
                }
            }

            System.Diagnostics.Debug.WriteLine($"Total outstanding issues: {OutstandingIssues.Count}");
            OnPropertyChanged(nameof(HasOutstandingIssues));
            OnPropertyChanged(nameof(OutstandingCount));
        }

        public void RefreshIssues()
        {
            LoadOutstandingIssues();
        }
    }

    public class AllRunsSummaryViewModel : BaseViewModel
    {
        private readonly Show _show;
        private readonly List<Product> _allProducts;

        public AllRunsSummaryViewModel(Show show, List<Product> allProducts)
        {
            _show = show;
            _allProducts = allProducts;

            IssuesByRun = new ObservableCollection<RunIssuesGroupViewModel>();
            LoadAllRunsIssues();
        }

        public ObservableCollection<RunIssuesGroupViewModel> IssuesByRun { get; }

        public int TotalIssuesCount => IssuesByRun.Sum(r => r.Issues.Count);

        private void LoadAllRunsIssues()
        {
            IssuesByRun.Clear();

            foreach (var run in _show.Runs.OrderBy(r => r.DateTime))
            {
                var runIssues = new List<EquipmentIssueDisplayViewModel>();

                // Get all records for this run that have issues
                var problemRecords = run.CheckInOutRecords.Where(r =>
                    r.Status == CheckInOutStatus.Lost ||
                    r.Status == CheckInOutStatus.Damaged ||
                    (r.Status == CheckInOutStatus.CheckedOut && r.CheckInTime == null)).ToList();

                foreach (var record in problemRecords)
                {
                    var allocation = _show.Scenes.SelectMany(s => s.Allocations)
                        .FirstOrDefault(a => a.Id == record.AllocationId);
                    if (allocation == null) continue;

                    var actor = _show.Cast.FirstOrDefault(a => a.Id == allocation.ActorId);
                    var product = _allProducts.FirstOrDefault(p => p.Id == allocation.ProductId);

                    if (actor != null && product != null)
                    {
                        var issueDisplay = new EquipmentIssueDisplayViewModel
                        {
                            ActorName = actor.DisplayName,
                            ProductName = product.Name,
                            AssetInfo = allocation.AssetInfo ?? "",
                            IssueType = record.Status,
                            StatusIcon = GetStatusIcon(record.Status),
                            Description = GetIssueDescription(actor, product, allocation, record.Status)
                        };

                        runIssues.Add(issueDisplay);
                    }
                }

                if (runIssues.Any())
                {
                    IssuesByRun.Add(new RunIssuesGroupViewModel
                    {
                        RunName = run.Name,
                        RunDate = run.DateTime,
                        Issues = new ObservableCollection<EquipmentIssueDisplayViewModel>(runIssues)
                    });
                }
            }

            OnPropertyChanged(nameof(TotalIssuesCount));
        }

        private string GetStatusIcon(CheckInOutStatus status)
        {
            return status switch
            {
                CheckInOutStatus.Lost => "❌",
                CheckInOutStatus.Damaged => "⚠️",
                CheckInOutStatus.CheckedOut => "📤",
                _ => "❓"
            };
        }

        private string GetIssueDescription(Actor actor, Product product, Allocation allocation, CheckInOutStatus status)
        {
            var assetInfo = !string.IsNullOrEmpty(allocation.AssetInfo) ? $" {allocation.AssetInfo}" : "";
            var productName = $"{product.Name}{assetInfo}";

            return status switch
            {
                CheckInOutStatus.Lost => $"{actor.DisplayName} lost {productName}",
                CheckInOutStatus.Damaged => $"{actor.DisplayName} damaged {productName}",
                CheckInOutStatus.CheckedOut => $"{actor.DisplayName} did not return {productName}",
                _ => $"{actor.DisplayName} - {productName} (unknown issue)"
            };
        }
    }

    public class RunIssuesGroupViewModel : BaseViewModel
    {
        public string RunName { get; set; } = string.Empty;
        public DateTime RunDate { get; set; }
        public ObservableCollection<EquipmentIssueDisplayViewModel> Issues { get; set; } = new();
    }

    public class EquipmentIssueDisplayViewModel : BaseViewModel
    {
        public string ActorName { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string AssetInfo { get; set; } = string.Empty;
        public CheckInOutStatus IssueType { get; set; }
        public string StatusIcon { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class EquipmentIssueViewModel : BaseViewModel
    {
        private readonly CheckInOutRecord _record;
        private readonly Allocation _allocation;
        private readonly Actor? _actor;
        private readonly Product _product;
        private readonly Run _run;
        private readonly RunSummaryViewModel _parent;

        public EquipmentIssueViewModel(CheckInOutRecord record, Allocation allocation, Actor? actor, Product product, Run run, RunSummaryViewModel parent)
        {
            _record = record;
            _allocation = allocation;
            _actor = actor;
            _product = product;
            _run = run;
            _parent = parent;

            ResolveIssueCommand = new RelayCommand(ResolveIssue);
        }

        public string StatusIcon
        {
            get
            {
                return _record.Status switch
                {
                    CheckInOutStatus.Lost => "❌",
                    CheckInOutStatus.Damaged => "⚠️",
                    CheckInOutStatus.CheckedOut => "📤",
                    _ => "❓"
                };
            }
        }

        public string Description
        {
            get
            {
                var assetInfo = !string.IsNullOrEmpty(_allocation.AssetInfo) ? $" {_allocation.AssetInfo}" : "";
                var productName = $"{_product.Name}{assetInfo}";
                var actorName = _actor?.DisplayName ?? "Unknown Actor";

                return _record.Status switch
                {
                    CheckInOutStatus.Lost => $"{actorName} lost {productName}",
                    CheckInOutStatus.Damaged => $"{actorName} damaged {productName}",
                    CheckInOutStatus.CheckedOut => $"{actorName} has not returned {productName}",
                    _ => $"{actorName} - {productName} (unknown issue)"
                };
            }
        }

        public ICommand ResolveIssueCommand { get; }

        private void ResolveIssue()
        {
            // Mark the issue as resolved by updating the asset status
            var show = _parent._show;
            var assetStatus = show.AssetStatuses.FirstOrDefault(a =>
                a.ProductId == _allocation.ProductId &&
                a.AssetNumber == _allocation.AssetInfo);

            if (assetStatus != null)
            {
                // Reset the asset status based on the current issue type
                switch (_record.Status)
                {
                    case CheckInOutStatus.CheckedOut:
                        assetStatus.Status = EquipmentStatus.CheckedIn;
                        assetStatus.CurrentlyAssignedToActorId = null;
                        break;
                    case CheckInOutStatus.Lost:
                    case CheckInOutStatus.Damaged:
                        assetStatus.Status = EquipmentStatus.Available;
                        assetStatus.CurrentlyAssignedToActorId = null;
                        break;
                }

                assetStatus.LastStatusChange = DateTime.Now;
            }

            // Refresh the parent summary
            _parent.RefreshIssues();
        }
    }
}