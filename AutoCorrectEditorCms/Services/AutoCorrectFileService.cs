using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using AutoCorrectEditor.Models;

namespace AutoCorrectEditor.Services
{
    public static class AutoCorrectFileService
    {
        public static ObservableCollection<AutoCorrectEntry> Read(string path, out List<string> headerLines)
        {
            var entries = new ObservableCollection<AutoCorrectEntry>();
            headerLines = new List<string>();
            bool pastHeader = false;

            Encoding enc = DetectEncoding(path);
            using (var reader = new StreamReader(path, enc, detectEncodingFromByteOrderMarks: true))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (!pastHeader && (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#")))
                    {
                        headerLines.Add(line);
                        continue;
                    }

                    int idx = line.IndexOf("-->");
                    if (idx >= 0)
                    {
                        pastHeader = true;
                        entries.Add(new AutoCorrectEntry
                        {
                            SearchPattern = line.Substring(0, idx),
                            Replacement   = line.Substring(idx + 3)
                        });
                    }
                    else if (!string.IsNullOrWhiteSpace(line))
                    {
                        pastHeader = true;
                    }
                }
            }

            return entries;
        }

        public static void Write(string path, IEnumerable<AutoCorrectEntry> entries, List<string> headerLines)
        {
            using (var writer = new StreamWriter(path, false, Encoding.Unicode))
            {
                if (headerLines != null)
                    foreach (var h in headerLines)
                        writer.Write(h + "\r\n");

                foreach (var entry in entries)
                    writer.Write(entry.SearchPattern + "-->" + entry.Replacement + "\r\n");
            }
        }

        private static Encoding DetectEncoding(string path)
        {
            var bom = new byte[4];
            using (var fs = File.OpenRead(path))
                fs.Read(bom, 0, 4);

            if (bom[0] == 0xFF && bom[1] == 0xFE) return Encoding.Unicode;          // UTF-16 LE
            if (bom[0] == 0xFE && bom[1] == 0xFF) return Encoding.BigEndianUnicode; // UTF-16 BE
            if (bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF) return Encoding.UTF8; // UTF-8 BOM
            return Encoding.UTF8; // default
        }
    }
}
