#r "nuget:Newtonsoft.Json, 13.0.2"
#load "BuildSystem/Steps.csx"
#load "BuildSystem/StyleDictionary.csx"
#load "BuildSystem/Repository.csx"
#load "BuildSystem/Dump.csx"
using Newtonsoft.Json;

private static string rootDir => Repository.RootDir();
private static string srcDir => Path.Combine(Repository.RootDir(), "src");
private static string variablesFilePath = Path.Combine(srcDir, "tokens/variables.json");
private static string outputDir => Path.Combine(Repository.RootDir(), "output");

AsyncStep generate = async () =>
{
    if(Directory.Exists(outputDir))
    {
        Directory.Delete(outputDir, true);
    }
    
    CreateDirectories();

    Console.WriteLine("🎨 Generating Android and iOS resources");
    //Generate native Android and iOS resources
    await StyleDictionary.Build(srcDir);

    await StyleDictionary.GenerateColors();

    MoveSizes(await StyleDictionary.GetSizes(srcDir));
    MoveIcons();
    MoveAnimations();
};

private static void CreateDirectories()
{
    Directory.CreateDirectory(outputDir);
    Directory.CreateDirectory(Path.Combine(outputDir, "tokens"));
    Directory.CreateDirectory(Path.Combine(outputDir, "tokens", "colors"));
    Directory.CreateDirectory(Path.Combine(outputDir, "tokens", "sizes"));
}

var args = Args;
if(args.Count() == 0){
    StyleDictionary.VariablesFilePath = variablesFilePath;
    StyleDictionary.OutputDirPath = outputDir;
    await ExecuteSteps(new string[]{"help"});
    WriteLine("Please select steps to run:");
    var input = ReadLine();
    args = input.Split(' ');
}

await ExecuteSteps(args);

public static async void MoveSizes(Dictionary<string,string> sizes)
{
    var resourceIconsDir = Path.Combine(outputDir, "tokens", "sizes");
    if(!Directory.Exists(resourceIconsDir))
    {
        Directory.CreateDirectory(resourceIconsDir);
    }
    var json = JsonConvert.SerializeObject(sizes, Formatting.Indented);
    var generatedSizes = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
    await System.IO.File.WriteAllTextAsync(resourceIconsDir+"/sizes.json", json);
}

public static async void MoveColors(Dictionary<string,string> colors)
{
    var resourceIconsDir = Path.Combine(outputDir, "tokens", "colors");
    if(!Directory.Exists(resourceIconsDir))
    {
        Directory.CreateDirectory(resourceIconsDir);
    }
    var json = JsonConvert.SerializeObject(colors, Formatting.Indented);
    await System.IO.File.WriteAllTextAsync(resourceIconsDir+"/colors.json", json);
}

public static void MoveIcons()
{
    var resourceIconsDir = Path.Combine(outputDir, "tokens", "icons");
    if(!Directory.Exists(resourceIconsDir))
    {
        Directory.CreateDirectory(resourceIconsDir);
    }

    CopyDirectory(Path.Combine(srcDir, "tokens", "icons"), resourceIconsDir, true, true);

}

public static void MoveAnimations()
{
    var animationsDir = Path.Combine(outputDir, "tokens", "animations");
    if(!Directory.Exists(animationsDir))
    {
        Directory.CreateDirectory(animationsDir);
    }

    CopyDirectory(Path.Combine(srcDir, "tokens", "animations"), animationsDir, true, true);

}

public static Dictionary<string, string> GetIcons()
{
    var iconsPath = Path.Combine(srcDir, "tokens", "icons");
     // Get information about the source directory
    var dir = new DirectoryInfo(iconsPath);

    // Check if the source directory exists
    if (!dir.Exists)
        throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

    // Cache directories before we start copying
    DirectoryInfo[] dirs = dir.GetDirectories();
    var iconNames = new Dictionary<string, string>();
    foreach (FileInfo file in dir.EnumerateFiles())
    {
        var iconName = file.Name.Replace(".svg", "");
        iconNames.Add(iconName, iconName+".png");
    }
    return iconNames;
}

public static Dictionary<string, string> GetAnimations()
{
    var animationsPath = Path.Combine(srcDir, "tokens", "animations");
     // Get information about the source directory
    var dir = new DirectoryInfo(animationsPath);

    // Check if the source directory exists
    if (!dir.Exists)
        throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

    // Cache directories before we start copying
    DirectoryInfo[] dirs = dir.GetDirectories();
    var animationNames = new Dictionary<string, string>();
    foreach (FileInfo file in dir.EnumerateFiles())
    {
        var iconName = file.Name.Replace(".json", "");
        animationNames.Add(iconName, iconName+".json");
    }
    return animationNames;
}

static void CopyDirectory(string sourceDir, string destinationDir, bool recursive=false, bool overwriteFiles=false)
{
    // Get information about the source directory
    var dir = new DirectoryInfo(sourceDir);

    // Check if the source directory exists
    if (!dir.Exists)
        throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

    // Cache directories before we start copying
    DirectoryInfo[] dirs = dir.GetDirectories();

    // Create the destination directory
    Directory.CreateDirectory(destinationDir);

    // Get the files in the source directory and copy to the destination directory
    foreach (FileInfo file in dir.GetFiles())
    {
        string targetFilePath = Path.Combine(destinationDir, file.Name);
        file.CopyTo(targetFilePath, true);
    }

    // If recursive and copying subdirectories, recursively call this method
    if (recursive)
    {
        foreach (DirectoryInfo subDir in dirs)
        {
            string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
            CopyDirectory(subDir.FullName, newDestinationDir, true);
        }
    }
}