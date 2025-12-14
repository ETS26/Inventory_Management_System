using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Commands.InventoriesCommand
{
    public class SoftDeleteInventoriesCommand : IRequest
    {
        public Guid Id { get; set; }
        public bool IsActive { get; set; }
        public DateTime UpdatedAt { get; set; }

    }
}
