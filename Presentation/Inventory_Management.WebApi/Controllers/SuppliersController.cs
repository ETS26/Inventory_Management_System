
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Inventory_Management.Application.Features.Queries.SuppliersQuery;
using Inventory_Management.Application.Features.Commands.SuppliersCommand;
using System.Threading.Tasks;

namespace Inventory_Management.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SuppliersController : ControllerBase
    {
        private readonly IMediator _mediator;
        public SuppliersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> SuppliersList()
        {
            var val = await _mediator.Send(new GetSuppliersQuery());
            return Ok(val);
        }

        [HttpPost]
        public async Task<IActionResult> CreateSuppliers(CreateSuppliersCommand command)
        {
            await _mediator.Send(command);
            return Ok("Tedarikçi ekleme başarılı");
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteSuppliers(Guid id)
        {
            await _mediator.Send(new DeleteSuppliersCommand(id));
            return Ok("Tedarikçi silme başarılı");
        }

        [HttpGet("GetSuppliersById")]
        public async Task<IActionResult> GetSuppliersById(Guid id)
        {
            var val = await _mediator.Send(new GetSuppliersByIdQuery(id));
            return Ok(val);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateSuppliers(UpdateSuppliersCommand command)
        {
            await _mediator.Send(command);
            return Ok("Tedarikçi güncelleme başarılı");
        }
    }
  }
