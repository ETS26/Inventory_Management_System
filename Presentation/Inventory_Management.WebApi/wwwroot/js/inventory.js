document.addEventListener('DOMContentLoaded', function() {
    loadInventory();
});

async function loadInventory() {
    const tableBody = document.getElementById('inventoryTableBody');
    const token = localStorage.getItem('jwtToken');

    try {
        // API'ye istek at (Backend'inizdeki endpoint adını kontrol edin)
        // Genelde: /api/Inventories veya /api/Products
        const response = await fetch('/api/Inventories', {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            }
        });

        if (!response.ok) {
            throw new Error('Veri çekilemedi. Yetkiniz olmayabilir.');
        }

        const data = await response.json();
        
        // Tabloyu Temizle
        tableBody.innerHTML = '';

        if (data.length === 0) {
            tableBody.innerHTML = '<tr><td colspan="7" class="text-center">Hiç kayıt bulunamadı.</td></tr>';
            return;
        }

        // Verileri Döngüye Sok ve Tabloya Ekle
        data.forEach((item, index) => {

            // 1. Durum ve Renk Ayarları
            let badgeClass = "bg-success-subtle text-success border border-success-subtle";
            let statusText = "Stokta Var";
            let quantityBarColor = "bg-primary";
            let quantityPercent = Math.min((item.quantity / 100) * 100, 100); // Basit bir progress bar mantığı

            if (item.quantity === 0) {
                badgeClass = "bg-secondary-subtle text-secondary border border-secondary-subtle";
                statusText = "Tükendi";
                quantityBarColor = "bg-secondary";
            }
            else if (item.quantity <= item.criticalStockQuantity) {
                badgeClass = "bg-danger-subtle text-danger border border-danger-subtle";
                statusText = "Kritik";
                quantityBarColor = "bg-danger";
            }

            // 2. Tarih Formatı
            const expDate = new Date(item.expirationDate);
            const formattedDate = expDate.toLocaleDateString('tr-TR', { day: 'numeric', month: 'short', year: 'numeric' });
            const isExpired = expDate < new Date();
            const dateClass = isExpired ? "text-danger fw-bold" : "text-muted";
            const dateIcon = isExpired ? "fa-exclamation-circle" : "fa-calendar-alt";

            // 3. Veri Kontrolleri
            const productName = item.productName || 'Tanımsız Ürün';
            const category = item.categoryName || '-';
            const batch = item.batchNumber || '-';
            // Baş harfi al
            const initial = productName.charAt(0).toUpperCase();

            // 4. Modern Satır Tasarımı
            const row = `
                <tr>
                    <td class="ps-4 fw-bold text-secondary">${index + 1}</td>
                    
                    <td>
                        <div class="d-flex align-items-center">
                            <div class="product-avatar me-3 shadow-sm">
                                ${initial}
                            </div>
                            <div>
                                <h6 class="mb-0 fw-bold text-dark">${productName}</h6>
                                <small class="text-muted" style="font-size: 0.75rem;">
                                    <i class="fas fa-layer-group me-1"></i>${category} 
                                    <span class="mx-1">•</span> 
                                    <span class="text-secondary">Seri: ${batch}</span>
                                </small>
                            </div>
                        </div>
                    </td>

                    <td>
                        <div class="d-flex flex-column">
                            <span class="fw-bold text-dark">${item.quantity} <small class="text-muted fw-normal">Adet</small></span>
                            <div class="progress mt-1" style="height: 4px; width: 80px;">
                                <div class="progress-bar ${quantityBarColor}" role="progressbar" style="width: ${quantityPercent}%"></div>
                            </div>
                            <small class="text-danger mt-1" style="font-size: 0.7rem;">Min: ${item.criticalStockQuantity}</small>
                        </div>
                    </td>

                    <td>
                        <div class="d-flex flex-column">
                            <span class="fw-bold text-dark">₺${item.salePrice}</span>
                            <small class="text-muted" style="font-size: 0.75rem;">Maliyet: ₺${item.purchasePrice}</small>
                        </div>
                    </td>

                    <td>
                        <div class="${dateClass}" style="font-size: 0.9rem;">
                            <i class="far ${dateIcon} me-1"></i> ${formattedDate}
                        </div>
                    </td>

                    <td>
                        <span class="badge rounded-pill ${badgeClass} px-3 py-2 fw-normal">
                            ${statusText}
                        </span>
                    </td>

                    <td class="text-end pe-4">
                        <div class="btn-group">
                            <button class="btn btn-sm btn-light text-primary border-0 rounded-circle me-1 card-hover" title="Düzenle">
                                <i class="fas fa-pen"></i>
                            </button>
                            <button class="btn btn-sm btn-light text-danger border-0 rounded-circle card-hover" onclick="deleteInventory('${item.id}')" title="Sil">
                                <i class="fas fa-trash"></i>
                            </button>
                        </div>
                    </td>
                </tr>
            `;
            tableBody.innerHTML += row;
        });
            
    } catch (error) {
        console.error('Hata:', error);
        tableBody.innerHTML = `<tr><td colspan="7" class="text-center text-danger">Veriler yüklenirken hata oluştu: ${error.message}</td></tr>`;
    }
}

// Silme Fonksiyonu (Taslak)
function deleteInventory(id) {
    if(confirm('Bu kaydı silmek istediğinize emin misiniz?')) {
        console.log('Silinecek ID:', id);
        // Buraya silme API isteği gelecek
    }
}