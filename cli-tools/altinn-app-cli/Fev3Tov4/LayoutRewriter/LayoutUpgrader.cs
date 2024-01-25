namespace altinn_app_cli.fev3tov4.LayoutRewriter;

class LayoutUpgrader
{
    private readonly IList<string> warnings = new List<string>();
    private readonly LayoutMutator layoutMutator;
    private readonly bool preserveDefaultTriggers;
    private readonly bool convertGroupTitles;

    public LayoutUpgrader(string uiFolder, bool preserveDefaultTriggers, bool convertGroupTitles)
    {
        this.layoutMutator = new LayoutMutator(uiFolder);
        this.preserveDefaultTriggers = preserveDefaultTriggers;
        this.convertGroupTitles = convertGroupTitles;
    }

    public IList<string> GetWarnings()
    {
        // TODO: Since multiple mutators can add identical warnings, we should probably deduplicate them
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
        layoutMutator.Mutate(new TriggerMutator(this.preserveDefaultTriggers));
        layoutMutator.Mutate(new TrbMutator(this.convertGroupTitles));
    }

    public async Task Write()
    {
        await layoutMutator.WriteAllLayoutFiles();
    }
}
