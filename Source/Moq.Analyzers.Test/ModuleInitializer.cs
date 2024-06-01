using System.Runtime.CompilerServices;
using VerifyTests;

namespace Moq.Analyzers.Test;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        VerifyNupkg.Initialize();
    }
}
