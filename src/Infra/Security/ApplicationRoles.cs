namespace Craftsman.Infra.Security;

public static class ApplicationRoles
{
    public const string Admin = "Admin";
    public const string Operador = "Operador";
    public const string Consulta = "Consulta";

    public const string All = Admin + "," + Operador + "," + Consulta;
    public const string Writers = Admin + "," + Operador;
}
