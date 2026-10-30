namespace JEO3.Schema
{

    public sealed class PrincipalPermission : IPrincipalPermission
    {
        public int GranteePrincipalId { get; internal init; }
        public int GrantorPrincipalId { get; internal init; }
        public string? PermissionName { get; internal init; }
        public string? StateDescription { get; internal init; }
        public string? ClassDescription { get; internal init; }
        public string? SecurableSchemaName { get; internal init; }
        public string? SecurableName { get; internal init; }
        public IDatabasePrincipal? Grantee { get; internal init; }
        public IDatabasePrincipal? Grantor { get; internal init; }
    }
}
