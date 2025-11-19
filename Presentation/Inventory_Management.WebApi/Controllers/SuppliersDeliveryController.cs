
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Inventory_Management.Application.Features.Queries.Suppliers_DeliveryQuery;
using Inventory_Management.Application.Features.Commands.Suppliers_DeliveryCommand;
using System.Threading.Tasks;

namespace Inventory_Management.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SuppliersDeliveryController : ControllerBase
    {
        private readonly IMediator _mediator;
        public SuppliersDeliveryController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> SuppliersDeliveryList()
        {
            var val = await _mediator.Send(new GetSuppliers_DeliveryQuery());
            return Ok(val);
        }

        [HttpPost]
        public async Task<IActionResult> CreateSuppliersDelivery(CreateSuppliers_DeliveryCommand command)
        {
            await _mediator.Send(command);
            return Ok("Tedarikçi teslimatı ekleme başarılı");
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteSuppliersDelivery(Guid id)
        {
            await _mediator.Send(new DeleteSuppliers_DeliveryCommand(id));
            return Ok("Tedarikçi teslimatı silme başarılı");
        }

        [HttpGet("GetSuppliersDeliveryById")]
        public async Task<IActionResult> GetSuppliersDeliveryById(Guid id)
        {
            var val = await _mediator.Send(new GetSuppliers_DeliveryByIdQuery(id));
            return Ok(val);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateSuppliersDelivery(UpdateSuppliers_DeliveryCommand command)
        {
            await _mediator.Send(command);
            return Ok("Tedarikçi teslimatı güncelleme başarılı");
        }
    }
  }
