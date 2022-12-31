
CT.Color();
CT.ColorWriteLine("-------------\n");

Main(args);

CT.ColorWriteLine("\n-------------");
CT.Color();



void Main(string[] args)
{
    for (int i = 0; i < args.Length; i++)
    {
        args[i] = args[i].Trim('/', ' ', '-');
    }

    if (args.Length != 1 || args[0] == "help")
    {
        CT.ColorWriteLine("Error\n", CT.Error);
        CT.ColorWriteLine("Usage:", CT.Subtext);
        CT.ColorWrite("projectr ProjectName   ", CT.Highlight);
        CT.ColorWriteLine("Create a new project (initiate sequence).", CT.Subtext);
        return;
    }

    string name = args[0];
    string currentDir = Directory.GetCurrentDirectory();
    string projectDir = currentDir + "\\" + name;

    if (Directory.Exists(projectDir) && Directory.GetFileSystemEntries(projectDir).Length > 0)
    {
        CT.ColorWriteLine("Error\n", CT.Error);
        CT.ColorWriteLine("Project directory \"" + projectDir + "\" already exists and isn't empty. Aborting.", CT.Default);
        return;
    }

    Directory.CreateDirectory(projectDir);

    string[,] dirPresets =
    {
        { "Source", "Bin", "Int", "Lib", "Assets", "Workspace" }
    };



}