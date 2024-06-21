using System.Composition;
using System.Composition.Hosting.Core;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.VisualStudio.Composition;

namespace Moq.Analyzers.Benchmarks.Helpers;

// Originally from https://github.com/dotnet/roslyn-analyzers/blob/f1115edce8633ebe03a86191bc05c6969ed9a821/src/PerformanceTests/Utilities/Common/ExportProviderExtensions.cs
// See https://github.com/dotnet/roslyn-sdk/issues/1165 for discussion on providing these or similar helpers in the testing packages.
internal static class ExportProviderExtensions
{
    public static CompositionContext AsCompositionContext(this ExportProvider exportProvider)
    {
        return new CompositionContextShim(exportProvider);
    }

    private sealed class CompositionContextShim : CompositionContext
    {
        private readonly ExportProvider _exportProvider;

        public CompositionContextShim(ExportProvider exportProvider)
        {
            _exportProvider = exportProvider;
        }

        [SuppressMessage("Maintainability", "AV1500:Member or local function contains too many statements", Justification = "Minimizing divergence from upstream")]
        public override bool TryGetExport(CompositionContract contract, [NotNullWhen(true)] out object? export)
        {
            bool importMany = contract.MetadataConstraints.Contains(new KeyValuePair<string, object>("IsImportMany", true));
            (Type contractType, Type? metadataType) = GetContractType(contract.ContractType, importMany);

            if (metadataType != null)
            {
                MethodInfo methodInfo = (from method in _exportProvider.GetType().GetTypeInfo().GetMethods()
                                  where string.Equals(method.Name, nameof(ExportProvider.GetExports), StringComparison.Ordinal)
                                         where method.IsGenericMethod && method.GetGenericArguments().Length == 2
                                  where method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType == typeof(string)
                                  select method).Single();
                MethodInfo parameterizedMethod = methodInfo.MakeGenericMethod(contractType, metadataType);
                export = parameterizedMethod.Invoke(_exportProvider, [contract.ContractName]);
            }
            else
            {
                MethodInfo methodInfo = (from method in _exportProvider.GetType().GetTypeInfo().GetMethods()
                                  where string.Equals(method.Name, nameof(ExportProvider.GetExports), StringComparison.Ordinal)
                                         where method.IsGenericMethod && method.GetGenericArguments().Length == 1
                                  where method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType == typeof(string)
                                  select method).Single();
                MethodInfo parameterizedMethod = methodInfo.MakeGenericMethod(contractType);
                export = parameterizedMethod.Invoke(_exportProvider, [contract.ContractName]);
            }

#pragma warning disable CS8762 // Parameter must have a non-null value when exiting in some condition.
            return true;
#pragma warning restore CS8762 // Parameter must have a non-null value when exiting in some condition.
        }

        [SuppressMessage("Maintainability", "AV1500:Member or local function contains too many statements", Justification = "Minimizing divergence from upstream")]
        private static (Type ExportType, Type? MetadataType) GetContractType(Type contractType, bool importMany)
        {
            if (importMany && contractType.IsConstructedGenericType &&
                (contractType.GetGenericTypeDefinition() == typeof(IList<>)
                    || contractType.GetGenericTypeDefinition() == typeof(ICollection<>)
                    || contractType.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
            {
                contractType = contractType.GenericTypeArguments[0];
            }

            if (contractType.IsConstructedGenericType)
            {
                if (contractType.GetGenericTypeDefinition() == typeof(Lazy<>))
                {
                    return (contractType.GenericTypeArguments[0], null);
                }
                else if (contractType.GetGenericTypeDefinition() == typeof(Lazy<,>))
                {
                    return (contractType.GenericTypeArguments[0], contractType.GenericTypeArguments[1]);
                }
                else
                {
                    throw new NotSupportedException();
                }
            }

            throw new NotSupportedException();
        }
    }
}
