/**
 * INVENTORY.JS - Envanter Yönetim Sayfası
 */

'use strict';

// ==========================================
// INVENTORY MANAGER CLASS
// ==========================================
class InventoryManager {
    constructor() {
        this.token = localStorage.getItem('jwtToken');
        this.tableBody = document.getElementById('inventoryTableBody');
    }

    async init() {
        if (!this.tableBody) {
            console.warn('Inventory table body bulunamadı!');
            return;
        }

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
                throw new Error('Veri çekilemedi. Yetkiniz olmayabilir.');
            }

            const data = await response.json();
            console.log('📦 Envanter Verileri:', data);

            this.renderInventory(data);

        } catch (error) {
            console.error('Envanter Yükleme Hatası:', error);
            this.showError(error.message);
        }
    }

    showLoading() {
        this.tableBody.innerHTML = `
            <tr>
                <td colspan="8" class="text-center py-5">
                    <div class="spinner-border text-primary" role="status"></div>
                    <p class="text-muted mt-2">Yükleniyor...</p>
                </td>
            </tr>
        `;
    }

    showError(message) {
        this.tableBody.innerHTML = `
            <tr>
                <td colspan="8" class="text-center text-danger py-4">
                    <i class="fas fa-exclamation-triangle fs-3 mb-2"></i>
                    <p>Veriler yüklenirken hata oluştu: ${message}</p>
                </td>
            </tr>
        `;
    }

    renderInventory(data) {
        if (data.length === 0) {
            this.tableBody.innerHTML = `
                <tr>
                    <td colspan="8" class="text-center py-5">
                        <i class="fas fa-box-open fs-1 text-muted mb-3 d-block"></i>
                        <h5 class="text-muted">Hiç kayıt bulunamadı</h5>
                        <p class="text-muted small">Yeni stok eklemek için "Yeni Stok Ekle" butonuna tıklayın.</p>
                    </td>
                </tr>
            `;
            return;
        }

        this.tableBody.innerHTML = data.map((item, index) =>
            this.createTableRow(item, index + 1)
        ).join('');
    }

    createTableRow(item, rowNumber) {
        const status = this.calculateStatus(item);
        const dateInfo = this.formatExpirationDate(item.expirationDate);
        const productInfo = this.extractProductInfo(item);

        return `
            <tr class="fade-in">
                <td class="ps-4 fw-bold text-secondary">${rowNumber}</td>
                
                ${this.renderProductCell(productInfo)}
                ${this.renderBarcodeCell(item.barcode)}
                ${this.renderStockCell(item, status)}
                ${this.renderPriceCell(item)}
                ${this.renderExpirationCell(dateInfo)}
                ${this.renderStatusCell(status)}
                ${this.renderActionsCell(item.id)}
            </tr>
        `;
    }

    renderProductCell(productInfo) {
        return `
            <td>
                <div class="d-flex align-items-center">
                    <div class="product-avatar me-3 shadow-sm">
                        ${productInfo.initial}
                    </div>
                    <div>
                        <h6 class="mb-0 fw-bold text-dark">${productInfo.name}</h6>
                        <small class="text-muted" style="font-size: 0.75rem;">
                            <i class="fas fa-layer-group me-1"></i>${productInfo.category} 
                            <span class="mx-1">•</span> 
                            <span class="text-secondary">Seri: ${productInfo.batch}</span>
                        </small>
                    </div>
                </div>
            </td>
        `;
    }

    renderBarcodeCell(barcode) {
        const barcodeNumber = barcode || 'Barkod Yok';
        const barcodeClass = barcode ? 'text-dark' : 'text-muted';

        return `
            <td>
                <div class="d-flex flex-column">
                    <span class="fw-bold ${barcodeClass} font-monospace">${barcodeNumber}</span>
                    <small class="text-muted" style="font-size: 0.7rem;">
                        <i class="fas fa-barcode me-1"></i>Barkod No
                    </small>
                </div>
            </td>
        `;
    }

    renderStockCell(item, status) {
        return `
            <td>
                <div class="d-flex flex-column">
                    <span class="fw-bold text-dark">${item.quantity} <small class="text-muted fw-normal">Adet</small></span>
                    <div class="progress mt-1" style="height: 4px; width: 80px;">
                        <div class="progress-bar ${status.barColor}" role="progressbar" style="width: ${status.percent}%"></div>
                    </div>
                    <small class="text-danger mt-1" style="font-size: 0.7rem;">Min: ${item.criticalStockQuantity}</small>
                </div>
            </td>
        `;
    }

    renderPriceCell(item) {
        return `
            <td>
                <div class="d-flex flex-column">
                    <span class="fw-bold text-dark">₺${item.salePrice}</span>
                    <small class="text-muted" style="font-size: 0.75rem;">Maliyet: ₺${item.purchasePrice}</small>
                </div>
            </td>
        `;
    }

    renderExpirationCell(dateInfo) {
        return `
            <td>
                <div class="${dateInfo.class}" style="font-size: 0.9rem;">
                    <i class="far ${dateInfo.icon} me-1"></i> ${dateInfo.formatted}
                </div>
            </td>
        `;
    }

    renderStatusCell(status) {
        return `
            <td>
                <span class="badge rounded-pill ${status.badgeClass} px-3 py-2 fw-normal">
                    ${status.text}
                </span>
            </td>
        `;
    }

    renderActionsCell(id) {
        return `
            <td class="text-end pe-4">
                <div class="btn-group">
                    <button class="btn btn-sm btn-light text-primary border-0 rounded-circle me-1 card-hover" 
                            onclick="editInventory('${id}')" title="Düzenle">
                        <i class="fas fa-pen"></i>
                    </button>
                    <button class="btn btn-sm btn-light text-danger border-0 rounded-circle card-hover" 
                            onclick="deleteInventory('${id}')" title="Sil">
                        <i class="fas fa-trash"></i>
                    </button>
                </div>
            </td>
        `;
    }

    calculateStatus(item) {
        const percent = Math.min((item.quantity / 100) * 100, 100);

        if (item.quantity === 0) {
            return {
                badgeClass: 'bg-secondary-subtle text-secondary border border-secondary-subtle',
                text: 'Tükendi',
                barColor: 'bg-secondary',
                percent: 0
            };
        } else if (item.quantity <= item.criticalStockQuantity) {
            return {
                badgeClass: 'bg-danger-subtle text-danger border border-danger-subtle',
                text: 'Kritik',
                barColor: 'bg-danger',
                percent
            };
        } else {
            return {
                badgeClass: 'bg-success-subtle text-success border border-success-subtle',
                text: 'Stokta Var',
                barColor: 'bg-primary',
                percent
            };
        }
    }

    formatExpirationDate(dateString) {
        const expDate = new Date(dateString);
        const formatted = expDate.toLocaleDateString('tr-TR', {
            day: 'numeric',
            month: 'short',
            year: 'numeric'
        });
        const isExpired = expDate < new Date();

        return {
            formatted,
            class: isExpired ? 'text-danger fw-bold' : 'text-muted',
            icon: isExpired ? 'fa-exclamation-circle' : 'fa-calendar-alt'
        };
    }

    extractProductInfo(item) {
        const name = item.productName || 'Tanımsız Ürün';
        const category = item.categoryName || '-';
        const batch = item.batchNumber || '-';
        const initial = name.charAt(0).toUpperCase();

        return { name, category, batch, initial };
    }
}

// ==========================================
// GLOBAL FUNCTIONS
// ==========================================
window.editInventory = function (id) {
    console.log('Düzenlenecek ID:', id);
    alert('Düzenleme özelliği yakında eklenecek!');
};

window.deleteInventory = function (id) {
    if (confirm('Bu kaydı silmek istediğinize emin misiniz?')) {
        console.log('Silinecek ID:', id);
        // Silme API isteği buraya gelecek
        alert('Silme özelliği yakında eklenecek!');
    }
};

// ==========================================
// INITIALIZATION
// ==========================================
document.addEventListener('DOMContentLoaded', () => {
    const manager = new InventoryManager();
    manager.init();
});