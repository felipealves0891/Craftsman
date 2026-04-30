namespace Craftsman.Infra.Security;

public static class AuditAction
{
    public const string Created = "Created";
    public const string Modified = "Modified";
    public const string Deleted = "Deleted";
    public const string LoginSucceeded = "LoginSucceeded";
    public const string LoginFailed = "LoginFailed";
    public const string Logout = "Logout";
    public const string AccessDenied = "AccessDenied";
    public const string ManualImport = "ManualImport";
    public const string SendToProduction = "SendToProduction";
    public const string ProductionAdvanced = "ProductionAdvanced";
    public const string ShipmentStatusUpdated = "ShipmentStatusUpdated";
    public const string UserRoleChanged = "UserRoleChanged";
}
