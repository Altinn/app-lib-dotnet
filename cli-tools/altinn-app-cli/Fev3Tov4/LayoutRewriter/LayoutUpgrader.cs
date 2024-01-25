namespace altinn_app_cli.fev3tov4.LayoutRewriter;

class LayoutUpgrader
{
    private readonly IList<string> warnings = new List<string>();
    private readonly LayoutMutator layoutMutator;
    private readonly bool preserveDefaultTriggers;

    public LayoutUpgrader(string uiFolder, bool preserveDefaultTriggers)
    {
        this.layoutMutator = new LayoutMutator(uiFolder);
        this.preserveDefaultTriggers = preserveDefaultTriggers;
    }

    public IList<string> GetWarnings()
    {
        return warnings.Concat(layoutMutator.GetWarnings()).ToList();
    }

    /**
     * The order of mutators is important, it will do one mutation on all files before moving on to the next
     */
    public void Upgrade()
    {
        layoutMutator.ReadAllLayoutFiles();
        layoutMutator.Mutate(new LikertMutator());
        layoutMutator.Mutate(new RepeatingGroupMutator());
        layoutMutator.Mutate(new GroupMutator());
        layoutMutator.Mutate(new TriggerMutator(preserveDefaultTriggers));
    }

    public async Task Write()
    {
        await layoutMutator.WriteAllLayoutFiles();
    }
}
