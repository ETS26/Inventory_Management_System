
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Inventory_Management.Application.Features.Queries.Stock_MovementsQuery;
using Inventory_Management.Application.Features.Commands.Stock_MovementsCommand;
using System.Threading.Tasks;

namespace Inventory_Management.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StockMovementsController : ControllerBase
    {
        private readonly IMediator _mediator;
        public StockMovementsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> StockMovementsList()
        {
            var val = await _mediator.Send(new GetStock_MovementsQuery());
            return Ok(val);
        }

        [HttpPost]
        public async Task<IActionResult> CreateStockMovements(CreateStock_MovementsCommand command)
        {
            await _mediator.Send(command);
            return Ok(new { isSuccess = true, message = "Stok hareketi ekleme başarılı" });
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteStockMovements(Guid id)
        {
            await _mediator.Send(new DeleteStock_MovementsCommand(id));
            return Ok("Stok hareketi silme başarılı");
        }

        [HttpGet("GetStockMovementsById")]
        public async Task<IActionResult> GetStockMovementsById(Guid id)
        {
            var val = await _mediator.Send(new GetStock_MovementsByIdQuery(id));
            return Ok(val);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateStockMovements(UpdateStock_MovementsCommand command)
        {
            await _mediator.Send(command);
            return Ok("Stok hareketi güncelleme başarılı");
        }
    }
  }
