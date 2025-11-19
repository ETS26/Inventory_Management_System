
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Inventory_Management.Application.Features.Queries.Unit_TypesQuery;
using Inventory_Management.Application.Features.Commands.Unit_TypesCommand;
using System.Threading.Tasks;

namespace Inventory_Management.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UnitTypesController : ControllerBase
    {
        private readonly IMediator _mediator;
        public UnitTypesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> UnitTypesList()
        {
            var val = await _mediator.Send(new GetUnit_TypesQuery());
            return Ok(val);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUnitTypes(CreateUnit_TypesCommand command)
        {
            await _mediator.Send(command);
            return Ok("Birim türü ekleme başarılı");
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteUnitTypes(Guid id)
        {
            await _mediator.Send(new DeleteUnit_TypesCommand(id));
            return Ok("Birim türü silme başarılı");
        }

        [HttpGet("GetUnitTypesById")]
        public async Task<IActionResult> GetUnitTypesById(Guid id)
        {
            var val = await _mediator.Send(new GetUnit_TypesByIdQuery(id));
            return Ok(val);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateUnitTypes(UpdateUnit_TypesCommand command)
        {
            await _mediator.Send(command);
            return Ok("Birim türü güncelleme başarılı");
        }
    }
  }
