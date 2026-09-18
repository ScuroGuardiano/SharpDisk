// netstandard2.0 has no IsExternalInit, and records will not compile without it.
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit;
}
