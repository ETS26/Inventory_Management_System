
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Inventory_Management.Application.Features.Queries.UserRolesQuery;
using Inventory_Management.Application.Features.Commands.UsersRolesCommand;
using System.Threading.Tasks;

namespace Inventory_Management.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserRolesController : ControllerBase
    {
        private readonly IMediator _mediator;
        public UserRolesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> UserRolesList()
        {
            var val = await _mediator.Send(new GetUserRolesQuery());
            return Ok(val);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUserRoles(CreateUsersRolesCommand command)
        {
            await _mediator.Send(command);
            return Ok("Kullanıcı rolü ekleme başarılı");
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteUserRoles(Guid id)
        {
            await _mediator.Send(new DeleteUsersRolesCommand(id));
            return Ok("Kullanıcı rolü silme başarılı");
        }

        [HttpGet("GetUserRolesById")]
        public async Task<IActionResult> GetUserRolesById(Guid id)
        {
            var val = await _mediator.Send(new GetUserRolesByIdQuery(id));
            return Ok(val);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateUserRoles(UpdateUsersRolesCommand command)
        {
            await _mediator.Send(command);
            return Ok("Kullanıcı rolü güncelleme başarılı");
        }
    }
  }
