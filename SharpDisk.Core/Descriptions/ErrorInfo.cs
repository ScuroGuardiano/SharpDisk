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
/// <param name="flag">The single flag this describes.</param>
/// <param name="fieldName">Name of the enum member, e.g. <c>InvalidSignature</c>.</param>
/// <param name="nameInvariant">Untranslated English short label from the attribute.</param>
/// <param name="descriptionInvariant">Untranslated English description from the attribute.</param>
/// <param name="severity">How much the error matters.</param>
public sealed class ErrorInfo<TFlags>(
    TFlags flag,
    string fieldName,
    string nameInvariant,
    string descriptionInvariant,
    Severity severity)
    where TFlags : struct, Enum
{
    /// <summary>
    /// The flag this describes. Exactly one bit.
    /// </summary>
    public TFlags Flag { get; } = flag;

    /// <summary>
    /// Enum member name, e.g. <c>InvalidSignature</c>. Never translated, so it is the right thing
    /// to write into logs.
    /// </summary>
    public string FieldName { get; } = fieldName;

    /// <summary>
    /// How much this error matters: whether a UI shows it as a note, a warning or a blocker.
    /// </summary>
    public Severity Severity { get; } = severity;

    /// <summary>
    /// The untranslated English label, as written in the attribute. Doubles as the resource name
    /// a translation is stored under.
    /// </summary>
    public string NameInvariant { get; } = nameInvariant;

    /// <summary>
    /// The untranslated English description. Doubles as the resource name a translation is
    /// stored under.
    /// </summary>
    public string DescriptionInvariant { get; } = descriptionInvariant;

    /// <summary>
    /// Short label for the current UI culture, e.g. "Overlapping partitions".
    /// </summary>
    public string Name => SharpDiskLocalization.Get(NameInvariant);

    /// <summary>
    /// Longer explanation for the current UI culture.
    /// </summary>
    public string Description => SharpDiskLocalization.Get(DescriptionInvariant);

    /// <inheritdoc />
    public override string ToString() => $"{Severity}: {Name}";
}
