using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Win32;
using RenamingTool.Properties;
using RenamingTool.ViewModels;
using Syncfusion.Data.Extensions;
using Syncfusion.SfSkinManager;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.Windows.Shared;
using RecordViewModel = RenamingTool.ViewModels.RecordViewModel;

namespace RenamingTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ChromelessWindow
    {
        private readonly MainWindowViewModel _viewModel;

        public MainWindow()
        {
            System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("ja");
            SfSkinManager.SetTheme(this, new Theme { ThemeName = Settings.Default.ThemeName, ScrollBarMode = ScrollBarMode.Compact });

            _viewModel = new MainWindowViewModel();

            DataContext = _viewModel;
            InitializeComponent();

            Width = Settings.Default.WindowWidth;
            Height = Settings.Default.WindowHeight;

            //try
            //{
            //    var path = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath;
            //    var file = Path.Combine(Path.GetDirectoryName(path), "datagrid.xml");
            //    if (File.Exists(file))
            //    {
            //        using var stream = File.Open(file, FileMode.Open);
            //        DataGrid.Deserialize(stream);
            //    }
            //}
            //catch (Exception)
            //{
            //    // Do nothing
            //}

            Columns = DataGrid.Columns.ToList();

#if DEBUG
            //AddFiles(@"\\Mac\Home\Downloads", "*.*", SearchOption.TopDirectoryOnly);
#endif
        }

        public MainWindow(string[] args) : this()
        {
            AddFiles(args);
        }


        private void RefreshColumns()
        {
            DataGrid.GridColumnSizer.ResetAutoCalculationforAllColumns();
            DataGrid.GridColumnSizer.Refresh();
        }

        private void AddFiles(IEnumerable<string> args)
        {
            foreach (var a in args)
            {
                var attr = File.GetAttributes(a);

                if (attr.HasFlag(FileAttributes.Directory))
                {
                    var files = Directory.GetFiles(a, _viewModel.SearchPattern, _viewModel.SearchOption);
                    foreach (var file in files)
                    {
                        CreateRecord(file);
                    }
                }
                else
                    CreateRecord(a);
            }

            _viewModel.RecordsCount = _viewModel.Records.Count;
        }

        private void CreateRecord(string file)
        {
            // 追加済みがチェック
            if (_viewModel.Records.Any(r => r.FullPath == file))
                return;

            // 隠しファイル
            if (!_viewModel.IncludesHiddenFiles)
            {
                var attr = File.GetAttributes(file);
                if ((attr & FileAttributes.Hidden) == FileAttributes.Hidden)
                {
                    return;
                }
            }

            // 読み取り専用ファイル
            if (!_viewModel.IncludesReadOnlyFiles)
            {
                var attr = File.GetAttributes(file);
                if ((attr & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    return;
                }
            }

            var r = new RecordViewModel(file);
            _viewModel.Records.Add(r);
        }

        private void DataGrid_OnCurrentCellBeginEdit(object? sender, CurrentCellBeginEditEventArgs e)
        {
            // Cancel the editing for filter row cell in OrderID Column
            if (e.Column.MappingName == nameof(RecordViewModel.FullPath) &&
                DataGrid.IsFilterRowIndex(e.RowColumnIndex.RowIndex))
                e.Cancel = true;
        }

        private void DataGrid_OnCurrentCellActivated(object? sender, CurrentCellActivatedEventArgs e)
        {
            // キー入力でアクティブになったセルを編集モードにする
            if (DataGrid.SelectionController.CurrentCellManager.CurrentCell is { IsEditing: false } &&
                e.ActivationTrigger == ActivationTrigger.Keyboard)
            {
                var cell = DataGrid.SelectionController.CurrentCellManager.CurrentCell;
                DataGrid.ClearSelections(true);
                DataGrid.SelectionController.CurrentCellManager.CurrentCell = cell;
                DataGrid.SelectionController.CurrentCellManager.BeginEdit();
            }
        }


        private void MainWindow_OnDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) is not string[] files)
                return;

            AddFiles(files);
            RefreshColumns();
        }

        private void DataGrid_OnCurrentCellEndEdit(object? sender, CurrentCellEndEditEventArgs e)
        {
            if (e.RowColumnIndex.IsEmpty)
                return;

            CheckForDuplicates();

            _viewModel.EditedItemsCount = _viewModel.Records.Count(r => r.NameEdited || r.ExtensionEdited);
        }

        private void DataGrid_OnPasteGridCellContent(object? sender, GridCopyPasteCellEventArgs e)
        {
            if (e.ClipBoardValue is not string text)
                return;

            if (e.RowData is not RecordViewModel record)
                return;

            switch (e.Column.MappingName)
            {
                case nameof(RecordViewModel.NewName):
                    record.NewName = text;
                    e.Handled = true;
                    break;
                case nameof(RecordViewModel.NewExtension):
                    record.NewExtension = text;
                    e.Handled = true;
                    break;
            }

            if (!e.Handled)
                return;

            CheckForDuplicates();
            _viewModel.EditedItemsCount = _viewModel.Records.Count(r => r.NameEdited || r.ExtensionEdited);
        }


        private void CheckForDuplicates()
        {
            // Edited items
            var editedRecords = _viewModel.Records.Where(r => (r.NameEdited || r.ExtensionEdited));
            if (!editedRecords.Any())
                return;

            // Reset flags
            var duplicatedRecords = _viewModel.Records.Where(r => r.Duplicated);
            foreach (var r in duplicatedRecords)
            {
                r.Duplicated = false;
            }

            // Duplicated items
            var dictionary = new Dictionary<string, RecordViewModel>();
            var duplicatedSet = new HashSet<RecordViewModel>();
            foreach (var r in _viewModel.Records)
            {
                var key = r.NewFullPath.ToUpperInvariant();
                if (dictionary.ContainsKey(key))
                {
                    duplicatedSet.Add(r);
                    duplicatedSet.Add(dictionary[key]);
                    continue;
                }
                dictionary.Add(key, r);
            }

            foreach (var r in duplicatedSet)
            {
                r.Duplicated = true;
            }
        }

        //private void PrefixTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        //{
        //    var textBox = sender as TextBox;
        //    _viewModel.Records.ForEach(r => r.PrefixText = textBox.Text);
        //}

        private void ThumbnailCheckBox_OnClick(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;

            _viewModel.Records.ForEach(r => r.ThumbnailPath = _viewModel.ThumbnailEnabled ? r.FullPath : null);
        }


        public List<GridColumn> Columns;

        private void ResetColumnsButton_OnClick(object sender, RoutedEventArgs e)
        {
            DataGrid.Columns.Clear();
            // Restoring the original columns order
            foreach (var column in Columns)
            {
                DataGrid.Columns.Add(column);
                if (!double.IsNaN(column.Width))
                    column.Width = double.NaN;
            }


            DataGrid.GridColumnSizer.Refresh();
        }

        private void SwitchThemeButton_OnClick(object sender, RoutedEventArgs e)
        {
            var theme = SfSkinManager.GetTheme(this);
            var newThemeName = theme.ThemeName == "FluentDark" ? "FluentLight" : "FluentDark";

            Settings.Default.ThemeName = newThemeName;

            SfSkinManager.SetTheme(this, new Theme { ThemeName = newThemeName, ScrollBarMode = ScrollBarMode.Compact });
        }

        private void MainWindow_OnClosed(object? sender, EventArgs e)
        {
            try
            {
                WindowState = WindowState.Normal;
                Settings.Default.WindowWidth = Width;
                Settings.Default.WindowHeight = Height;
                Settings.Default.Save();
            }
            catch (Exception)
            {
                // Do nothing
            }

            //var path = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath;
            //var folder = Path.GetDirectoryName(path);
            //if (folder != null && !Directory.Exists(folder))
            //    Directory.CreateDirectory(folder);

            //using var stream = File.Create(Path.Combine(folder, "datagrid.xml"));
            //var options = new SerializationOptions
            //{
            //    SerializeFiltering = false,
            //};
            //DataGrid.Serialize(stream, options);

        }

        private void OpenFilesDialogButton_OnClick(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "すべてのファイル (*.*)|*.*",
                CheckFileExists = true,
                Multiselect = true
            };

            if (_viewModel.SearchPattern != "*.*")
            {
                openFileDialog.Filter =
                    $"{_viewModel.SearchPattern}|{_viewModel.SearchPattern}|{openFileDialog.Filter}";
            }


            if (openFileDialog.ShowDialog() != true)
                return;

            AddFiles(openFileDialog.FileNames);
        }

        private void RenameSplitButton_OnClick(object sender, RoutedEventArgs e)
        {
            TryRename();
        }

        private bool TryRename()
        {
            var invalidRecords = _viewModel.Records.Where(r => r.HasError || r.Duplicated);
            if (invalidRecords.Any())
            {
                MessageBox.Show("エラーを解決してください。", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Edited items
            var records = _viewModel.Records.Where(r => r.NameEdited || r.ExtensionEdited).ToList();
            if (!records.Any())
                return false;

            DataGrid.BeginInit();
            try
            {
                var errors = new List<string>();

                // Rename
                foreach (var r in records)
                {
                    try
                    {
                        r.Rename();
                    }
                    catch (Exception ex)
                    {
                        errors.Add(string.IsNullOrEmpty(r.AlternativeFullPath)
                            ? $"オリジナル:{r.FullPath}\t新しい名前:{r.NewFullPath}\t詳細:{ex.Message}"
                            : $"オリジナル:{r.FullPath}\t別の名前:{r.AlternativeFullPath}\t詳細:{ex.Message}");
                    }
                }

                // Retry
                foreach (var r in records.Where(r => !string.IsNullOrEmpty(r.AlternativeFullPath)))
                {
                    try
                    {
                        r.RetryToRename();
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"別の名前:{r.AlternativeFullPath}\t新しい名前:{r.NewFullPath}\t詳細:{ex.Message}");
                    }
                }

                // Refresh
                foreach (var r in records)
                {
                    r.ResetEditingStatuses();
                }

                if (errors.Any())
                {
                    MessageBox.Show(string.Join("\r\n", errors), "名前の変更に失敗した項目があります", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            finally
            {
                DataGrid.EndInit();
                DataGrid.View.Refresh();
                _viewModel.ThumbnailEnabled = false;
                _viewModel.EditedItemsCount = 0;
            }
        }

        private void RenameAndCloseMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (TryRename())
            {
                Close();
            }
        }

        private void ResetMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            var records = _viewModel.Records.Where(r => r.NameEdited || r.ExtensionEdited);
            if (!records.Any())
                return;

            if (MessageBox.Show("すべての編集をリセットします。", "", MessageBoxButton.OKCancel, MessageBoxImage.Question) !=
                MessageBoxResult.OK)
                return;

            foreach (var r in _viewModel.Records)
            {
                r.NewName = r.OriginalName;
                r.NewExtension = r.OriginalExtension;
                r.Duplicated = false;
            }
            _viewModel.EditedItemsCount = 0;

        }

        private void DeleteMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            var records = _viewModel.Records.Where(r => r.NameEdited || r.ExtensionEdited);
            if (records.Any())
            {
                if (MessageBox.Show("すべてリストから削除します。", "", MessageBoxButton.OKCancel, MessageBoxImage.Question) !=
                    MessageBoxResult.OK)
                    return;
            }

            _viewModel.Records.Clear();
            _viewModel.EditedItemsCount = 0;
            _viewModel.RecordsCount = _viewModel.Records.Count;
            _viewModel.ThumbnailEnabled = false;
        }

        private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });
            e.Handled = true;
        }
    }
}
