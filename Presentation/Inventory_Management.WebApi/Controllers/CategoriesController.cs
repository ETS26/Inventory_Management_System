using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Inventory_Management.Application.Features.Queries.CategoriesQuery;
using Inventory_Management.Application.Features.Commands.CategoriesCommand;
using System.Threading.Tasks;

namespace Inventory_Management.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly IMediator _mediator;
        public CategoriesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> CategoriesList()
        {
            var val = await _mediator.Send(new GetCategoriesQuery());
            return Ok(val);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCategories(CreateCategoriesCommand command)
        {
            await _mediator.Send(command);
            return Ok("Kategori ekleme başarılı");
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteCategories(Guid id)
        {
            await _mediator.Send(new DeleteCategoriesCommand(id));
            return Ok("Kategori silme başarılı");
        }

        [HttpGet("GetCategoriesById")]
        public async Task<IActionResult> GetCategoriesById(Guid id)
        {
            var val = await _mediator.Send(new GetCategoriesByIdQuery(id));
            return Ok(val);
        }

        [HttpGet("GetCategoriesByName")]
        public async Task<IActionResult> GetCategoriesByName(string name)
        {
            var val = await _mediator.Send(new GetCategoriesByNameQuery(name));
            return Ok(val);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateCategories(UpdateCategoriesCommand command)
        {
            await _mediator.Send(command);
            return Ok("Kategori güncelleme başarılı");
        }
    }
  }
