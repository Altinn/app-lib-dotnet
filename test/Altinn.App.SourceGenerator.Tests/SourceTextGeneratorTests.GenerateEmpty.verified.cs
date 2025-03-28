#nullable enable
using System.Diagnostics.CodeAnalysis;
using Altinn.App.Core.Features;
using Altinn.App.Core.Helpers;

internal class Altinn_App_SourceGenerator_Tests_EmptyFormDataWrapper
    : IFormDataWrapper<global::Altinn.App.SourceGenerator.Tests.Empty>
{
    private readonly global::Altinn.App.SourceGenerator.Tests.Empty _dataModel;

    public Type BackingDataType => typeof(global::Altinn.App.SourceGenerator.Tests.Empty);

    public T BackingData<T>()
        where T : class
    {
        return _dataModel as T
            ?? throw new InvalidCastException(
                $"Attempted to cast data model of type global::Altinn.App.SourceGenerator.Tests.Empty to {typeof(T).FullName}"
            );
    }

    public Altinn_App_SourceGenerator_Tests_EmptyFormDataWrapper(object dataModel)
    {
        _dataModel =
            dataModel as global::Altinn.App.SourceGenerator.Tests.Empty
            ?? throw new ArgumentException(
                $"Data model must be of type Altinn.App.SourceGenerator.Tests.Empty, (was {dataModel.GetType().FullName})"
            );
    }

    /// <inheritdoc />
    public object? GetRaw(ReadOnlySpan<char> path) => null;

    /// <inheritdoc />
    public bool TryAddIndexToPath(
        ReadOnlySpan<char> path,
        ReadOnlySpan<int> rowIndexes,
        Span<char> buffer,
        out ReadOnlySpan<char> indexedPath
    )
    {
        indexedPath = path;
        return true;
    }

    /// <inheritdoc />
    public IFormDataWrapper Copy()
    {
        return new Altinn_App_SourceGenerator_Tests_EmptyFormDataWrapper(CopyRecursive(_dataModel));
    }

    [return: NotNullIfNotNull("data")]
    private static global::Altinn.App.SourceGenerator.Tests.Empty? CopyRecursive(
        global::Altinn.App.SourceGenerator.Tests.Empty? data
    )
    {
        if (data is null)
        {
            return null;
        }

        return new();
    }

    /// <inheritdoc />
    public void RemoveField(ReadOnlySpan<char> path, RowRemovalOption rowRemovalOption) { }

    /// <inheritdoc />
    public void RemoveAltinnRowIds() { }

    /// <inheritdoc />
    public void InitializeAltinnRowIds() { }

    /// <inheritdoc />
    public void PrepareModelForXmlStorage()
    {
        ObjectUtils.PrepareModelForXmlStorage(_dataModel);
    }
}
