using System.Collections.ObjectModel;
using System.IO;

namespace RenamingTool.ViewModels
{
    internal class MainWindowViewModel : BindableBase
    {
        public ObservableCollection<RecordViewModel> Records { get; } = new();

        public int EditedItemsCount { get => _editedItemsCount; set => SetProperty(ref _editedItemsCount, value); }
        private int _editedItemsCount;

        public bool ThumbnailEnabled { get => _thumbnailEnabled; set => SetProperty(ref _thumbnailEnabled, value); }
        private bool _thumbnailEnabled;

        public double CellFontSize { get => _cellFontSize; set => SetProperty(ref _cellFontSize, value); }
        private double _cellFontSize = 20;

        public string SearchPattern
        {
            get => _searchPattern;
            set
            {
                var pattern = value?.Trim();
                if (string.IsNullOrWhiteSpace(value))
                {
                    pattern = "*.*";
                }
                SetProperty(ref _searchPattern, pattern);
            }
        }
        private string _searchPattern = "*.*";

        public SearchOption SearchOption { get => _searchOption; set => SetProperty(ref _searchOption, value); }
        private SearchOption _searchOption;


        public bool IncludesHiddenFiles { get => _includesHiddenFiles; set => SetProperty(ref _includesHiddenFiles, value); }
        private bool _includesHiddenFiles ;

        public bool IncludesReadOnlyFiles { get => _includesReadOnlyFiles; set => SetProperty(ref _includesReadOnlyFiles, value); }
        private bool _includesReadOnlyFiles;

        public int RecordsCount { get=>_recordsCount; set=>SetProperty(ref _recordsCount, value); }
        private int _recordsCount;
    }
}
