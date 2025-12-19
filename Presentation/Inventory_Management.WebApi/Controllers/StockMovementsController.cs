
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
        public async Task<IActionResult> StockMovementsList([FromQuery] bool? isActive)
        {
            var query = new GetStock_MovementsQuery { IsActive = isActive };
            var val = await _mediator.Send(query);
            return Ok(val);
        }

        [HttpPost]
        public async Task<IActionResult> CreateStockMovements(CreateStock_MovementsCommand command)
        {
            try {
                await _mediator.Send(command);
                return Ok(new { isSuccess = true, message = "Stok hareketi ekleme başarılı" });
            }
            catch(System.Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStockMovements(Guid id)
        {
            try
            {
                await _mediator.Send(new DeleteStock_MovementsCommand(id));
                return Ok(new { isSuccess = true, message = "Stok hareketi silme başarılı" });
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("GetStockMovementsById")]
        public async Task<IActionResult> GetStockMovementsById(Guid id)
        {
            var val = await _mediator.Send(new GetStock_MovementsByIdQuery(id));
            return Ok(val);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStockMovements(Guid id, UpdateStock_MovementsCommand command)
        {
            if (id != command.Id)
            {
                return BadRequest(new { message = "ID uyuşmazlığı." });
            }

            try
            {
                await _mediator.Send(command);
                return Ok(new { isSuccess = true, message = "Stok hareketi güncelleme başarılı" });
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
