using SharpDisk.Core;
using SharpDisk.Core.Mbr;
using SharpDisk.Gui.Models;

namespace SharpDisk.Gui.Services;

/// <summary>
/// Expands the analyzer's flags into localized messages, worst first.
/// </summary>
/// <remarks>
/// All the text comes from the generated description classes, which read the
/// <c>[ErrorDescription]</c> attributes on the two error enums. Adding a flag to the analyzer and
/// giving it an attribute is therefore all it takes for it to show up here - there is no list in
/// the GUI to keep in step.
/// </remarks>
public sealed class AnalysisReportBuilder : IAnalysisReportBuilder
{
    public IReadOnlyList<AnalysisMessage> Build(MbrAnalyzeResult result)
    {
        var messages = new List<AnalysisMessage>();

        foreach (var error in MbrErrorDescriptions.Describe(result.TableErrors))
        {
            messages.Add(new AnalysisMessage(error.Severity, "Tablica", error.Name, error.Description, error.FieldName));
        }

        var perPartition = new[]
        {
            result.Partition1Errors,
            result.Partition2Errors,
            result.Partition3Errors,
            result.Partition4Errors,
        };

        for (var slot = 0; slot < perPartition.Length; slot++)
        {
            foreach (var error in MbrPartitionErrorDescriptions.Describe(perPartition[slot]))
            {
                messages.Add(new AnalysisMessage(
                    error.Severity,
                    $"Partycja {slot + 1}",
                    error.Name,
                    error.Description,
                    error.FieldName));
            }
        }

        return messages
            .OrderByDescending(message => message.Severity)
            .ThenBy(message => message.Scope, StringComparer.Ordinal)
            .ToList();
    }
}
