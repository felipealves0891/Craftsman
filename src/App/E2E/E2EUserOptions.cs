namespace Craftsman.App.E2E;

public sealed class E2EUserOptions
{
    public const string SectionName = "E2EUsers";

    public string AdminEmail { get; set; } = "e2e.admin@craftsman.local";

    public string OperadorEmail { get; set; } = "e2e.operador@craftsman.local";

    public string ConsultaEmail { get; set; } = "e2e.consulta@craftsman.local";

    public string Password { get; set; } = "E2e_user_123!";
}
