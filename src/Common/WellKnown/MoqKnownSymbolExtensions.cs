namespace Moq.Analyzers.Common.WellKnown;

internal static class MoqKnownSymbolExtensions
{
    internal static bool IsMockReferenced(this MoqKnownSymbols mqs)
    {
        return mqs.Mock is not null || mqs.Mock1 is not null || mqs.MockRepository is not null;
    }
}
