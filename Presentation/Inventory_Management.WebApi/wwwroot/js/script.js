/**
 * SCRIPT.JS - Ana JavaScript Dosyası
 * Tüm sayfalarda ortak işlevler ve Dashboard özel kodları
 */

'use strict';

// ==========================================
// GLOBAL VARIABLES
// ==========================================
const API_CONFIG = {
    baseURL: '/api',
    headers: {
        'Content-Type': 'application/json',
        get Authorization() {
            const token = localStorage.getItem('jwtToken');
            return token ? `Bearer ${token}` : '';
        }
    }
};

// ==========================================
// UTILITY FUNCTIONS
// ==========================================
const Utils = {
    /**
     * API çağrısı yapar
     */
    async fetchAPI(endpoint, options = {}) {
        try {
            const response = await fetch(`${API_CONFIG.baseURL}${endpoint}`, {
                ...options,
                headers: { ...API_CONFIG.headers, ...options.headers }
            });

            if (!response.ok) {
                const error = await response.text();
                throw new Error(error || `HTTP ${response.status}`);
            }

            return await response.json();
        } catch (error) {
            console.error(`API Error (${endpoint}):`, error);
            throw error;
        }
    },

    /**
     * Tarih formatlama
     */
    formatDate(dateString, options = { day: 'numeric', month: 'short', year: 'numeric' }) {
        return new Date(dateString).toLocaleDateString('tr-TR', options);
    },

    /**
     * Para formatı
     */
    formatCurrency(amount) {
        return `$${parseFloat(amount).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
    },

    /**
     * İsimden baş harfleri al
     */
    getInitials(name, maxLength = 2) {
        if (!name || name === "Misafir") return "U";
        const matches = name.match(/\b(\w)/g);
        return matches
            ? matches.join('').substring(0, maxLength).toUpperCase()
            : name.substring(0, maxLength).toUpperCase();
    },

    /**
     * Yüzde hesaplama
     */
    calculatePercentage(current, previous) {
        if (!previous || previous === 0) return 0;
        return (((current - previous) / previous) * 100).toFixed(1);
    }
};

// ==========================================
// AUTH GUARD
// ==========================================
class AuthGuard {
    static check() {
        const token = localStorage.getItem('jwtToken');
        const isLoginPage = window.location.pathname.includes('login.html');

        if (!token && !isLoginPage) {
            window.location.href = 'login.html';
            return false;
        }
        return true;
    }

    static logout() {
        localStorage.clear();
        window.location.href = 'login.html';
    }
}

// ==========================================
// USER PROFILE MANAGER
// ==========================================
class UserProfile {
    static load() {
        const userData = {
            name: localStorage.getItem('userName') || 'Misafir',
            company: localStorage.getItem('userCompany') || '',
            role: localStorage.getItem('userRole') || ''
        };

        this.render(userData);
    }

    static render(userData) {
        const elements = {
            name: document.getElementById('navFullName'),
            company: document.getElementById('navCompany'),
            role: document.getElementById('navRole'),
            circle: document.getElementById('navProfileCircle')
        };

        if (elements.name) elements.name.textContent = userData.name;
        if (elements.company) elements.company.textContent = userData.company;
        if (elements.role) elements.role.textContent = userData.role;

        if (elements.circle) {
            elements.circle.textContent = Utils.getInitials(userData.name);
        }
    }
}

// ==========================================
// DASHBOARD MANAGER
// ==========================================
class Dashboard {
    constructor() {
        this.charts = {
            movement: null,
            category: null
        };
        this.stats = {
            inventoryValue: 0,
            criticalStock: 0,
            suppliers: 0,
            monthlySales: 0
        };
    }

    async init() {
        console.log("📊 Dashboard başlatılıyor...");

        try {
            await Promise.all([
                this.loadDashboardStats(),
                this.loadInventoryStats(),
                this.loadRecentActivity()
            ]);
        } catch (error) {
            console.error("Dashboard Yükleme Hatası:", error);
        }
    }

    /**
     * Ana İstatistik Kartlarını Yükle
     */
    async loadDashboardStats() {
        try {
            // Paralel olarak tüm verileri çek
            const [inventories, suppliers, movements] = await Promise.all([
                Utils.fetchAPI('/Inventories'),
                Utils.fetchAPI('/Suppliers'),
                Utils.fetchAPI('/StockMovements')
            ]);

            // 1. Toplam Envanter Değeri
            const totalInventoryValue = inventories.reduce((sum, item) => {
                return sum + (item.quantity * item.salePrice);
            }, 0);

            // 2. Kritik Stok Sayısı
            const criticalCount = inventories.filter(p =>
                p.quantity <= p.criticalStockQuantity
            ).length;

            // 3. Aktif Tedarikçi Sayısı
            const activeSuppliers = suppliers.filter(s => s.isActive !== false).length;

            // 4. Aylık Satış Tutarı (Bu ayın çıkış hareketleri)
            const currentMonth = new Date().getMonth();
            const currentYear = new Date().getFullYear();

            const monthlySalesValue = movements
                .filter(m => {
                    const moveDate = new Date(m.createdAt);
                    const isOutcome = (m.moveTypeName || '').toLowerCase().includes('out') ||
                        (m.moveTypeName || '').toLowerCase().includes('çıkış');
                    return isOutcome &&
                        moveDate.getMonth() === currentMonth &&
                        moveDate.getFullYear() === currentYear;
                })
                .reduce((sum, m) => sum + (m.payment || 0), 0);

            // Geçen ay değerleri (Yüzde hesabı için)
            const lastMonth = currentMonth === 0 ? 11 : currentMonth - 1;
            const lastYear = currentMonth === 0 ? currentYear - 1 : currentYear;

            const lastMonthSales = movements
                .filter(m => {
                    const moveDate = new Date(m.createdAt);
                    const isOutcome = (m.moveTypeName || '').toLowerCase().includes('out');
                    return isOutcome &&
                        moveDate.getMonth() === lastMonth &&
                        moveDate.getFullYear() === lastYear;
                })
                .reduce((sum, m) => sum + (m.payment || 0), 0);

            // Kartları Güncelle
            this.updateStatsCard('totalInventoryValue', totalInventoryValue, 12); // Örnek: +12%
            this.updateStatsCard('criticalStock', criticalCount, null);
            this.updateStatsCard('activeSuppliers', activeSuppliers, null);
            this.updateStatsCard('monthlySales', monthlySalesValue,
                Utils.calculatePercentage(monthlySalesValue, lastMonthSales)
            );

            console.log("✅ Dashboard istatistikleri yüklendi:", {
                totalInventoryValue,
                criticalCount,
                activeSuppliers,
                monthlySalesValue
            });

        } catch (error) {
            console.error("❌ Dashboard İstatistikleri Hatası:", error);
        }
    }

    /**
     * İstatistik Kartlarını Güncelle
     */
    updateStatsCard(type, value, percentage) {
        const cards = {
            totalInventoryValue: {
                element: document.querySelector('.border-primary h3'),
                formatter: Utils.formatCurrency,
                percentElement: document.querySelector('.border-primary .text-success')
            },
            criticalStock: {
                element: document.getElementById('criticalCount'),
                formatter: (v) => v.toString(),
                percentElement: null
            },
            activeSuppliers: {
                element: document.querySelector('.border-warning h3'),
                formatter: (v) => v.toString(),
                percentElement: null
            },
            monthlySales: {
                element: document.querySelector('.border-success h3'),
                formatter: Utils.formatCurrency,
                percentElement: document.querySelector('.border-success .text-success')
            }
        };

        const card = cards[type];
        if (!card || !card.element) return;

        // Değeri güncelle
        card.element.textContent = card.formatter(value);

        // Yüzde değişimini güncelle
        if (card.percentElement && percentage !== null) {
            const isPositive = percentage >= 0;
            const icon = isPositive ? 'fa-arrow-up' : 'fa-arrow-down';
            const colorClass = isPositive ? 'text-success' : 'text-danger';

            card.percentElement.className = `small mb-0 fw-bold ${colorClass}`;
            card.percentElement.innerHTML = `
                <i class="fas ${icon}"></i> ${isPositive ? '+' : ''}${percentage}% geçen ay
            `;
        }
    }

    async loadInventoryStats() {
        try {
            const products = await Utils.fetchAPI('/Inventories');

            // Kritik stok analizi
            const critical = products.filter(p => p.quantity <= p.criticalStockQuantity);
            this.renderCriticalTable(critical.slice(0, 5));

            // SKT analizi
            const expiring = this.getExpiringProducts(products);
            this.renderExpirationList(expiring.slice(0, 5));

            // Grafikler
            this.renderCategoryChart(products);
            this.renderMovementChart();

        } catch (error) {
            console.error("Envanter İstatistikleri Hatası:", error);
        }
    }

    getExpiringProducts(products, days = 30) {
        const today = new Date();
        const warningDate = new Date();
        warningDate.setDate(today.getDate() + days);

        return products.filter(p => {
            const expDate = new Date(p.expirationDate);
            return expDate >= today && expDate <= warningDate;
        });
    }

    renderCriticalTable(items) {
        const tbody = document.getElementById('criticalStockTable');
        if (!tbody) return;

        if (items.length === 0) {
            tbody.innerHTML = `
                <tr>
                    <td colspan="5" class="text-center text-success py-3 small">
                        <i class="fas fa-check-circle me-2"></i>Kritik stokta ürün yok.
                    </td>
                </tr>`;
            return;
        }

        tbody.innerHTML = items.map(item => `
            <tr>
                <td class="fw-bold text-dark small">${item.productName || 'Ürün'}</td>
                <td class="text-danger fw-bold small">${item.quantity}</td>
                <td class="text-muted small">${item.criticalStockQuantity}</td>
                <td><span class="badge bg-danger-subtle text-danger small">KRİTİK</span></td>
                <td class="text-end">
                    <button class="btn btn-sm btn-light text-primary border-0" title="Sipariş Ver">
                        <i class="fas fa-shopping-cart"></i>
                    </button>
                </td>
            </tr>
        `).join('');
    }

    renderExpirationList(items) {
        const list = document.getElementById('expirationList');
        if (!list) return;

        if (items.length === 0) {
            list.innerHTML = '<div class="text-center small text-muted py-3">Yaklaşan SKT yok.</div>';
            return;
        }

        list.innerHTML = items.map(item => {
            const expDate = new Date(item.expirationDate);
            const daysLeft = Math.ceil((expDate - new Date()) / (1000 * 60 * 60 * 24));
            const colorClass = daysLeft < 7 ? "text-danger" : "text-warning";

            return `
                <div class="d-flex justify-content-between align-items-center mb-2 pb-2 border-bottom">
                    <div>
                        <h6 class="mb-0 small fw-bold text-dark">${item.productName}</h6>
                        <small class="${colorClass} fw-bold" style="font-size: 0.7rem;">
                            <i class="fas fa-clock me-1"></i>${daysLeft} Gün Kaldı
                        </small>
                    </div>
                    <span class="badge bg-light text-secondary border">${Utils.formatDate(item.expirationDate)}</span>
                </div>
            `;
        }).join('');
    }

    async loadRecentActivity() {
        const container = document.getElementById('recentActivityList');
        if (!container) return;

        try {
            const movements = await Utils.fetchAPI('/StockMovements');

            if (!movements || movements.length === 0) {
                container.innerHTML = '<div class="text-center small text-muted py-3">Henüz hareket yok.</div>';
                return;
            }

            container.innerHTML = movements.slice(0, 5).map(item => {
                const isIncome = (item.moveTypeName || '').toLowerCase().includes('in') ||
                    (item.moveTypeName || '').toLowerCase().includes('giriş');
                const iconClass = isIncome ? 'fa-arrow-down text-success' : 'fa-arrow-up text-danger';
                const bgClass = isIncome ? 'bg-success-subtle' : 'bg-danger-subtle';

                const today = new Date().toLocaleDateString('tr-TR');
                const itemDate = new Date(item.createdAt).toLocaleDateString('tr-TR');
                const displayDate = itemDate === today ? "Bugün" : itemDate;

                return `
                    <div class="d-flex align-items-center mb-3 border-bottom pb-2">
                        <div class="${bgClass} rounded-circle d-flex align-items-center justify-content-center me-3" 
                             style="width: 40px; height: 40px;">
                            <i class="fas ${iconClass}"></i>
                        </div>
                        <div class="flex-grow-1">
                            <h6 class="mb-0 small fw-bold text-dark">${item.productName || 'Ürün'}</h6>
                            <small class="text-muted" style="font-size: 0.75rem;">
                                <i class="fas fa-user me-1"></i>${item.userName || 'Sistem'} • <strong>${item.quantity} Adet</strong>
                            </small>
                        </div>
                        <div class="text-end">
                            <small class="text-muted fw-bold" style="font-size: 0.7rem;">${displayDate}</small>
                        </div>
                    </div>
                `;
            }).join('');

        } catch (error) {
            console.error("Aktivite Yükleme Hatası:", error);
            container.innerHTML = '<small class="text-danger">Veri yüklenemedi.</small>';
        }
    }

    renderCategoryChart(products) {
        const ctx = document.getElementById('categoryChart');
        if (!ctx) return;

        const categories = products.reduce((acc, p) => {
            const cat = p.categoryName || 'Diğer';
            acc[cat] = (acc[cat] || 0) + 1;
            return acc;
        }, {});

        if (this.charts.category) this.charts.category.destroy();

        this.charts.category = new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: Object.keys(categories),
                datasets: [{
                    data: Object.values(categories),
                    backgroundColor: ['#0d6efd', '#6610f2', '#198754', '#ffc107', '#dc3545', '#0dcaf0'],
                    borderWidth: 2,
                    borderColor: '#ffffff'
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: { usePointStyle: true, boxWidth: 8, font: { size: 10 } }
                    }
                },
                cutout: '75%'
            }
        });
    }

    renderMovementChart() {
        const ctx = document.getElementById('myChart');
        if (!ctx) return;

        if (this.charts.movement) this.charts.movement.destroy();

        this.charts.movement = new Chart(ctx, {
            type: 'line',
            data: {
                labels: ['Oca', 'Şub', 'Mar', 'Nis', 'May', 'Haz', 'Tem', 'Ağu', 'Eyl', 'Eki', 'Kas', 'Ara'],
                datasets: [
                    {
                        label: 'Giriş',
                        data: [12, 19, 3, 5, 2, 3, 15, 10, 20, 15, 25, 30],
                        borderColor: '#0d6efd',
                        backgroundColor: 'rgba(13, 110, 253, 0.05)',
                        borderWidth: 2,
                        fill: true,
                        tension: 0.4
                    },
                    {
                        label: 'Çıkış',
                        data: [5, 10, 15, 10, 20, 15, 10, 5, 15, 10, 20, 25],
                        borderColor: '#dc3545',
                        backgroundColor: 'rgba(220, 53, 69, 0.05)',
                        borderWidth: 2,
                        fill: true,
                        tension: 0.4
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: true, position: 'top', align: 'end' } },
                scales: {
                    y: { beginAtZero: true, grid: { borderDash: [5, 5] } },
                    x: { grid: { display: false } }
                }
            }
        });
    }
}

// ==========================================
// MENU TOGGLE
// ==========================================
class MenuManager {
    static init() {
        const toggleButton = document.getElementById("menu-toggle");
        const wrapper = document.getElementById("wrapper");

        if (toggleButton && wrapper) {
            toggleButton.addEventListener('click', () => {
                wrapper.classList.toggle("toggled");
            });
        }
    }
}

// ==========================================
// CALENDAR INITIALIZATION
// ==========================================
class CalendarManager {
    static init() {
        const calendarEl = document.getElementById('calendar');
        if (!calendarEl) return;

        const calendar = new FullCalendar.Calendar(calendarEl, {
            initialView: 'dayGridMonth',
            headerToolbar: {
                left: 'prev,next today',
                center: 'title',
                right: 'dayGridMonth,timeGridWeek,listWeek'
            },
            height: 650,
            themeSystem: 'bootstrap5',
            events: [
                {
                    title: 'TechSupply Co. (50 Mouse)',
                    start: '2025-12-05',
                    backgroundColor: '#0d6efd',
                    borderColor: '#0d6efd'
                },
                {
                    title: 'Cable World (100 USB-C)',
                    start: '2025-12-10',
                    backgroundColor: '#198754',
                    borderColor: '#198754'
                },
                {
                    title: 'Vision Tech (Webcams)',
                    start: '2025-12-15',
                    backgroundColor: '#ffc107',
                    borderColor: '#ffc107',
                    textColor: '#000'
                }
            ],
            eventClick: function (info) {
                alert('Teslimat Detayı:\n' + info.event.title);
            }
        });

        calendar.render();
    }
}

// ==========================================
// PAGE INITIALIZATION
// ==========================================
document.addEventListener('DOMContentLoaded', async () => {
    // Auth kontrolü
    if (!AuthGuard.check()) return;

    // Login sayfası değilse kullanıcı bilgilerini yükle
    const isLoginPage = window.location.pathname.includes('login.html');
    if (!isLoginPage) {
        UserProfile.load();
        MenuManager.init();
    }

    // Dashboard sayfasıysa dashboard'u başlat
    if (document.getElementById('myChart')) {
        const dashboard = new Dashboard();
        await dashboard.init();
        CalendarManager.init();
    }
});

// ==========================================
// GLOBAL FUNCTIONS
// ==========================================
window.logout = AuthGuard.logout;