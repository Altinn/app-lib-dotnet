namespace Altinn.App.ProcessEngine.Models;

internal abstract record ProcessEngineItem : IDisposable
{
    public long Id { get; set; }
    public required string Key { get; init; }
    public ProcessEngineItemStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public Task? DatabaseTask { get; set; }

    // TODO: Write a test for equality for inheritors. A bit suss on the persistence of these overrides during inheritance
    public virtual bool Equals(ProcessEngineItem? other) =>
        other?.Key.Equals(Key, StringComparison.OrdinalIgnoreCase) is true;

    public override int GetHashCode() => Key.GetHashCode();

    public void Dispose() => DatabaseTask?.Dispose();
}
