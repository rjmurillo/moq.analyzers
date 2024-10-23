﻿namespace Moq.Analyzers.Common;

internal static class WellKnownTypeNames
{
    internal static readonly string MoqNamespace = "Moq";
    internal static readonly string MoqSymbolName = "Moq";
    internal static readonly string Mock = nameof(Mock);
    internal static readonly string MockBehavior = nameof(MockBehavior);
    internal static readonly string MockFactory = nameof(MockFactory);
    internal static readonly string MoqMock = $"{MoqNamespace}.{Mock}";
    internal static readonly string MoqMock1 = $"{MoqMock}`1";
    internal static readonly string MoqBehavior = $"{MoqNamespace}.{MockBehavior}";
    internal static readonly string MoqRepository = $"{MoqNamespace}.MockRepository";
    internal static readonly string As = nameof(As);
    internal static readonly string Create = nameof(Create);
    internal static readonly string Of = nameof(Of);
}
