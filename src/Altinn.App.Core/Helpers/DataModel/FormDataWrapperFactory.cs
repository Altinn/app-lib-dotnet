using System.Collections.Frozen;
using System.Diagnostics;
using System.Reflection;
using Altinn.App.Core.Features;

namespace Altinn.App.Core.Helpers.DataModel;

internal static class FormDataWrapperFactory
{
    private static readonly FrozenDictionary<Type, Type> _pathAccessors = InitializePathAccessorLookup();

    private static FrozenDictionary<Type, Type> InitializePathAccessorLookup()
    {
        return Assembly
            .GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsClass && t.IsAssignableTo(typeof(IFormDataWrapper<>)))
            .ToFrozenDictionary(k => k.GenericTypeArguments[0], v => v);
    }

    public static IFormDataWrapper Create(object dataModel)
    {
        if (_pathAccessors.TryGetValue(dataModel.GetType(), out var accessorType))
        {
            return Activator.CreateInstance(accessorType, dataModel) as IFormDataWrapper
                ?? throw new UnreachableException($"Failed to create path accessor for {dataModel.GetType().FullName}");
        }

        return new ReflectionFormDataWrapper(dataModel);
    }
}
