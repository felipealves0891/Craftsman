using System.ComponentModel.DataAnnotations;

namespace Craftsman.App.Models;

public sealed record UserListItemViewModel(int Id, string Email, IReadOnlyCollection<string> Roles);

public sealed class CreateUserInputModel
{
    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail valido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a senha.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a role.")]
    public string Role { get; set; } = string.Empty;
}
