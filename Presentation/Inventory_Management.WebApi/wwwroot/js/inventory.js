/**
 * INVENTORY.JS - Envanter Yönetim Sayfası (Gelişmiş Filtreleme - Güncellenmiş)
 */

'use strict';

// Debounce (Arama performansını artırır)
function debounce(func, delay = 300) {
    let timeout;
    return (...args) => {
        clearTimeout(timeout);
        timeout = setTimeout(() => {
            func.apply(this, args);
        }, delay);
    };
}

class InventoryManager {
    constructor() {
        this.token = localStorage.getItem('jwtToken');
        this.tableBody = document.getElementById('inventoryTableBody');
        this.searchInput = document.getElementById('searchInput');

        this.allData = [];    // Tüm ham veri (Backend'den gelen)
        this.activeData = []; // Ekranda gösterilen filtrelenmiş veri
    }

    async init() {
        if (!this.tableBody) return;

        await this.loadInventory();
        this.setupSearch();
    }

    // ==========================================
    // 1. VERİ YÜKLEME
    // ==========================================
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

            if (!response.ok) throw new Error('Veri çekilemedi.');

            const data = await response.json();
            console.log('📦 Envanter Verileri:', data);

            this.allData = data;

            // 1. Adım: Sadece IsActive olanları ayıkla (Soft Delete Kontrolü)
            this.activeData = this.allData.filter(item => {
                const status = (item.isActive !== undefined) ? item.isActive : item.IsActive;
                return status !== false;
            });

            // 2. Adım: Kategorileri Filtre Modalı için Yükle
            this.loadCategoriesForFilter();

            // 3. Adım: Tabloyu Çiz
            this.renderInventory(this.activeData);

        } catch (error) {
            console.error('Hata:', error);
            this.showError(error.message);
        }
    }

    // ==========================================
    // 2. KATEGORİLERİ FİLTRE İÇİN HAZIRLA
    // ==========================================
    loadCategoriesForFilter() {
        const select = document.getElementById('filterCategory');
        if (!select) return;

        select.innerHTML = '<option value="">Tümü</option>';

        // Benzersiz kategorileri bul
        const categories = [...new Set(this.activeData.map(item => item.categoryName).filter(c => c))];

        categories.sort().forEach(cat => {
            const opt = document.createElement('option');
            opt.value = cat;
            opt.text = cat;
            select.appendChild(opt);
        });
    }

    // ==========================================
    // 3. GELİŞMİŞ FİLTRELEME MANTIĞI
    // ==========================================
    applyAdvancedFilter(filters) {
        console.log("Filtreler Uygulanıyor:", filters);

        // Her zaman en baştaki temiz veriden (ama soft-delete yapılmış halinden) başla
        let filtered = this.allData.filter(item => {
            const status = (item.isActive !== undefined) ? item.isActive : item.IsActive;
            return status !== false;
        });

        // 1. Kategori Filtresi
        if (filters.category) {
            filtered = filtered.filter(item => item.categoryName === filters.category);
        }

        // 2. Stok Durumu Filtresi
        if (filters.stockStatus) {
            filtered = filtered.filter(item => {
                if (filters.stockStatus === 'out') return item.quantity === 0;
                if (filters.stockStatus === 'low') return item.quantity > 0 && item.quantity <= item.criticalStockQuantity;
                if (filters.stockStatus === 'in') return item.quantity > item.criticalStockQuantity;
                return true;
            });
        }

        // 3. Satış Fiyatı Aralığı
        if (filters.priceMin) {
            filtered = filtered.filter(item => item.salePrice >= parseFloat(filters.priceMin));
        }
        if (filters.priceMax) {
            filtered = filtered.filter(item => item.salePrice <= parseFloat(filters.priceMax));
        }

        // 4. Alış Fiyatı Aralığı
        if (filters.purchasePriceMin) {
            filtered = filtered.filter(item => item.purchasePrice >= parseFloat(filters.purchasePriceMin));
        }
        if (filters.purchasePriceMax) {
            filtered = filtered.filter(item => item.purchasePrice <= parseFloat(filters.purchasePriceMax));
        }

        // 5. Son Kullanma Tarihi Aralığı
        if (filters.expDateStart || filters.expDateEnd) {
            filtered = filtered.filter(item => {
                if (!item.expirationDate) return false;
                const itemDate = new Date(item.expirationDate).setHours(0, 0, 0, 0);

                let isValid = true;
                if (filters.expDateStart) {
                    const start = new Date(filters.expDateStart).setHours(0, 0, 0, 0);
                    if (itemDate < start) isValid = false;
                }
                if (filters.expDateEnd) {
                    const end = new Date(filters.expDateEnd).setHours(0, 0, 0, 0);
                    if (itemDate > end) isValid = false;
                }
                return isValid;
            });
        }

        // Global activeData'yı güncelle ki Arama çubuğu bu sonuçlar içinde arasın
        this.activeData = filtered;
        this.renderInventory(this.activeData);
    }

    // ==========================================
    // 4. ARAMA (SEARCH)
    // ==========================================
    setupSearch() {
        if (!this.searchInput) return;

        this.searchInput.addEventListener('keyup', debounce(() => {
            const term = this.searchInput.value.trim().toLowerCase();

            if (!term) {
                // Arama boşsa mevcut filtrelenmiş listeyi göster
                this.renderInventory(this.activeData);
                return;
            }

            // Mevcut (belki de filtrelenmiş) liste üzerinde arama yap
            const searchResults = this.activeData.filter(item => {
                const name = (item.productName || '').toLowerCase();
                const barcode = (item.barcode || '').toLowerCase();
                const category = (item.categoryName || '').toLowerCase();
                const batch = (item.batchNumber || '').toLowerCase();

                return name.includes(term) || barcode.includes(term) || category.includes(term) || batch.includes(term);
            });

            this.renderInventory(searchResults);
        }, 300));
    }

    // ==========================================
    // 5. TABLO RENDER İŞLEMLERİ
    // ==========================================
    renderInventory(data) {
        if (!data || data.length === 0) {
            this.tableBody.innerHTML = `<tr><td colspan="8" class="text-center py-5 text-muted">Kayıt bulunamadı.</td></tr>`;
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
                                onclick="deleteInventoryItem('${item.id}')" title="Sil">
                            <i class="fas fa-trash"></i>
                        </button>
                    </div>
                </td>
            </tr>`;
    }

    // --- Render Helpers ---
    renderProductCell(info) {
        return `<td><div class="d-flex align-items-center"><div class="product-avatar me-3 shadow-sm">${info.initial}</div><div><h6 class="mb-0 fw-bold text-dark">${info.name}</h6><small class="text-muted">${info.category} • <span class="text-secondary">Seri: ${info.batch}</span> • <span class="text-secondary">${info.unitType}</span></small></div></div></td>`;
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
        return { name, category: item.categoryName || '-', batch: item.batchNumber || '-', unitType: item.unitTypeName || '-', initial: name.charAt(0).toUpperCase() };
    }
    showLoading() { this.tableBody.innerHTML = `<tr><td colspan="8" class="text-center py-5"><div class="spinner-border text-primary"></div></td></tr>`; }
    showError(msg) { this.tableBody.innerHTML = `<tr><td colspan="8" class="text-center text-danger py-4">${msg}</td></tr>`; }
}

// Global Manager
let inventoryManagerInstance;

// ==========================================
// GLOBAL BUTON FONKSİYONLARI
// ==========================================

// 1. Filtrele Butonu
window.applyFilters = function () {
    const filters = {
        category: document.getElementById('filterCategory').value,
        stockStatus: document.getElementById('filterStockStatus').value,

        // Fiyatlar
        priceMin: document.getElementById('filterPriceMin').value,
        priceMax: document.getElementById('filterPriceMax').value,
        purchasePriceMin: document.getElementById('filterPurchasePriceMin').value,
        purchasePriceMax: document.getElementById('filterPurchasePriceMax').value,

        // Tarihler (Son Kullanma Tarihi)
        expDateStart: document.getElementById('filterExpDateStart').value,
        expDateEnd: document.getElementById('filterExpDateEnd').value
    };

    if (inventoryManagerInstance) {
        inventoryManagerInstance.applyAdvancedFilter(filters);
    }

    // Modalı kapat
    const modalEl = document.getElementById('filterInventoryModal');
    if (modalEl) bootstrap.Modal.getInstance(modalEl)?.hide();
};

window.clearFilters = function () {
    document.getElementById('filterForm').reset();
    if (inventoryManagerInstance) {
        // Filtreleri sıfırla = activeData'yı tekrar IsActive olan ham veriye eşitle
        inventoryManagerInstance.activeData = inventoryManagerInstance.allData.filter(item => {
            const status = (item.isActive !== undefined) ? item.isActive : item.IsActive;
            return status !== false;
        });
        inventoryManagerInstance.renderInventory(inventoryManagerInstance.activeData);
    }
};

// 2. Düzenleme
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

// 3. Kaydetme
window.saveInventoryItem = async function () {
    const id = document.getElementById('editId').value;
    const token = localStorage.getItem('jwtToken');
    const btn = document.querySelector('#editInventoryModal .btn-primary');

    btn.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>Kaydediliyor...';
    btn.disabled = true;

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
        productId: originalItem.productId,
        companyId: originalItem.companyId,
        isActive: true,
        updatedAt: new Date().toISOString()
    };

    try {
        const res = await fetch(`/api/Inventories`, {
            method: 'PUT',
            headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
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
        btn.innerHTML = '<i class="fas fa-save me-2"></i>Kaydet';
        btn.disabled = false;
    }
};

// 4. Silme (Soft Delete)
window.deleteInventoryItem = async function (id) {
    if (!confirm('⚠️ Bu ürünü arşivlemek istediğinize emin misiniz?')) return;
    const token = localStorage.getItem('jwtToken');

    try {
        const res = await fetch(`/api/Inventories/soft-delete`, {
            method: 'PUT',
            headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
            body: JSON.stringify({ Id: id, IsActive: false })
        });

        if (res.ok) {
            alert('✅ Ürün başarıyla arşivlendi.');
            await inventoryManagerInstance.loadInventory();
        } else {
            const err = await res.json();
            alert('❌ Hata: ' + (err.message || "İşlem başarısız."));
        }
    } catch (e) {
        console.error(e);
        alert('❌ Sunucu hatası.');
    }
};

// Başlatma
document.addEventListener('DOMContentLoaded', () => {
    console.log('🚀 Inventory.js başlatılıyor...');
    inventoryManagerInstance = new InventoryManager();
    inventoryManagerInstance.init();
});