using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace RenamingTool.ViewModels
{
    internal class RecordViewModel : BindableBase, IEditableObject
    {
        public RecordViewModel(string file)
        {
            CreateOrUpdateValues(file);
        }

        private void CreateOrUpdateValues(string file)
        {
            FullPath = file;
            FolderPath = Path.GetDirectoryName(file);
            OriginalName = Path.GetFileNameWithoutExtension(file);
            CreationTime = File.GetCreationTime(file);
            LastWriteTime = File.GetLastWriteTime(file);

            var ext = Path.GetExtension(file);
            OriginalExtension = ext.StartsWith(".") ? ext[1..] : ext;

            NewName = OriginalName;
            NewExtension = OriginalExtension;
        }

        public void ResetEditingStatuses()
        {
            OnPropertyChanged(nameof(NameEdited));
            OnPropertyChanged(nameof(ExtensionEdited));

            AlternativeFullPath = null;
            Duplicated = false;
        }

        public void Rename()
        {
            // Create folder 
            var folder = Path.GetDirectoryName(NewFullPath);
            if (folder != null && !Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            // 
            if (File.Exists(NewFullPath))
            {
                CreateAlternativeName();
                File.Move(FullPath, AlternativeFullPath, false);
                CreateOrUpdateValues(AlternativeFullPath);
            }
            else
            {
                File.Move(FullPath, NewFullPath, false);
                CreateOrUpdateValues(NewFullPath);
            }
        }

        public void RetryToRename()
        {
            if (string.IsNullOrEmpty(AlternativeFullPath))
                return;

            if (File.Exists(NewFullPath))
                return;

            File.Move(AlternativeFullPath, NewFullPath, false);
            AlternativeFullPath = null;

            CreateOrUpdateValues(NewFullPath);
        }

        public string ThumbnailPath { get => _thumbnailPath; set => SetProperty(ref _thumbnailPath, value); }
        private string _thumbnailPath;

        public string PrefixText
        {
            get => _prefixText;
            set
            {
                if (SetProperty(ref _prefixText, value))
                {
                    OnPropertyChanged(nameof(PreviewName));
                }

            }
        }
        private string _prefixText;

        public string PreviewName => PrefixText + NewName + NewExtension;

        public string FullPath { get; set; }

        public string FolderPath { get; set; }

        public string OriginalName { get; set; }
        public string OriginalExtension { get; set; }


        public string NewName
        {
            get => _newName;
            set
            {
                if (SetProperty(ref _newName, value))
                {
                    OnPropertyChanged(nameof(NameEdited));
                    OnPropertyChanged(nameof(HasError));
                }
            }
        }
        private string _newName;

        public string NewExtension
        {
            get => _newExtension;
            set
            {
                if (SetProperty(ref _newExtension, value))
                {
                    OnPropertyChanged(nameof(ExtensionEdited));
                    OnPropertyChanged(nameof(HasError));
                }
            }
        }
        private string _newExtension;

        public DateTime CreationTime { get; set; }
        public DateTime LastWriteTime { get; set; }

        public bool NameEdited => OriginalName != NewName;
        public bool ExtensionEdited => OriginalExtension != NewExtension;

        public string NewFullPath => Path.GetFullPath(Path.Combine(FolderPath, CreateNewFileName()));

        private string CreateNewFileName() => NewName.Replace("/", @"\")
                                     + (string.IsNullOrWhiteSpace(NewExtension) ? "" : "." + NewExtension.Trim());
        public bool HasError
        {
            get
            {
                // 禁止文字チェック
                if (":*?\"<>|".Any(c => NewName.Contains(c) || NewExtension.Contains(c)))
                {
                    return true;
                }

                var file = CreateNewFileName();
                // 空白
                if (string.IsNullOrEmpty(file))
                {
                    return true;
                }

                return false;
            }
        }


        public bool Duplicated { get => _duplicated; set => SetProperty(ref _duplicated, value); }
        private bool _duplicated;

        public string AlternativeFullPath { get; set; }


        public void BeginEdit()
        {
        }

        public void CancelEdit()
        {
        }

        public void EndEdit()
        {
        }

        public void CreateAlternativeName()
        {
            var name = Path.GetFileNameWithoutExtension(NewFullPath);
            var folder = Path.GetDirectoryName(NewFullPath);

            while (true)
            {
                var m = Regex.Match(name, "\\s\\((?<n>\\d+)\\)$");
                if (m.Success)
                {
                    if (decimal.TryParse(m.Groups["n"].Value, out var n))
                    {
                        name = name[..^m.Groups[0].Value.Length] + m.Result($" ({n + 1})");
                    }
                    else
                    {
                        AlternativeFullPath = NewFullPath; // 作成失敗
                        break;
                    }
                }
                else
                {
                    name += " (1)";
                }

                var filename = name + (string.IsNullOrWhiteSpace(NewExtension) ? "" : "." + NewExtension.Trim());
                var path = Path.Combine(folder, filename);
                if (File.Exists(path))
                    continue;

                AlternativeFullPath = path;
                break;
            }
        }
    }
}
