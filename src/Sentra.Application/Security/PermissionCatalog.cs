namespace Sentra.Application.Security;

public sealed record PermissionDefinition(string Code, string Description);

public static class PermissionCatalog
{
    public const string DashboardRead = "dashboard.read";
    public const string UnitsRead = "units.read";
    public const string UnitsManage = "units.manage";
    public const string ResidentsRead = "residents.read";
    public const string ResidentsManage = "residents.manage";
    public const string EmployeesRead = "employees.read";
    public const string EmployeesManage = "employees.manage";
    public const string ConversationsRead = "conversations.read";
    public const string ConversationsManage = "conversations.manage";
    public const string VisitorsRead = "visitors.read";
    public const string VisitorsManage = "visitors.manage";
    public const string PackagesRead = "packages.read";
    public const string PackagesManage = "packages.manage";
    public const string OccurrencesRead = "occurrences.read";
    public const string OccurrencesManage = "occurrences.manage";
    public const string ShiftsRead = "shifts.read";
    public const string ShiftsManage = "shifts.manage";
    public const string IntegrationsManage = "integrations.manage";
    public const string AuditRead = "audit.read";

    public static IReadOnlyList<PermissionDefinition> All { get; } =
    [
        new(DashboardRead, "Consultar painel operacional."),
        new(UnitsRead, "Consultar blocos e unidades."),
        new(UnitsManage, "Cadastrar e alterar blocos e unidades."),
        new(ResidentsRead, "Consultar moradores."),
        new(ResidentsManage, "Cadastrar e alterar moradores."),
        new(EmployeesRead, "Consultar usuários internos."),
        new(EmployeesManage, "Gerenciar usuários e perfis."),
        new(ConversationsRead, "Consultar conversas operacionais."),
        new(ConversationsManage, "Enviar mensagens e operar conversas."),
        new(VisitorsRead, "Consultar visitantes e autorizações."),
        new(VisitorsManage, "Gerenciar visitantes e autorizações."),
        new(PackagesRead, "Consultar encomendas."),
        new(PackagesManage, "Registrar e entregar encomendas."),
        new(OccurrencesRead, "Consultar ocorrências."),
        new(OccurrencesManage, "Registrar e encerrar ocorrências."),
        new(ShiftsRead, "Consultar turnos."),
        new(ShiftsManage, "Abrir e encerrar turnos."),
        new(IntegrationsManage, "Configurar integrações."),
        new(AuditRead, "Consultar trilha de auditoria.")
    ];
}
