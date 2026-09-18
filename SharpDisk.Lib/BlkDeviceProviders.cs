using System.Runtime.InteropServices;
using SharpDisk.Core;
using SharpDisk.Linux;

#if SHARPDISK_WINDOWS
using SharpDisk.Windows;
#endif

namespace SharpDisk.Lib;

/// <summary>
/// Hands out the <see cref="IBlkDeviceProvider"/> for the OS this process is running on.
/// </summary>
/// <remarks>
/// The backend is picked at runtime, so a single build of this library works on every
/// supported platform - there is no per-RID build to keep track of. The
/// <c>OperatingSystem.IsXxx()</c> checks below are not just branches: the platform compatibility
/// analyzer reads them as guards, which is what keeps CA1416 quiet without a #pragma, and the
/// trimmer can fold them away when publishing for a known RID.
/// </remarks>
public static class BlkDeviceProviders
{
    /// <summary>
    /// Whether this OS has a backend. When false, <see cref="ForCurrentPlatform"/> throws.
    /// </summary>
    public static bool IsSupported
    {
        get
        {
            if (OperatingSystem.IsLinux())
            {
                return true;
            }

#if SHARPDISK_WINDOWS
            if (OperatingSystem.IsWindows())
            {
                return true;
            }
#endif

            return false;
        }
    }

    /// <summary>
    /// Creates the provider for the OS this process is running on.
    /// </summary>
    /// <exception cref="PlatformNotSupportedException">
    /// No backend for this OS - check <see cref="IsSupported"/> first if you want to degrade gracefully.
    /// </exception>
    public static IBlkDeviceProvider ForCurrentPlatform()
    {
        if (OperatingSystem.IsLinux())
        {
            return new LinuxBlkDeviceProvider();
        }

#if SHARPDISK_WINDOWS
        if (OperatingSystem.IsWindows())
        {
            return new WindowsBlkDeviceProvider();
        }
#endif

        throw new PlatformNotSupportedException(
            $"SharpDisk has no block device backend for {RuntimeInformation.OSDescription}. " +
            "Supported platforms are Linux and Windows.");
    }
}
