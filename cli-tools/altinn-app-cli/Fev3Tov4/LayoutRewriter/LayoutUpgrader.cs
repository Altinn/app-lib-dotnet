namespace altinn_app_cli.fev3tov4.LayoutRewriter;

class LayoutUpgrader
{
    private readonly IList<string> warnings = new List<string>();
    private readonly LayoutMutator layoutMutator;

    public LayoutUpgrader(string uiFolder)
    {
        this.layoutMutator = new LayoutMutator(uiFolder);
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
    }

    public async Task Write()
    {
        await layoutMutator.WriteAllLayoutFiles();
    }
}
