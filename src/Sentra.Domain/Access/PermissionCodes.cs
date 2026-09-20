namespace Sentra.Domain.Access;

public static class PermissionCodes
{
    public const string CondominiumRead = "condominium.read";
    public const string CondominiumWrite = "condominium.write";
    public const string ResidentsRead = "residents.read";
    public const string ResidentsWrite = "residents.write";
    public const string EmployeesRead = "employees.read";
    public const string EmployeesManage = "employees.manage";
    public const string RolesManage = "roles.manage";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        CondominiumRead,
        CondominiumWrite,
        ResidentsRead,
        ResidentsWrite,
        EmployeesRead,
        EmployeesManage,
        RolesManage
    };
}
