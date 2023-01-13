#load "BuildSystem/Steps.csx"
#load "BuildSystem/Command.csx"
#load "BuildSystem/StyleDictionary.csx"
#load "BuildSystem/Repository.csx"
#load "BuildSystem/Dump.csx"
using System.Xml;

private static string rootDir => Repository.RootDir();
private static string srcDir => Path.Combine(Repository.RootDir(),"src");

//Generate native Android and iOS resources
Console.WriteLine("🎨 Generating Android and iOS resources");
(await Command.CaptureAsync("style-dictionary", "build", Path.Combine(rootDir, "src"))).Dump();

//Use Android resources to generate XAML Resources
var colors = await StyleDictionary.GetAndroidColors(srcDir);
//Write colors to XAML file and enum file
Dump.It(colors);
