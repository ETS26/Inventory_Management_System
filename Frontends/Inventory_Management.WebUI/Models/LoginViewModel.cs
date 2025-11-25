using System.ComponentModel.DataAnnotations;

namespace Inventory_Management.WebUI.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email adresi gereklidir.")]
        [EmailAddress(ErrorMessage = "Geçerli bir email giriniz.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Şifre gereklidir.")]
        public string Password { get; set; }
    }
}
