using System.Diagnostics.CodeAnalysis;

namespace Altinn.App.ProcessEngine.Models;

internal abstract record ProcessEngineDatabaseItem : IDisposable
{
    public required string Identifier { get; init; }
    public ProcessEngineItemStatus Status { get; set; }
    public required DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Task? DatabaseTask { get; set; }

    [MemberNotNullWhen(true, nameof(DatabaseTask))]
    public bool IsUpdatingDatabase => DatabaseTask is not null;

    public override string ToString() => $"{GetType().Name}: {Identifier} ({Status})";

    public bool Equals(ProcessEngineJob? other) =>
        other?.Identifier.Equals(Identifier, StringComparison.OrdinalIgnoreCase) is true;

    public override int GetHashCode() => Identifier.GetHashCode();

    public void Dispose() => DatabaseTask?.Dispose();
}
