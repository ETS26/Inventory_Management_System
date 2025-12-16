
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Inventory_Management.Application.Features.Queries.ProductsQuery;
using Inventory_Management.Application.Features.Commands.ProductsCommand;
using System.Threading.Tasks;

namespace Inventory_Management.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IMediator _mediator;
        public ProductsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> ProductsList([FromQuery] bool? isActive)
        {
            var val = await _mediator.Send(new GetProductsQuery { IsActive = isActive });
            return Ok(val);
        }

        [HttpPost]
        public async Task<IActionResult> CreateProducts(CreateProductsCommand command)
        {
            await _mediator.Send(command);
            return Ok("Ürün ekleme başarılı");
        }

        [HttpPut("activate/{id}")]
        public async Task<IActionResult> ActivateProduct(Guid id)
        {
            await _mediator.Send(new ActivateProductCommand(id));
            return Ok("Ürün başarıyla aktif edildi.");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProducts(Guid id)
        {
            await _mediator.Send(new DeleteProductsCommand(id));
            return Ok("Ürün silme başarılı");
        }

        [HttpGet("GetProductsById")]
        public async Task<IActionResult> GetProductsById(Guid id)
        {
            var val = await _mediator.Send(new GetProductsByIdQuery(id));
            return Ok(val);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProducts(Guid id, UpdateProductsCommand command)
        {
            if (id != command.Id)
            {
                return BadRequest("ID mismatch");
            }
            await _mediator.Send(command);
            return Ok("Ürün güncelleme başarılı");
        }
    }
  }
