namespace SharpDisk.Core.Attributes;

/// <summary>
/// Attribute that will be used by code gen to give a list of nice descriptions of partition types.
/// </summary>
/// <remarks>
/// Intended for later usage, currently code gen for it is not implemented
/// </remarks>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
public class PartitionTypeDescriptionAtrribute : Attribute
{
    /// <param name="type">Partition named type</param>
    /// <param name="description">Partition description</param>
    /// <param name="common">
    ///     Is commonly used, useful to display only common partition type list to the user
    ///     skipping obsolete ones
    /// </param>
    public PartitionTypeDescriptionAtrribute(string type, string description, bool common)
    {
        Type = type;
        Description = description;
        Common = common;
    }

    /// <summary>
    /// Partition named type
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// Partition description
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Is commonly used, useful to display only common partition type list to the user
    /// skipping obsolete ones
    /// </summary>
    public bool Common { get; }
}
