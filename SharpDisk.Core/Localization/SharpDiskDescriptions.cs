namespace SharpDisk.Core.Localization;

/// <summary>
/// Marker type naming the resource set that holds translations of the generated descriptions.
/// </summary>
/// <remarks>
/// <see cref="Microsoft.Extensions.Localization.IStringLocalizer{T}"/> derives the resource path
/// from <c>T</c>, so asking for <c>IStringLocalizer&lt;SharpDiskDescriptions&gt;</c> looks in this
/// assembly for <c>Resources/Localization.SharpDiskDescriptions.&lt;culture&gt;.resx</c>. The class
/// has no members and is never instantiated; it exists only to give that lookup a name.
/// </remarks>
public sealed class SharpDiskDescriptions;
