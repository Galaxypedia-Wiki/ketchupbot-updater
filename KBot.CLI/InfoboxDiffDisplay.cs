using Spectre.Console;

namespace KBot.CLI;

internal static class InfoboxDiffDisplay
{
    public static void Write(string oldInfobox, string newInfobox)
    {
        // Wikitext construction escapes dollars for replacement; display actual text.
        string[] before = Normalize(oldInfobox);
        string[] after = Normalize(newInfobox.Replace("$$", "$"));
        var lengths = new int[before.Length + 1, after.Length + 1];
        for (int i = before.Length - 1; i >= 0; i--)
            for (int j = after.Length - 1; j >= 0; j--)
                lengths[i, j] = before[i] == after[j]
                    ? lengths[i + 1, j + 1] + 1
                    : Math.Max(lengths[i + 1, j], lengths[i, j + 1]);

        AnsiConsole.WriteLine("Infobox diff (red: removed, green: added)");
        int oldIndex = 0, newIndex = 0;
        while (oldIndex < before.Length || newIndex < after.Length)
        {
            if (oldIndex < before.Length && newIndex < after.Length && before[oldIndex] == after[newIndex])
            {
                WriteLine("  ", before[oldIndex++], Color.Grey);
                newIndex++;
            }
            else if (oldIndex < before.Length &&
                     (newIndex == after.Length || lengths[oldIndex + 1, newIndex] >= lengths[oldIndex, newIndex + 1]))
                WriteLine("- ", before[oldIndex++], Color.Red);
            else
                WriteLine("+ ", after[newIndex++], Color.Green);
        }
    }

    private static string[] Normalize(string text) => text.Replace("\r\n", "\n").Split('\n');

    private static void WriteLine(string prefix, string line, Color color)
    {
        AnsiConsole.Write(new Text(prefix + line + Environment.NewLine, new Style(color)));
    }
}
