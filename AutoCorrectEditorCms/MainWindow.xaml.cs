using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AutoCorrectEditor.Models;
using AutoCorrectEditor.Services;
using Microsoft.Win32;

namespace AutoCorrectEditor
{
    public partial class MainWindow : Window
    {
        public static readonly RoutedUICommand SaveAsCommand = new RoutedUICommand(
            "Save As", "SaveAs", typeof(MainWindow),
            new InputGestureCollection { new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift) });

        private ObservableCollection<AutoCorrectEntry> _entries = new ObservableCollection<AutoCorrectEntry>();
        private List<string> _headerLines = new List<string>();
        private string _currentPath = null;
        private bool _isDirty = false;
        private bool _isEditingCell = false;
        private bool _browserVisible = true;
        private double _savedBrowserHeight = 210;
        private AutoCorrectEntry _editingEntry = null;
        private TextBox _lastFocusedInputBox = null;

        public MainWindow()
        {
            InitializeComponent();
            EntriesGrid.ItemsSource = _entries;
            _entries.CollectionChanged += Entries_CollectionChanged;
            PopulateFontCombo();
            PopulateFontSizeCombo();
            PopulateLanguageCombo();
            UpdateLocalizedColumnHeaders();
            UpdateLocalizedDynamic();
            UpdateStatus();
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private static string GetStr(string key)
        {
            object val = Application.Current.Resources[key];
            return val as string ?? key;
        }

        // ── Font and language toolbar ──────────────────────────────────────────

        private void PopulateFontCombo()
        {
            var fontNames = Fonts.SystemFontFamilies
                .Select(f => f.Source)
                .OrderBy(s => s)
                .ToList();
            FontCombo.ItemsSource = fontNames;

            int idx = fontNames.IndexOf("Charis SIL");
            FontCombo.SelectedIndex = idx >= 0 ? idx : 0;
        }

        private void PopulateFontSizeCombo()
        {
            var sizes = new List<double> { 10, 11, 12, 13, 14, 16, 18, 20, 22, 24, 28, 32, 36, 48 };
            FontSizeCombo.ItemsSource = sizes;
            FontSizeCombo.SelectedItem = 18.0;
        }

        private void PopulateLanguageCombo()
        {
            var languages = new List<LanguageOption>
            {
                new LanguageOption { DisplayName = "English",      Culture = "en" },
                new LanguageOption { DisplayName = "Español",      Culture = "es" },
                new LanguageOption { DisplayName = "Français",     Culture = "fr" },
                new LanguageOption { DisplayName = "Português",    Culture = "pt" },
                new LanguageOption { DisplayName = "中文 (简体)",  Culture = "zh-Hans" }
            };
            LanguageCombo.ItemsSource = languages;
            LanguageCombo.DisplayMemberPath = "DisplayName";

            LanguageCombo.SelectionChanged -= LanguageCombo_SelectionChanged;
            LanguageCombo.SelectedIndex = 0;
            LanguageCombo.SelectionChanged += LanguageCombo_SelectionChanged;
        }

        private void FontCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string name = FontCombo.SelectedItem as string;
            if (!string.IsNullOrEmpty(name))
                Resources["OutputFont"] = new FontFamily(name);
        }

        private void FontSizeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FontSizeCombo.SelectedItem != null)
                Resources["OutputFontSize"] = (double)FontSizeCombo.SelectedItem;
        }

        private void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LanguageOption opt = LanguageCombo.SelectedItem as LanguageOption;
            if (opt == null) return;
            App.SwitchLanguage(opt.Culture);
            UpdateLocalizedColumnHeaders();
            UpdateLocalizedDynamic();
            UpdateStatus();
        }

        private void UpdateLocalizedColumnHeaders()
        {
            if (EntriesGrid.Columns.Count >= 3)
            {
                EntriesGrid.Columns[0].Header = GetStr("Column_TypeKeys");
                EntriesGrid.Columns[1].Header = GetStr("Column_OutputCharacter");
                EntriesGrid.Columns[2].Header = GetStr("Column_UnicodeValues");
            }
        }

        private void UpdateLocalizedDynamic()
        {
            AddBtn.Content = _editingEntry != null
                ? GetStr("Btn_Update")
                : GetStr("Btn_Add");
            ToggleBrowserBtn.Content = _browserVisible
                ? GetStr("Browser_Hide")
                : GetStr("Browser_Show");
        }

        // ── Dirty tracking ─────────────────────────────────────────────────────

        private void Entries_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (AutoCorrectEntry item in e.NewItems)
                    item.PropertyChanged += Entry_PropertyChanged;

            if (e.OldItems != null)
                foreach (AutoCorrectEntry item in e.OldItems)
                    item.PropertyChanged -= Entry_PropertyChanged;

            MarkDirty();
        }

        private void Entry_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            MarkDirty();
        }

        private void MarkDirty()
        {
            _isDirty = true;
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            string appTitle = GetStr("App_Title");
            string dirty = _isDirty ? "* " : "";
            string file  = _currentPath != null
                ? _currentPath
                : GetStr("Status_Unsaved");
            string entriesText = string.Format(GetStr("Status_EntriesFormat"), _entries.Count);
            StatusText.Text = dirty + file + "  —  " + entriesText;
            if (_currentPath != null)
                Title = dirty + appTitle + " — " + Path.GetFileName(_currentPath);
            else
                Title = dirty + appTitle;
        }

        // ── File operations ────────────────────────────────────────────────────

        private void LoadFile(string path)
        {
            _entries.CollectionChanged -= Entries_CollectionChanged;
            foreach (AutoCorrectEntry en in _entries)
                en.PropertyChanged -= Entry_PropertyChanged;
            _entries.Clear();

            List<string> headers;
            var loaded = AutoCorrectFileService.Read(path, out headers);
            _headerLines = headers;
            foreach (var entry in loaded)
            {
                entry.PropertyChanged += Entry_PropertyChanged;
                _entries.Add(entry);
            }

            _entries.CollectionChanged += Entries_CollectionChanged;
            _currentPath = path;
            _isDirty = false;
            UpdateStatus();
        }

        private bool SaveToPath(string path)
        {
            try
            {
                AutoCorrectFileService.Write(path, _entries, _headerLines);
                _currentPath = path;
                _isDirty = false;
                UpdateStatus();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(GetStr("Dlg_SaveErrorBody") + "\n" + ex.Message,
                    GetStr("Dlg_SaveErrorTitle"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private bool PromptSaveIfDirty()
        {
            if (!_isDirty) return true;
            var result = MessageBox.Show(
                GetStr("Dlg_UnsavedBody"),
                GetStr("Dlg_UnsavedTitle"),
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)  return DoSave();
            if (result == MessageBoxResult.No)   return true;
            return false;
        }

        private bool DoSave()
        {
            if (_currentPath != null) return SaveToPath(_currentPath);
            return DoSaveAs();
        }

        private bool DoSaveAs()
        {
            var dlg = new SaveFileDialog
            {
                Title      = GetStr("Dlg_SaveTitle"),
                Filter     = "AutoCorrect files (*.txt)|*.txt|All files (*.*)|*.*",
                DefaultExt = "txt",
                FileName   = _currentPath != null
                    ? Path.GetFileName(_currentPath)
                    : "autocorrect.txt"
            };
            if (_currentPath != null)
                dlg.InitialDirectory = Path.GetDirectoryName(_currentPath);

            if (dlg.ShowDialog() == true)
                return SaveToPath(dlg.FileName);
            return false;
        }

        // ── Command handlers ───────────────────────────────────────────────────

        private void Always_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void New_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (!PromptSaveIfDirty()) return;
            _entries.CollectionChanged -= Entries_CollectionChanged;
            foreach (AutoCorrectEntry en in _entries)
                en.PropertyChanged -= Entry_PropertyChanged;
            _entries.Clear();
            _entries.CollectionChanged += Entries_CollectionChanged;
            _headerLines.Clear();
            _currentPath = null;
            _isDirty = false;
            UpdateStatus();
        }

        private void Open_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (!PromptSaveIfDirty()) return;
            var dlg = new OpenFileDialog
            {
                Title  = GetStr("Dlg_OpenTitle"),
                Filter = "AutoCorrect files (*.txt)|*.txt|All files (*.*)|*.*"
            };
            if (dlg.ShowDialog() == true)
                LoadFile(dlg.FileName);
        }

        private void Save_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DoSave();
        }

        private void SaveAs_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DoSaveAs();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!PromptSaveIfDirty()) e.Cancel = true;
        }

        // ── Entry editing ──────────────────────────────────────────────────────

        private void AddEntry_Click(object sender, RoutedEventArgs e)
        {
            CommitNewEntry();
        }

        private void CommitNewEntry()
        {
            if (string.IsNullOrEmpty(PatternBox.Text))
            {
                PatternBox.Focus();
                return;
            }
            if (string.IsNullOrEmpty(OutputBox.Text))
            {
                OutputBox.Focus();
                return;
            }

            if (_editingEntry != null)
            {
                _editingEntry.SearchPattern = PatternBox.Text;
                _editingEntry.Replacement   = OutputBox.Text;
                _editingEntry = null;
                EntriesGrid.SelectedIndex = -1;
            }
            else
            {
                var entry = new AutoCorrectEntry
                {
                    SearchPattern = PatternBox.Text,
                    Replacement   = OutputBox.Text
                };
                _entries.Add(entry);
                EntriesGrid.ScrollIntoView(entry);
            }

            PatternBox.Clear();
            OutputBox.Clear();
            PatternBox.Focus();
        }

        private void ClearEntry_Click(object sender, RoutedEventArgs e)
        {
            _editingEntry = null;
            EntriesGrid.SelectedIndex = -1;
            PatternBox.Clear();
            OutputBox.Clear();
            PatternBox.Focus();
        }

        private void DeleteEntry_Click(object sender, RoutedEventArgs e)
        {
            AutoCorrectEntry entry = EntriesGrid.SelectedItem as AutoCorrectEntry;
            if (entry != null)
                _entries.Remove(entry);
        }

        private void InputBox_GotFocus(object sender, RoutedEventArgs e)
        {
            _lastFocusedInputBox = sender as TextBox;
        }

        private void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                CommitNewEntry();
        }

        // ── DataGrid edit tracking ─────────────────────────────────────────────

        private void EntriesGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            _isEditingCell = true;
        }

        private void EntriesGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            _isEditingCell = false;
        }

        private void EntriesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AutoCorrectEntry entry = EntriesGrid.SelectedItem as AutoCorrectEntry;
            if (entry != null)
            {
                _editingEntry  = entry;
                PatternBox.Text = entry.SearchPattern;
                OutputBox.Text  = entry.Replacement;
                AddBtn.Content  = GetStr("Btn_Update");
            }
            else
            {
                _editingEntry  = null;
                AddBtn.Content = GetStr("Btn_Add");
            }
        }

        private void EntriesGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (_isEditingCell) return;

            if (e.Key == Key.Delete)
            {
                AutoCorrectEntry entry = EntriesGrid.SelectedItem as AutoCorrectEntry;
                if (entry != null)
                {
                    _entries.Remove(entry);
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Insert)
            {
                PatternBox.Focus();
                e.Handled = true;
            }
        }

        // ── Character browser ──────────────────────────────────────────────────

        private void CharBrowser_CharacterSelected(string character)
        {
            TextBox target = (_lastFocusedInputBox == PatternBox) ? PatternBox : OutputBox;
            target.Text += character;
            target.CaretIndex = target.Text.Length;
            target.Focus();
        }

        private void ToggleBrowser_Click(object sender, RoutedEventArgs e)
        {
            _browserVisible = !_browserVisible;
            if (_browserVisible)
            {
                BrowserRow.Height = new GridLength(_savedBrowserHeight);
                ToggleBrowserBtn.Content = GetStr("Browser_Hide");
            }
            else
            {
                _savedBrowserHeight = BrowserRow.ActualHeight;
                BrowserRow.Height = new GridLength(0);
                ToggleBrowserBtn.Content = GetStr("Browser_Show");
            }
        }

        public void OpenFile(string path)
        {
            LoadFile(path);
        }
    }

    public class LanguageOption
    {
        public string DisplayName { get; set; }
        public string Culture     { get; set; }
    }
}
