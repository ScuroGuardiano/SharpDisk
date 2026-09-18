using System.Runtime.InteropServices;

namespace SharpDisk.Core.Mbr;

// ReSharper disable file InconsistentNaming
public readonly record struct CHSAddress
{
    private readonly CHSAddress_Raw _raw;

    public CHSAddress(ushort cylinder, byte head, byte sector)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(cylinder, (ushort)1023);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(sector, (byte)63);

        _raw = new CHSAddress_Raw
        {
            Head = head,
            CylinderHighSector = (byte)(((cylinder >> 2) & 0b1100_0000) | sector),
            CylinderLow = (byte)(cylinder & 0xFF)
        };
    }

    private CHSAddress(CHSAddress_Raw raw) => _raw = raw;


    /// <summary>
    /// Not valid as CHS starts with (0, 0, 1) but in case of an empty partition it is valid.
    /// </summary>
    public static readonly CHSAddress Zero = new(0, 0, 0);

    /// <summary>
    /// 00 02 00 - first sector of the first track, used as StartingCHS of a protective MBR.
    /// </summary>
    public static readonly CHSAddress Second = new(0, 0, 2);

    /// <summary>
    /// FE FF FF - conventional marker meaning "this LBA is beyond CHS addressing".
    /// </summary>
    public static readonly CHSAddress TooLarge = new(1023, 254, 63);

    /// <summary>
    /// FF FF FF - alternative saturation marker, mandated for the EndingCHS of a protective MBR.
    /// </summary>
    public static readonly CHSAddress ProtectiveMbr = new(1023, 255, 63);

    public readonly byte Head => _raw.Head;

    public readonly ushort Cylinder => (ushort)(((_raw.CylinderHighSector & 0b1100_0000) << 2) | _raw.CylinderLow);

    public readonly byte Sector => (byte)(_raw.CylinderHighSector & 0b0011_1111);

    /// <summary>
    /// True for either conventional "beyond CHS range" marker (FE FF FF / FF FF FF).
    /// </summary>
    public readonly bool IsSaturated => this == TooLarge || this == ProtectiveMbr;

    public readonly void ToBinary(Span<byte> target) => _raw.ToBinary(target);

    public static CHSAddress FromBinary(ReadOnlySpan<byte> source)
        => new(CHSAddress_Raw.FromBinary(source));
}

internal readonly record struct CHSAddress_Raw
{
    public byte Head { get; init; }
    public byte CylinderHighSector { get; init; }
    public byte CylinderLow { get; init; }

    public readonly void ToBinary(Span<byte> target)
    {
        target[0] = Head;
        target[1] = CylinderHighSector;
        target[2] = CylinderLow;
    }

    public static CHSAddress_Raw FromBinary(ReadOnlySpan<byte> source) => new()
    {
        Head = source[0],
        CylinderHighSector = source[1],
        CylinderLow = source[2]
    };
}

