namespace SharpDisk.Core;

public record DriveInfo(ulong SizeInLba, uint SectorSizeInBytes, uint HeadsPerCylinder = 255, uint SectorsPerTrack = 63);