#load "BuildSystem/Steps.csx"
#load "BuildSystem/StyleDictionary.csx"
#load "BuildSystem/Repository.csx"
#load "BuildSystem/Dump.csx"
#load "BuildSystem/MobileFramework.csx"


private static string rootDir => Repository.RootDir();
private static string srcDir => Path.Combine(Repository.RootDir(),"src");
private static string outputDir => Path.Combine(Repository.RootDir(),"output");

Console.WriteLine("🎨 Generating Android and iOS resources");
//Generate native Android and iOS resources
await StyleDictionary.Build(srcDir);

//Use Android resources to generate XAML Resources
var colors = await StyleDictionary.GetAndroidColors(srcDir);

//Create mobile resources
MobileFramework.CreateResources(colors, outputDir);
