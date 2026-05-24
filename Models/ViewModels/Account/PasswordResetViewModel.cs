using System.ComponentModel.DataAnnotations;

public class PasswordResetViewModel
{
    public string Email { get; set; }
    public string Token { get; set; }

    [Required(ErrorMessage = "A senha é obrigatória.")]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    [Required(ErrorMessage = "A confirmação de senha é obrigatória.")]
    [DataType(DataType.Password)]
    // O C# vai olhar para a propriedade "Password" e garantir que o valor seja idêntico
    [Compare("Password", ErrorMessage = "A senha e a confirmação não coincidem.")]
    public string ConfirmPassword { get; set; }
}
