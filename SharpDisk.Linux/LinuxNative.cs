using System.Runtime.InteropServices;
using System.Runtime.Versioning;

// ReSharper disable InconsistentNaming

namespace SharpDisk.Linux;

[SupportedOSPlatform("linux")]
public static class LinuxNative
{
    private static readonly CULong BLKRRPART = new(0x125f);
    private static readonly CULong BLKSSZGET = new(0x1268);
    private static readonly CULong BLKGETSIZE64 = new(0x80081272);
    
    [DllImport("libc", SetLastError = true)]
    private static extern unsafe int ioctl(int fd, CULong op, void* ptr);
    
    [DllImport("libc")]
    private static extern IntPtr strerror(int errnum);

    public static string? StrError(int errnum)
        => Marshal.PtrToStringAnsi(strerror(errnum));
    
    public static void Ioctl_BLKRRPART(FileStream file)
        => IoctlOnFile(file, Ioctl_BLKRRPART);

    public static uint Ioctl_BLKSZGET(FileStream file)
        => IoctlOnFile(file, Ioctl_BLKSZGET);

    public static ulong Ioctl_BLKGETSIZE64(FileStream file)
        => IoctlOnFile(file, Ioctl_BLKGETSIZE64);
    
    public static unsafe void Ioctl_BLKRRPART(int fd)
        => EnsureSuccess(ioctl(fd, BLKRRPART, null), nameof(BLKRRPART));

    public static unsafe uint Ioctl_BLKSZGET(int fd)
    {
        uint ret = 0;
        EnsureSuccess(ioctl(fd, BLKSSZGET, &ret), nameof(BLKSSZGET));
        return ret;
    }

    public static unsafe ulong Ioctl_BLKGETSIZE64(int fd)
    {
        ulong ret = 0;
        EnsureSuccess(ioctl(fd, BLKGETSIZE64, &ret), nameof(BLKGETSIZE64));
        return ret;
    }

    private static void IoctlOnFile(FileStream file, Action<int> operation)
    {
        var success = false;
        file.SafeFileHandle.DangerousAddRef(ref success);

        try
        {
            var handle = file.SafeFileHandle.DangerousGetHandle().ToInt32();
            operation(handle);
        }
        finally
        {
            if (success)
            {
                file.SafeFileHandle.DangerousRelease();
            }
        }
        
    }
    
    private static T IoctlOnFile<T>(FileStream file, Func<int, T> operation)
    {
        var success = false;
        file.SafeFileHandle.DangerousAddRef(ref success);

        try
        {
            var handle = file.SafeFileHandle.DangerousGetHandle().ToInt32();
            return operation(handle);
        }
        finally
        {
            if (success)
            {
                file.SafeFileHandle.DangerousRelease();
            }
        }
    }

    private static void EnsureSuccess(int rc, string opName)
    {
        if (rc < 0)
        {
            throw new IoctlException(opName);
        }
    }
}

[SupportedOSPlatform("linux")]
public class IoctlException : Exception
{
    public int Errno { get; }

    public override string Message { get; }

    public IoctlException(string opName)
    {
        Errno = Marshal.GetLastWin32Error();
        Message = ConstructMessage(opName);
    }

    private string ConstructMessage(string opName)
    {
        var errstr = LinuxNative.StrError(Errno);

        return $"ioctl {opName} error: {Errno}: {errstr}";
    }
}
