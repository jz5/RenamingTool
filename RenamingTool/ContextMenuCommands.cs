using System.Windows;
using System.Windows.Input;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.UI.Xaml.Utility;

namespace RenamingTool
{
    internal static class ContextMenuCommands
    {

        #region Copy
        static ICommand copy;
        public static ICommand Copy
        {
            get
            {
                if (copy == null)
                    copy = new BaseCommand(OnCopyClicked);

                return copy;
            }
        }


        private static void OnCopyClicked(object obj)
        {
            if (obj is GridRecordContextMenuInfo)
            {
                var grid = (obj as GridRecordContextMenuInfo).DataGrid;
                grid.GridCopyPaste.Copy();
            }
        }

        #endregion

        #region Paste
        static ICommand paste;
        public static ICommand Paste
        {
            get
            {
                if (paste == null)
                    paste = new BaseCommand(OnPasteClicked, CanPaste);

                return paste;
            }
        }

        private static bool CanPaste(object obj)
        {
            if (Clipboard.GetDataObject() != null && (Clipboard.GetDataObject() as DataObject).ContainsText())
                return true;
            return false;
        }

        private static void OnPasteClicked(object obj)
        {
            if (obj is GridRecordContextMenuInfo)
            {
                var grid = (obj as GridRecordContextMenuInfo).DataGrid;
                grid.GridCopyPaste.Paste();
            }
        }


        #endregion

    }
}
