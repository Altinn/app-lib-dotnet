using System.Text;

namespace Altinn.App.Analyzers.SourceTextGenerator;

internal static class PrepareModelForXmlStorageGenerator
{
    public static void Generate(StringBuilder builder, ModelPathNode rootNode)
    {
        builder.Append(
            """

                /// <inheritdoc />
                public void PrepareModelForXmlStorage()
                {
                    ObjectUtils.PrepareModelForXmlStorage(_dataModel);
                }

            """
        );
    }
}
