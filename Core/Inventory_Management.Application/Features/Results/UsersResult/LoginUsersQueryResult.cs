namespace Inventory_Management.Application.Features.Results.UsersResult
{
    public class LoginUsersQueryResult
    {
        public bool IsSuccess { get; set; }
        public string Token { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string FullName { get; set; }
        public string ErrorMessage { get; set; }

        public static LoginUsersQueryResult Success(string token, DateTime expiresAt, string fullName)
        {
            return new LoginUsersQueryResult
            {
                IsSuccess = true,
                Token = token,
                ExpiresAt = expiresAt,
                FullName = fullName
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