using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration; // IConfiguration için
using BCrypt.Net; 
using System.IdentityModel.Tokens.Jwt; // JWT için (Metodun içini kopyalayın)
using System.Security.Claims; // Claims için
using Microsoft.IdentityModel.Tokens; // Token için
using System.Text; // Encoding için
using Inventory_Management.Application.Features.Queries.UsersQuery;
using Inventory_Management.Application.Features.Results.UsersResult;
using Inventory_Management.Persistance.Context; // DbContext'iniz için
using Inventory_Management.Domain.Entities; // Users entity'niz için

namespace Inventory_Management.Application.Features.Handlers.UsersHandler
{
    public class LoginUsersQueryHandler : IRequestHandler<LoginUsersQuery, LoginUsersQueryResult>
    {
        private readonly Inventory_Management_Context _context;
        private readonly IConfiguration _configuration;

        public LoginUsersQueryHandler(Inventory_Management_Context context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<LoginUsersQueryResult> Handle(LoginUsersQuery request, CancellationToken cancellationToken)
        {
            var user = await _context.Users
                    .Include(u => u.UsersRoles)       
                    .ThenInclude(ur => ur.Role)
                    .Include(u => u.Company)
                    .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);


            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
            {
                return LoginUsersQueryResult.Failure("Email veya şifre hatalı.");
            }

            string userRole = user.UsersRoles?.FirstOrDefault()?.Role?.RoleName ?? "Misafir";
            string userCompany = user.Company?.CompanyName ?? "Şirket Belirtilmemiş";
            Guid userId = user.Id;
            var tokenResult = GenerateJwtToken(user);

            return LoginUsersQueryResult.Success(
                tokenResult.Token,
                tokenResult.ExpiresAt,
                $"{user.FirstName} {user.LastName}",
                userRole,
                userCompany,
                userId
            );
        }
        private (string Token, DateTime ExpiresAt) GenerateJwtToken(Users user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("firstName", user.FirstName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddDays(1);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return (new JwtSecurityTokenHandler().WriteToken(token), expires);
        }
    }
}