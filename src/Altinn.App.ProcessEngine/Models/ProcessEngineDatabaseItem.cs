namespace Altinn.App.ProcessEngine.Models;

internal abstract record ProcessEngineDatabaseItem : IDisposable
{
    public required string Identifier { get; init; }
    public ProcessEngineItemStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public Task? DatabaseTask { get; set; }

    // TODO: Write a test for equality for inheritors. A bit suss on the persistence of these overrides during inheritance
    public virtual bool Equals(ProcessEngineDatabaseItem? other) =>
        other?.Identifier.Equals(Identifier, StringComparison.OrdinalIgnoreCase) is true;

    public override int GetHashCode() => Identifier.GetHashCode();

    public void Dispose() => DatabaseTask?.Dispose();
}
