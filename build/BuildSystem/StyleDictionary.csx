#r "nuget:Newtonsoft.Json, 13.0.2"
#load "Command.csx"
#load "Models/Models.csx"
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Xml;

public static class StyleDictionary
{
    public static string VariablesFilePath { get; set; }
    public static string OutputDirPath { get; set; }
    public static string ColorTokenDirPath { get; set; }

    private static readonly string s_globalColorCollectionName = "global colors";
    private static readonly string s_semanticColorCollectionName = "semantic colors";


    public static Task Build(string configPath)
    {
        return Command.ExecuteAsync($"npx", $"style-dictionary build", configPath, verbose: true);
    }
    /// <summary>
    /// Get the config file from style dictionary root folder
    /// </summary>
    /// <param name="rootPath">The filepath to style dictionary root folder with the config.json file</param>
    /// <returns></returns>
    public static async Task<Config> GetConfig(string rootPath){
        var json = await System.IO.File.ReadAllTextAsync(Path.Combine(rootPath, "config.json"));
        return JsonConvert.DeserializeObject<Config>(json);
    }

    private static async Task<Root> GetVariablesFile()
    {
        var json = await System.IO.File.ReadAllTextAsync(VariablesFilePath);
        return JsonConvert.DeserializeObject<Root>(json);
    }

    /// <summary>
    /// Get Android Colors
    /// </summary>
    /// <param name="rootPath">The filepath to style dictionary root folder with the config.json file</param>
    public static async Task<Dictionary<string, string>> GetAndroidColors(string rootPath){
        var config = await GetConfig(rootPath);
        var buildPath = config.Platforms.Android.BuildPath;
        var actualBuildPath = Path.Combine(rootPath, buildPath);
        var file = config.Platforms.Android.Files.FirstOrDefault(f => f.Format.Equals("android/colors"));
        var androidResourceFile = actualBuildPath + file.Destination;
        var androidColorsRawXml = await System.IO.File.ReadAllTextAsync(androidResourceFile);

        var colorXml = new XmlDocument();
        colorXml.LoadXml(androidColorsRawXml);

        var colorDictionary = new Dictionary<string, string>();
        for (int i = 0; i < colorXml.ChildNodes.Count; i++)
        {
            if (colorXml.ChildNodes[i].Name == "resources")
            {
                var resources = colorXml.ChildNodes[i];
                var colors = resources.ChildNodes;
                for (int j = 0; j < colors.Count; j++)
                {
                    var colorNode = colors[j];
                    //Get color name
                    var attributes = colorNode.Attributes;
                    string colorName = null;
                    if (attributes != null)
                    {
                        if (attributes[0].Name == "name")
                        {
                            colorName = attributes[0].Value.Replace("-", "_");    
                        }
                    }
                    
                    //Get color value
                    var valueNode = colorNode.FirstChild;
                    string colorValue = null;
                    if (valueNode != null)
                    {
                        colorValue = valueNode.Value;
                    }

                    if (colorName != null && colorValue != null)
                    {
                        colorDictionary.Add(colorName, colorValue);    
                    }
                }
            }
        }
        return colorDictionary;
    }

    public static async Task<Dictionary<string,string>> GetSizes(string rootPath)
    {
        var sizes = new Dictionary<string,string> ();
        var config = await GetConfig(rootPath);
        var fileConfig = config.Platforms.Raw.Files.FirstOrDefault(f => f.Filter.Attributes.Category.Equals("size"));
        if(fileConfig != null)
        {
            var buildPath = config.Platforms.Raw.BuildPath;
            var actualBuildPath = Path.Combine(rootPath, buildPath);
            var fileName = fileConfig.Destination;
            var rawJson = await System.IO.File.ReadAllTextAsync(actualBuildPath + fileName);
            var jObject = JObject.Parse(rawJson);

            foreach (var property in jObject.Properties()) //Each size
            {
                sizes.Add(property.Name.ToString().Replace("-", "_"), property.Value.ToString());
            }

        }else
        {
            WriteLine("No size defined in StyleDictionary config");
        }
        return sizes;
    }

    //public async Task GenerateSizes()
    //{
        // TODO
    //}

    public static async Task GenerateColors()
    {
        var globalColors = await GetGlobalColors();
        var semanticColors = await GetSemanticColors(globalColors);

        var colors = globalColors.Concat(semanticColors).ToList();

        var colorDictionary = colors.ToDictionary(c => c.Name, c => c.Value);

        var androidColors = ConvertAndGenerateAndroidColors(colors);

        await Task.WhenAll(WriteToFile("tokens/colors/colors", JsonConvert.SerializeObject(colorDictionary, Newtonsoft.Json.Formatting.Indented), "json"), WriteToFile("android/colors", androidColors, "xml"));
    }

    private static Task WriteToFile(string filepath, string content, string format)
    {
        Console.WriteLine($"Writing file: {Path.Combine(OutputDirPath, $"{filepath}.{format}")}");

        string contentToWrite = string.Empty;
        if(format == "xml")
        {
            contentToWrite = $"\n\n <!-- Do not modify, generated at: {DateTime.Now} --> \n\n";
        }

        contentToWrite += content;

        return System.IO.File.WriteAllTextAsync(Path.Combine(OutputDirPath, $"{filepath}.{format}"), contentToWrite);
    }

    public static string ConvertAndGenerateAndroidColors(List<ColorVariable> colors)
    {
        var androidColors = colors.Select(c => $"   <color name=\"{c.Name}\">{c.Value}</color>");
        return $"<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<resources>\n{string.Join("\n", androidColors)}\n</resources>";
    }

    private static async Task<List<ColorVariable>> GetGlobalColors()
    {
        var variables = await GetVariablesFile();
        
        var globalColorVariables = await GetCollectionVariables(s_globalColorCollectionName);

        return globalColorVariables
        .Select(v => new ColorVariable(v.Name, v.Value.ToString())).ToList();
    }

    private static async Task<List<ColorVariable>> GetSemanticColors(List<ColorVariable> globalColors)
    {
        var semanticColorVariables = await GetCollectionVariables(s_semanticColorCollectionName);

        var semanticColors = new List<ColorVariable>();

        foreach(var variable in semanticColorVariables)
        {
            if(variable.IsAlias)
            {
                var globalColor = globalColors.FirstOrDefault(c => {
                    if (variable.Value is JObject variableValue)
                    {
                        var name = variableValue["name"]?.ToString();
                        return c.OriginalName == name;
                    }
                    return false;
                });
                if(globalColor != null)
                {
                    semanticColors.Add(new ColorVariable(variable.Name, globalColor.Value));
                }
            }
            else
            {
                semanticColors.Add(new ColorVariable(variable.Name, variable.Value.ToString()));
                WriteLine($"Semantic color: {variable.Name} does not point to a global color, using its direct value: {variable.Value}");
            }
        }

        return semanticColors;
    }

    private static async Task<List<Variable>> GetCollectionVariables(string collectionName)
    {
        var variables = await GetVariablesFile();
        var collection = variables.Collections.FirstOrDefault(c => c.Name.ToLower() == collectionName) ?? throw new Exception($"No Collection with name `{collectionName}' found in the variables.json file");
        var firstMode = collection.Modes.FirstOrDefault() ?? throw new Exception($"No modes found in the `{collectionName}' collection");
        return firstMode.Variables;
    }
}


public class ColorVariable
{
    private const string ColorPrefix = "color_";

    public ColorVariable(string name, string value)
    {
        Name = ColorPrefix + name.Replace("/", "_").ToLower();
        OriginalName = name;
        Value = value;
    }


    public string OriginalName { get; set; }
    public string Name { get; set; }
    public string Value { get; set; }
}


#region config objects
    public class Android
    {
        public string TransformGroup { get; set; }
        public string BuildPath { get; set; }
        public List<File> Files { get; set; }
    }

    public class Attributes
    {
        public string Category { get; set; }
    }

    public class File
    {
        public string Destination { get; set; }
        public string Format { get; set; }
        public string ClassName { get; set; }
        public string Type { get; set; }
        public Filter Filter { get; set; }
    }

    public class Filter
    {
        public Attributes Attributes { get; set; }
    }

    public class iOS
    {
        public string TransformGroup { get; set; }
        public string BuildPath { get; set; }
        public List<File> Files { get; set; }
    }

    public class Raw
    {
        public string TransformGroup { get; set; }
        public string BuildPath { get; set; }
        public List<File> Files { get; set; }
    }

    public class Platforms
    {
        public Android Android { get; set; }
        public iOS iOS { get; set; }
        public Raw Raw { get; set; }
    }

    public class Config
    {
        public List<string> Source { get; set; }
        public Platforms Platforms { get; set; }
    }
#endregion

