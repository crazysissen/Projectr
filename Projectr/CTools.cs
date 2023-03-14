
using System.Numerics;

static class CT
{
    public static int Default      => 0;
    public static int Subtext      => 1;
    public static int Highlight    => 2;
    public static int Good         => 3;
    public static int Warning      => 4;
    public static int Error        => 5;

    static int s_currentThemeColor = 0;
    static ConsoleColor[,] s_theme =
    {
        { ConsoleColor.Gray, ConsoleColor.Black },          // Default
        { ConsoleColor.DarkGray, ConsoleColor.Black },      // Subtext
        { ConsoleColor.White, ConsoleColor.Black },         // Highlights
        { ConsoleColor.Green, ConsoleColor.Black },         // Good highlights
        { ConsoleColor.Yellow, ConsoleColor.Black },        // Warnings and notices
        { ConsoleColor.Red, ConsoleColor.Black },           // Warnings and errors
    };

    static int s_alignState = 0;
    static int s_alignOffset = 0;
    static char[] s_alignBuffer = new char[1];

    public static void Color(int themeColor = 0)
    {
        s_currentThemeColor = themeColor;
        Console.ForegroundColor = s_theme[themeColor, 0];
        Console.BackgroundColor = s_theme[themeColor, 1];
    }

    private static void Write<T>(T content, bool newLine)
    {
        if (!newLine)
        {
            Console.Write(content);
            return;
        }

        switch (s_alignState)
        {
            case 0:
                Console.WriteLine(content);
                break;

            case 1:
                Console.Write(content);
                Console.Write(' ');
                ConsoleColor bookmark = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write(s_alignBuffer, 0, Math.Max(s_alignOffset - Console.CursorLeft, 1));
                Console.ForegroundColor = bookmark;
                Console.Write(' ');
                s_alignState = 2;
                break;

            case 2:
                Console.WriteLine(content);
                s_alignState = 1;
                break;
        }
    }

    public static void Write<T>(T content)
        => Write(content, false);

    public static void WriteLine<T>(T content)
        => Write(content, true);

    public static void ColorWrite<T>(T content, int themeColor = 0)
    {
        int bookmark = s_currentThemeColor;
        Color(themeColor);
        Write(content, false);
        Color(bookmark);
    }

    public static void ColorWriteLine<T>(T content, int themeColor = 0)
    {
        int bookmark = s_currentThemeColor;
        Color(themeColor);
        Write(content, true);
        Color(bookmark);
    }

    public static void Align(int offset = 0, char separator = ' ')
    {
        if (offset == 0)
        {
            s_alignState = 0;
            s_alignOffset = 0;
            return;
        }

        s_alignState = 1;
        s_alignOffset = offset;

        s_alignBuffer = new char[offset];
        for (int i = 0; i < offset; i++)
        {
            s_alignBuffer[i] = separator;
        }
    }

    public static void ChoosePreset(ref int presetIndex, string[] categories, string[,] presets, int valueColor = 2)
    {
        if (s_alignOffset == 0)
        {
            throw new Exception("Call .Align before calling .ChoosePreset.");
        }

        bool chosen = false;

        do
        {
            int bookmark = Console.CursorTop;

            for (int i = 0; i < categories.Length; i++)
            {
                CT.WriteLine(categories[i]);
                CT.ColorWrite(presets[presetIndex, i], valueColor);
                CT.WriteLine(new string(' ', Console.WindowWidth - Console.CursorLeft));
            }

            Console.Write("Press Y to confirm this preset, or any other key to switch. ");

            if (Console.ReadKey().Key == ConsoleKey.Y)
            {
                chosen = true;
            }
            else
            {
                presetIndex = (presetIndex + 1) % presets.GetLength(0);
                Console.SetCursorPosition(0, bookmark);
            }
        }
        while (!chosen);

        Console.Write("\r                                                              \r");
    }

    public static string GetString(string prompt, bool clear = true, bool allowBlank = false)
    {
        (int x, int y) bookmark = Console.GetCursorPosition();

        for(;;)
        {
            Console.Write(prompt);
            string? returnValue = Console.ReadLine();

            if (returnValue == null)
            {
                returnValue = "";
            }

            if (returnValue != "" || allowBlank)
            {
                if (clear)
                {
                    Console.SetCursorPosition(bookmark.x, bookmark.y);
                    Console.Write(new string(' ', Console.WindowWidth - bookmark.x));
                    Console.SetCursorPosition(bookmark.x, bookmark.y);
                }

                return returnValue;
            }
        }
    }

    public static bool GetYN(string prompt, bool clear = true)
    {
        (int x, int y) bookmark = Console.GetCursorPosition();

        //string[] truePrompts = { "y", "yes", "t", "true" };
        //string[] falsePrompts = { "n", "no", "f", "false" };

        for (;;)
        {
            Console.Write(prompt);
            ConsoleKeyInfo input = Console.ReadKey();

            Console.SetCursorPosition(bookmark.x, bookmark.y);
            Console.Write(new string(' ', Console.WindowWidth - bookmark.x));
            Console.SetCursorPosition(bookmark.x, bookmark.y);

            if (input.Key == ConsoleKey.Y)
            {
                return true;
            }

            if (input.Key == ConsoleKey.N)
            {
                return false;
            }
        }
    }

}