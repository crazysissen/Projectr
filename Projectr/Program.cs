
CT.Color();
CT.ColorWriteLine("-------------\n");

Run(args);

CT.Align();

CT.ColorWriteLine("\n-------------");
CT.Color();



static void Run(string[] args)
{
    // Initial logic and checks

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





    // Creating project directory

    CT.Align(24, '-');
    CT.WriteLine("Project Name");
    CT.ColorWriteLine(name, CT.Highlight);
    CT.WriteLine("Project Directory");

    if (Directory.Exists(projectDir))
    {
        // TODO: Test code only
        Directory.Delete(projectDir, true);
        Directory.CreateDirectory(projectDir);

        if (Directory.GetFileSystemEntries(projectDir).Length > 0)
        {
            CT.Align();
            CT.ColorWriteLine("Error\n", CT.Error);
            CT.ColorWriteLine("Project directory \"" + projectDir + "\" already exists and isn't empty. Aborting.", CT.Default);
            return;
        }

        CT.ColorWrite("Exists (Empty): ", CT.Warning);
    }
    else
    {
        Directory.CreateDirectory(projectDir);
        CT.ColorWrite("Created: ", CT.Good);
    }

    CT.ColorWriteLine(projectDir, CT.Highlight);

    Console.WriteLine();





    // Creating subdirectories

    string[] dirTypes =
    {
        "Source Directory", "Binary Directory", "Intermediate Directory", "Library Directory", "Assets Directory", "Workspace Directory"
    };

    string[,] dirPresets =
    {
        { "Source", "Bin", "Int", "Lib", "Assets", "Workspace" },
        { "source", "bin", "int", "lib", "assets", "workspace" },
        { "Source", "Binary", "Intermediate", "Library", "Assets", "Workspace" },
        { "source", "binary", "intermediate", "library", "assets", "workspace" },
        { "src", "bin", "int", "lib", "assets", "workspace" }
    };

    int dirPresetIndex = 0;

    CT.ChoosePreset(ref dirPresetIndex, dirTypes, dirPresets, CT.Highlight);

    string[] dirsRelative = new string[dirTypes.Length];
    string[] dirsFull = new string[dirTypes.Length];

    CT.WriteLine("Subdirectories");
    CT.ColorWriteLine("Created", CT.Good);

    Console.WriteLine();

    for (int i = 0; i < dirTypes.Length; i++)
    {
        dirsRelative[i] = dirPresets[dirPresetIndex, i];
        dirsFull[i] = projectDir + "\\" + dirPresets[dirPresetIndex, i];
        Directory.CreateDirectory(dirsFull[i]);
    }





    // Creating build instructions

    string[] buildParams =
    {
        "Build System"
    };

    string[,] buildPresets =
    {
        { "MSBuild" },
        { "Raw CL" }
    };

    int buildPresetIndex = 0;

    CT.ChoosePreset(ref buildPresetIndex, buildParams, buildPresets, CT.Good);

    if (buildPresetIndex == 0)
    {
        FileBuilder.MSBuild(projectDir, name, dirsRelative);
    }

    if (buildPresetIndex == 1)
    {
        CT.Align();
        CT.ColorWriteLine("Error\n", CT.Error);
        CT.ColorWriteLine("CL currently not supported. Aborting.", CT.Default);
        return;
    }
}



