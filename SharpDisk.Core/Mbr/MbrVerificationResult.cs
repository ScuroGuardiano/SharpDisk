using System.Runtime.InteropServices;

namespace SharpDisk.Core.Mbr;

public struct MbrVerificationResult
{
    public bool IsEmpty { get; internal init; }
    public bool IsProtective { get; internal init; }
    
    public MbrErrors TableErrors { get; internal init; }
    
    public MbrPartitionErrors Partition1Errors { get; internal init; }
    public MbrPartitionErrors Partition2Errors { get; internal init; }
    public MbrPartitionErrors Partition3Errors { get; internal init; }
    public MbrPartitionErrors Partition4Errors { get; internal init; }
}
