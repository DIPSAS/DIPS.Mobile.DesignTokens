using System.Xml;
using System.IO;

public static class MobileFramework
{
    public static void CreateResources(Dictionary<string,string> colors, string outputDir)
    {
        Console.WriteLine("Building XAML file");
        //Build the xaml file
        var commentText = $"\nDo not edit directly,\ngenerated {DateTime.Now} from DIPS.Mobile.DesignTokens\n";
        var resourceDictionaryXaml = new XmlDocument();
        var resourceDictionary = resourceDictionaryXaml.CreateElement("ResourceDictionary");
        var comment = resourceDictionaryXaml.CreateComment(commentText);
        resourceDictionaryXaml.AppendChild(comment);
        resourceDictionaryXaml.AppendChild(resourceDictionary);
        resourceDictionary.SetAttribute("xmlns", "http://xamarin.com/schemas/2014/forms");
        resourceDictionary.SetAttribute("xmlns:x", "http://schemas.microsoft.com/winfx/2009/xaml");
        resourceDictionary.SetAttribute("x:Class", "DIPS.Mobile.UI.Resources.Colors.Colors");

        foreach (var (colorName, colorHex) in colors)
        {
            var colorElement = resourceDictionaryXaml.CreateElement("Color");
            colorElement.SetAttribute("x:Key", colorName); //Somehow does not add the x: before 
            colorElement.InnerText = colorHex;
            resourceDictionary.AppendChild(colorElement);
        }
        var resourceDictionaryRaw = resourceDictionaryXaml.OuterXml.Replace("Key=", "x:Key=")
                .Replace("Class=", "x:Class=")
                .Replace("<Color", "\n<Color")
                .Replace("</ResourceDictionary", "\n</ResourceDictionary");

        Console.WriteLine("Building enum csharp file");
        //Build the enum file
        var csharpComment = commentText.Replace("\nDo", "/*\nDo").Replace("DIPS.Mobile.DesignTokens", "DIPS.Mobile.DesignTokens\n*/");
        string colorsEnumCsharpContent = csharpComment+@"
namespace DIPS.Mobile.UI.Resources.Colors
{
    public enum ColorName
    {
        "+string.Join(", \n", colors.Keys.Select(colorName => colorName))+@"
    }
}";
        //Write colors to XAML file and write enum file
        var xamarinFormsOutputDir = Directory.CreateDirectory(Path.Combine(outputDir, "xamarin", "forms"));
        var resourceDictionaryFullPath = Path.Combine(xamarinFormsOutputDir.FullName, "ResourceDictionary.xaml");
        var enumFilePath = Path.Combine(xamarinFormsOutputDir.FullName, "ColorName.cs");
        System.IO.File.WriteAllText(resourceDictionaryFullPath, resourceDictionaryRaw);
        System.IO.File.WriteAllText(enumFilePath, colorsEnumCsharpContent);
        Console.WriteLine($"XAML output file: {resourceDictionaryFullPath}");
        Console.WriteLine($"C# Enum file: {enumFilePath}");
    }
}