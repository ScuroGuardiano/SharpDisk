namespace SharpDisk.Core.Descriptions;

/// <summary>
/// How translation keys for generated descriptions are spelled.
/// </summary>
/// <remarks>
/// Keys are built here rather than baked into the generated code, so the shape of a key is
/// decided in one place and the generated files stay short. A key looks like
/// <c>MbrPartitionTypes.Fat32Lba.Type</c>: declaring class, then field, then which string.
/// Fields carrying more than one meaning get an <c>.altN</c> segment on all but the first:
/// <c>MbrPartitionTypes.Ps2Iml.alt1.Description</c>.
/// </remarks>
public static class DescriptionKeys
{
    /// <summary>
    /// The key prefix identifying one described field, without the trailing string name.
    /// </summary>
    public static string Entity(string keyPrefix, string fieldName, int ordinal)
        => ordinal == 0
            ? $"{keyPrefix}.{fieldName}"
            : $"{keyPrefix}.{fieldName}.alt{ordinal}";
}
