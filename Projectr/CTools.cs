
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
    static char s_alignSeparator = ' ';

    public static void Color(int themeColor = 0)
    {
        s_currentThemeColor = themeColor;
        Console.ForegroundColor = s_theme[themeColor, 0];
        Console.BackgroundColor = s_theme[themeColor, 1];
    }

    private static void Write<T>(T content, bool newLine)
    {
        if (newLine)
        {
            switch (s_alignState)
            {
                case 0:
                    Console.WriteLine(content);
                    break;

                case 1:
                    Console.Write(content);
                    Console.Write(' ');
                    Console.Write(" ".Insert * 2);
                    Console.CursorLeft
                    break;

                case 2:
                    Console.WriteLine(content);
                    s_alignState = 1;
                    break;
            }
        }
    

        Console.Write(content);

        switch (s_alignState)
        {
            case 0:
                break;

            case 1:

                break;

            case 2:
                break;
        }
    }

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
        s_alignSeparator = separator;
    }

}