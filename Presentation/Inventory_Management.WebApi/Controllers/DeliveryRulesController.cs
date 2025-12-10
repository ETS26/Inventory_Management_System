
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Inventory_Management.Application.Features.Queries.Delivery_RulesQuery;
using Inventory_Management.Application.Features.Commands.Delivery_RulesCommand;
using System.Threading.Tasks;

namespace Inventory_Management.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeliveryRulesController : ControllerBase
    {
        private readonly IMediator _mediator;
        public DeliveryRulesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> DeliveryRulesList()
        {
            var val = await _mediator.Send(new GetDelivery_RulesQuery());
            return Ok(val);
        }

        [HttpPost]
        public async Task<IActionResult> CreateDeliveryRules(CreateDelivery_RulesCommand command)
        {
            await _mediator.Send(command);
            return Ok("Teslimat kuralı ekleme başarılı");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDeliveryRules(Guid id)
        {
            await _mediator.Send(new DeleteDelivery_RulesCommand(id));
            return Ok("Teslimat kuralı silme başarılı");
        }

        [HttpGet("GetDeliveryRulesById")]
        public async Task<IActionResult> GetDeliveryRulesById(Guid id)
        {
            var val = await _mediator.Send(new GetDelivery_RulesByIdQuery(id));
            return Ok(val);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateDeliveryRules(UpdateDelivery_RulesCommand command)
        {
            await _mediator.Send(command);
            return Ok("Teslimat kuralı güncelleme başarılı");
        }
    }
  }
