#r "nuget:Newtonsoft.Json, 13.0.2"
using Newtonsoft.Json;
public static class Dump{
    public static string It(object obj) => JsonConvert.SerializeObject(obj, Formatting.Indented);
}