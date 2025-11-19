
using Inventory_Management.Domain.Entities;
using System;

namespace Inventory_Management.Application.Features.Results.UserRolesResult
{
    public class GetUserRolesQueryResult : BaseEntity
    {
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
    }
}
