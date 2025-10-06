using System.Runtime.InteropServices;

namespace SharpDisk.Core.Mbr;

// ReSharper disable file InconsistentNaming
public class CHSAddress
{
    private CHSAddress_Raw _raw;

    public CHSAddress(ushort cyllinder, byte head, byte sector)
    {
        Cyllinder = cyllinder;
        Head = head;
        Sector = sector;
    }

    /// <summary>
    /// Not valid as CHS starts with (0, 0, 1) but in case of an empty partition it is valid.
    /// </summary>
    public static readonly CHSAddress Zero = new(0, 0, 0);
    public static readonly CHSAddress Second = new(0, 0, 2);
    public static readonly CHSAddress TooLarge = new(1023, 254, 63);
    public static readonly CHSAddress ProtectiveMBR = new(1023, 255, 63);
    
    public byte Head { get => _raw.Head; private set => _raw.Head = value; }

    public ushort Cyllinder
    {
        get => (ushort)(((_raw.CyllinderHighSector & 0b1100_0000) << 2) | _raw.CyllinderLow);
        private set
        {
            if (value > 1023)
            {
                throw new InvalidOperationException("Cyllinder cannot be larger than 1023.");
            }

            byte high = (byte)((value >> 2) & 0b1100_0000);
            _raw.CyllinderHighSector = (byte)((_raw.CyllinderHighSector & 0b0011_1111) | high);
            _raw.CyllinderLow = (byte)(value & 0xFF);
        }
    }

    public byte Sector
    {
        get => (byte)(_raw.CyllinderHighSector & 0b0011_1111);
        private set
        {
            if (value > 63)
            {
                throw new InvalidOperationException("Sector cannot be larger than 63.");
            }

            _raw.CyllinderHighSector = (byte)((_raw.CyllinderHighSector & 0b1100_0000) | value);
        }
    }

    public void ToBinary(Memory<byte> target)
    {
        _raw.ToBinary(target);
    }
}

internal struct CHSAddress_Raw
{
    public byte Head { get; set; }
    public byte CyllinderHighSector { get; set; }
    public byte CyllinderLow { get; set; }

    public void ToBinary(Memory<byte> target)
    {
        target.Span[0] = Head;
        target.Span[1] = CyllinderHighSector;
        target.Span[2] = CyllinderLow;
    }
}
