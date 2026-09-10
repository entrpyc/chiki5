// Required for C# 9 records and init-only setters on netstandard2.1,
// which does not ship this marker type.
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit
    {
    }
}
