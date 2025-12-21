using System.Threading.Tasks;

namespace Inventory_Management.Application.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
    }
}
