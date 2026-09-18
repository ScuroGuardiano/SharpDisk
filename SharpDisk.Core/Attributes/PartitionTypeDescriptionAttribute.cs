namespace SharpDisk.Core.Attributes;

/// <summary>
/// Attribute that will be used by code gen to give a list of nice descriptions of partition types.
/// </summary>
/// <remarks>
/// Read by <c>PartitionTypeDescriptionGenerator</c> in SharpDisk.Core.Generators, which emits a
/// lookup class per container - <c>MbrPartitionTypes</c> gets <c>MbrPartitionTypeDescriptions</c>.
/// Several of these on one field is normal: MBR type bytes were never centrally assigned, so many
/// are claimed by more than one vendor. Put the most likely meaning first; that is the one a UI
/// shows when it has room for only one.
/// </remarks>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
public sealed class PartitionTypeDescriptionAttribute : Attribute
{
    /// <param name="type">Partition named type</param>
    /// <param name="description">Partition description</param>
    /// <param name="common">
    ///     Is commonly used, useful to display only common partition type list to the user
    ///     skipping obsolete ones
    /// </param>
    public PartitionTypeDescriptionAttribute(string type, string description, bool common)
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
