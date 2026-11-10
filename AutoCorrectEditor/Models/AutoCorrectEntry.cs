using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace AutoCorrectEditor.Models
{
    public class AutoCorrectEntry : INotifyPropertyChanged
    {
        private string _searchPattern = string.Empty;
        private string _replacement = string.Empty;

        public string SearchPattern
        {
            get { return _searchPattern; }
            set
            {
                _searchPattern = DecodeUnicodeEscapes(value ?? string.Empty);
                OnPropertyChanged("SearchPattern");
            }
        }

        public string Replacement
        {
            get { return _replacement; }
            set
            {
                _replacement = DecodeUnicodeEscapes(value ?? string.Empty);
                OnPropertyChanged("Replacement");
                OnPropertyChanged("UnicodeValues");
            }
        }

        public string UnicodeValues
        {
            get { return FormatUnicodeValues(_replacement); }
        }

        private static string DecodeUnicodeEscapes(string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('\\') < 0)
                return text;

            var chars = new List<char>(text.Length);
            int i = 0;
            while (i < text.Length)
            {
                if (text[i] == '\\' && i + 6 <= text.Length &&
                    (text[i + 1] == 'u' || text[i + 1] == 'U') &&
                    IsHex4(text, i + 2))
                {
                    int codePoint = Convert.ToInt32(text.Substring(i + 2, 4), 16);
                    chars.Add((char)codePoint);
                    i += 6;
                }
                else
                {
                    chars.Add(text[i]);
                    i += 1;
                }
            }
            return new string(chars.ToArray());
        }

        private static bool IsHex4(string text, int start)
        {
            if (start + 4 > text.Length)
                return false;
            for (int j = start; j < start + 4; j++)
            {
                char c = text[j];
                bool isHex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
                if (!isHex)
                    return false;
            }
            return true;
        }

        private static string FormatUnicodeValues(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            var parts = new List<string>();
            int i = 0;
            while (i < text.Length)
            {
                int codePoint;
                if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                {
                    codePoint = char.ConvertToUtf32(text[i], text[i + 1]);
                    i += 2;
                }
                else
                {
                    codePoint = text[i];
                    i += 1;
                }
                parts.Add(codePoint.ToString("X4"));
            }
            return string.Join(" ", parts);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
    }
}
