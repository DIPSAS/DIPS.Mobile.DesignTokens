#r "nuget:Newtonsoft.Json, 13.0.2"
using System.Xml;
using System.IO;
using Newtonsoft.Json;

public static class MobileFramework
{
    public static void CreateResources(
        Dictionary<string,string> colors,
        Dictionary<string,string> sizes, 
        Dictionary<string, string> icons,
        Dictionary<string, string> animations, 
        string outputDir)
    {
        //Colors
        WriteLine("Colors");
        var comment = $"\nDo not edit directly,\ngenerated {DateTime.Now} from DIPS.Mobile.DesignTokens\n";
        var colorsResourceDictionaryRaw = BuildResourceDictionary("ColorResources", "Colors", colors, "Color", comment, "DIPS.Mobile.UI.Resources.Colors");

        WriteLine("Building enum csharp file");
        var csharpComment = comment.Replace("\nDo", "/*\nDo").Replace("DIPS.Mobile.DesignTokens", "DIPS.Mobile.DesignTokens\n*/");
        string colorsEnumCsharpContent = csharpComment+@"
namespace DIPS.Mobile.UI.Resources.Colors
{
    public enum ColorName
    {
        "+string.Join(", \n", colors.Keys.Select(colorName => colorName))+@"
    }
}";
        
        var mauiOutputDir = Directory.CreateDirectory(Path.Combine(outputDir, "dotnet", "maui"));
        var colorsOutputDir = Directory.CreateDirectory(Path.Combine(mauiOutputDir.FullName, "Colors"));
        var sizesOutputDir = Directory.CreateDirectory(Path.Combine(mauiOutputDir.FullName, "Sizes"));
        var iconsOutputDir = Directory.CreateDirectory(Path.Combine(mauiOutputDir.FullName, "Icons"));
        var animationsOutputDir = Directory.CreateDirectory(Path.Combine(mauiOutputDir.FullName, "Animations"));

        //Write colors to XAML file and write enum file
        var colorsResourceDictionaryFullPath = Path.Combine(colorsOutputDir.FullName, "ColorResources.cs");
        var colorsEnumFilePath = Path.Combine(colorsOutputDir.FullName, "ColorName.cs");
        System.IO.File.WriteAllText(colorsResourceDictionaryFullPath, colorsResourceDictionaryRaw);
        System.IO.File.WriteAllText(colorsEnumFilePath, colorsEnumCsharpContent);
        WriteLine($"C# Resources output file: {colorsResourceDictionaryFullPath}");
        WriteLine($"C# Enum file: {colorsEnumFilePath}");

        WriteLine("Building enum csharp file");
        string sizesEnumCsharpContent = csharpComment+@"
namespace DIPS.Mobile.UI.Resources.Sizes
{
    public enum SizeName
    {
        "+string.Join(", \n", sizes.Keys.Select(colorName => colorName))+@"
    }
}";
        //Sizes
        WriteLine("Building Sizes");
        var sizesResourceDictionaryRaw = BuildResourceDictionary("SizeResources", "Sizes", sizes, "int", comment, "DIPS.Mobile.UI.Resources.Sizes");

        var sizesResourceDictionaryFullPath = Path.Combine(sizesOutputDir.FullName, "SizeResources.cs");
        var sizesEnumFilePath = Path.Combine(sizesOutputDir.FullName, "SizeName.cs");
        System.IO.File.WriteAllText(sizesResourceDictionaryFullPath, sizesResourceDictionaryRaw);
        System.IO.File.WriteAllText(sizesEnumFilePath, sizesEnumCsharpContent);
        WriteLine($"C# Resources output file: {sizesResourceDictionaryFullPath}");
        WriteLine($"C# Enum file: {sizesEnumFilePath}");

        WriteLine("Building enum csharp file");
        string iconsEnumCsharpContent = csharpComment+@"
namespace DIPS.Mobile.UI.Resources.Icons
{
    public enum IconName
    {
        "+string.Join(", \n", icons.Keys.Select(iconName => $"///<summary><a href=\"https://raw.githubusercontent.com/DIPSAS/DIPS.Mobile.UI/main/src/library/DIPS.Mobile.UI/Resources/Icons/{iconName}.svg\">View the icon in the browser</a></summary>"+"\n"+iconName))+@"
    }
}";

         //Icons
        WriteLine("Building Icons");
        var iconsResourceDictionary = BuildResourceDictionary("IconResources", "Icons", icons, "ImageSource", comment, "DIPS.Mobile.UI.Resources.Icons");

        var iconsResourceDictionaryFullPath = Path.Combine(iconsOutputDir.FullName, "IconResources.cs");
        var iconsEnumFilePath = Path.Combine(iconsOutputDir.FullName, "IconName.cs");
        System.IO.File.WriteAllText(iconsResourceDictionaryFullPath, iconsResourceDictionary);
        System.IO.File.WriteAllText(iconsEnumFilePath, iconsEnumCsharpContent);
        WriteLine($"C# Resources output file: {iconsResourceDictionaryFullPath}");
        WriteLine($"C# Enum file: {iconsEnumFilePath}");

        //Animations
        WriteLine("Building Animations");
         string animationsEnumCsharpContent = csharpComment+@"
namespace DIPS.Mobile.UI.Resources.Animations
{
    public enum AnimationName
    {
        "+string.Join(", \n", animations.Keys.Select(iconName => iconName))+@"
    }
}";
        var animationsResourceDictionary = BuildResourceDictionary("AnimationResources", "Animations", animations, "string", comment, "DIPS.Mobile.UI.Resources.Animations");

        var animationsResourceDictionaryFullPath = Path.Combine(animationsOutputDir.FullName, "AnimationResources.cs");
        var animationsEnumFilePath = Path.Combine(animationsOutputDir.FullName, "AnimationName.cs");
        System.IO.File.WriteAllText(animationsResourceDictionaryFullPath, animationsResourceDictionary);
        System.IO.File.WriteAllText(animationsEnumFilePath, animationsEnumCsharpContent);
        WriteLine($"C# Resources output file: {animationsResourceDictionaryFullPath}");
        WriteLine($"C# Enum file: {animationsEnumFilePath}");
    }

    private static string BuildResourceDictionary(string className, string dictionaryName, Dictionary<string,string> resources, string resourceType, string comment, string resourceNamespace)
    {
        string dictionaryContent = "";


        foreach(var resource in resources)
        {
            if(resourceType == "Color")
            {
                dictionaryContent += $"[\"{resource.Key}\"] = Color.FromArgb(\"{resource.Value}\"),\n";
            }
            else if(resourceType == "ImageSource")
            {
                dictionaryContent += $"[\"{resource.Key}\"] = \"{resource.Value}\",\n";
            }
            else if(resourceType == "string")
            {
                dictionaryContent += $"[\"{resource.Key}\"] = \"{resource.Value}\",\n";

            }
            else
            {
                dictionaryContent += $"[\"{resource.Key}\"] = {resource.Value},\n";
            }
        }

        var csharpComment = comment.Replace("\nDo", "/*\nDo").Replace("DIPS.Mobile.DesignTokens", "DIPS.Mobile.DesignTokens\n*/");
        string csharpContent = @$"{csharpComment}
        namespace {resourceNamespace};
        internal static class {className}
        {{
            public static Dictionary<string, {resourceType}> {dictionaryName} {{ get; }} = new()
            {{
                {dictionaryContent}
            }};
        }}";

       return csharpContent;
    }
}