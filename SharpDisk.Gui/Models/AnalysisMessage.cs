using SharpDisk.Core;

namespace SharpDisk.Gui.Models;

/// <summary>
/// One finding from the analyzer, flattened into something a list can render.
/// </summary>
/// <param name="Severity">How much it matters.</param>
/// <param name="Scope">What it is about - the table as a whole, or a numbered partition.</param>
/// <param name="Name">Short label, already localized.</param>
/// <param name="Description">Explanation, already localized.</param>
/// <param name="FieldName">The analyzer flag name, untranslated - handy when reporting a bug.</param>
public sealed record AnalysisMessage(Severity Severity, string Scope, string Name, string Description, string FieldName);
