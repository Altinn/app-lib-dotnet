internal sealed class AppCodesSettings
{
    public AppCodesInner AppCodes { get; set; } = new();
}

internal sealed class AppCodesInner
{
    public List<string> Monthly { get; set; } = [];
}
