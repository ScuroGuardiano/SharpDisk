# SharpDisk - something like cfdisk/gdisk but worse.

I just wanted to have some fun with low level direct drive access and CRUD partition tables
by reading them directly from the drive, parsing it, validating and writing back directly aswell while using C#.

Very WIP, don't use it, it won't get better and as soon as I get bored I will abandon it.
Use mature and complete tools like fdisk, cfdisk, gdisk, gparted.

# What I learned from this
- There is a class `BinaryPrimitives` that allows you to read/write from/to `Span<byte>` with endianness choice.
- Every integer numeric type implements `IBinaryInteger<T>` which also allows you to write to `Span<byte>` with endianness choice.
- There is no single MBR standard and it's clusterfuck.
- How to use some of `ioctl` operations.
- How to read drives and partition data using on Linux using `/sys/class/block` and their mountpoints with `/proc/mounts`.

![Me being thirsty for tasty low level stuff](assets/YUMM.png)

# LICENSE
3-Clause BSD License