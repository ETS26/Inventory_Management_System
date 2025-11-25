// script.js - Ana JavaScript Dosyası
// Tüm sayfalarda ortak çalışan ve Dashboard'a özel kodları içerir.

document.addEventListener('DOMContentLoaded', function () {

    // --- 1. AUTH GUARD (GÜVENLİK KONTROLÜ) ---
    const token = localStorage.getItem('jwtToken');
    const isLoginPage = window.location.pathname.includes('login.html');

    if (!token && !isLoginPage) {
        window.location.href = 'login.html';
        return;
    }

    // --- 2. KULLANICI BİLGİLERİNİ YÜKLE (HEADER) ---
    if (!isLoginPage) {
        loadUserInfo();
    }

    // --- 3. DASHBOARD SAYFASI KONTROLÜ ---
    // Eğer sayfada 'myChart' elementi varsa, burası Dashboard'dur.
    if (document.getElementById('myChart')) {
        console.log("📊 Dashboard Yüklendi. Veriler çekiliyor...");
        loadDashboardData();     // Kartlar, Tablolar, Grafikler
        loadRecentActivity();    // Sağdaki Son Aktiviteler
    }

    // --- 4. MENU TOGGLE İŞLEMİ ---
    const toggleButton = document.getElementById("menu-toggle");
    if (toggleButton) {
        toggleButton.onclick = function () {
            document.getElementById("wrapper").classList.toggle("toggled");
        };
    }
});

// --- KULLANICI BİLGİLERİNİ GÖSTERME ---
function loadUserInfo() {
    const userName = localStorage.getItem('userName') || "Misafir";
    const userCompany = localStorage.getItem('userCompany') || "";
    const userRole = localStorage.getItem('userRole') || "";

    // HTML Elementlerini Bul
    const nameEl = document.getElementById('navFullName');
    const companyEl = document.getElementById('navCompany');
    const circleEl = document.getElementById('navProfileCircle');

    // Verileri Yaz
    if (nameEl) nameEl.innerText = userName;
    if (companyEl) companyEl.innerText = userCompany; // Şirket İsmi

    // Profil Baş Harflerini Ayarla
    if (circleEl && userName !== "Misafir") {
        const matches = userName.match(/\b(\w)/g);
        const initials = matches ? matches.join('').substring(0, 2).toUpperCase() : userName.substring(0, 2).toUpperCase();
        circleEl.innerText = initials;
    }
}

// --- DASHBOARD VERİLERİNİ ÇEK VE İŞLE ---
async function loadDashboardData() {
    const token = localStorage.getItem('jwtToken');
    if (!token) return;

    try {
        // 1. Envanter Verilerini Çek
        const response = await fetch('/api/Inventories', { headers: { 'Authorization': `Bearer ${token}` } });
        if (!response.ok) throw new Error("Veri alınamadı");

        const products = await response.json();

        // A. Kritik Stok Sayısını ve Tablosunu Güncelle
        const criticalItems = products.filter(p => p.quantity <= p.criticalStockQuantity);
        const countEl = document.getElementById('criticalCount');
        if (countEl) countEl.innerText = criticalItems.length;

        renderCriticalTable(criticalItems.slice(0, 5)); // İlk 5 kritik ürünü göster

        // B. SKT (Son Kullanma Tarihi) Listesi
        const today = new Date();
        const warningDate = new Date();
        warningDate.setDate(today.getDate() + 30); // 30 gün kala uyar

        const expiringItems = products.filter(p => {
            const expDate = new Date(p.expirationDate);
            return expDate >= today && expDate <= warningDate;
        });
        renderExpirationList(expiringItems.slice(0, 5));

        // C. Kategori Grafiği (Pasta)
        renderCategoryChart(products);

        // D. Hareket Grafiği (Çizgi) - Şimdilik Statik Veri
        renderMovementChart();

        // E. Toplam Stok Değeri Hesapla (Ekstra Özellik)
        // (Eğer HTML'de id="totalValue" olan bir alan varsa oraya yazar)
        // const totalValue = products.reduce((sum, item) => sum + (item.quantity * item.purchasePrice), 0);
        // document.getElementById('totalValue').innerText = `$${totalValue.toLocaleString()}`;

    } catch (error) {
        console.error("Dashboard Veri Hatası:", error);
    }
}

// --- SON AKTİVİTELERİ ÇEK (Stock Movements) ---
async function loadRecentActivity() {
    const container = document.getElementById('recentActivityList');
    const token = localStorage.getItem('jwtToken');
    if (!container || !token) return;

    try {
        const response = await fetch('/api/StockMovements', { headers: { 'Authorization': `Bearer ${token}` } });
        if (response.ok) {
            const data = await response.json();
            container.innerHTML = '';

            // Sadece ilk 5 hareketi göster
            data.slice(0, 5).forEach(item => {
                const isIncome = (item.moveTypeName || "").toLowerCase().includes('in') || (item.moveTypeName || "").toLowerCase().includes('giriş');
                const iconClass = isIncome ? 'fa-arrow-down text-success' : 'fa-arrow-up text-danger';
                const bgClass = isIncome ? 'bg-success-subtle' : 'bg-danger-subtle';

                // Tarih Formatı (Bugün, Dün veya Tarih)
                const date = new Date(item.createdAt).toLocaleDateString('tr-TR');
                const today = new Date().toLocaleDateString('tr-TR');
                const displayDate = date === today ? "Bugün" : date;

                container.innerHTML += `
                <div class="d-flex align-items-center mb-3 border-bottom pb-2">
                    <div class="${bgClass} rounded-circle d-flex align-items-center justify-content-center me-3" style="width: 40px; height: 40px;">
                        <i class="fas ${iconClass}"></i>
                    </div>
                    <div class="flex-grow-1">
                        <h6 class="mb-0 small fw-bold text-dark">${item.productName}</h6>
                        <small class="text-muted" style="font-size: 0.75rem;">
                            <i class="fas fa-user me-1"></i>${item.userName} • <strong>${item.quantity} Adet</strong>
                        </small>
                    </div>
                    <div class="text-end">
                        <small class="text-muted fw-bold" style="font-size: 0.7rem;">${displayDate}</small>
                    </div>
                </div>`;
            });
        }
    } catch (e) {
        console.error("Aktivite Hatası:", e);
        container.innerHTML = '<small class="text-danger">Veri yüklenemedi.</small>';
    }
}

// --- GRAFİK VE TABLO YARDIMCILARI ---

function renderCriticalTable(items) {
    const tbody = document.getElementById('criticalStockTable');
    if (!tbody) return;
    tbody.innerHTML = '';

    if (items.length === 0) {
        tbody.innerHTML = '<tr><td colspan="5" class="text-center text-success py-3 small"><i class="fas fa-check-circle me-2"></i>Kritik stokta ürün yok.</td></tr>';
        return;
    }

    items.forEach(item => {
        const pName = item.productName || "Ürün";
        const row = `
            <tr>
                <td class="fw-bold text-dark small">${pName}</td>
                <td class="text-danger fw-bold small">${item.quantity}</td>
                <td class="text-muted small">${item.criticalStockQuantity}</td>
                <td><span class="badge bg-danger-subtle text-danger" style="font-size: 0.65rem;">KRİTİK</span></td>
                <td class="text-end">
                    <button class="btn btn-sm btn-light text-primary border-0" title="Sipariş Ver">
                        <i class="fas fa-shopping-cart"></i>
                    </button>
                </td>
            </tr>`;
        tbody.innerHTML += row;
    });
}

function renderExpirationList(items) {
    const list = document.getElementById('expirationList');
    if (!list) return;
    list.innerHTML = '';

    if (items.length === 0) {
        list.innerHTML = '<div class="text-center small text-muted py-3">Yaklaşan SKT yok.</div>';
        return;
    }

    items.forEach(item => {
        const expDate = new Date(item.expirationDate);
        const daysLeft = Math.ceil((expDate - new Date()) / (1000 * 60 * 60 * 24));
        const colorClass = daysLeft < 7 ? "text-danger" : "text-warning";

        list.innerHTML += `
            <div class="d-flex justify-content-between align-items-center mb-2 pb-2 border-bottom">
                <div>
                    <h6 class="mb-0 small fw-bold text-dark">${item.productName}</h6>
                    <small class="${colorClass} fw-bold" style="font-size: 0.7rem;">
                        <i class="fas fa-clock me-1"></i>${daysLeft} Gün Kaldı
                    </small>
                </div>
                <span class="badge bg-light text-secondary border">${expDate.toLocaleDateString('tr-TR')}</span>
            </div>`;
    });
}

function renderCategoryChart(products) {
    const ctx = document.getElementById('categoryChart');
    if (!ctx) return;

    const categories = {};
    products.forEach(p => {
        const cat = p.categoryName || "Diğer";
        categories[cat] = (categories[cat] || 0) + 1;
    });

    // Eski grafik varsa yok et (Hata önlemek için)
    if (window.myCategoryChart) window.myCategoryChart.destroy();

    window.myCategoryChart = new Chart(ctx, {
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
                legend: { position: 'bottom', labels: { usePointStyle: true, boxWidth: 8, font: { size: 10 } } }
            },
            cutout: '75%'
        }
    });
}

function renderMovementChart() {
    const ctx = document.getElementById('myChart');
    if (!ctx) return;

    // Eski grafik varsa yok et
    if (window.myLineChart) window.myLineChart.destroy();

    window.myLineChart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: ['Oca', 'Şub', 'Mar', 'Nis', 'May', 'Haz', 'Tem', 'Ağu', 'Eyl', 'Eki', 'Kas', 'Ara'],
            datasets: [{
                label: 'Giriş',
                data: [12, 19, 3, 5, 2, 3, 15, 10, 20, 15, 25, 30], // Örnek Veri
                borderColor: '#0d6efd',
                backgroundColor: 'rgba(13, 110, 253, 0.05)',
                borderWidth: 2,
                fill: true,
                tension: 0.4
            }, {
                label: 'Çıkış',
                data: [5, 10, 15, 10, 20, 15, 10, 5, 15, 10, 20, 25], // Örnek Veri
                borderColor: '#dc3545',
                backgroundColor: 'rgba(220, 53, 69, 0.05)',
                borderWidth: 2,
                fill: true,
                tension: 0.4
            }]
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

// --- ÇIKIŞ YAP ---
function logout() {
    localStorage.clear();
    window.location.href = 'login.html';
}