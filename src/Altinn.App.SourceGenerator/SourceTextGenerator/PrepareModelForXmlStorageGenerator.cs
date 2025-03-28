using System.Text;

namespace Altinn.App.SourceGenerator.SourceTextGenerator;

public static class PrepareModelForXmlStorageGenerator
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
