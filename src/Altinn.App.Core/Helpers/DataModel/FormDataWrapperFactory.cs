using System.Collections.Frozen;
using System.Diagnostics;
using Altinn.App.Core.Features;

namespace Altinn.App.Core.Helpers.DataModel;

internal static class FormDataWrapperFactory
{
    private static readonly FrozenDictionary<Type, Type> _pathAccessors = InitializePathAccessorLookup();

    private static FrozenDictionary<Type, Type> InitializePathAccessorLookup()
    {
        var interfaceType = typeof(IFormDataWrapper<>);
        return AppDomain
            .CurrentDomain.GetAssemblies()
            .SelectMany(a =>
            {
                List<KeyValuePair<Type, Type>> pathAccessors = new();
                var assemblyTypes = a.GetExportedTypes();
                foreach (var type in assemblyTypes)
                {
                    var formDataWrapperInterface = type.GetInterfaces()
                        .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == interfaceType);
                    if (formDataWrapperInterface is not null)
                    {
                        pathAccessors.Add(KeyValuePair.Create(formDataWrapperInterface.GenericTypeArguments[0], type));
                    }
                }
                return pathAccessors;
            })
            .ToFrozenDictionary();
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
