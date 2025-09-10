using System.Text;

namespace Altinn.App.Analyzers.SourceTextGenerator;

internal static class PrepareModelForXmlStorageGenerator
{
    public static void Generate(StringBuilder builder, ModelPathNode rootNode)
    {
        _ = rootNode; // Currently not used as we rely on the reflection implementation
        builder.Append(
            """

                /// <inheritdoc />
                public void PrepareModelForXmlStorage()
                {
                    global::Altinn.App.Core.Helpers.ObjectUtils.PrepareModelForXmlStorage(_dataModel);
                }

            """
        );
    }
}
