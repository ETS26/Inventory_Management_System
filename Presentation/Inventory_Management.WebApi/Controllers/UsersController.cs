using Inventory_Management.Application.Features.Commands.UsersCommand;
using Inventory_Management.Application.Features.Queries.UsersQuery;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;


namespace Inventory_Management.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IMediator _mediator;
        public UsersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> UsersList()
        {
            var val = await _mediator.Send(new GetUsersQuery());
            return Ok(val);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUsers(CreateUsersCommand command)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            await _mediator.Send(command);
            return Ok("Kullanıcı ekleme başarılı");
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteUsers(Guid id)
        {
            await _mediator.Send(new DeleteUsersCommand(id));
            return Ok("Kullanıcı silme başarılı");
        }

        [HttpGet("GetUsersById")]
        public async Task<IActionResult> GetUsersById(Guid id)
        {
            var val = await _mediator.Send(new GetUsersByIdQuery(id));
            return Ok(val);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateUsers(UpdateUsersCommand command)
        {
            await _mediator.Send(command);
            return Ok("Kullanıcı güncelleme başarılı");
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginUsersQuery query)
        {
            var result = await _mediator.Send(query);
            if (!result.IsSuccess)
            {
                return Unauthorized(new { message = result.ErrorMessage });
            }
            return Ok(result);
        }
    }
  }
