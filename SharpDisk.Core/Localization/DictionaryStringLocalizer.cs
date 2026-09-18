using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Globalization;
using System.Text.Json;

namespace SharpDisk.Core.Localization;

/// <summary>
/// An <see cref="ISharpDiskStringLocalizer"/> backed by plain key/value tables held in memory,
/// one per culture.
/// </summary>
/// <remarks>
/// The lightweight route to translations: ship a JSON file per language next to the executable,
/// load it at startup and you are done. No satellite assemblies, no build step, and a translator
/// can edit the file without a C# toolchain.
/// <code>
/// SharpDiskLocalization.Localizer = new DictionaryStringLocalizer()
///     .AddJson(new CultureInfo("pl"), File.ReadAllText("i18n/pl.json"));
/// </code>
/// A starting point for that file comes from
/// <see cref="TranslationCatalog.ToJsonTemplate"/>, which dumps every key with its English text.
/// </remarks>
public sealed class DictionaryStringLocalizer : ISharpDiskStringLocalizer
{
    // Culture names are case insensitive: "PL-pl" and "pl-PL" are the same culture.
    private readonly ConcurrentDictionary<string, FrozenDictionary<string, string>> _byCulture
        = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Adds (or replaces) the table for <paramref name="culture"/>.
    /// </summary>
    /// <returns>This instance, so calls can be chained.</returns>
    public DictionaryStringLocalizer Add(CultureInfo culture, IEnumerable<KeyValuePair<string, string>> translations)
    {
        ArgumentNullException.ThrowIfNull(culture);
        ArgumentNullException.ThrowIfNull(translations);

        _byCulture[culture.Name] = translations.ToFrozenDictionary(StringComparer.Ordinal);
        return this;
    }

    /// <summary>
    /// Adds (or replaces) the table for <paramref name="culture"/> from a flat JSON object,
    /// the shape <see cref="TranslationCatalog.ToJsonTemplate"/> produces.
    /// </summary>
    /// <remarks>
    /// Read with <see cref="JsonDocument"/> rather than <c>JsonSerializer</c> on purpose: the
    /// serializer needs reflection, which is off in trimmed and AOT-published apps, and a flat
    /// map of strings is not worth a source-generated serializer context.
    /// </remarks>
    /// <exception cref="JsonException">The document is not a flat object of string values.</exception>
    public DictionaryStringLocalizer AddJson(CultureInfo culture, string json)
    {
        ArgumentNullException.ThrowIfNull(culture);
        ArgumentNullException.ThrowIfNull(json);

        using var document = JsonDocument.Parse(json);

        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException($"Translation document must be a JSON object, got {document.RootElement.ValueKind}.");
        }

        var table = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.String)
            {
                throw new JsonException($"Translation for '{property.Name}' must be a string, got {property.Value.ValueKind}.");
            }

            table[property.Name] = property.Value.GetString()!;
        }

        return Add(culture, table);
    }

    /// <summary>
    /// Cultures this localizer currently holds a table for.
    /// </summary>
    public IReadOnlyCollection<string> Cultures => (IReadOnlyCollection<string>)_byCulture.Keys;

    /// <inheritdoc />
    public string? TryGetString(string key, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(culture);

        // Walk the culture chain, so a pl-PL table falls back to pl and then to whatever was
        // registered as invariant. InvariantCulture.Parent is itself, hence the name check.
        for (var current = culture; ; current = current.Parent)
        {
            if (_byCulture.TryGetValue(current.Name, out var table)
                && table.TryGetValue(key, out var value))
            {
                return value;
            }

            if (current.Name.Length == 0 || ReferenceEquals(current, current.Parent))
            {
                return null;
            }
        }
    }
}
