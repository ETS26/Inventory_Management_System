
using Inventory_Management.Domain.Entities;
using System;

namespace Inventory_Management.Application.Features.Results.UserRolesResult
{
    public class GetUserRolesByIdQueryResult : BaseEntity
    {
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
    }
}
