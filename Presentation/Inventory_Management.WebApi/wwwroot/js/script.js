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
        return `₺${parseFloat(amount).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
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
        sessionStorage.clear(); // Clear chat history as well
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
    }

    async init() {
        console.log("📊 Dashboard başlatılıyor...");
        try {
            // 1. Tüm verileri tek seferde, verimli bir şekilde çek
            const [inventories, suppliers, movements, calendarData] = await Promise.all([
                Utils.fetchAPI('/Inventories?IsActive=true'), // Sadece aktif envanteri çek
                Utils.fetchAPI('/Suppliers?IsActive=true'),   // Sadece aktif tedarikçileri çek
                Utils.fetchAPI('/StockMovements'),
                Utils.fetchAPI('/Suppliers/calendar')      // Takvim/sipariş verilerini çek
            ]);

            console.log("✅ API Verileri Yüklendi", {
                inventories: inventories.length,
                suppliers: suppliers.length,
                movements: movements.length,
                calendar: calendarData.length
            });

            // 2. Verileri ilgili modüllere dağıt
            this.processStatsCards(inventories, suppliers, movements, calendarData);
            this.processInventoryWidgets(inventories);
            this.processRecentActivity(movements);
            this.renderMovementChart(movements); // DİNAMİK GRAFİK

        } catch (error) {
            console.error("❌ Dashboard Yükleme Hatası:", error);
            // Hata durumunda kullanıcıya bilgi ver
            document.getElementById('criticalStockTable').innerHTML = `<tr><td colspan="5" class="text-center text-danger py-4">Dashboard verileri yüklenemedi.</td></tr>`;
        }
    }

    /**
     * Ana İstatistik Kartlarını İşle
     */
    processStatsCards(inventories, suppliers, movements, calendarData) {
        // 1. Toplam Envanter Değeri
        const totalInventoryValue = inventories
            .filter(item => {
                const status = (item.IsActive !== undefined) ? item.IsActive : item.isActive;
                return status === true || status === 1;
            })
            .reduce((sum, item) => sum + (item.quantity * item.salePrice), 0);

        // 2. Kritik Stok Sayısı
        const criticalCount = inventories.filter(p => p.quantity <= p.criticalStockQuantity && p.quantity > 0).length;

        // 3. Aktif Tedarikçi Sayısı (Veri zaten filtrelenmiş geldi)
        const activeSuppliers = suppliers.length;

        // Date objects for filtering
        const today = new Date();
        today.setHours(0, 0, 0, 0); // Günün başlangıcı
        const tomorrow = new Date(today);
        tomorrow.setDate(tomorrow.getDate() + 1);

        // 4. Günlük Satış
        const dailySalesValue = movements
            .filter(m => {
                const moveDate = new Date(m.createdAt);
                const isOutcome = (m.moveTypeName || '').toLowerCase().includes('out') || (m.moveTypeName || '').toLowerCase().includes('çıkış');
                return isOutcome && moveDate >= today && moveDate < tomorrow;
            })
            .reduce((sum, m) => sum + (m.payment || 0), 0);

        // 5. Haftalık Beklenen Siparişler
        const startOfWeek = new Date(today);
        startOfWeek.setDate(startOfWeek.getDate() - today.getDay() + (today.getDay() === 0 ? -6 : 1)); // Monday
        const endOfWeek = new Date(startOfWeek);
        endOfWeek.setDate(endOfWeek.getDate() + 7);

        const weeklyExpectedOrders = calendarData.filter(event => {
            if (!event.start) return false;
            const eventDate = new Date(event.start);
            return eventDate >= startOfWeek && eventDate < endOfWeek;
        }).length;

        // 6. Aylık Satış
        const currentMonth = today.getMonth();
        const currentYear = today.getFullYear();
        const monthlySalesValue = movements
            .filter(m => {
                const moveDate = new Date(m.createdAt);
                const isOutcome = (m.moveTypeName || '').toLowerCase().includes('out') || (m.moveTypeName || '').toLowerCase().includes('çıkış');
                return isOutcome && moveDate.getMonth() === currentMonth && moveDate.getFullYear() === currentYear;
            })
            .reduce((sum, m) => sum + (m.payment || 0), 0);
        
        const lastMonth = currentMonth === 0 ? 11 : currentMonth - 1;
        const lastYear = currentMonth === 0 ? currentYear - 1 : currentYear;
        const lastMonthSales = movements
            .filter(m => {
                const moveDate = new Date(m.createdAt);
                const isOutcome = (m.moveTypeName || '').toLowerCase().includes('out');
                return isOutcome && moveDate.getMonth() === lastMonth && moveDate.getFullYear() === lastYear;
            })
            .reduce((sum, m) => sum + (m.payment || 0), 0);

        // Kartları Güncelle
        this.updateStatsCard('totalInventoryValue', totalInventoryValue, 12);
        this.updateStatsCard('criticalStock', criticalCount, null);
        this.updateStatsCard('activeSuppliers', activeSuppliers, null);
        this.updateStatsCard('monthlySales', monthlySalesValue, Utils.calculatePercentage(monthlySalesValue, lastMonthSales));
        this.updateStatsCard('dailySales', dailySalesValue, null); // Yüzdelik şimdilik null
        this.updateStatsCard('weeklyOrders', weeklyExpectedOrders, null);
    }

    /**
     * İstatistik Kartlarını Güncelle
     */
    updateStatsCard(type, value, percentage) {
        const cards = {
            totalInventoryValue: { element: document.querySelector('.border-primary h3'), formatter: Utils.formatCurrency, percentElement: document.querySelector('.border-primary .text-success') },
            criticalStock: { element: document.getElementById('criticalCount'), formatter: (v) => v.toString(), percentElement: null },
            activeSuppliers: { element: document.querySelector('.border-warning h3'), formatter: (v) => v.toString(), percentElement: null },
            monthlySales: { element: document.querySelector('.border-success h3'), formatter: Utils.formatCurrency, percentElement: document.querySelector('.border-success .text-success') },
            dailySales: { element: document.getElementById('dailySales'), formatter: Utils.formatCurrency, percentElement: document.querySelector('.border-info .small') }, // TODO: Yüzde eklenebilir
            weeklyOrders: { element: document.getElementById('weeklyExpectedOrders'), formatter: (v) => v.toString(), percentElement: null }
        };

        const card = cards[type];
        if (!card || !card.element) return;
        card.element.textContent = card.formatter(value);

        if (card.percentElement && percentage !== null && isFinite(percentage)) {
            const isPositive = percentage >= 0;
            const icon = isPositive ? 'fa-arrow-up' : 'fa-arrow-down';
            const colorClass = isPositive ? 'text-success' : 'text-danger';
            card.percentElement.className = `small mb-0 fw-bold ${colorClass}`;
            card.percentElement.innerHTML = `<i class="fas ${icon}"></i> ${isPositive ? '+' : ''}${percentage}% geçen ay`;
        }
    }

    /**
     * Envanter ile ilgili Widget'ları (Kritik Stok, SKT, Kategori) işle
     */
    processInventoryWidgets(inventories) {
        // Kritik stok analizi
        const critical = inventories.filter(p => p.quantity <= p.criticalStockQuantity && p.quantity > 0);
        this.renderCriticalTable(critical.slice(0, 5));

        // SKT analizi
        const expiring = this.getExpiringProducts(inventories);
        this.renderExpirationList(expiring.slice(0, 5));

        // Kategori grafiği
        this.renderCategoryChart(inventories);
    }

    getExpiringProducts(products, days = 30) {
        const today = new Date();
        const warningDate = new Date();
        warningDate.setDate(today.getDate() + days);
        return products
            .filter(p => p.expirationDate)
            .map(p => ({ ...p, expDate: new Date(p.expirationDate) }))
            .filter(p => p.expDate >= today && p.expDate <= warningDate)
            .sort((a,b) => a.expDate - b.expDate);
    }

    renderCriticalTable(items) {
        const tbody = document.getElementById('criticalStockTable');
        if (!tbody) return;
        if (items.length === 0) {
            tbody.innerHTML = `<tr><td colspan="5" class="text-center text-success py-3 small"><i class="fas fa-check-circle me-2"></i>Kritik stokta ürün yok.</td></tr>`;
            return;
        }
        tbody.innerHTML = items.map(item => `
            <tr>
                <td class="fw-bold text-dark small">${item.productName || 'Ürün'}</td>
                <td class="text-danger fw-bold small">${item.quantity}</td>
                <td class="text-muted small">${item.criticalStockQuantity}</td>
                <td><span class="badge bg-danger-subtle text-danger small">KRİTİK</span></td>
                <td class="text-end">
                    <button class="btn btn-sm btn-light text-primary border-0" title="Sipariş Ver"><i class="fas fa-shopping-cart"></i></button>
                </td>
            </tr>`).join('');
    }

    renderExpirationList(items) {
        const list = document.getElementById('expirationList');
        if (!list) return;
        if (items.length === 0) {
            list.innerHTML = '<div class="text-center small text-muted py-3">Yaklaşan SKT yok.</div>';
            return;
        }
        list.innerHTML = items.map(item => {
            const daysLeft = Math.ceil((new Date(item.expirationDate) - new Date()) / (1000 * 60 * 60 * 24));
            const colorClass = daysLeft < 7 ? "text-danger" : "text-warning";
            return `
                <div class="d-flex justify-content-between align-items-center mb-2 pb-2 border-bottom">
                    <div>
                        <h6 class="mb-0 small fw-bold text-dark">${item.productName}</h6>
                        <small class="${colorClass} fw-bold" style="font-size: 0.7rem;"><i class="fas fa-clock me-1"></i>${daysLeft} Gün Kaldı</small>
                    </div>
                    <span class="badge bg-light text-secondary border">${Utils.formatDate(item.expirationDate)}</span>
                </div>`;
        }).join('');
    }

    /**
     * Son Aktiviteler Widget'ını işle
     */
    processRecentActivity(movements) {
        const container = document.getElementById('recentActivityList');
        if (!container) return;
        if (!movements || movements.length === 0) {
            container.innerHTML = '<div class="text-center small text-muted py-3">Henüz hareket yok.</div>';
            return;
        }
        // En son hareketleri göstermek için sırala (en yeni en üstte)
        const sortedMovements = movements.sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt));
        container.innerHTML = sortedMovements.slice(0, 5).map(item => {
            const isIncome = (item.moveTypeName || '').toLowerCase().includes('in') || (item.moveTypeName || '').toLowerCase().includes('giriş');
            const iconClass = isIncome ? 'fa-arrow-down text-success' : 'fa-arrow-up text-danger';
            const bgClass = isIncome ? 'bg-success-subtle' : 'bg-danger-subtle';
            const displayDate = new Date(item.createdAt).toLocaleDateString('tr-TR', { day: '2-digit', month: '2-digit' });
            return `
                <div class="d-flex align-items-center mb-3 border-bottom pb-2">
                    <div class="${bgClass} rounded-circle d-flex align-items-center justify-content-center me-3" style="width: 40px; height: 40px;">
                        <i class="fas ${iconClass}"></i>
                    </div>
                    <div class="flex-grow-1">
                        <h6 class="mb-0 small fw-bold text-dark">${item.productName || 'Ürün'}</h6>
                        <small class="text-muted" style="font-size: 0.75rem;"><i class="fas fa-user me-1"></i>${item.userName || 'Sistem'} • <strong>${item.quantity} Adet</strong></small>
                    </div>
                    <div class="text-end">
                        <small class="text-muted fw-bold" style="font-size: 0.7rem;">${displayDate}</small>
                    </div>
                </div>`;
        }).join('');
    }

    renderCategoryChart(products) {
        const ctx = document.getElementById('categoryChart');
        if (!ctx) return;
        const categories = products.reduce((acc, p) => {
            const cat = p.categoryName || 'Diğer';
            acc[cat] = (acc[cat] || 0) + p.quantity; // Adet bazlı gösterim
            return acc;
        }, {});
        if (this.charts.category) this.charts.category.destroy();
        this.charts.category = new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: Object.keys(categories),
                datasets: [{
                    data: Object.values(categories),
                    backgroundColor: ['#0d6efd', '#6f42c1', '#198754', '#ffc107', '#dc3545', '#0dcaf0', '#fd7e14', '#20c997'],
                    borderWidth: 2,
                    borderColor: '#ffffff'
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { position: 'bottom', labels: { usePointStyle: true, boxWidth: 8, font: { size: 10 } } } },
                cutout: '75%'
            }
        });
    }

    renderMovementChart(movements) {
        const ctx = document.getElementById('myChart');
        if (!ctx) return;

        const monthlyData = { in: Array(12).fill(0), out: Array(12).fill(0) };
        const currentYear = new Date().getFullYear();

        movements.forEach(m => {
            const moveDate = new Date(m.createdAt);
            if (moveDate.getFullYear() === currentYear) {
                const month = moveDate.getMonth();
                const isIncome = (m.moveTypeName || '').toLowerCase().includes('in') || (m.moveTypeName || '').toLowerCase().includes('giriş');
                // Adet bazlı sayım yapıyoruz
                if (isIncome) {
                    monthlyData.in[month] += m.quantity;
                } else {
                    monthlyData.out[month] += m.quantity;
                }
            }
        });

        if (this.charts.movement) this.charts.movement.destroy();
        this.charts.movement = new Chart(ctx, {
            type: 'line',
            data: {
                labels: ['Oca', 'Şub', 'Mar', 'Nis', 'May', 'Haz', 'Tem', 'Ağu', 'Eyl', 'Eki', 'Kas', 'Ara'],
                datasets: [
                    {
                        label: 'Giriş (Adet)',
                        data: monthlyData.in,
                        borderColor: '#198754',
                        backgroundColor: 'rgba(25, 135, 84, 0.05)',
                        borderWidth: 2, fill: true, tension: 0.4
                    },
                    {
                        label: 'Çıkış (Adet)',
                        data: monthlyData.out,
                        borderColor: '#dc3545',
                        backgroundColor: 'rgba(220, 53, 69, 0.05)',
                        borderWidth: 2, fill: true, tension: 0.4
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: true, position: 'top', align: 'end' } },
                scales: { y: { beginAtZero: true, grid: { borderDash: [5, 5] } }, x: { grid: { display: false } } }
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
    static async init() {
        const calendarEl = document.getElementById('calendar');
        if (!calendarEl) return;

        const token = localStorage.getItem('jwtToken');

        try {
            // 1. Verileri DOĞRU adresten çek
            const response = await fetch('/api/Suppliers/calendar', {
                headers: { 'Authorization': `Bearer ${token}` },
                cache: 'no-cache' // Tarayıcı önbelleğini devre dışı bırak
            });
            if (!response.ok) {
                throw new Error('Takvim verileri alınamadı.');
            }
            const apiData = await response.json();

            // 2. Verileri suppliers.js'teki güncel mantıkla işle
            const eventsData = apiData.flatMap(item => {
                const baseEvent = {
                    id: item.id,
                    title: item.title || item.ruleName,
                    backgroundColor: item.calendarColor || '#0d6efd',
                    borderColor: item.calendarColor || '#0d6efd',
                    textColor: '#fff',
                    extendedProps: {
                        description: "Tedarikçi Planı"
                    }
                };

                // Backend'in hesapladığı kesin tarihli eventler (Aralıklı veya Aylık)
                if (item.start) {
                    return [{ ...baseEvent, start: item.start, allDay: false }];
                }

                // Tekrarlayan haftalık eventler (interval=1)
                if (item.frequency === 1 && item.daysOfWeek) {
                    return [{
                        ...baseEvent,
                        daysOfWeek: Array.isArray(item.daysOfWeek)
                            ? item.daysOfWeek
                            : item.daysOfWeek.split(',').map(Number),
                        startRecur: item.startRecur,
                        endRecur: item.endRecur,
                        startTime: item.startTime || '09:00'
                    }];
                }

                return [];
            });

            // 3. Takvimi Salt Okunur modda başlat
            const calendar = new FullCalendar.Calendar(calendarEl, {
                initialView: 'dayGridMonth',
                headerToolbar: {
                    left: 'prev,next today',
                    center: 'title',
                    right: 'dayGridMonth,timeGridWeek,listWeek'
                },
                height: 500,
                locale: 'tr',
                events: eventsData,
                
                // Salt okunur özellikler
                selectable: false,
                editable: false,
                eventClick: function(info) {
                    // Tıklamayı engelle, hiçbir şey yapma
                    info.jsEvent.preventDefault(); 
                },

                // Mouse üzerine gelince ipucu göstermeye devam et
                eventDidMount: function (info) {
                    if (info.event.title) {
                        new bootstrap.Tooltip(info.el, {
                            title: info.event.title,
                            placement: 'top',
                            trigger: 'hover',
                            container: 'body'
                        });
                        info.el.style.cursor = 'default'; // İmleci standart yap
                    }
                }
            });

            calendar.render();

        } catch (error) {
            console.error("Dashboard Takvim Hatası:", error);
            calendarEl.innerHTML = '<div class="alert alert-danger text-center p-4 small">Takvim verileri yüklenemedi.</div>';
        }
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