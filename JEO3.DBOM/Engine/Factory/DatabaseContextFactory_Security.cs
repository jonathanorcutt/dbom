using JEO3.Engine.Models;
using JEO3.Providers;
using JEO3.Schema;

namespace JEO3.Engine
{
    public static partial class DatabaseContextFactory
    {
        #region Principals & Permissions

        internal static IReadOnlyList<IDatabasePrincipal> HydratePrincipals(
                    IEnumerable<FlatDatabasePrincipal> principalSources,
                    IEnumerable<FlatDatabasePermission> permissionSources,
                    IEnumerable<FlatRoleMembership> roleMembershipSources)
        {
            var permissions = permissionSources?.ToList() ?? [];
            var memberships = roleMembershipSources?.ToList() ?? [];

            // Group permissions by grantee principal ID
            var permissionsByPrincipal = permissions
                .GroupBy(p => p.GranteePrincipalId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(p => (IPrincipalPermission)new PrincipalPermission
                    {
                        PermissionName = p.PermissionName,
                        StateDescription = p.StateDesc,
                        ClassDescription = p.ClassDesc,
                        SecurableSchemaName = p.SecurableSchemaName,
                        SecurableName = p.SecurableName
                    }).ToList() as IReadOnlyList<IPrincipalPermission>
                );

            // 2. First Pass: Instantiate all base principal objects
            var principalList = new List<IDatabasePrincipal>();

            foreach (var group in principalSources.GroupBy(p => p.PrincipalId))
            {
                var header = group.First();
                var assignedPerms = permissionsByPrincipal.TryGetValue(header.PrincipalId, out var perms)
                    ? perms
                    : Array.Empty<IPrincipalPermission>();

                IDatabasePrincipal principalModel = header.PrincipalType switch
                {
                    "R" => new DatabaseRole
                    {
                        PrincipalId = header.PrincipalId,
                        Name = header.Name,
                        TypeDescription = header.TypeDescription,
                        IsFixedRole = header.IsFixedRole,
                        Permissions = assignedPerms
                    },
                    "A" => new ApplicationRole
                    {
                        PrincipalId = header.PrincipalId,
                        Name = header.Name,
                        TypeDescription = header.TypeDescription,
                        Permissions = assignedPerms
                    },
                    _ => new DatabaseUser
                    {
                        PrincipalId = header.PrincipalId,
                        Name = header.Name,
                        TypeDescription = header.TypeDescription,
                        DefaultSchemaName = header.DefaultSchemaName,
                        Permissions = assignedPerms
                    }
                };

                principalList.Add(principalModel);
            }

            // 3. Second Pass: Wire up Role Memberships using actual object references
            var membersByRole = memberships
                .GroupBy(rm => rm.RolePrincipalId)
                .ToDictionary(g => g.Key, g => g.Select(rm => rm.MemberPrincipalId).ToHashSet());

            foreach (var principal in principalList)
            {
                if (principal is DatabaseRole role && membersByRole.TryGetValue(role.PrincipalId, out var memberIds))
                {
                    role.Members = principalList
                        .Where(p => memberIds.Contains(p.PrincipalId))
                        .ToList();
                }
            }

            return principalList;
        }

        #endregion
    }
}
