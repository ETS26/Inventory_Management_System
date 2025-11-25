namespace Inventory_Management.WebUI.Models
{
    public class LoginResponseDto
    {
        public bool IsSuccess { get; set; }
        public string Token { get; set; } // JWT Token buraya gelecek
        public string FullName { get; set; }
        public string ErrorMessage { get; set; }
    }
}
