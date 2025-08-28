namespace Charter.ReporterApp.Domain.Entities
{
    public static class Role
    {
        public const string CharterAdmin = "Charter-Admin";
        public const string RebosaAdmin = "Rebosa-Admin";
        public const string PPRAAdmin = "PPRA-Admin";
        
        public static readonly string[] AllRoles = new[] 
        { 
            CharterAdmin, 
            RebosaAdmin, 
            PPRAAdmin 
        };
        
        public static bool IsValidRole(string role)
        {
            return !string.IsNullOrEmpty(role) && System.Array.Exists(AllRoles, r => r == role);
        }
    }
}