
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
        public async Task<IActionResult> ProductsList()
        {
            var val = await _mediator.Send(new GetProductsQuery());
            return Ok(val);
        }

        [HttpPost]
        public async Task<IActionResult> CreateProducts(CreateProductsCommand command)
        {
            await _mediator.Send(command);
            return Ok("Ürün ekleme başarılı");
        }

        [HttpDelete]
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

        [HttpPut]
        public async Task<IActionResult> UpdateProducts(UpdateProductsCommand command)
        {
            await _mediator.Send(command);
            return Ok("Ürün güncelleme başarılı");
        }
    }
  }
