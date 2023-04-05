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
    Console.WriteLine("🎨 Generating Android and iOS resources");
    //Generate native Android and iOS resources
    await StyleDictionary.Build(srcDir);

    //Use Android resources to generate XAML Resources
    var colors = await StyleDictionary.GetAndroidColors(srcDir);

    var sizes = await StyleDictionary.GetSizes(srcDir);

    //Create mobile resources
    MobileFramework.CreateResources(colors, sizes, outputDir);
};
var args = Args;
if(args.Count() == 0){
    await ExecuteSteps(new string[]{"help"});
    WriteLine("Please select steps to run:");
    var input = ReadLine();
    args = input.Split(' ');
}

await ExecuteSteps(args);