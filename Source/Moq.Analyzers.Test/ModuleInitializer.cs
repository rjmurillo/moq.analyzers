namespace Moq.Analyzers.Test
{
    using System.Runtime.CompilerServices;
    using VerifyTests;

    public static class ModuleInitializer
    {
        [ModuleInitializer]
        public static void Initialize()
        {
            VerifyNupkg.Initialize();
        }
    }
}
