using System.Runtime.CompilerServices;

namespace SharpDisk.Core.Mbr;

public struct MbrAnalyzeResult
{
    public bool IsEmpty { get; internal set; }
    public bool IsProtective { get; internal set; }
    
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
