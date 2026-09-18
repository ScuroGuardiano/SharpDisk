using SharpDisk.Core.Localization;

namespace SharpDisk.Core.Descriptions;

/// <summary>
/// One human-readable meaning of a partition type value, produced by the description generator
/// from a <see cref="Attributes.PartitionTypeDescriptionAttribute"/>.
/// </summary>
/// <typeparam name="TValue">
/// How the type is identified on disk: <see cref="byte"/> for MBR, <see cref="Guid"/> for GPT.
/// </typeparam>
/// <remarks>
/// A single value can carry several of these. MBR type bytes were never centrally assigned, so
/// plenty are claimed by more than one vendor - 0xFE alone means IBM PS/2 IML, hidden NTFS,
/// LANstep and SpeedStor depending on who wrote the table. The generator keeps every meaning and
/// puts the most likely one first.
/// <para>
/// <see cref="Type"/> and <see cref="Description"/> go through
/// <see cref="SharpDiskLocalization"/> on every read, so switching
/// <see cref="System.Globalization.CultureInfo.CurrentUICulture"/> at runtime is enough to change
/// the language. Nothing is cached, and untranslated text falls back to the English the attribute
/// carried.
/// </para>
/// </remarks>
/// <param name="value">The on-disk type value.</param>
/// <param name="fieldName">Name of the field the value was declared as, e.g. <c>Fat32Lba</c>.</param>
/// <param name="typeInvariant">Untranslated English short name from the attribute.</param>
/// <param name="descriptionInvariant">Untranslated English description from the attribute.</param>
/// <param name="common">Whether this is a type a user would realistically pick today.</param>
public sealed class PartitionTypeInfo<TValue>(
    TValue value,
    string fieldName,
    string typeInvariant,
    string descriptionInvariant,
    bool common)
    where TValue : struct
{
    /// <summary>
    /// The on-disk type value this describes.
    /// </summary>
    public TValue Value { get; } = value;

    /// <summary>
    /// Field the value was declared as, e.g. <c>Fat32Lba</c>. Never translated, so it is the right
    /// thing to write into config files and logs.
    /// </summary>
    public string FieldName { get; } = fieldName;

    /// <summary>
    /// Whether this is a type a user would realistically pick today, as opposed to a historical
    /// or vendor-specific one. Filter a type picker on this.
    /// </summary>
    public bool Common { get; } = common;

    /// <summary>
    /// The untranslated English short name, as written in the attribute. Doubles as the resource
    /// name a translation is stored under.
    /// </summary>
    public string TypeInvariant { get; } = typeInvariant;

    /// <summary>
    /// The untranslated English description. Doubles as the resource name a translation is
    /// stored under.
    /// </summary>
    public string DescriptionInvariant { get; } = descriptionInvariant;

    /// <summary>
    /// Short name for the current UI culture, e.g. "Linux filesystem".
    /// </summary>
    public string Type => SharpDiskLocalization.Get(TypeInvariant);

    /// <summary>
    /// Longer explanation for the current UI culture.
    /// </summary>
    public string Description => SharpDiskLocalization.Get(DescriptionInvariant);

    /// <inheritdoc />
    public override string ToString() => $"{Value}: {Type}";
}
