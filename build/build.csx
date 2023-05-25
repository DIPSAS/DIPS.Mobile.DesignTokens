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

    //Use Android resources to generate XAML Resources
    var colors = await StyleDictionary.GetAndroidColors(srcDir);

    // CreateSizes(); TODO: Generate sizes from a multiplier
    var sizes = await StyleDictionary.GetSizes(srcDir);

    var icons = GetIcons();

    //Create mobile resources
    MobileFramework.CreateResources(colors, sizes,icons, outputDir);

    //Move icons to output folder
    MoveIcons();
};


AsyncStep createPR = async () =>
{
    var organization = "DIPSAS";
    var repoName = "DIPS.Mobile.UI";
    var repoDir = Path.Combine(outputDir, repoName);
    var prBranchName = "designTokens-update";

    //Clone repo if it does not exist in the output folder
    if(!Directory.Exists(repoDir))
    {
        WriteLine($"Cloning {repoName} to {repoDir}");
        await Command.ExecuteAsync("git", $"clone https://github.com/{organization}/{repoName}", outputDir);
    }

    
    if(!Directory.Exists(repoDir)) throw new Exception($"Something went wrong when cloning. Repo is not located at {repoDir}");

    //checkout new branch
    try
    {
        WriteLine($"Trying to create {prBranchName} in {repoDir}");
        await Command.ExecuteAsync("git", $"checkout -b {prBranchName}", repoDir);
    }catch(Exception) //If you have already created the branch, this will throw and you can simply checkout the branch
    {
        WriteLine($"Branch was found from before, checking out {prBranchName} in {repoDir}");
        await Command.ExecuteAsync("git", $"checkout {prBranchName}", repoDir);   
    }
    

    //Where is everything located

    //Generated resources
    var generatedAndroidDir = new DirectoryInfo(Path.Combine(outputDir, "android"));
    var generatedDotnetMauiDir = new DirectoryInfo(Path.Combine(outputDir, "dotnet", "maui"));

    var generatedAndroidColorFile = generatedAndroidDir.GetFiles().FirstOrDefault(f => f.Name.Equals("colors.xml"));
    var generatedDotnetMauiColorsDir = generatedDotnetMauiDir.GetDirectories().FirstOrDefault(d => d.Name.Equals("Colors"));
    var generatedDotnetMauiIconsDir = generatedDotnetMauiDir.GetDirectories().FirstOrDefault(d => d.Name.Equals("Icons"));
    var generatedDotnetMauiSizesDir = generatedDotnetMauiDir.GetDirectories().FirstOrDefault(d => d.Name.Equals("Sizes"));

    //The source repository paths
    var libraryPath = Path.Combine(repoDir, "src", "library", "DIPS.Mobile.UI");
    var libraryResourcesDir = new DirectoryInfo(Path.Combine(libraryPath, "Resources"));
    var libraryAndroidDir = new DirectoryInfo(Path.Combine(libraryPath, "Platforms", "Android"));

    var libraryDotnetMauiColorsDir = libraryResourcesDir.GetDirectories().FirstOrDefault(d => d.Name.Equals("Colors"));
    var libraryDotnetMauiIconsDir = libraryResourcesDir.GetDirectories().FirstOrDefault(d => d.Name.Equals("Icons"));
    var libraryDotnetMauiSizesDir = libraryResourcesDir.GetDirectories().FirstOrDefault(d => d.Name.Equals("Sizes"));


    //Copy to the correct folders in the branch
    generatedAndroidColorFile.CopyTo(Path.Combine(libraryAndroidDir.FullName, "Resources", "values", generatedAndroidColorFile.Name), true);
    CopyDirectory(generatedDotnetMauiColorsDir.FullName, libraryDotnetMauiColorsDir.FullName, true, true);
    CopyDirectory(generatedDotnetMauiIconsDir.FullName, libraryDotnetMauiIconsDir.FullName, true, true);
    CopyDirectory(generatedDotnetMauiSizesDir.FullName, libraryDotnetMauiSizesDir.FullName, true, true);

    //Commit changes
    WriteLine($"Resources moved to folders, commiting changes");
    await Command.ExecuteAsync("git", "add .", repoDir);
    //Have to use a file due to bug with dotnet script in line commit message
    var commitMessageFile = new FileInfo(Path.Combine(outputDir, "commitmessage.txt"));
    System.IO.File.Create(commitMessageFile.FullName).Close();
    using (StreamWriter outputFile = new StreamWriter(commitMessageFile.FullName, true))
    {
        outputFile.WriteLine("Resources update from DIPS.Mobile.DesignTokens");
    }
    await Command.ExecuteAsync("git", $"commit -F {commitMessageFile.FullName}", repoDir);

    WriteLine($"Pushing {prBranchName} to repository");
    await Command.ExecuteAsync("git", $"push origin {prBranchName}", repoDir);
};

var args = Args;
// args = new List<string>() { "createPR" };
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