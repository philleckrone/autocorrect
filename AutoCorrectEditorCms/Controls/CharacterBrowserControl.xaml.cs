using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using AutoCorrectEditor.Services;

namespace AutoCorrectEditor.Controls
{
    public partial class CharacterBrowserControl : UserControl
    {
        public static readonly DependencyProperty DisplayFontProperty =
            DependencyProperty.Register("DisplayFont", typeof(FontFamily), typeof(CharacterBrowserControl),
                new PropertyMetadata(new FontFamily("Segoe UI")));

        public FontFamily DisplayFont
        {
            get { return (FontFamily)GetValue(DisplayFontProperty); }
            set { SetValue(DisplayFontProperty, value); }
        }

        public static readonly DependencyProperty DisplayFontSizeProperty =
            DependencyProperty.Register("DisplayFontSize", typeof(double), typeof(CharacterBrowserControl),
                new PropertyMetadata(16.0));

        public double DisplayFontSize
        {
            get { return (double)GetValue(DisplayFontSizeProperty); }
            set { SetValue(DisplayFontSizeProperty, value); }
        }

        public event Action<string> CharacterSelected;

        private List<CharacterItem> _blockItems = new List<CharacterItem>();
        private List<CharacterItem> _allItems   = new List<CharacterItem>();
        private DispatcherTimer     _searchTimer;

        public CharacterBrowserControl()
        {
            InitializeComponent();
            BlockCombo.ItemsSource = UnicodeBlockService.Blocks;
            BlockCombo.SelectedIndex = 0;
            LoadAllItems();
            _searchTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            _searchTimer.Tick += SearchTimer_Tick;
        }

        private void LoadAllItems()
        {
            _allItems = new List<CharacterItem>();
            foreach (UnicodeBlock block in UnicodeBlockService.Blocks)
            {
                for (int cp = block.Start; cp <= block.End; cp++)
                {
                    if (cp > 0xFFFF) continue;
                    char ch = (char)cp;
                    var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
                    if (cat == UnicodeCategory.OtherNotAssigned ||
                        cat == UnicodeCategory.Surrogate ||
                        cat == UnicodeCategory.PrivateUse)
                        continue;
                    _allItems.Add(new CharacterItem
                    {
                        Display   = GetDisplay(ch, cat),
                        Character = ch.ToString(),
                        Tooltip   = "U+" + cp.ToString("X4"),
                        CodePoint = cp,
                        Block     = block
                    });
                }
            }
        }

        private void BlockCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SearchBox.Text = string.Empty;
            LoadBlock(BlockCombo.SelectedItem as UnicodeBlock);
        }

        private void LoadBlock(UnicodeBlock block)
        {
            if (block == null) return;
            _blockItems = new List<CharacterItem>();

            for (int cp = block.Start; cp <= block.End; cp++)
            {
                if (cp > 0xFFFF) continue;

                char ch = (char)cp;
                var cat = CharUnicodeInfo.GetUnicodeCategory(ch);

                if (cat == UnicodeCategory.OtherNotAssigned ||
                    cat == UnicodeCategory.Surrogate ||
                    cat == UnicodeCategory.PrivateUse)
                    continue;

                _blockItems.Add(new CharacterItem
                {
                    Display   = GetDisplay(ch, cat),
                    Character = ch.ToString(),
                    Tooltip   = "U+" + cp.ToString("X4"),
                    CodePoint = cp
                });
            }

            ApplySearch(string.Empty);
        }

        private static string GetDisplay(char ch, UnicodeCategory cat)
        {
            if (cat == UnicodeCategory.NonSpacingMark ||
                cat == UnicodeCategory.EnclosingMark  ||
                cat == UnicodeCategory.SpacingCombiningMark)
            {
                return "◌" + ch;
            }
            if (cat == UnicodeCategory.Control || cat == UnicodeCategory.Format)
                return "[" + ((int)ch).ToString("X2") + "]";

            return ch.ToString();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchTimer.Stop();
            _searchTimer.Start();
        }

        private void SearchTimer_Tick(object sender, EventArgs e)
        {
            _searchTimer.Stop();
            ApplySearch(SearchBox.Text ?? string.Empty);
        }

        private const int MaxSearchResults = 200;

        private void ApplySearch(string text)
        {
            string fmt = Application.Current.Resources["Browser_CharsFormat"] as string ?? "{0} chars";

            if (string.IsNullOrWhiteSpace(text))
            {
                SearchResultsPanel.Visibility = Visibility.Collapsed;
                CharGrid.Visibility = Visibility.Visible;
                CharGrid.ItemsSource = _blockItems;
                CountLabel.Text = string.Format(fmt, _blockItems.Count);
                return;
            }

            CharGrid.Visibility = Visibility.Collapsed;
            SearchResultsPanel.Visibility = Visibility.Visible;

            string upper = text.ToUpperInvariant();
            var matched = _allItems
                .Where(i => i.Character.Contains(text) || i.Tooltip.Contains(upper))
                .Take(MaxSearchResults + 1)
                .ToList();
            bool truncated = matched.Count > MaxSearchResults;
            if (truncated)
                matched.RemoveAt(MaxSearchResults);

            var groups = new List<SearchResultGroup>();
            foreach (UnicodeBlock block in UnicodeBlockService.Blocks)
            {
                List<CharacterItem> items = matched.Where(i => i.Block == block).ToList();
                if (items.Count > 0)
                    groups.Add(new SearchResultGroup { BlockName = block.Name, Block = block, Items = items });
            }

            SearchResultsPanel.ItemsSource = groups;
            CountLabel.Text = truncated
                ? string.Format(fmt, MaxSearchResults) + "+"
                : string.Format(fmt, matched.Count);
        }

        private void BrowseBlock_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null) return;
            SearchResultGroup grp = btn.DataContext as SearchResultGroup;
            if (grp == null) return;

            if (BlockCombo.SelectedItem == grp.Block)
                SearchBox.Text = string.Empty;
            else
                BlockCombo.SelectedItem = grp.Block;
        }

        private void CharButton_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null && btn.Tag is string)
            {
                string ch = (string)btn.Tag;
                if (CharacterSelected != null)
                    CharacterSelected(ch);
            }
        }
    }

    public class CharacterItem
    {
        public string      Display   { get; set; }
        public string      Character { get; set; }
        public string      Tooltip   { get; set; }
        public int         CodePoint { get; set; }
        public UnicodeBlock Block    { get; set; }
    }

    public class SearchResultGroup
    {
        public string       BlockName { get; set; }
        public UnicodeBlock Block     { get; set; }
        public List<CharacterItem> Items { get; set; }
    }
}
