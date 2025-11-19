
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Inventory_Management.Application.Features.Queries.CompaniesQuery;
using Inventory_Management.Application.Features.Commands.CompaniesCommand;
using System.Threading.Tasks;

namespace Inventory_Management.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CompaniesController : ControllerBase
    {
        private readonly IMediator _mediator;
        public CompaniesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> CompaniesList()
        {
            var val = await _mediator.Send(new GetCompaniesQuery());
            return Ok(val);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCompanies(CreateCompaniesCommand command)
        {
            await _mediator.Send(command);
            return Ok("Şirket ekleme başarılı");
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteCompanies(Guid id)
        {
            await _mediator.Send(new DeleteCompaniesCommand(id));
            return Ok("Şirket silme başarılı");
        }

        [HttpGet("GetCompaniesById")]
        public async Task<IActionResult> GetCompaniesById(Guid id)
        {
            var val = await _mediator.Send(new GetCompaniesByIdQuery(id));
            return Ok(val);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateCompanies(UpdateCompaniesCommand command)
        {
            await _mediator.Send(command);
            return Ok("Şirket güncelleme başarılı");
        }
    }
  }
