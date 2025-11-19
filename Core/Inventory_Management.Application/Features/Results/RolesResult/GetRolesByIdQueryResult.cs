
using Inventory_Management.Domain.Entities;
using System;

namespace Inventory_Management.Application.Features.Results.RolesResult
{
    public class GetRolesByIdQueryResult : BaseEntity
    {
        public string RoleName { get; set; }
    }
}
