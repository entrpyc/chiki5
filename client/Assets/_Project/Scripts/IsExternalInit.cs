// C# 9 records and init-only setters need this marker type, which Unity's API profile does not ship.
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit
    {
    }
}
