namespace SharpDisk.Core.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Enum)]
public sealed class ErrorDescriptionAttribute : Attribute
{
    public ErrorDescriptionAttribute(string name, string description, Severity severity)
    {
        Name = name;
        Description = description;
        Severity = severity;
    }

    public string Name { get; }
    public string Description { get; }
    public Severity Severity { get; }
}