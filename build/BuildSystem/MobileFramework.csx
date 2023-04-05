#r "nuget:Newtonsoft.Json, 13.0.2"
using System.Xml;
using System.IO;
using Newtonsoft.Json;

public static class MobileFramework
{
    public static void CreateResources(Dictionary<string,string> colors,Dictionary<string,string> sizes, string outputDir)
    {
        WriteLine("Colors");
        WriteLine("Building XAML file");
        var comment = $"\nDo not edit directly,\ngenerated {DateTime.Now} from DIPS.Mobile.DesignTokens\n";
        var colorsResourceDictionaryRaw = BuildResourceDictionary("DIPS.Mobile.UI.Resources.Colors.Colors", colors, "Color", comment);

        WriteLine("Building enum csharp file");
        var csharpComment = comment.Replace("\nDo", "/*\nDo").Replace("DIPS.Mobile.DesignTokens", "DIPS.Mobile.DesignTokens\n*/");
        string colorsEnumCsharpContent = csharpComment+@"
namespace DIPS.Mobile.UI.Resources.Colors
{
    public enum ColorName
    {
        "+string.Join(", \n", colors.Keys.Select(colorName => colorName)).Replace("-", "_")+@"
    }
}";
        
        var xamarinFormsOutputDir = Directory.CreateDirectory(Path.Combine(outputDir, "xamarin", "forms"));
        var colorsOutputDir = Directory.CreateDirectory(Path.Combine(xamarinFormsOutputDir.FullName, "Colors"));
        var sizesOutputDir = Directory.CreateDirectory(Path.Combine(xamarinFormsOutputDir.FullName, "Sizes"));

        //Write colors to XAML file and write enum file
        var colorsResourceDictionaryFullPath = Path.Combine(colorsOutputDir.FullName, "Colors.xaml");
        var colorsEnumFilePath = Path.Combine(colorsOutputDir.FullName, "ColorName.cs");
        System.IO.File.WriteAllText(colorsResourceDictionaryFullPath, colorsResourceDictionaryRaw);
        System.IO.File.WriteAllText(colorsEnumFilePath, colorsEnumCsharpContent);
        WriteLine($"XAML output file: {colorsResourceDictionaryFullPath}");
        WriteLine($"C# Enum file: {colorsEnumFilePath}");

         WriteLine("Building enum csharp file");
        string sizesEnumCsharpContent = csharpComment+@"
namespace DIPS.Mobile.UI.Sizes.Sizes
{
    public enum SizeName
    {
        "+string.Join(", \n", sizes.Keys.Select(colorName => colorName)).Replace("-", "_")+@"
    }
}";

        //Sizes
        WriteLine("Building Sizes");
        var extraNamespaces = new Dictionary<string, string>(){{"xmlns:sys","clr-namespace:System;assembly=System.Runtime"}};
        var sizesResourceDictionaryRaw = BuildResourceDictionary("DIPS.Mobile.UI.Resources.Sizes.Sizes", sizes, "Int32", comment, extraNamespaces);
        sizesResourceDictionaryRaw = sizesResourceDictionaryRaw.Replace("Int32", "sys:Int32");

        var sizesResourceDictionaryFullPath = Path.Combine(sizesOutputDir.FullName, "Sizes.xaml");
        var sizesEnumFilePath = Path.Combine(sizesOutputDir.FullName, "SizeName.cs");
        System.IO.File.WriteAllText(sizesResourceDictionaryFullPath, sizesResourceDictionaryRaw);
        System.IO.File.WriteAllText(sizesEnumFilePath, sizesEnumCsharpContent);
        WriteLine($"XAML output file: {sizesResourceDictionaryFullPath}");
        WriteLine($"C# Enum file: {sizesEnumFilePath}");
    }

    private static string BuildResourceDictionary(string className, Dictionary<string,string> resources, string resourceType, string comment, Dictionary<string,string> extraNamespaces = null)
    {
        var resourceDictionaryXaml = new XmlDocument();
        var resourceDictionary = resourceDictionaryXaml.CreateElement("ResourceDictionary");
        var commentElement = resourceDictionaryXaml.CreateComment(comment);
        resourceDictionaryXaml.AppendChild(commentElement);
        resourceDictionaryXaml.AppendChild(resourceDictionary);
        resourceDictionary.SetAttribute("xmlns", "http://schemas.microsoft.com/dotnet/2021/maui");
        resourceDictionary.SetAttribute("xmlns:x", "http://schemas.microsoft.com/winfx/2009/xaml");
        if(extraNamespaces != null)
        {
            foreach(var nameSpace in extraNamespaces)
            {
                resourceDictionary.SetAttribute(nameSpace.Key, nameSpace.Value);
            }
        }
        resourceDictionary.SetAttribute("x:Class", className);

        foreach (var (key, value) in resources)
        {
            var theElement = resourceDictionaryXaml.CreateElement(resourceType);
            theElement.SetAttribute("x:Key", key); //Somehow does not add the x: before 
            theElement.InnerText = value;
            resourceDictionary.AppendChild(theElement);
        }
        return resourceDictionaryXaml.OuterXml.Replace("Key=", "x:Key=")
                .Replace("Class=", "x:Class=")
                .Replace($"<{resourceType}", $"\n<{resourceType}")
                .Replace("</ResourceDictionary", "\n</ResourceDictionary");
    }
}