/**
 * INVENTORY.JS - Envanter Yönetim Sayfası
 */

'use strict';

class InventoryManager {
    constructor() {
        this.token = localStorage.getItem('jwtToken');
        this.tableBody = document.getElementById('inventoryTableBody');
        this.allData = [];
    }

    async init() {
        if (!this.tableBody) return;
        await this.loadInventory();
    }

    async loadInventory() {
        this.showLoading();

        try {
            const response = await fetch('/api/Inventories', {
                method: 'GET',
                headers: {
                    'Authorization': `Bearer ${this.token}`,
                    'Content-Type': 'application/json'
                }
            });

            if (!response.ok) {
                throw new Error('Veri çekilemedi.');
            }

            const data = await response.json();

            // Debug için konsola bakalım (Tarayıcıda F12 -> Console sekmesinden kontrol edin)
            if (data.length > 0) {
                console.log("🔍 Gelen Veri Örneği:", data[0]);
                // Düzeltme: Hem camelCase (isActive) hem de PascalCase (IsActive) kontrolü için loglar eklendi.
                console.log("👉 isActive propertysi:", data[0].isActive);
                console.log("👉 IsActive propertysi:", data[0].IsActive);
            }

            this.allData = data;

            // ✅ DÜZELTME: item.isActive (camelCase) kontrolü eklendi.
            const activeData = this.allData.filter(item => {
                // Backend'den gelen değer hangisiyse onu al (Önce IsActive, yoksa isActive)
                const status = (item.IsActive !== undefined) ? item.IsActive : item.isActive;

                // true veya 1 ise listeye ekle
                return status === true || status === 1;
            });

            console.log(`✅ Toplam: ${data.length}, Ekrana Basılan: ${activeData.length}`);

            this.renderInventory(activeData);

        } catch (error) {
            console.error('Hata:', error);
            this.showError(error.message);
        }
    }

    showLoading() {
        this.tableBody.innerHTML = `<tr><td colspan="8" class="text-center py-5"><div class="spinner-border text-primary"></div></td></tr>`;
    }

    showError(message) {
        this.tableBody.innerHTML = `<tr><td colspan="8" class="text-center text-danger py-4">${message}</td></tr>`;
    }

    renderInventory(data) {
        if (data.length === 0) {
            this.tableBody.innerHTML = `<tr><td colspan="8" class="text-center py-5 text-muted">Kayıt yok.</td></tr>`;
            return;
        }
        this.tableBody.innerHTML = data.map((item, index) => this.createTableRow(item, index + 1)).join('');
    }

    createTableRow(item, rowNumber) {
        const status = this.calculateStatus(item);
        const dateInfo = this.formatExpirationDate(item.expirationDate);
        const productInfo = this.extractProductInfo(item);

        return `
            <tr class="fade-in">
                <td class="ps-4 fw-bold text-secondary">${rowNumber}</td>
                ${this.renderProductCell(productInfo)}
                <td><span class="fw-bold text-dark font-monospace">${item.barcode || '-'}</span></td>
                ${this.renderStockCell(item, status)}
                ${this.renderPriceCell(item)}
                <td><div class="${dateInfo.class}"><i class="far ${dateInfo.icon} me-1"></i> ${dateInfo.formatted}</div></td>
                <td><span class="badge rounded-pill ${status.badgeClass} px-3 py-2 fw-normal">${status.text}</span></td>
                <td class="text-end pe-4">
                    <div class="btn-group">
                        <button class="btn btn-sm btn-light text-primary border-0 rounded-circle me-1 card-hover" 
                                onclick="editInventoryItem('${item.id}')" title="Düzenle">
                            <i class="fas fa-pen"></i>
                        </button>
                        <button class="btn btn-sm btn-light text-danger border-0 rounded-circle card-hover" 
                                onclick="deleteInventoryItem('${item.id}')" title="Arşivle/Sil">
                            <i class="fas fa-trash"></i>
                        </button>
                    </div>
                </td>
            </tr>`;
    }

    // --- Render Helpers ---
    renderProductCell(info) {
        return `<td><div class="d-flex align-items-center"><div class="product-avatar me-3 shadow-sm">${info.initial}</div><div><h6 class="mb-0 fw-bold text-dark">${info.name}</h6><small class="text-muted">${info.category} • <span class="text-secondary">Seri: ${info.batch}</span></small></div></div></td>`;
    }
    renderStockCell(item, status) {
        return `<td><div class="d-flex flex-column"><span class="fw-bold text-dark">${item.quantity} <small class="text-muted">Adet</small></span><div class="progress mt-1" style="height: 4px; width: 80px;"><div class="progress-bar ${status.barColor}" style="width: ${status.percent}%"></div></div><small class="text-danger mt-1">Min: ${item.criticalStockQuantity}</small></div></td>`;
    }
    renderPriceCell(item) {
        return `<td><div class="d-flex flex-column"><span class="fw-bold text-dark">₺${item.salePrice}</span><small class="text-muted">Maliyet: ₺${item.purchasePrice}</small></div></td>`;
    }
    calculateStatus(item) {
        const percent = Math.min((item.quantity / 100) * 100, 100);
        if (item.quantity === 0) return { badgeClass: 'bg-secondary-subtle text-secondary', text: 'Tükendi', barColor: 'bg-secondary', percent: 0 };
        if (item.quantity <= item.criticalStockQuantity) return { badgeClass: 'bg-danger-subtle text-danger', text: 'Kritik', barColor: 'bg-danger', percent };
        return { badgeClass: 'bg-success-subtle text-success', text: 'Stokta Var', barColor: 'bg-primary', percent };
    }
    formatExpirationDate(dateString) {
        if (!dateString) return { formatted: '-', class: 'text-muted', icon: 'fa-calendar' };
        const expDate = new Date(dateString);
        const isExpired = expDate < new Date();
        return { formatted: expDate.toLocaleDateString('tr-TR'), class: isExpired ? 'text-danger fw-bold' : 'text-muted', icon: isExpired ? 'fa-exclamation-circle' : 'fa-calendar-alt' };
    }
    extractProductInfo(item) {
        const name = item.productName || 'Tanımsız';
        return { name, category: item.categoryName || '-', batch: item.batchNumber || '-', initial: name.charAt(0).toUpperCase() };
    }
}

// Global Manager
let inventoryManagerInstance;

// ==========================================
// 1. DÜZENLEME (Update)
// ==========================================
window.editInventoryItem = function (id) {
    if (!inventoryManagerInstance) return;
    const item = inventoryManagerInstance.allData.find(d => d.id === id);
    if (!item) return;

    document.getElementById('editId').value = item.id;
    document.getElementById('editProductName').value = item.productName || '';
    document.getElementById('editBatchNumber').value = item.batchNumber || '';
    document.getElementById('editQuantity').value = item.quantity || 0;
    document.getElementById('editCriticalStock').value = item.criticalStockQuantity || 0;
    document.getElementById('editPurchasePrice').value = item.purchasePrice || 0;
    document.getElementById('editSalePrice').value = item.salePrice || 0;
    document.getElementById('editDescription').value = item.description || '';

    if (item.expirationDate) {
        document.getElementById('editExpirationDate').value = item.expirationDate.split('T')[0];
    } else {
        document.getElementById('editExpirationDate').value = '';
    }

    const modalEl = document.getElementById('editInventoryModal');
    if (modalEl) {
        const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
        modal.show();
    }
};

// ==========================================
// 2. KAYDETME (Update - PUT)
// ==========================================
window.saveInventoryItem = async function () {
    const id = document.getElementById('editId').value;
    const token = localStorage.getItem('jwtToken');
    const btn = document.querySelector('#editInventoryModal .btn-primary');

    // Yükleniyor efekti
    const originalText = btn.innerHTML;
    btn.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>Kaydediliyor...';
    btn.disabled = true;

    // Orijinal veriyi al (Değişmeyen alanları korumak için)
    const originalItem = inventoryManagerInstance.allData.find(d => d.id === id);

    const payload = {
        id: id,
        quantity: parseInt(document.getElementById('editQuantity').value) || 0,
        criticalStockQuantity: parseInt(document.getElementById('editCriticalStock').value) || 0,
        purchasePrice: parseFloat(document.getElementById('editPurchasePrice').value) || 0,
        salePrice: parseFloat(document.getElementById('editSalePrice').value) || 0,
        expirationDate: document.getElementById('editExpirationDate').value || null,
        batchNumber: document.getElementById('editBatchNumber').value,
        description: document.getElementById('editDescription').value,

        // Değişmeyen kritik alanlar
        productId: originalItem.productId,
        companyId: originalItem.companyId,
        isActive: true, // Normal güncellemede aktif kalır
        updatedAt: new Date().toISOString()
    };

    try {
        const res = await fetch(`/api/Inventories`, {
            method: 'PUT',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(payload)
        });

        if (res.ok) {
            alert('✅ Başarıyla güncellendi!');
            const modalEl = document.getElementById('editInventoryModal');
            bootstrap.Modal.getInstance(modalEl)?.hide();
            await inventoryManagerInstance.loadInventory();
        } else {
            const err = await res.json();
            alert('❌ Hata: ' + (err.message || "Güncelleme başarısız."));
        }
    } catch (e) {
        console.error(e);
        alert('❌ Sunucu hatası.');
    } finally {
        btn.innerHTML = originalText;
        btn.disabled = false;
    }
};


// ==========================================
// 3. SİLME (SOFT DELETE) - En Kritik Kısım
// ==========================================
window.deleteInventoryItem = async function (id) {
    if (!confirm('⚠️ Bu ürünü arşivlemek (silmek) istediğinize emin misiniz?')) return;

    const token = localStorage.getItem('jwtToken');

    // ✅ DÜZELTME 1: C# PascalCase (Büyük Harf) Bekleyebilir
    // Güvenlik için hem Id hem IsActive büyük harfle başlasın
    const payload = {
        Id: id,          // Backend'deki "public Guid Id" ile eşleşir
        IsActive: false  // Backend'deki "public bool IsActive" ile eşleşir
    };

    console.log("🗑️ Silme İsteği Gönderiliyor:", payload);

    try {
        // ✅ DÜZELTME 2: Doğru Endpoint (Route)
        const res = await fetch(`/api/Inventories/SoftDeleteInventories`, {
            method: 'PUT',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(payload)
        });

        if (res.ok) {
            alert('✅ Ürün başarıyla arşivlendi.');
            await inventoryManagerInstance.loadInventory(); // Tabloyu yenile
        } else {
            // Hata detayını yakala
            let errorMessage = "İşlem başarısız.";
            try {
                const errorJson = await res.json();
                errorMessage = errorJson.message || errorJson.title || JSON.stringify(errorJson);
            } catch {
                errorMessage = await res.text();
            }
            console.error("Backend Hatası:", errorMessage);
            alert('❌ Hata: ' + errorMessage);
        }
    } catch (e) {
        console.error('Ağ Hatası:', e);
        alert('❌ Sunucu ile iletişim kurulamadı.');
    }
};

// Başlatma
document.addEventListener('DOMContentLoaded', () => {
    inventoryManagerInstance = new InventoryManager();
    inventoryManagerInstance.init();
});