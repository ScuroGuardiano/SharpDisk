using System.Runtime.CompilerServices;

namespace SharpDisk.Core.Mbr;

public struct MbrAnalyzeResult
{
    public bool IsEmpty { get; internal set; }

    /// <summary>
    /// True only for a <b>clean</b> protective MBR - a 0xEE partition and nothing else.
    /// A hybrid MBR sets <see cref="IsHybrid"/> instead.
    /// </summary>
    public bool IsProtective { get; internal set; }

    /// <summary>
    /// True when a 0xEE partition coexists with real ones. Not an error on its own -
    /// hybrid ISO images do this on purpose - but on a mutable drive the MBR and GPT
    /// describe the same sectors twice and can drift apart.
    /// </summary>
    public bool IsHybrid { get; internal set; }
    
    public MbrErrors TableErrors { get; internal set; }

    public MbrPartitionsErrors PartitionErrors;

    public MbrPartitionErrors Partition1Errors => PartitionErrors[0];
    public MbrPartitionErrors Partition2Errors => PartitionErrors[1];
    public MbrPartitionErrors Partition3Errors => PartitionErrors[2];
    public MbrPartitionErrors Partition4Errors => PartitionErrors[3];

}

[InlineArray(4)]
public struct MbrPartitionsErrors
{
    private MbrPartitionErrors _errors;
}
