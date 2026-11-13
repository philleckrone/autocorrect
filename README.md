# AutoCorrect Editor

*This page is the English user guide, [AutoCorrectEditor_UserGuide.rtf](AutoCorrectEditor/AutoCorrectEditor_UserGuide.rtf). The guide is also available in Spanish ([AutoCorrectEditor_UserGuide_sp.rtf](AutoCorrectEditor/AutoCorrectEditor_UserGuide_sp.rtf)) and French ([AutoCorrectEditor_UserGuide_fr.rtf](AutoCorrectEditor/AutoCorrectEditor_UserGuide_fr.rtf)).*

This program builds the list of key combinations that Paratext turns into special characters. The list is saved in a file called *autocorrect.txt*. For example, you might type `a'` and Paratext changes it to *`á`*.

## 1. Opening the program

**From Paratext (plugin):** open a project, then choose Tools > AutoCorrect Editor... The program opens that project's autocorrect.txt file, with its menus in the same language as Paratext's (English if Paratext uses a language the program does not have). If the project has no autocorrect.txt file yet, the table starts empty, and File > Save creates the file in the project's folder. The plugin must be installed once first (see below).

**From Paratext (Custom Tools menu):** if you have the version built into Paratext, open it from Paratext's Tools > Custom Tools > Autocorrect menu.

**Installing the plugin:** download Install_Phils_tools.exe from the Paratext_Plugins folder (if your browser warns about the file, choose to keep it) and close Paratext. Double-click Install_Phils_tools.exe. If Windows says it protected your PC, click “More info”, then “Run anyway” (the installer is not signed yet). Tick AutoCorrect Editor and click “Install / update ticked”. If Windows asks for permission, click Yes. The installer puts the plugin into every Paratext 9 installation on the computer, in plugins\AutoCorrectEditor, and checks that Paratext can load it. The same window later shows “Update available” when there is a newer version, updates the plugin, removes it (“Remove ticked”), and opens this guide (“User guide”). The plugin was made for Paratext 9.5 and may not work in older versions; the Custom Tools menu works in any Paratext 9.

## 2. The main window

The window has six parts:

- **Menu bar** (top): File and Edit menus.
- **Toolbar:** pick a font, a size, and a language for how the program looks on your screen.
- **Yellow banner:** reminds you to restart Paratext after you save.
- **Table** (middle): every entry you have made, with three columns: Type Keys, Output Character, and Unicode Value(s), the code of each output character (for example, 00E1).
- **Add Entry bar** (below the table): where you type new entries.
- **Character Browser** (bottom): a picker for special characters.

## 3. Adding an entry

Type the keys you want Paratext to watch for in the "Type sequence" box. Type or paste the special character you want in the "Output character" box. You can also click a character in the Character Browser to add it there. Click Add, or press Enter. The new entry appears in the table.

## 4. Changing an entry

Click the entry in the table. Its keys and character fill the Add Entry bar, and the Add button changes to Update. Change the text, then click Update (or press Enter) to save the change. Click Clear if you change your mind.

## 5. Removing an entry

Click the entry in the table. Then press the Delete key, or use Edit > Delete Entry.

## 6. Using the Character Browser

Pick a group of characters from the Block list. Or type in the Search box to find a character in any group by its shape or its code (for example, 00E0). Click any character button to add it into whichever box, Type Keys or Output Character, you clicked last. Click Hide or Show to hide or show the browser and give the table more room.

## 7. Font, size, and language

The Font and Size boxes only change how characters look on your screen. They do not change your saved data. The Preview box shows a sample right away. The Language box changes the words in the program's own menus and labels. Choices are English, Español, Français, Português, and 中文 (简体). When you open the program from the Paratext plugin, it starts in the same language as Paratext's menus.

## 8. Saving your file

- **File > Save:** saves to the file you already have open.
- **File > Save As** (Ctrl+Shift+S): saves to a new file.
- **File > New:** clears the table so you can start over.
- **File > Open:** opens a different *autocorrect.txt* file.

If you have changes you have not saved, the program asks whether to save them before it closes, opens another file, or starts a new one.

## 9. Restart Paratext

Paratext will not use your new changes until you restart it. That is why the yellow banner is always there to remind you.

## Quick tips

- Press Insert to jump straight to the Type sequence box.
- Press Delete to remove the entry you have selected.
- Press Enter in either box to add or update an entry.
