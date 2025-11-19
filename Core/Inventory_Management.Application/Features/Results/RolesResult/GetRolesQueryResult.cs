
using Inventory_Management.Domain.Entities;
using System;

namespace Inventory_Management.Application.Features.Results.RolesResult
{
    public class GetRolesQueryResult : BaseEntity
    {
        public string RoleName { get; set; }
    }
}
