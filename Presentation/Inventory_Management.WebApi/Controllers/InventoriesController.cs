
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Inventory_Management.Application.Features.Queries.InventoriesQuery;
using Inventory_Management.Application.Features.Commands.InventoriesCommand;
using System.Threading.Tasks;

namespace Inventory_Management.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InventoriesController : ControllerBase
    {
        private readonly IMediator _mediator;
        public InventoriesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> InventoriesList()
        {
            var val = await _mediator.Send(new GetInventoriesQuery());
            return Ok(val);
        }

        [HttpPost]
        public async Task<IActionResult> CreateInventories(CreateInventoriesCommand command)
        {
            await _mediator.Send(command);
            return Ok("Envanter ekleme başarılı");
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteInventories(Guid id)
        {
            await _mediator.Send(new DeleteInventoriesCommand(id));
            return Ok("Envanter silme başarılı");
        }

        [HttpGet("GetInventoriesById")]
        public async Task<IActionResult> GetInventoriesById(Guid id)
        {
            var val = await _mediator.Send(new GetInventoriesByIdQuery(id));
            return Ok(val);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateInventories(UpdateInventoriesCommand command)
        {
            await _mediator.Send(command);
            return Ok("Envanter güncelleme başarılı");
        }
    }
  }
