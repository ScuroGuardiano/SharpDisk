namespace SharpDisk.Core.Mbr;

public enum MbrBootable : byte
{
    NonBootable = 0x00,
    Bootable = 0x80
}
