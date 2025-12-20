using Inventory_Management.Domain.Common;
using System.Security.Claims;

namespace Inventory_Management.WebApi.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid? CompanyId
        {
            get
            {
                // Token içindeki "companyId" bilgisini okur (Küçük harfe dikkat)
                var claim = _httpContextAccessor.HttpContext?.User?.FindFirst("companyId");
                return claim != null ? Guid.Parse(claim.Value) : null;
            }
        }

        public Guid UserId
        {
            get
            {
                var claim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
                return claim != null ? Guid.Parse(claim.Value) : Guid.Empty;
            }
        }
    }
}