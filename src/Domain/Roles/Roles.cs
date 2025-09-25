namespace Charter.Reporter.Domain.Roles;

public static class AppRoles
{
    public const string CharterAdmin = "CharterAdmin";
    public const string RebosaAdmin = "RebosaAdmin";
    public const string PpraAdmin = "PpraAdmin";

    public static readonly string[] All = new[] { CharterAdmin, RebosaAdmin, PpraAdmin };
}


