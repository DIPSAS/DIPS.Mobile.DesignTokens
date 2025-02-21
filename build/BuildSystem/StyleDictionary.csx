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
#nullable enable
    public static Root? Root { get; set; }
#nullable disable

    private static readonly string s_globalColorCollectionName = "global colors";
    private static readonly string s_sizeCollectionName = "sizes";
    private static readonly string s_semanticColorCollectionName = "semantic colors";
    private static readonly string s_darkModeColorCollectionName = "dark colors";

    private const string ColorPrefix = "color_";
    
    private static async Task<Root> GetVariablesFile()
    {
        if(Root is not null)
        {
            return Root;
        }

        WriteLine($"Reading variables file: {VariablesFilePath}");

        var json = await System.IO.File.ReadAllTextAsync(VariablesFilePath);
        Root = JsonConvert.DeserializeObject<Root>(json);
        return Root;
    }

    public static async Task GenerateSizes()
    {
        var sizeVariables = await GetCollectionVariables(s_sizeCollectionName);

        var semanticSizeVariables = sizeVariables.Where(sizeVariable => sizeVariable.IsAlias).ToList();
        
        var nonSemanticSizes = sizeVariables.Where(sizeVariable => !sizeVariable.IsAlias).Select(sizeVariable => new TokenVariable(sizeVariable.Name, sizeVariable.Value.ToString(), charactersToRemove: "Global/")).ToList();
        var semanticSizes = new List<TokenVariable>();

        foreach(var semanticVariable in semanticSizeVariables)
        {
            var globalSize = nonSemanticSizes.FirstOrDefault(size => {
                if (semanticVariable.Value is JObject variableValue)
                {
                    var name = variableValue["name"]?.ToString();
                    return size.OriginalName == name;
                }
                return false;
            });

            if(globalSize is not null)
            {
                semanticSizes.Add(new TokenVariable(semanticVariable.Name, globalSize.Value, charactersToRemove: "Global/"));
            }
        }

        var sizeDictionary = nonSemanticSizes.Concat(semanticSizes).ToDictionary(s => s.Name, s => s.Value);

        await WriteToFile("tokens/sizes/sizes", JsonConvert.SerializeObject(sizeDictionary, Newtonsoft.Json.Formatting.Indented), "json");
    }

    public static async Task GenerateColors()
    {
        var globalColors = await GetGlobalColors();
        var semanticColors = await GetColorsWithAlias(globalColors, s_semanticColorCollectionName);
        var darkModeColors = await GetColorsWithAlias(globalColors, s_darkModeColorCollectionName);

        var colors = globalColors.Concat(semanticColors).Concat(darkModeColors).ToList();

        var colorDictionary = colors.ToDictionary(c => c.Name, c => c.Value);

        var androidColors = ConvertAndGenerateAndroidColors(colors);

        await Task.WhenAll(WriteToFile("tokens/colors/colors", JsonConvert.SerializeObject(colorDictionary, Newtonsoft.Json.Formatting.Indented), "json"), WriteToFile("android/colors", androidColors, "xml"));
    }

    private static Task WriteToFile(string filepath, string content, string format)
    {
        WriteLine($"Writing file: {Path.Combine(OutputDirPath, $"{filepath}.{format}")}");

        return System.IO.File.WriteAllTextAsync(Path.Combine(OutputDirPath, $"{filepath}.{format}"), content);
    }

    public static string ConvertAndGenerateAndroidColors(List<TokenVariable> colors)
    {
        var androidColors = colors.Select(c => $"   <color name=\"{c.Name}\">{c.Value}</color>");
        return $"<?xml version=\"1.0\" encoding=\"utf-8\"?>\n \n\n <!-- Do not modify, generated at: {DateTime.Now} --> \n\n  <resources>\n{string.Join("\n", androidColors)}\n</resources>";
    }

    private static async Task<List<TokenVariable>> GetGlobalColors()
    {
        var globalColorVariables = await GetCollectionVariables(s_globalColorCollectionName);

        return globalColorVariables
        .Select(v => new TokenVariable(v.Name, v.Value.ToString(), ColorPrefix)).ToList();
    }

    private static async Task<List<TokenVariable>> GetColorsWithAlias(List<TokenVariable> globalColors, string colorCollectionName)
    {
        var variables = await GetCollectionVariables(colorCollectionName);

        var colors = new List<TokenVariable>();

        foreach(var variable in variables)
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
                    colors.Add(new TokenVariable(variable.Name, globalColor.Value, ColorPrefix));
                }
            }
            else
            {
                colors.Add(new TokenVariable(variable.Name, variable.Value.ToString(), ColorPrefix));
                WriteLine($"Alias color: {variable.Name} does not point to a global color, using its direct value: {variable.Value}");
            }
        }

        return colors;
    }

    private static async Task<List<Variable>> GetCollectionVariables(string collectionName)
    {
        var variables = await GetVariablesFile();
        var collection = variables.Collections.FirstOrDefault(c => c.Name.ToLower() == collectionName) ?? throw new Exception($"No Collection with name `{collectionName}' found in the variables.json file");
        var firstMode = collection.Modes.FirstOrDefault() ?? throw new Exception($"No modes found in the `{collectionName}' collection");
        return firstMode.Variables;
    }
}


public class TokenVariable
{

#nullable enable
    public TokenVariable(string name, string value, string? prefix = null, string? charactersToRemove = null)
    {
        var modifiedName = name;
        if(charactersToRemove is not null)
        {
            modifiedName = modifiedName.Replace(charactersToRemove, string.Empty);
        }
        Name = (prefix is not null ? prefix : string.Empty) + modifiedName.Replace("/", "_").Replace(" ", "_").Replace(",", "_").ToLower();
        OriginalName = name;
        Value = value;
    }
#nullable disable
    public string OriginalName { get; set; }
    public string Name { get; set; }
    public string Value { get; set; }
}