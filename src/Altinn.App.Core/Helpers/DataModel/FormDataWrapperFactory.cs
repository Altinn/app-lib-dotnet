using Altinn.App.Core.Features;

namespace Altinn.App.Core.Helpers.DataModel;

internal static class FormDataWrapperFactory
{
    public static IFormDataWrapper Create(object dataModel)
    {
        return new ReflectionFormDataWrapper(dataModel);
    }
}
