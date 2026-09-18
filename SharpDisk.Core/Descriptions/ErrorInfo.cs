using SharpDisk.Core.Localization;

namespace SharpDisk.Core.Descriptions;

/// <summary>
/// The human-readable side of one analyzer error flag, produced by the description generator from
/// an <see cref="Attributes.ErrorDescriptionAttribute"/>.
/// </summary>
/// <typeparam name="TFlags">The flags enum the error belongs to.</typeparam>
/// <remarks>
/// The analyzer reports a bitmask; this is what turns a bit into something to put in front of a
/// user, together with a <see cref="Core.Severity"/> that says whether it is worth stopping for.
/// <para>
/// <see cref="Name"/> and <see cref="Description"/> go through
/// <see cref="SharpDiskLocalization"/> on every read, falling back to the English text from the
/// attribute.
/// </para>
/// </remarks>
public sealed class ErrorInfo<TFlags> where TFlags : struct, Enum
{
    /// <param name="flag">The single flag this describes.</param>
    /// <param name="keyPrefix">Name of the enum the flag belongs to, e.g. <c>MbrErrors</c>.</param>
    /// <param name="fieldName">Name of the enum member, e.g. <c>InvalidSignature</c>.</param>
    /// <param name="defaultName">English short label from the attribute.</param>
    /// <param name="defaultDescription">English description from the attribute.</param>
    /// <param name="severity">How much the error matters.</param>
    public ErrorInfo(
        TFlags flag,
        string keyPrefix,
        string fieldName,
        string defaultName,
        string defaultDescription,
        Severity severity)
    {
        Flag = flag;
        KeyPrefix = keyPrefix;
        FieldName = fieldName;
        DefaultName = defaultName;
        DefaultDescription = defaultDescription;
        Severity = severity;

        var entity = DescriptionKeys.Entity(keyPrefix, fieldName, 0);
        NameKey = $"{entity}.Name";
        DescriptionKey = $"{entity}.Description";
    }

    /// <summary>
    /// The flag this describes. Exactly one bit.
    /// </summary>
    public TFlags Flag { get; }

    /// <summary>
    /// Enum the flag belongs to, e.g. <c>MbrErrors</c>. Also the translation key prefix.
    /// </summary>
    public string KeyPrefix { get; }

    /// <summary>
    /// Enum member name, e.g. <c>InvalidSignature</c>. Stable across translations, so it is the
    /// right thing to write into logs.
    /// </summary>
    public string FieldName { get; }

    /// <summary>
    /// How much this error matters: whether a UI shows it as a note, a warning or a blocker.
    /// </summary>
    public Severity Severity { get; }

    /// <summary>
    /// Translation key for <see cref="Name"/>.
    /// </summary>
    public string NameKey { get; }

    /// <summary>
    /// Translation key for <see cref="Description"/>.
    /// </summary>
    public string DescriptionKey { get; }

    /// <summary>
    /// English label, used when there is no translation.
    /// </summary>
    public string DefaultName { get; }

    /// <summary>
    /// English description, used when there is no translation.
    /// </summary>
    public string DefaultDescription { get; }

    /// <summary>
    /// Short label for the current UI culture, e.g. "Overlapping partitions".
    /// </summary>
    public string Name => SharpDiskLocalization.GetString(NameKey, DefaultName);

    /// <summary>
    /// Longer explanation for the current UI culture.
    /// </summary>
    public string Description => SharpDiskLocalization.GetString(DescriptionKey, DefaultDescription);

    /// <summary>
    /// The two translatable strings this description carries.
    /// </summary>
    public IEnumerable<TranslationEntry> TranslationEntries
    {
        get
        {
            yield return new TranslationEntry(NameKey, DefaultName);
            yield return new TranslationEntry(DescriptionKey, DefaultDescription);
        }
    }

    /// <inheritdoc />
    public override string ToString() => $"{Severity}: {Name}";
}
