
using System.Diagnostics;
using System.Text;
using System;
using System.Xml;
using System.Globalization;

static class FileBuilder
{
    class ConfigType
    {
        public string name = "";

        // Properties
        public bool wholeProgramOptimization = false;
        public bool debugLibraries = false;
        public bool incrementalLink = false;
        
        // Definitions
        public string[] directives = { };
        public string subsystem = "";
        public string entryPointDirective = "";
        public bool optimizeReferences = false;
        public bool comdatFolding = false;
        public bool functionLevelLinking = false;
        
    }

    class PlatformType
    {
        public string[] directives = { };
        public string name = "";
    }

    class Library
    {
        public string name = "";

        public string dirInclude = "";

        public List<string> libDirs = new();
        public List<string> libDirsDebug = new();
        public List<string> libDirsRelease = new();
        public List<string> libFiles = new();
        public List<string> libFilesDebug = new();
        public List<string> libFilesRelease = new();

        public List<string> dllFilePaths = new();
        public List<string> dllFilePathsDebug = new();
        public List<string> dllFilePathsRelease = new();
    }

    public class MSBuildFileInfo
    {
        public string filename = "";
        public string defaultConfig = "";
        public string defaultPlatform = "";
    }

    private static void WriteWrappedElement(this XmlWriter w, string elementName, string attributeName, string attributeValue)
    {
        w.WriteStartElement(elementName);
        w.WriteAttributeString(attributeName, attributeValue);
        w.WriteEndElement();
    }

    public static MSBuildFileInfo MSBuild(string projectDir, string projectName, string[] dirs)
    {
        string fileName = projectName + ".vcxproj";
        string filePath = projectDir + "/" + fileName;

        XmlWriterSettings s = new XmlWriterSettings();
        s.Indent = true;
        s.NewLineOnAttributes = false;

        XmlWriter w = XmlWriter.Create(filePath, s);

        CT.WriteLine("Project File");
        CT.ColorWrite("Created: ", CT.Good);
        CT.ColorWriteLine(fileName, CT.Highlight);

        List<ConfigType> configs = new List<ConfigType>
        {
            new ConfigType()
            {
                name = "Debug",

                wholeProgramOptimization = false,
                debugLibraries = true,
                incrementalLink = true,

                directives = new string[] { "_DEBUG", "BUILD_DEBUG" },

            },

            new ConfigType()
            {
                name = "Release",

                wholeProgramOptimization = true,
                debugLibraries = false,
                incrementalLink = false,

                directives = new string[] { "NDEBUG", "BUILD_RELEASE", "BUILD_OPTIMIZED" },
                optimizeReferences = true,
                comdatFolding = true,
                functionLevelLinking = true
            }
        };

        List<PlatformType> platforms = new List<PlatformType>
        {
            new PlatformType()
            { 
                name = "x64",
                directives = new string[] { "BUILD_X64", "BUILD_WINDOWS" }
            }
        };





        // File writing

        w.WriteStartElement("Project", "http://schemas.microsoft.com/developer/msbuild/2003");

        w.WriteAttributeString("DefaultTargets", "Build");
        w.WriteAttributeString("ToolsVersion", "16.0");
        w.WriteAttributeString("ReplaceWildcardsInProjectItems", "True");
        w.WriteAttributeString("xmlns", "", null, "http://schemas.microsoft.com/developer/msbuild/2003");

        w.WriteComment("");
        w.WriteComment("Configurations and platforms");

        w.WriteStartElement("ItemGroup");
        foreach (ConfigType c in configs)
        {
            foreach (PlatformType p in platforms) 
            {
                w.WriteStartElement("ProjectConfiguration");
                w.WriteAttributeString("Include", c.name + "|" + p.name);
                w.WriteElementString("Configuration", c.name);
                w.WriteElementString("Platform", p.name);
                w.WriteEndElement(); // ProjectConfiguration
            }
        }
        w.WriteEndElement(); // ItemGroup



        // Properties

        w.WriteComment("");
        w.WriteComment("Property settings");

        w.WriteWrappedElement("Import", "Project", "$(VCTargetsPath)\\Microsoft.Cpp.default.props");

        w.WriteStartElement("PropertyGroup");
        w.WriteElementString("ConfigurationType", "Application");
        w.WriteElementString("PlatformToolset", "v143");
        w.WriteElementString("PreferredToolArchitecture", "x64");
        w.WriteEndElement(); // PropertyGroup

        foreach (ConfigType c in configs)
        {
            w.WriteStartElement("PropertyGroup");
            w.WriteAttributeString("Condition", "'$(Configuration)'=='" + c.name + "'");
            w.WriteElementString("UseDebugLibraries", c.debugLibraries.ToString());
            w.WriteElementString("WholeProgramOptimization", c.wholeProgramOptimization.ToString());
            w.WriteElementString("LinkIncremental", c.incrementalLink.ToString());
            w.WriteEndElement(); // PropertyGroup
        }

        w.WriteWrappedElement("Import", "Project", "$(VCTargetsPath)\\Microsoft.Cpp.props");

        w.WriteStartElement("PropertyGroup");
        w.WriteElementString("OutDir", "$(ProjectDir)Bin\\$(ProjectName)-$(Platform)-$(Configuration)\\");
        w.WriteElementString("IntDir", "$(ProjectDir)Int\\$(ProjectName)-$(Platform)-$(Configuration)\\");
        w.WriteEndElement();



        // Choose PCH parameters

        string[] pchParams = { "Use PCH", "PCH name", "PCH source file name" };
        string[,] pchPresets = 
        { 
            { "No", "None", "None" }, 
            { "Yes", "core.h", "core.cpp" },
            { "Yes", "pch.h", "pch.cpp" },
        };

        int pchChoice = 0;
        CT.ChoosePreset(ref pchChoice, pchParams, pchPresets);

        if (pchChoice > 0)
        {
            StreamWriter pchHeader = File.CreateText(projectDir + "\\" + dirs[0] + "\\" + pchPresets[pchChoice, 1]);
            pchHeader.WriteLine("#pragma once");
            pchHeader.Close();

            StreamWriter pchSource = File.CreateText(projectDir + "\\" + dirs[0] + "\\" + pchPresets[pchChoice, 2]);
            pchSource.WriteLine("#include \"" + pchPresets[pchChoice, 1] + "\"");
            pchSource.Close();

            CT.WriteLine("PCH");
            CT.ColorWriteLine("Created", CT.Good);

            CT.WriteLine("PCH source file");
            CT.ColorWriteLine("Created", CT.Good);
        }



        // Choose subsystem and entry

        string[] subsystemParams = { "Subsystem", "Entry point" };
        string[,] subsystemPresets =
        {
            { "Console", "main()" },
            { "Console", "main(int argc, char* argv[])" },
            { "Console", "wmain(int argc, wchar_t* argv[], wchar_t* envp[])" },
            { "Windows", "main()" },
            { "Windows", "main(int argc, char* argv[])" },
            { "Windows", "wmain(int argc, wchar_t* argv[], wchar_t* envp[])" },
            { "Windows", "WinMain(...)" },
            { "Windows", "wWinMain(...)" }
        };

        int subsystemChoice = 0;
        CT.ChoosePreset(ref subsystemChoice, subsystemParams, subsystemPresets);

        bool releaseSubsystemSwitch = false;

        string entryFilename = CT.GetString("Enter the desired name for the entry point source file: ");
        if (!entryFilename.Contains('.'))
        {
            entryFilename += ".cpp";
        }

        string entryFileTop = (pchChoice > 0) ? ("\n#include \"" + pchPresets[pchChoice, 1] + "\"\n") : ("\n");
 
        StreamWriter entryFile = File.CreateText(projectDir + "\\" + dirs[0] + "\\" + entryFilename);
        switch (subsystemChoice)
        {
            case 0:
            case 3:
                entryFile.WriteLine(entryFileTop + "\nint main()\n{\n\n\n\treturn 0;\n}");
                configs[0].subsystem = "Console";
                configs[0].entryPointDirective = "mainCRTStartup";
                break;
            case 1:
            case 4:
                entryFile.WriteLine(entryFileTop + "\nint main(int argc, char* argv[])\n{\n\n\n\treturn 0;\n}");
                configs[0].subsystem = "Console";
                configs[0].entryPointDirective = "mainCRTStartup";
                break;
            case 2:
            case 5:
                entryFile.WriteLine(entryFileTop + "\nint wmain(int argc, wchar_t* argv[], wchar_t* envp[])\n{\n\n\n\treturn 0;\n}");
                configs[0].subsystem = "Console";
                configs[0].entryPointDirective = "wmainCRTStartup";
                break;
            case 6:
                entryFile.WriteLine(entryFileTop + "#include <Windows.h>\n\nint __stdcall WinMain(\n\tHINSTANCE instance,\n\tHINSTANCE previousInstance,\n\tLPSTR arguments,\n\tint showCommand)\n{\n\n\n\treturn 0;\n}");
                configs[0].subsystem = "Windows";
                configs[0].entryPointDirective = "WinMainCRTStartup";
                break;
            case 7:
                entryFile.WriteLine(entryFileTop + "#include <Windows.h>\n\nint __stdcall wWinMain(\n\tHINSTANCE instance,\n\tHINSTANCE previousInstance,\n\tLPWSTR arguments,\n\tint showCommand)\n{\n\n\n\treturn 0;\n}");
                configs[0].subsystem = "Windows";
                configs[0].entryPointDirective = "wWinMainCRTStartup";
                break;
        }
        entryFile.Close();

        configs[1].subsystem = configs[0].subsystem;
        configs[1].entryPointDirective = configs[0].entryPointDirective;

        CT.WriteLine("Entry point file");
        CT.ColorWrite("Created: ", CT.Good);
        CT.ColorWriteLine(dirs[0] + "\\" + entryFilename, CT.Highlight);

        if (subsystemChoice < 3)
        {
            releaseSubsystemSwitch = CT.GetYN("Set subsytem to Windows (same entry point) for Release configuration? (Y/N) ");
            configs[1].subsystem = releaseSubsystemSwitch ? "Windows" : "Console";
            CT.WriteLine("Release subsystem");
            CT.ColorWriteLine(configs[1].subsystem, CT.Warning);
        }




        // Libraries

        List<Library> libraries = new List<Library>();

        Console.WriteLine();

        for (;;)
        {
            if (!CT.GetYN("Add a library? (Y/N) "))
            {
                break;
            }

            Library l = new Library();

            l.name = CT.GetString("Enter library name: " + dirs[3] + "\\");
            if (l.name.Last() == '\\')
            {
                l.name.Remove(l.name.Length - 1);
            }

            CT.WriteLine("Library name");
            CT.ColorWriteLine(l.name, CT.Highlight);

            string libraryPath = "$(ProjectDir)" + dirs[3] + "\\" + l.name + "\\";

            l.dirInclude = libraryPath + CT.GetString("Input path to the include folder: " + dirs[3] + "\\" + l.name + "\\");



            // Library folders

            string inputString;
            while ((inputString = CT.GetString("Input path to a lib-folder, or nothing. (Debug and Release): " + dirs[3] + "\\" + l.name + "\\", true, true)) != "")
            {
                l.libDirs.Add(libraryPath + inputString);
            }

            while ((inputString = CT.GetString("Input path to a lib-folder, or nothing. (Debug only): " + dirs[3] + "\\" + l.name + "\\", true, true)) != "")
            {
                l.libDirsDebug.Add(libraryPath + inputString);
            }

            while ((inputString = CT.GetString("Input path to a lib-folder, or nothing. (Release only): " + dirs[3] + "\\" + l.name + "\\", true, true)) != "")
            {
                l.libDirsRelease.Add(libraryPath + inputString);
            }



            // Library files

            while ((inputString = CT.GetString("Input name of a lib-file, or nothing. (Debug and Release): ", true, true)) != "")
            {
                l.libFiles.Add(inputString);
            }

            while ((inputString = CT.GetString("Input name of a lib-file, or nothing. (Debug only): ", true, true)) != "")
            {
                l.libFilesDebug.Add(inputString);
            }

            while ((inputString = CT.GetString("Input name of a lib-file, or nothing. (Release only): ", true, true)) != "")
            {
                l.libFilesRelease.Add(inputString);
            }



            // DLL:s

            while ((inputString = CT.GetString("Input path to a dll-folder, or nothing: " + dirs[3] + "\\" + l.name + "\\", true, true)) != "")
            {
                string dllFolderName = inputString;
                string dllFolderPath = libraryPath + inputString + "\\";

                while ((inputString = CT.GetString("Input name of a dll-file, or nothing. (Debug and Release): " + dirs[3] + "\\" + l.name + "\\" + dllFolderName + "\\", true, true)) != "")
                {
                    l.dllFilePaths.Add(dllFolderPath + inputString);
                }

                while ((inputString = CT.GetString("Input name of a dll-file, or nothing. (Debug only): " + dirs[3] + "\\" + l.name + "\\" + dllFolderName + "\\", true, true)) != "")
                {
                    l.dllFilePathsDebug.Add(dllFolderPath + inputString);
                }

                while ((inputString = CT.GetString("Input name of a dll-file, or nothing. (Release only): " + dirs[3] + "\\" + l.name + "\\" + dllFolderName + "\\", true, true)) != "")
                {
                    l.dllFilePathsRelease.Add(dllFolderPath + inputString);
                }
            }



            libraries.Add(l);
            Console.WriteLine();
        }





        // Definitions

        w.WriteComment("");
        w.WriteComment("Definition settings");

        w.WriteStartElement("ItemDefinitionGroup");

        w.WriteStartElement("ClCompile");
        w.WriteElementString("WarningLevel", "Level3");
        w.WriteElementString("SDLCheck", "True");
        w.WriteElementString("ConformanceMode", "True");
        w.WriteElementString("LanguageStandard", "stdcpp20");
        w.WriteElementString("FunctionLevelLinking", "True");
        if (pchChoice > 0)
        {
            w.WriteElementString("PrecompiledHeader", "Use");
            w.WriteElementString("PrecompiledHeaderFile", pchPresets[pchChoice, 1]);
        }
        w.WriteEndElement(); // ClCompile

        w.WriteStartElement("Link");
        w.WriteElementString("GenerateDebugInformation", "True");
        w.WriteEndElement(); // Link

        w.WriteEndElement(); // ItemDefinitionGroup

        foreach (ConfigType c in configs)
        {
            w.WriteStartElement("ItemDefinitionGroup");
            w.WriteAttributeString("Condition", "'$(Configuration)'=='" + c.name + "'");

            //w.WriteStartElement("ClCompile");
            //w.WriteEndElement(); // ClCompile

            w.WriteStartElement("Link");
            w.WriteElementString("SubSystem", c.subsystem);
            w.WriteElementString("EntryPointSymbol", c.entryPointDirective);
            w.WriteElementString("OptimizeReferences", c.optimizeReferences.ToString());
            w.WriteElementString("EnableCOMDATFolding", c.comdatFolding.ToString());
            w.WriteEndElement(); // Link

            w.WriteEndElement(); // ItemDefinitionGroup
        }



        // Preprocessor definitions

        w.WriteComment("");
        w.WriteComment("Preprocessor definitions");

        foreach (ConfigType c in configs)
        {
            foreach (PlatformType p in platforms)
            {
                string ppdString = c.directives[0];
                for (int i = 1; i < c.directives.Length; i++)
                {
                    ppdString += ";" + c.directives[i];
                }
                for (int i = 1; i < p.directives.Length; i++)
                {
                    ppdString += ";" + p.directives[i];
                }
                ppdString += ";%(PreprocessorDefinitions)";

                w.WriteStartElement("ItemDefinitionGroup");
                w.WriteAttributeString("Condition", "'$(Configuration)|$(Platform)'=='" + c.name + "|" + p.name +"'");

                w.WriteStartElement("ClCompile");
                w.WriteElementString("PreprocessorDefinitions", ppdString);
                w.WriteEndElement(); // ClCompile

                w.WriteEndElement(); // ItemDefinitionGroup
            }
        }



        // Post-build events

        w.WriteComment("");
        w.WriteComment("Post-build events");

        w.WriteStartElement("ItemDefinitionGroup");
        w.WriteStartElement("PostBuildEvent");
        string assetFolderName = CT.GetString("Enter the desired name for the asset output folder: ");
        CT.WriteLine("Asset output folder");
        CT.ColorWriteLine(assetFolderName, CT.Highlight);
        string commandString = "xcopy /y /e /q \"$(ProjectDir)Assets\" \"$(OutDir)" + assetFolderName + "\\\"";
        w.WriteElementString("Command", commandString);
        w.WriteEndElement(); // PostBuildEvent
        w.WriteEndElement(); // ItemDefinitionGroup



        // Library directives

        w.WriteComment("");
        w.WriteComment("Library directives");

        List<string> includes = new List<string>();

        List<string> dllFiles = new List<string>();
        List<string> dllFilesDebug = new List<string>();
        List<string> dllFilesRelease = new List<string>();

        List<string> libDirs = new List<string>();
        List<string> libDirsRelease = new List<string>();
        List<string> libDirsDebug = new List<string>();

        List<string> libFiles = new List<string>();
        List<string> libFilesDebug = new List<string>();
        List<string> libFilesRelease = new List<string>();

        void AddFiles(ref List<string> target, List<string> source, string desiredEnd)
        {
            foreach (string item in source)
            {
                if (!item.EndsWith(desiredEnd))
                {
                    target.Add(item + desiredEnd);
                }
                else
                {
                    target.Add(item);
                }
            }
        }

        void WriteLibraryContent(List<string> includePaths, List<string> libPaths, List<string> libs, List<string> dlls, string? conditionConfig)
        {
            w.WriteStartElement("ItemDefinitionGroup");
            if (conditionConfig != null)
            {
                w.WriteAttributeString("Condition", "'$(Configuration)'=='" + conditionConfig + "'");
            }

            w.WriteStartElement("ClCompile");
            string includeString = "";
            foreach (string includePath in includePaths)
            {
                includeString += includePath + ";";
            }
            w.WriteElementString("AdditionalIncludeDirectories", includeString + "%(AdditionalIncludeDirectories)");
            w.WriteEndElement(); // ClCompile

            w.WriteStartElement("Link");
            string libPathString = "";
            foreach (string libPath in libPaths)
            {
                libPathString += libPath + ";";
            }
            w.WriteElementString("AdditionalLibraryDirectories", libPathString + "%(AdditionalLibraryDirectories)");
            string libString = "";
            foreach (string lib in libs)
            {
                libString += lib + ";";
            }
            w.WriteElementString("AdditionalDependencies", libString + "%(AdditionalDependencies)");
            w.WriteEndElement(); // Link

            w.WriteStartElement("PostBuildEvent");
            string copyString = "";
            foreach (string dllFile in dlls)
            {
                copyString += "xcopy /y /e /q \"" + dllFile + "\" \"$(OutDir)\"\n";
            }
            w.WriteElementString("Command", copyString);
            w.WriteEndElement(); // PostBuildEvent

            w.WriteEndElement(); // ItemDefinitionGroup
        }

        foreach (Library l in libraries)
        {
            AddFiles(ref includes, new List<string> { l.dirInclude }, "\\");

            AddFiles(ref dllFiles, l.dllFilePaths, ".dll");
            AddFiles(ref dllFilesDebug, l.dllFilePathsDebug, ".dll");
            AddFiles(ref dllFilesRelease, l.dllFilePathsRelease, ".dll");

            AddFiles(ref libFiles, l.libFiles, ".lib");
            AddFiles(ref libFilesDebug, l.libFilesDebug, ".lib");
            AddFiles(ref libFilesRelease, l.libFilesRelease, ".lib");

            AddFiles(ref libDirs, l.libDirs, "\\");
            AddFiles(ref libDirsDebug, l.libDirsDebug, "\\");
            AddFiles(ref libDirsRelease, l.libDirsRelease, "\\");
        }

        WriteLibraryContent(includes, libDirs, libFiles, dllFiles, null);
        WriteLibraryContent(includes, libDirsDebug, libFilesDebug, dllFilesDebug, "Debug");
        WriteLibraryContent(includes, libDirsRelease, libFilesRelease, dllFilesRelease, "Release");



        // Files

        w.WriteComment("");
        w.WriteComment("File arguments");

        string[,] wildcards =
        {
            { "*.cpp", "*.c" }, // Source
            { "*.h", "*.hpp" }  // Header
        };

        w.WriteStartElement("ItemGroup");
        if (pchChoice > 0)
        {
            w.WriteStartElement("ClCompile");
            w.WriteAttributeString("Include", "$(ProjectDir)" + dirs[0] + "\\" + pchPresets[pchChoice, 2]);
            w.WriteElementString("PrecompiledHeader", "Create");
            w.WriteEndElement(); // ClCompile
        }
        for (int i = 0; i < wildcards.GetLength(0); i++)
        {
            w.WriteStartElement("_WildCardClCompile");
            w.WriteAttributeString("Include", "$(ProjectDir)" + dirs[0] + "\\" + wildcards[0, i]);
            if (pchChoice > 0)
            {
                w.WriteAttributeString("Exclude", "$(ProjectDir)" + dirs[0] + "\\" + pchPresets[pchChoice, 2]);
            }
            w.WriteEndElement(); // _WildCardClCompile
        }
        w.WriteEndElement(); // ItemGroup

        w.WriteStartElement("ItemGroup");
        for (int i = 0; i < wildcards.GetLength(1); i++)
        {
            w.WriteStartElement("_WildCardClInclude");
            w.WriteAttributeString("Include", "$(ProjectDir)" + dirs[0] + "\\" + wildcards[1, i]);
            w.WriteEndElement(); // _WildCardClInclude
        }
        w.WriteEndElement(); // ItemGroup

        w.WriteStartElement("Target");
        w.WriteAttributeString("Name", "AddWildCardItems");
        w.WriteAttributeString("AfterTargets", "BuildGenerateSources");
        w.WriteStartElement("ItemGroup");
        w.WriteWrappedElement("ClCompile", "Include", "@(_WildCardClCompile)");
        w.WriteEndElement(); // ItemGroup
        w.WriteStartElement("ItemGroup");
        w.WriteWrappedElement("ClInclude", "Include", "@(_WildCardClInclude)");
        w.WriteEndElement(); // ItemGroup
        w.WriteEndElement(); // Target

        w.WriteWrappedElement("Import", "Project", "$(VCTargetsPath)\\Microsoft.Cpp.Targets");

        w.WriteEndElement(); // Project


        
        // End

        w.Flush();
        w.Close();

        string text = File.ReadAllText(filePath);
        text = text.Replace("<!---->\r\n", "<!---->\r\n\r\n\r\n\r\n");
        File.WriteAllText(filePath, text);

        return new()
        {
            filename = fileName,
            defaultConfig = configs[0].name,
            defaultPlatform = platforms[0].name,
        };
    }



    public static void BATFiles(string projectDir, string projectName, MSBuildFileInfo info, string[] dirs)
    {
        string fileNameBuild = projectDir + "/build.bat";
        string fileNameRun = projectDir + "/run.bat";
        string fileNameDebug = projectDir + "/debug.bat";
        string fileNameDev = projectDir + "/dev.bat";

        string targetDirectory = ".\\" + dirs[1] + "\\" + projectName + "-" + info.defaultPlatform + "-%ConfigName%\\";
        string targetExecutable = projectName + ".exe";

        try
        {
            CT.WriteLine("Build.bat");
            StreamWriter fileBuild = new(fileNameBuild);
            fileBuild.WriteLine("@echo off\n");
            // fileBuild.WriteLine("echo Executing build.bat\n");
            fileBuild.WriteLine("if not defined VCToolsVersion call vcvarsall x64");
            fileBuild.WriteLine("if [%1] == [] (set ConfigName=Debug) else set ConfigName=%1\n");
            fileBuild.WriteLine("msbuild " + info.filename + " /p:configuration=%ConfigName% /v:m");
            fileBuild.Flush();
            fileBuild.Close();
            CT.ColorWriteLine("Created", CT.Good);
        }
        catch
        {
            CT.ColorWriteLine("Failed", CT.Error);
        }

        try
        {
            CT.WriteLine("Run.bat");
            StreamWriter fileRun = new(fileNameRun);
            fileRun.WriteLine("@echo off\n");
            fileRun.WriteLine("echo Executing run.bat\n");
            fileRun.WriteLine("if [%1] == [] (set ConfigName=Debug) else set ConfigName=%1\n");
            fileRun.WriteLine("start /D \"" + targetDirectory + "\" \"" + projectName + " (Running)\" \"" + targetDirectory + targetExecutable + "\"");
            fileRun.Flush();
            fileRun.Close();
            CT.ColorWriteLine("Created", CT.Good);
        }
        catch
        {
            CT.ColorWriteLine("Failed", CT.Error);
        }

        try
        {
            CT.WriteLine("Debug.bat");
            StreamWriter fileDebug = new(fileNameDebug);
            fileDebug.WriteLine("@echo off\n");
            fileDebug.WriteLine("echo Executing debug.bat\n");
            fileDebug.WriteLine("if [%1] == [] (set ConfigName=Debug) else set ConfigName=%1\n");
            fileDebug.WriteLine("pushd \"" + targetDirectory + "\"");
            fileDebug.WriteLine("call remedy " + targetExecutable);
            fileDebug.WriteLine("popd");
            fileDebug.Flush();
            fileDebug.Close();
            CT.ColorWriteLine("Created", CT.Good);
        }
        catch
        {
            CT.ColorWriteLine("Failed", CT.Error);
        }

        try
        {
            CT.WriteLine("Dev.bat");
            StreamWriter fileDev = new(fileNameDev);
            fileDev.WriteLine("@echo off\n");
            fileDev.WriteLine("call vcvarsall x64");
            fileDev.WriteLine("call 4ed . -W\n");
            fileDev.WriteLine("$wshell = New-Object -ComObject wscript.shell");
            fileDev.WriteLine("$wshell.AppActivate('4coder')");
            fileDev.Flush();
            fileDev.Close();
            CT.ColorWriteLine("Created", CT.Good);
        }
        catch
        {
            CT.ColorWriteLine("Failed", CT.Error);
        }
    }

    public static void ProjectFile4Coder(string projectDir, string projectName, MSBuildFileInfo info, string[] dirs)
    {
        string[] rows =
        {
            "version(2);",
            "project_name = \"" + projectName + "\";",
            "",
            "patterns = ",
            "{",
            "\t\"*.c\",",
            "\t\"*.cpp\",",
            "\t\"*.h\",",
            "\t\"*.hpp\",",
            "\t\"*.hlsl\",",
            "\t\"*.glsl\",",
            "\t\"*.bat\",",
            "\t\"*.4coder\",",
            "\t\"*.vcxproj\"",
            "};",
            "",
            "blacklist_patterns = ",
            "{ ",
            "\t\".*\" ",
            "};",
            "",
            "load_paths_base = ",
            "{",
            "\t{ .path = \"" + dirs[0] + "/\", .relative = true, .recursive = true, },",
            "};",
            "",
            "load_paths = ",
            "{",
            "\t.win = load_paths_base,",
            "\t.linux = load_paths_base,",
            "\t.mac = load_paths_base,",
            "};",
            "",
            "commands =",
            "{",
            "\t.build = ",
            "\t{ ",
            "\t\t.name = \"build\",",
            "\t\t.out = \"*build*\", ",
            "\t\t.footer_panel = true, ",
            "\t\t.save_dirty_files = true,",
            "\t\t.cursor_at_end = true,",
            "\t\t.win = \"build.bat\"",
            "\t},",
            "",
            "\t.run = ",
            "\t{ ",
            "\t\t.name = \"run\",",
            "\t\t.out = \"*run*\", ",
            "\t\t.footer_panel = true, ",
            "\t\t.save_dirty_files = false,",
            "\t\t.cursor_at_end = true,",
            "\t\t.win = \"run.bat\"",
            "\t},",
            "",
            "\t.debug = ",
            "\t{ ",
            "\t\t.name = \"debug\",",
            "\t\t.out = \"*run*\", ",
            "\t\t.footer_panel = true, ",
            "\t\t.save_dirty_files = false,",
            "\t\t.cursor_at_end = true,",
            "\t\t.win = \"debug.bat\"",
            "\t},",
            "",
            "\t.build_release =",
            "\t{",
            "\t\t.name = \"build_release\",",
            "\t\t.out = \"*build*\",",
            "\t\t.footer_panel = true,",
            "\t\t.save_dirty_files = true,",
            "\t\t.cursor_at_end = true,",
            "\t\t.win = \"build.bat Release\"",
            "\t},",
            "",
            "\t.run_release = ",
            "\t{ ",
            "\t\t.name = \"run_release\",",
            "\t\t.out = \"*run*\", ",
            "\t\t.footer_panel = true, ",
            "\t\t.save_dirty_files = false,",
            "\t\t.cursor_at_end = true,",
            "\t\t.win = \"run.bat Release\"",
            "\t},",
            "",
            "\t.debug_release = ",
            "\t{ ",
            "\t\t.name = \"debug_release\",",
            "\t\t.out = \"*run*\", ",
            "\t\t.footer_panel = true, ",
            "\t\t.save_dirty_files = false,",
            "\t\t.cursor_at_end = true,",
            "\t\t.win = \"debug.bat Release\"",
            "\t}",
            "};",
            "",
        };

        try
        {
            CT.WriteLine("4coder Project");
            File.WriteAllLines(projectDir + "\\project.4coder", rows);
            CT.ColorWriteLine("Created", CT.Good);
        }
        catch
        {
            CT.ColorWriteLine("Failed", CT.Error);
        }
    }
}