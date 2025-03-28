#nullable enable
using System.Diagnostics.CodeAnalysis;
using Altinn.App.Core.Features;
using Altinn.App.Core.Helpers;

internal class Altinn_App_SourceGenerator_Tests_SkjemaFormDataWrapper
    : IFormDataWrapper<global::Altinn.App.SourceGenerator.Tests.Skjema>
{
    private readonly global::Altinn.App.SourceGenerator.Tests.Skjema _dataModel;

    public Type BackingDataType => typeof(global::Altinn.App.SourceGenerator.Tests.Skjema);

    public T BackingData<T>()
        where T : class
    {
        return _dataModel as T
            ?? throw new InvalidCastException(
                $"Attempted to cast data model of type global::Altinn.App.SourceGenerator.Tests.Skjema to {typeof(T).FullName}"
            );
    }

    public Altinn_App_SourceGenerator_Tests_SkjemaFormDataWrapper(object dataModel)
    {
        _dataModel =
            dataModel as global::Altinn.App.SourceGenerator.Tests.Skjema
            ?? throw new ArgumentException(
                $"Data model must be of type Altinn.App.SourceGenerator.Tests.Skjema, (was {dataModel.GetType().FullName})"
            );
    }

    /// <inheritdoc />
    public object? GetRaw(ReadOnlySpan<char> path)
    {
        if (path.IsEmpty)
        {
            return null;
        }

        return GetRecursive(_dataModel, path, 0);
    }

    private static object? GetRecursive(
        global::Altinn.App.SourceGenerator.Tests.Skjema? model,
        ReadOnlySpan<char> path,
        int offset
    )
    {
        return PathHelper.GetNextSegment(path, offset, out int nextOffset) switch
        {
            "skjemanummer" when nextOffset is -1 => model?.Skjemanummer,
            "skjemaversjon" when nextOffset is -1 => model?.Skjemaversjon,
            "skjemainnhold" => GetRecursive(model?.Skjemainnhold, path, nextOffset),
            "eierAdresse" => GetRecursive(model?.EierAdresse, path, nextOffset),
            "" => model,
            // _ => throw new ArgumentException("{path} is not a valid path."),
            _ => null,
        };
    }

    private static object? GetRecursive(
        global::System.Collections.Generic.List<global::Altinn.App.SourceGenerator.Tests.SkjemaInnhold?>? model,
        ReadOnlySpan<char> path,
        int offset
    )
    {
        int index = PathHelper.GetIndex(path, offset, out int nextOffset);
        if (index < 0 || index >= model?.Count)
        {
            // throw new IndexOutOfRangeException($"Index {index} is out of range for list of length {model.Count}.");
            return null;
        }

        return GetRecursive(model?[index], path, nextOffset);
    }

    private static object? GetRecursive(
        global::Altinn.App.SourceGenerator.Tests.SkjemaInnhold? model,
        ReadOnlySpan<char> path,
        int offset
    )
    {
        return PathHelper.GetNextSegment(path, offset, out int nextOffset) switch
        {
            "altinnRowId" when nextOffset is -1 => model?.AltinnRowId,
            "navn" when nextOffset is -1 => model?.Navn,
            "alder" when nextOffset is -1 => model?.Alder,
            "deltar" when nextOffset is -1 => model?.Deltar,
            "adresse" => GetRecursive(model?.Adresse, path, nextOffset),
            "tidligere-adresse" => GetRecursive(model?.TidligereAdresse, path, nextOffset),
            "" => model,
            // _ => throw new ArgumentException("{path} is not a valid path."),
            _ => null,
        };
    }

    private static object? GetRecursive(
        global::Altinn.App.SourceGenerator.Tests.Adresse? model,
        ReadOnlySpan<char> path,
        int offset
    )
    {
        return PathHelper.GetNextSegment(path, offset, out int nextOffset) switch
        {
            "altinnRowId" when nextOffset is -1 => model?.AltinnRowId,
            "gate" when nextOffset is -1 => model?.Gate,
            "postnummer" when nextOffset is -1 => model?.Postnummer,
            "poststed" when nextOffset is -1 => model?.Poststed,
            "" => model,
            // _ => throw new ArgumentException("{path} is not a valid path."),
            _ => null,
        };
    }

    private static object? GetRecursive(
        global::System.Collections.Generic.List<global::Altinn.App.SourceGenerator.Tests.Adresse?>? model,
        ReadOnlySpan<char> path,
        int offset
    )
    {
        int index = PathHelper.GetIndex(path, offset, out int nextOffset);
        if (index < 0 || index >= model?.Count)
        {
            // throw new IndexOutOfRangeException($"Index {index} is out of range for list of length {model.Count}.");
            return null;
        }

        return GetRecursive(model?[index], path, nextOffset);
    }

    /// <inheritdoc />
    public bool TryAddIndexToPath(
        ReadOnlySpan<char> path,
        ReadOnlySpan<int> rowIndexes,
        Span<char> buffer,
        out ReadOnlySpan<char> indexedPath
    )
    {
        if (path.IsEmpty)
        {
            indexedPath = path;
            return false;
        }

        return TryAddIndexToPathRecursive(
            _dataModel,
            path,
            rowIndexes,
            buffer,
            out indexedPath
        );
    }

    private static void TryAddIndexToPathRecursive(Altinn.App.SourceGenerator.Tests.Skjema dataModel, bool initialize)
    {
        if (dataModel.Skjemainnhold is { } group0)
        {
            foreach (var row in group0)
            {
                if (row is not null)
                {
                    TryAddIndexToPathRecursive(row, initialize);
                }
            }
        }
    }

    private static void TryAddIndexToPathRecursive(Altinn.App.SourceGenerator.Tests.SkjemaInnhold dataModel, bool initialize)
    {
        if (dataModel.TidligereAdresse is { } group0)
        {
            foreach (var row in group0)
            {
                if (row is not null)
                {
                    TryAddIndexToPathRecursive(row, initialize);
                }
            }
        }
    }

    private static void TryAddIndexToPathRecursive(Altinn.App.SourceGenerator.Tests.Adresse dataModel, bool initialize)
    {
    }

    /// <inheritdoc />
    public IFormDataWrapper Copy()
    {
        return new Altinn_App_SourceGenerator_Tests_SkjemaFormDataWrapper(CopyRecursive(_dataModel));
    }

    [return: NotNullIfNotNull("data")]
    private static global::Altinn.App.SourceGenerator.Tests.Skjema? CopyRecursive(
        global::Altinn.App.SourceGenerator.Tests.Skjema? data
    )
    {
        if (data is null)
        {
            return null;
        }

        return new()
        {
            Skjemanummer = data.Skjemanummer,
            Skjemaversjon = data.Skjemaversjon,
            Skjemainnhold = CopyRecursive(data.Skjemainnhold),
            EierAdresse = CopyRecursive(data.EierAdresse),
        };
    }

    [return: NotNullIfNotNull("list")]
    private static global::System.Collections.Generic.List<global::Altinn.App.SourceGenerator.Tests.SkjemaInnhold?>? CopyRecursive(
        global::System.Collections.Generic.List<global::Altinn.App.SourceGenerator.Tests.SkjemaInnhold?>? list
    )
    {
        if (list is null)
        {
            return null;
        }

        return [.. list.Select(CopyRecursive)];
    }

    [return: NotNullIfNotNull("data")]
    private static global::Altinn.App.SourceGenerator.Tests.SkjemaInnhold? CopyRecursive(
        global::Altinn.App.SourceGenerator.Tests.SkjemaInnhold? data
    )
    {
        if (data is null)
        {
            return null;
        }

        return new()
        {
            AltinnRowId = data.AltinnRowId,
            Navn = data.Navn,
            Alder = data.Alder,
            Deltar = data.Deltar,
            Adresse = CopyRecursive(data.Adresse),
            TidligereAdresse = CopyRecursive(data.TidligereAdresse),
        };
    }

    [return: NotNullIfNotNull("data")]
    private static global::Altinn.App.SourceGenerator.Tests.Adresse? CopyRecursive(
        global::Altinn.App.SourceGenerator.Tests.Adresse? data
    )
    {
        if (data is null)
        {
            return null;
        }

        return new()
        {
            AltinnRowId = data.AltinnRowId,
            Gate = data.Gate,
            Postnummer = data.Postnummer,
            Poststed = data.Poststed,
        };
    }

    [return: NotNullIfNotNull("list")]
    private static global::System.Collections.Generic.List<global::Altinn.App.SourceGenerator.Tests.Adresse?>? CopyRecursive(
        global::System.Collections.Generic.List<global::Altinn.App.SourceGenerator.Tests.Adresse?>? list
    )
    {
        if (list is null)
        {
            return null;
        }

        return [.. list.Select(CopyRecursive)];
    }

    /// <inheritdoc />
    public void RemoveField(ReadOnlySpan<char> path, RowRemovalOption rowRemovalOption)
    {
        if (path.IsEmpty)
        {
            return;
        }

        RemoveRecursive(_dataModel, path, 0, rowRemovalOption);
    }

    private static void RemoveRecursive(
        global::Altinn.App.SourceGenerator.Tests.Skjema? model,
        ReadOnlySpan<char> path,
        int offset,
        RowRemovalOption rowRemovalOption
    )
    {
        if (model is null)
        {
            return;
        }
        switch (PathHelper.GetNextSegment(path, offset, out int nextOffset))
        {
            case "skjemanummer" when nextOffset is -1:
                model.Skjemanummer = null;
                break;
            case "skjemaversjon" when nextOffset is -1:
                model.Skjemaversjon = null;
                break;
            case "skjemainnhold" when nextOffset is -1:
                model.Skjemainnhold = null;
                break;
            case "skjemainnhold":
                RemoveRecursive(model.Skjemainnhold, path, nextOffset, rowRemovalOption);
                break;
            case "eierAdresse" when nextOffset is -1:
                model.EierAdresse = null;
                break;
            case "eierAdresse":
                RemoveRecursive(model.EierAdresse, path, nextOffset, rowRemovalOption);
                break;
            default:
                // throw new ArgumentException("{path} is not a valid path.");
                return;
        }
    }

    private static void RemoveRecursive(
        global::System.Collections.Generic.List<global::Altinn.App.SourceGenerator.Tests.SkjemaInnhold?>? model,
        ReadOnlySpan<char> path,
        int offset,
        RowRemovalOption rowRemovalOption
    )
    {
        int index = PathHelper.GetIndex(path, offset, out int nextOffset);
        if (index < 0 || index >= model?.Count || model is null)
        {
            return;
        }
        if (nextOffset == -1)
        {
            switch (rowRemovalOption)
            {
                case RowRemovalOption.DeleteRow:
                    model.RemoveAt(index);
                    break;
                case RowRemovalOption.SetToNull:
                    model[index] = null;
                    break;
            }
        }
        else
        {
            RemoveRecursive(model?[index], path, nextOffset, rowRemovalOption);
        }
    }

    private static void RemoveRecursive(
        global::Altinn.App.SourceGenerator.Tests.SkjemaInnhold? model,
        ReadOnlySpan<char> path,
        int offset,
        RowRemovalOption rowRemovalOption
    )
    {
        if (model is null)
        {
            return;
        }
        switch (PathHelper.GetNextSegment(path, offset, out int nextOffset))
        {
            case "navn" when nextOffset is -1:
                model.Navn = null;
                break;
            case "alder" when nextOffset is -1:
                model.Alder = null;
                break;
            case "deltar" when nextOffset is -1:
                model.Deltar = null;
                break;
            case "adresse" when nextOffset is -1:
                model.Adresse = null;
                break;
            case "adresse":
                RemoveRecursive(model.Adresse, path, nextOffset, rowRemovalOption);
                break;
            case "tidligere-adresse" when nextOffset is -1:
                model.TidligereAdresse = null;
                break;
            case "tidligere-adresse":
                RemoveRecursive(model.TidligereAdresse, path, nextOffset, rowRemovalOption);
                break;
            default:
                // throw new ArgumentException("{path} is not a valid path.");
                return;
        }
    }

    private static void RemoveRecursive(
        global::Altinn.App.SourceGenerator.Tests.Adresse? model,
        ReadOnlySpan<char> path,
        int offset,
        RowRemovalOption rowRemovalOption
    )
    {
        if (model is null)
        {
            return;
        }
        switch (PathHelper.GetNextSegment(path, offset, out int nextOffset))
        {
            case "gate" when nextOffset is -1:
                model.Gate = null;
                break;
            case "postnummer" when nextOffset is -1:
                model.Postnummer = null;
                break;
            case "poststed" when nextOffset is -1:
                model.Poststed = null;
                break;
            default:
                // throw new ArgumentException("{path} is not a valid path.");
                return;
        }
    }

    private static void RemoveRecursive(
        global::System.Collections.Generic.List<global::Altinn.App.SourceGenerator.Tests.Adresse?>? model,
        ReadOnlySpan<char> path,
        int offset,
        RowRemovalOption rowRemovalOption
    )
    {
        int index = PathHelper.GetIndex(path, offset, out int nextOffset);
        if (index < 0 || index >= model?.Count || model is null)
        {
            return;
        }
        if (nextOffset == -1)
        {
            switch (rowRemovalOption)
            {
                case RowRemovalOption.DeleteRow:
                    model.RemoveAt(index);
                    break;
                case RowRemovalOption.SetToNull:
                    model[index] = null;
                    break;
            }
        }
        else
        {
            RemoveRecursive(model?[index], path, nextOffset, rowRemovalOption);
        }
    }

    /// <inheritdoc />
    public void RemoveAltinnRowIds()
    {
        SetAltinnRowIds(_dataModel, initialize: false);
    }

    /// <inheritdoc />
    public void InitializeAltinnRowIds()
    {
        SetAltinnRowIds(_dataModel, initialize: true);
    }

    private static void SetAltinnRowIds(Altinn.App.SourceGenerator.Tests.Skjema dataModel, bool initialize)
    {
        if (dataModel.Skjemainnhold is { } group0)
        {
            foreach (var row in group0)
            {
                if (row is not null)
                {
                    SetAltinnRowIds(row, initialize);
                }
            }
        }
    }

    private static void SetAltinnRowIds(Altinn.App.SourceGenerator.Tests.SkjemaInnhold dataModel, bool initialize)
    {
        dataModel.AltinnRowId = initialize ? Guid.NewGuid() : Guid.Empty;
        if (dataModel.TidligereAdresse is { } group0)
        {
            foreach (var row in group0)
            {
                if (row is not null)
                {
                    SetAltinnRowIds(row, initialize);
                }
            }
        }
    }

    private static void SetAltinnRowIds(Altinn.App.SourceGenerator.Tests.Adresse dataModel, bool initialize)
    {
        dataModel.AltinnRowId = initialize ? Guid.NewGuid() : Guid.Empty;
    }

    /// <inheritdoc />
    public void PrepareModelForXmlStorage()
    {
        ObjectUtils.PrepareModelForXmlStorage(_dataModel);
    }
}
