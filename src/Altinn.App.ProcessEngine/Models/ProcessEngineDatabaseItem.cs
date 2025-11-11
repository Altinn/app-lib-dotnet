namespace Altinn.App.ProcessEngine.Models;

internal abstract record ProcessEngineDatabaseItem : IDisposable
{
    public required string Identifier { get; init; }
    public ProcessEngineItemStatus Status { get; set; }
    public required DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Task? DatabaseTask { get; set; }

    public override string ToString() => $"{GetType().Name}: {Identifier} ({Status})";

    public bool Equals(ProcessEngineJob? other) =>
        other?.Identifier.Equals(Identifier, StringComparison.OrdinalIgnoreCase) is true;

    public override int GetHashCode() => Identifier.GetHashCode();

    public void Dispose() => DatabaseTask?.Dispose();
}
