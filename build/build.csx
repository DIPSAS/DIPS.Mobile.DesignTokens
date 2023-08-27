#load "BuildSystem/Steps.csx"
#load "BuildSystem/StyleDictionary.csx"
#load "BuildSystem/Repository.csx"
#load "BuildSystem/Dump.csx"
#load "BuildSystem/MobileFramework.csx"

private static string rootDir => Repository.RootDir();
private static string srcDir => Path.Combine(Repository.RootDir(),"src");
private static string outputDir => Path.Combine(Repository.RootDir(),"output");

AsyncStep generate = async () =>
{
    if(Directory.Exists(outputDir))
    {
        Directory.Delete(outputDir, true);
    }
    
    Directory.CreateDirectory(outputDir);

    Console.WriteLine("🎨 Generating Android and iOS resources");
    //Generate native Android and iOS resources
    await StyleDictionary.Build(srcDir);

    // Use Android resources to generate XAML Resources
    var colors = await StyleDictionary.GetAndroidColors(srcDir);

    // CreateSizes(); TODO: Generate sizes from a multiplier
    var sizes = await StyleDictionary.GetSizes(srcDir);

    var icons = GetIcons();

    var animations = GetAnimations();

    //Create mobile resources
    MobileFramework.CreateResources(colors, sizes,icons,animations, outputDir);

    //Move icons to output folder
    MoveIcons();

    //Move animations to output folder
    MoveAnimations();
};

var args = Args;
if(args.Count() == 0){
    await ExecuteSteps(new string[]{"help"});
    WriteLine("Please select steps to run:");
    var input = ReadLine();
    args = input.Split(' ');
}

await ExecuteSteps(args);

public static void MoveIcons()
{
    var resourceIconsDir = Path.Combine(outputDir, "dotnet", "maui", "Icons");
    if(!Directory.Exists(resourceIconsDir))
    {
        Directory.CreateDirectory(resourceIconsDir);
    }

    CopyDirectory(Path.Combine(srcDir, "tokens", "icons"), resourceIconsDir, true, true);

}

public static void MoveAnimations()
{
    var animationsDir = Path.Combine(outputDir, "dotnet", "maui", "Animations");
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