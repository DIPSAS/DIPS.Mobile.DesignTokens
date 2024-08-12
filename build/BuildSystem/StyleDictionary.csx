#r "nuget:Newtonsoft.Json, 13.0.2"
#load "Command.csx"
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Xml;

public static class StyleDictionary
{
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

