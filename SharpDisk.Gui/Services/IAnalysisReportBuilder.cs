using SharpDisk.Core.Mbr;
using SharpDisk.Gui.Models;

namespace SharpDisk.Gui.Services;

/// <summary>
/// Flattens an analyzer bitmask into a list a page can render.
/// </summary>
public interface IAnalysisReportBuilder
{
    IReadOnlyList<AnalysisMessage> Build(MbrAnalyzeResult result);
}
