using Inventory_Management.Domain.Entities;

namespace Inventory_Management.Application.Features.Results.UsersResult
{
    public class LoginUsersQueryResult
    {
        public bool IsSuccess { get; set; }
        public string Token { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string FullName { get; set; }
        public string ErrorMessage { get; set; }
        public string Role { get; set; }
        public string CompanyName { get; set; }
        public Guid UserId { get; set; } 
        

        public static LoginUsersQueryResult Success(string token, DateTime expiresAt, string fullName,string role,string companyname,Guid userid)
        {
            return new LoginUsersQueryResult
            {
                IsSuccess = true,
                Token = token,
                ExpiresAt = expiresAt,
                FullName = fullName,
                Role = role,
                CompanyName = companyname,
                UserId = userid
            };
        }
        public static LoginUsersQueryResult Failure(string message)
        {
            return new LoginUsersQueryResult
            {
                IsSuccess = false,
                ErrorMessage = message
            };
        }
    }
}