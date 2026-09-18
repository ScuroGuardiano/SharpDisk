using Microsoft.Extensions.Localization;

namespace SharpDisk.Core.Localization;

/// <summary>
/// The localizer that generated descriptions run their text through.
/// </summary>
/// <remarks>
/// Descriptions are static data generated from attributes, so there is nothing to inject a
/// localizer into - hence this one ambient hook. Leave it unset and everything stays English.
/// <para>
/// Strings are looked up by their English text, the way <see cref="IStringLocalizer"/> is meant to
/// be used, so a .resx needs a <c>data name</c> of "Linux filesystem" and nothing here has to know
/// about keys. An entry that is missing from the resources comes back as the English text.
/// </para>
/// <example>
/// Wiring it up from a host that already has DI, with .resx files under <c>Resources/</c>:
/// <code>
/// services.AddLocalization(options => options.ResourcesPath = "Resources");
/// // ...
/// SharpDiskLocalization.Localizer = provider.GetRequiredService&lt;IStringLocalizer&lt;SharpDiskDescriptions&gt;&gt;();
/// CultureInfo.CurrentUICulture = new CultureInfo("pl");
/// </code>
/// </example>
/// </remarks>
public static class SharpDiskLocalization
{
    /// <summary>
    /// Localizer used by every description. <c>null</c> (the default) means English only.
    /// </summary>
    public static IStringLocalizer? Localizer { get; set; }

    /// <summary>
    /// <paramref name="text"/> translated for the current UI culture, or unchanged when there is
    /// no localizer or no translation.
    /// </summary>
    public static string Get(string text)
    {
        var localizer = Localizer;
        if (localizer is null)
        {
            return text;
        }

        // This sits on property getters the GUI reads on every repaint; a localizer that throws
        // (a missing satellite assembly, a malformed .resx) should degrade to English, not take
        // the window down.
        try
        {
            return localizer[text];
        }
        catch
        {
            return text;
        }
    }
}
