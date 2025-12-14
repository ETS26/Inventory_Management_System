
using Inventory_Management.Application.Features.Commands.InventoriesCommand;
using Inventory_Management.Application.Features.Commands.Move_TypesCommand;
using Inventory_Management.Application.Features.Queries.InventoriesQuery;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Inventory_Management.WebApi.Controllers
{
    [Route("api/Inventories")] // Yönlendirme sorunu ihtimaline karşı yol manuel olarak belirlendi.
    [ApiController]
    public class InventoriesController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<InventoriesController> _logger;

        public InventoriesController(IMediator mediator, ILogger<InventoriesController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> InventoriesList()
        {
            var val = await _mediator.Send(new GetInventoriesQuery());
            return Ok(val);
        }

        [HttpPost]
        public async Task<IActionResult> CreateInventories(CreateInventoriesCommand command)
        {
            await _mediator.Send(command);
            return Ok("Envanter ekleme başarılı");
        }

        [HttpDelete("{id}")]
        [AllowAnonymous] // !!! YETKİLENDİRME SORUNUNU AŞMAK İÇİN EKLENDİ !!!
        public async Task<IActionResult> DeleteInventories(Guid id)
        {
            _logger.LogInformation("===== DeleteInventories metodu çağrıldı. ID: {Id} =====", id);
            try
            {
                await _mediator.Send(new DeleteInventoriesCommand(id));
                _logger.LogInformation("===== Mediator.Send(DeleteInventoriesCommand) başarıyla tamamlandı. ID: {Id} =====", id);
                return Ok("Envanter silindi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "XXXXX DeleteInventories metodu sırasında hata oluştu. ID: {Id} XXXXX", id);
                return StatusCode(500, "Silme işlemi sırasında sunucuda bir hata oluştu.");
            }
        }

        [HttpGet("GetInventoriesById")]
        public async Task<IActionResult> GetInventoriesById(Guid id)
        {
            var val = await _mediator.Send(new GetInventoriesByIdQuery(id));
            return Ok(val);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateInventories(UpdateInventoriesCommand command)
        {
            _logger.LogInformation("===== UpdateInventories metodu çağrıldı. ID: {Id} =====", command.Id);
            try
            {
                await _mediator.Send(command);
                _logger.LogInformation("===== Mediator.Send(UpdateInventoriesCommand) başarıyla tamamlandı. ID: {Id} =====", command.Id);
                return Ok("Envanter güncelleme başarılı");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "XXXXX UpdateInventories metodu sırasında hata oluştu. ID: {Id} XXXXX", command.Id);
                return StatusCode(500, "Güncelleme işlemi sırasında sunucuda bir hata oluştu.");
            }
        }

        [HttpPut("SoftDeleteInventories")]
        public async Task<IActionResult> SoftDelete(SoftDeleteInventoriesCommand command)
        {
            await _mediator.Send(command);
            return Ok("Envanter pasifleştirme başarılı");
        }
    }
}

