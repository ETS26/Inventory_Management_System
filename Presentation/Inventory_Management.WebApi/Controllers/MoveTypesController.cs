
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Inventory_Management.Application.Features.Queries.Move_TypesQuery;
using Inventory_Management.Application.Features.Commands.Move_TypesCommand;
using System.Threading.Tasks;

namespace Inventory_Management.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MoveTypesController : ControllerBase
    {
        private readonly IMediator _mediator;
        public MoveTypesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> MoveTypesList()
        {
            var val = await _mediator.Send(new GetMove_TypesQuery());
            return Ok(val);
        }

        [HttpPost]
        public async Task<IActionResult> CreateMoveTypes(CreateMove_TypesCommand command)
        {
            await _mediator.Send(command);
            return Ok("Taşıma türü ekleme başarılı");
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteMoveTypes(Guid id)
        {
            await _mediator.Send(new DeleteMove_TypesCommand(id));
            return Ok("Taşıma türü silme başarılı");
        }

        [HttpGet("GetMoveTypesById")]
        public async Task<IActionResult> GetMoveTypesById(Guid id)
        {
            var val = await _mediator.Send(new GetMove_TypesByIdQuery(id));
            return Ok(val);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateMoveTypes(UpdateMove_TypesCommand command)
        {
            await _mediator.Send(command);
            return Ok("Taşıma türü güncelleme başarılı");
        }
    }
  }
