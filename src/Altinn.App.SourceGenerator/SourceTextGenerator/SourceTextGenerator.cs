using System.Text;

namespace Altinn.App.SourceGenerator.SourceTextGenerator;

public static class SourceTextGenerator
{
    public static string GenerateSourceText(ModelPathNode rootNode, string classModifier)
    {
        var builder = new StringBuilder();
        builder.Append("// <auto-generated/>\r\n");
        var className = $"{rootNode.Name}FormDataWrapper";
        builder.Append(
            $$"""
            #nullable enable
            using System.Diagnostics.CodeAnalysis;
            using Altinn.App.Core.Features;
            using Altinn.App.Core.Helpers;

            {{classModifier}} class {{className}}
                : IFormDataWrapper<global::{{rootNode.Type}}>
            {
                private readonly global::{{rootNode.Type}} _dataModel;

                public Type BackingDataType => typeof(global::{{rootNode.Type}});

                public T BackingData<T>()
                    where T : class
                {
                    return _dataModel as T
                        ?? throw new InvalidCastException(
                            $"Attempted to cast data model of type global::{{rootNode.Type}} to {typeof(T).FullName}"
                        );
                }

                public {{rootNode.Name}}FormDataWrapper(object dataModel)
                {
                    _dataModel =
                        dataModel as global::{{rootNode.Type}}
                        ?? throw new ArgumentException(
                            $"Data model must be of type {{rootNode.Type}}, (was {dataModel.GetType().FullName})"
                        );
                }
            
            """
        );
        builder.Append("\r\n    #region Getters\r\n");
        GetterGenerator.Generate(builder, rootNode);
        builder.Append("\r\n    #endregion Getters\r\n");
        builder.Append("    #region AddIndexToPath\r\n");
        AddIndexToPathGenerator.Generate(builder, rootNode);
        builder.Append("\r\n    #endregion AddIndexToPath\r\n");
        builder.Append("    #region Copy\r\n");
        CopyGenerator.Generate(builder, rootNode, className);
        builder.Append("\r\n    #endregion Copy\r\n");
        builder.Append("    #region Remove\r\n");
        RemoveGenerator.Generate(builder, rootNode);
        builder.Append("\r\n    #endregion Remove\r\n");
        builder.Append("    #region AltinnRowIds\r\n");
        AltinnRowIdsGenerator.Generate(builder, rootNode);
        builder.Append("\r\n    #endregion AltinnRowIds\r\n");
        builder.Append("    #region XmlStorage\r\n");
        PrepareModelForXmlStorageGenerator.Generate(builder, rootNode);
        builder.Append("\r\n    #endregion XmlStorage\r\n");
        builder.Append("}\r\n");

        return builder.ToString();
    }
}
