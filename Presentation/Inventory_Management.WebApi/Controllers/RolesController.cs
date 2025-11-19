
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Inventory_Management.Application.Features.Queries.RolesQuery;
using Inventory_Management.Application.Features.Commands.RolesCommand;
using System.Threading.Tasks;

namespace Inventory_Management.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RolesController : ControllerBase
    {
        private readonly IMediator _mediator;
        public RolesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> RolesList()
        {
            var val = await _mediator.Send(new GetRolesQuery());
            return Ok(val);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRoles(CreateRolesCommand command)
        {
            await _mediator.Send(command);
            return Ok("Rol ekleme başarılı");
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteRoles(Guid id)
        {
            await _mediator.Send(new DeleteRolesCommand(id));
            return Ok("Rol silme başarılı");
        }

        [HttpGet("GetRolesById")]
        public async Task<IActionResult> GetRolesById(Guid id)
        {
            var val = await _mediator.Send(new GetRolesByIdQuery(id));
            return Ok(val);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateRoles(UpdateRolesCommand command)
        {
            await _mediator.Send(command);
            return Ok("Rol güncelleme başarılı");
        }
    }
  }
