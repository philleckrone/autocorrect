using System;
using System.IO;
using System.Windows;

namespace AutoCorrectEditor
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            string filePath = null;

            // The launcher (autocorrecteditor.py) passes the project's autocorrect.txt. An older
            // branch that read a target path out of a .cms file given here was never used and
            // could point the editor's Save anywhere, so it was removed.
            if (e.Args.Length >= 1 && File.Exists(e.Args[0]))
                filePath = e.Args[0];

            var window = new MainWindow();
            MainWindow = window;
            window.Show();

            if (filePath != null)
            {
                string captured = filePath;
                window.Dispatcher.BeginInvoke(new Action(delegate()
                {
                    window.OpenFile(captured);
                }));
            }
        }

        public static void SwitchLanguage(string culture)
        {
            var dicts = Current.Resources.MergedDictionaries;
            ResourceDictionary toRemove = null;
            foreach (ResourceDictionary d in dicts)
            {
                if (d.Source != null && d.Source.OriginalString.Contains("Strings."))
                {
                    toRemove = d;
                    break;
                }
            }
            if (toRemove != null)
                dicts.Remove(toRemove);

            var newDict = new ResourceDictionary();
            newDict.Source = new Uri("Resources/Strings." + culture + ".xaml", UriKind.Relative);
            dicts.Add(newDict);
        }
    }
}
