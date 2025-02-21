public class VariableValue
{
    public string Collection { get; set; }
    public string Name { get; set; }
}

public class Variable
{
    public string Name { get; set; }
    public string Type { get; set; }
    public bool IsAlias { get; set; }
    public object Value { get; set; }
}

public class Mode
{
    public string Name { get; set; }
    public List<Variable> Variables { get; set; }
}

public class Collection
{
    public string Name { get; set; }
    public List<Mode> Modes { get; set; }
}

public class Root
{
    public List<Collection> Collections { get; set; }
}