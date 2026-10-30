namespace JEO3.Schema
{
    public interface IPrincipalPermission
    {
        int GranteePrincipalId { get; }
        int GrantorPrincipalId { get; }

        string? PermissionName { get; }
        string? StateDescription { get; }
        string? ClassDescription { get; }
        string? SecurableName { get; }

        IDatabasePrincipal? Grantee { get; }
        IDatabasePrincipal? Grantor { get; }
    }
}
