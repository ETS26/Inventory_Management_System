using Inventory_Management.Application.Features.Results.UsersResult;
using System;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Queries.UsersQuery
{
    public class LoginUsersQuery : IRequest<LoginUsersQueryResult>
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
