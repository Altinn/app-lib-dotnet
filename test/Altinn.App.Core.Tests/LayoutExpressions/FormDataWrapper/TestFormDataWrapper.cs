using Altinn.App.Core.Features;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Helpers.DataModel;

namespace Altinn.App.Core.Tests.LayoutExpressions.FormDataWrapper;

file class TestType
{
    public TestInfo? Info { get; set; }
    public List<Certificate?>? Certificates { get; set; }
}

file class TestInfo
{
    public string? Name { get; set; }
    public int? Age { get; set; }
}

file class Certificate
{
    public string? Title { get; set; }
    public string? Issuer { get; set; }
}

file class TestTypeFormDataWrapper : IFormDataWrapper<TestType>
{
    private readonly TestType _dataModel;
    public Type BackingDataType => typeof(TestType);

    public T BackingData<T>()
        where T : class
    {
        return _dataModel as T
            ?? throw new InvalidCastException(
                $"Attempted to cast data model of type {typeof(TestType).FullName} to {typeof(T).FullName}"
            );
    }

    public object? GetRaw(ReadOnlySpan<char> path = default)
    {
        return GetInternal(_dataModel, path, 0);
    }

    public TestTypeFormDataWrapper(object dataModel)
    {
        _dataModel =
            dataModel as TestType
            ?? throw new ArgumentException(
                $"Data model must be of type {typeof(TestType).FullName}, (was {dataModel.GetType().FullName})"
            );
    }

    private static object? GetInternal(TestType dataModel, ReadOnlySpan<char> path, int offset)
    {
        switch (path.Slice(offset))
        {
            case "Info.Name":
                return dataModel.Info?.Name;
            case "Info.Age":
                return dataModel.Info?.Age;
        }
        switch (path.Slice(0, 11))
        {
            case "Certificates":
                return GetInternal(dataModel.Certificates, path, offset + 12);
        }

        // if (throwIfInvalid)
        //     throw new ArgumentException(
        //         $"""Path "{path}" not found in data model of type {typeof(TestType).FullName}"""
        //     );
        return null;
    }

    private static object? GetInternal(List<Certificate?>? certificates, ReadOnlySpan<char> path, int offset)
    {
        ParseIndex(path, offset, out int index, out int newOffset);

        if (index < 0)
        {
            return certificates;
        }
        var certificate = certificates?.ElementAtOrDefault(index);
        return GetInternal(certificate, path, newOffset);
    }

    private static object? GetInternal(Certificate? certificate, ReadOnlySpan<char> path, int offset)
    {
        switch (path.Slice(offset))
        {
            case "Title":
                return certificate?.Title;
            case "Issuer":
                return certificate?.Issuer;
        }

        throw new ArgumentException(
            $"""Path "{path}" not found in data model of type {typeof(Certificate).FullName}"""
        );
    }

    private static void ParseIndex(ReadOnlySpan<char> fullPath, int offset, out int index, out int newOffset)
    {
        var path = fullPath.Slice(offset);
        if (path.Length == 0)
        {
            index = -1;
            newOffset = offset;
            return;
        }
        var i = path.IndexOf('[');
        if (i < 0)
        {
            throw new ArgumentException($"Invalid path: {fullPath}, missing '['");
        }
        var endIndex = path.IndexOf(']');
        if (endIndex < 0)
        {
            throw new ArgumentException($"Invalid path: {path}, unmatched '['");
        }
        if (!int.TryParse(path.Slice(1, endIndex - 1), out index))
        {
            throw new ArgumentException($"Invalid path: {path}, index is not a number");
        }
        newOffset = offset + endIndex + 1;
    }

    public void RemoveField(ReadOnlySpan<char> path, RowRemovalOption rowRemovalOption)
    {
        switch (path)
        {
            case "Info.Name":
                if (_dataModel.Info != null)
                {
                    _dataModel.Info.Name = null;
                }

                return;
            case "Info.Age":
                if (_dataModel.Info != null)
                {
                    _dataModel.Info.Age = null;
                }

                return;
        }

        // if (throwIfInvalid)
        //     throw new InvalidOperationException(
        //         $"Path {path} not found in data model of type {typeof(TestType).FullName}"
        //     );
    }

    public bool TryAddIndexToPath(
        ReadOnlySpan<char> path,
        ReadOnlySpan<int> rowIndexes,
        Span<char> buffer,
        out ReadOnlySpan<char> indexedPath
    )
    {
        throw new NotImplementedException();
    }

    public IFormDataWrapper Copy()
    {
        return FormDataWrapperFactory.Create(
            new TestType()
            {
                Info = new() { Name = _dataModel.Info?.Name, Age = _dataModel.Info?.Age },
                Certificates = _dataModel.Certificates?.Select(c => c).ToList(),
            }
        );
    }

    public void RemoveAltinnRowIds()
    {
        throw new NotImplementedException();
    }

    public void InitializeAltinnRowIds()
    {
        throw new NotImplementedException();
    }

    public void PrepareModelForXmlStorage()
    {
        throw new NotImplementedException();
    }
}
