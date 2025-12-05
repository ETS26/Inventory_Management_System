using System;

namespace Inventory_Management.Domain.Common
{
    public interface ICurrentUserService
    {
        Guid? CompanyId { get; }
    }
}