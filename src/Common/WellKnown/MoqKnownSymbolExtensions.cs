using System.Runtime.CompilerServices;

namespace Moq.Analyzers.Common.WellKnown;

internal static class MoqKnownSymbolExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsMockReferenced(this MoqKnownSymbols mqs)
    {
        return mqs.Mock is not null || mqs.Mock1 is not null || mqs.MockRepository is not null;
    }
}
