using System.Runtime.CompilerServices;

namespace Moq.Analyzers.Test;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        VerifyNupkg.Initialize();
    }
}
