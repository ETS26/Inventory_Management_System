using System;

namespace Inventory_Management.Domain.Common
{
    // Bu arayüzü uygulayan her tablo, "Şirkete Özel" kabul edilir.
    public interface IHasCompany
    {
        Guid CompanyId { get; set; }
    }
}