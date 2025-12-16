(function () {
    'use strict';

    const token = localStorage.getItem('jwtToken');

    // Dropdown verilerini hafızada tutmak için
    let allInventories = [];
    let allProducts = [];

    // 1. AUTH GUARD & BAŞLATMA
    document.addEventListener('DOMContentLoaded', function () {
        if (!token) {
            window.location.href = 'login.html';
            return;
        }

        // Menü Toggle
        const toggleButton = document.getElementById("menu-toggle");
        if (toggleButton) {
            toggleButton.addEventListener('click', () => {
                document.getElementById("wrapper").classList.toggle("toggled");
            });
        }

        // Sadece bu sayfada çalışacak fonksiyonları başlat
        if (document.getElementById('movementsContainer')) {
            console.log("🚀 Stok Hareketleri Sayfası Başlatılıyor...");
            loadStockMovements();
            loadStockDropdowns();
        }
    });

    // ==========================================
    // 2. STOK HAREKETLERİNİ YÜKLE
    // ==========================================
    async function loadStockMovements() {
        const container = document.getElementById('movementsContainer');
        if (!container) return;

        // Yükleniyor göstergesi
        container.innerHTML = `
            <div class="col-12 text-center py-5">
                <div class="spinner-border text-primary mb-3" role="status">
                    <span class="visually-hidden">Yükleniyor...</span>
                </div>
                <p class="text-muted">Stok hareketleri yükleniyor...</p>
            </div>
        `;

        try {
            const response = await fetch('/api/StockMovements?IsActive=true', {
                method: 'GET',
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json'
                }
            });

            if (!response.ok) {
                const errorText = await response.text();
                throw new Error(`HTTP ${response.status}: ${errorText || 'Sunucu hatası'}`);
            }

            const data = await response.json();
            console.log("📦 Backend'den Gelen Veri:", data);

            // Container'ı temizle
            container.innerHTML = '';

            // Veri yoksa
            if (!data || data.length === 0) {
                container.innerHTML = `
                    <div class="col-12 text-center py-5">
                        <i class="fas fa-box-open fs-1 text-muted mb-3 d-block"></i>
                        <h5 class="text-muted">Henüz Stok Hareketi Yok</h5>
                        <p class="text-muted small">İlk kaydınızı eklemek için "Hareket Ekle" butonuna tıklayın.</p>
                    </div>
                `;
                return;
            }

            // Verileri işle ve kartları oluştur
            data.forEach(movement => {
                const card = createMovementCard(movement);
                container.innerHTML += card;
            });

            console.log(`✅ ${data.length} adet hareket başarıyla yüklendi.`);

        } catch (error) {
            console.error("❌ Stok Hareketleri Yükleme Hatası:", error);

            container.innerHTML = `
                <div class="col-12 text-center py-5">
                    <i class="fas fa-exclamation-triangle fs-1 text-danger mb-3 d-block"></i>
                    <h5 class="text-danger">Veriler Yüklenemedi</h5>
                    <p class="text-muted">${error.message}</p>
                    <button class="btn btn-primary mt-3" onclick="location.reload()">
                        <i class="fas fa-redo me-2"></i>Tekrar Dene
                    </button>
                </div>
            `;
        }
    }

    // ==========================================
    // 3. HAREKET KARTINI OLUŞTUR (GÜNCELLENDİ: Birim Türü Eklendi)
    // ==========================================
    function createMovementCard(m) {
        // --- Tarih Formatı ---
        const dateObj = new Date(m.createdAt);
        const date = dateObj.toLocaleDateString('tr-TR', { day: 'numeric', month: 'long', year: 'numeric', timeZone: 'Europe/Istanbul' });
        const time = dateObj.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit', timeZone: 'Europe/Istanbul' });

        // --- Tutar (Payment) Formatlama ---
        const payment = m.payment || 0;
        const formattedPayment = payment.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

        // --- SKT ve Seri No İşlemleri ---
        let expInfo = `<span class="text-muted small">-</span>`;
        if (m.expirationDate) {
            const expDate = new Date(m.expirationDate);
            const isExpired = expDate < new Date();
            const expColor = isExpired ? "text-danger fw-bold" : "text-dark";
            const expIcon = isExpired ? "fa-exclamation-circle" : "fa-calendar-alt";
            expInfo = `<span class="${expColor} small"><i class="far ${expIcon} me-1"></i>${expDate.toLocaleDateString('tr-TR')}</span>`;
        }

        const batchNo = m.batchNumber ? `<span class="font-monospace fw-bold text-dark small">${m.batchNumber}</span>` : `<span class="text-muted small">Yok</span>`;

        // --- BİRİM TÜRÜ BİLGİSİ (YENİ) ---
        // Backend'den 'unitTypeName' olarak gelmesini bekliyoruz. Gelmezse '-' yazar.
        const unitType = m.unitTypeName || '-';

        // --- Renk ve İkon Mantığı ---
        const moveName = (m.moveTypeName || "").toLowerCase();
        const isIncome = moveName.includes('income') || moveName.includes('giriş') || moveName.includes('in') || moveName.includes('stock in');

        const borderClass = isIncome ? 'border-left-success' : 'border-left-danger';
        const iconBg = isIncome ? 'bg-success-subtle text-success' : 'bg-danger-subtle text-danger';
        const icon = isIncome ? 'fa-arrow-down' : 'fa-arrow-up';
        const amountColor = isIncome ? 'text-success' : 'text-danger';
        const amountPrefix = isIncome ? '+' : '-';

        const productName = m.productName || 'Bilinmeyen Ürün';
        const userName = m.userName || 'Sistem';
        const userInitial = userName.charAt(0).toUpperCase();

        return `
        <div class="col-md-6 col-lg-4">
            <div class="card movement-card shadow-sm h-100 ${borderClass} p-3">
                
                <div class="d-flex justify-content-between align-items-start mb-2">
                    <div class="d-flex align-items-center overflow-hidden">
                        <div class="icon-box-sm ${iconBg} me-3 flex-shrink-0">
                            <i class="fas ${icon}"></i>
                        </div>
                        <div class="text-truncate">
                            <h6 class="fw-bold mb-0 text-dark text-truncate" title="${productName}">${productName}</h6>
                            <span class="badge bg-light text-secondary border mt-1 custom-badge">
                                ${m.moveTypeName || '-'}
                            </span>
                        </div>
                    </div>
                    <div class="text-end ms-2 flex-shrink-0">
                        <h4 class="fw-bold mb-0 ${amountColor}">${amountPrefix}${m.quantity}</h4>
                        
                        <div class="mt-1 px-2 py-1 bg-light rounded border border-light-subtle">
                            <span class="fw-bold text-dark small">₺${formattedPayment}</span>
                        </div>
                    </div>
                </div>

                <div class="bg-light rounded p-2 mb-3 mt-2 border border-1">
                    <div class="d-flex justify-content-between align-items-center mb-1">
                        <small class="text-muted">Birim:</small>
                        <span class="fw-bold text-dark small">${unitType}</span>
                    </div>
                    
                    <div class="d-flex justify-content-between align-items-center mb-1">
                        <small class="text-muted">Seri No:</small>
                        ${batchNo}
                    </div>
                    <div class="d-flex justify-content-between align-items-center">
                        <small class="text-muted">SKT:</small>
                        ${expInfo}
                    </div>
                    ${m.description ? `<div class="mt-2 border-top pt-1 small text-muted fst-italic"><i class="fas fa-info-circle me-1"></i>${m.description}</div>` : ''}
                </div>
                
                <div class="d-flex justify-content-between align-items-end mt-auto pt-2 border-top">
                    <div class="d-flex align-items-center">
                        <div class="bg-primary text-white rounded-circle d-flex align-items-center justify-content-center me-2" style="width: 24px; height: 24px; font-size: 0.7rem;">
                            ${userInitial}
                        </div>
                        <span class="fw-bold small text-dark">${userName}</span>
                    </div>
                    <div class="text-end" style="line-height: 1.1;">
                        <small class="text-muted d-block" style="font-size: 0.7rem;">${date}</small>
                        <small class="fw-bold text-secondary small">${time}</small>
                    </div>
                </div>
            </div>
        </div>`;
    }

    // ==========================================
    // 4. DROPDOWNLARI GÜVENLİ YÜKLE
    // ==========================================
    async function loadStockDropdowns() {
        const inventorySelect = document.getElementById('inventorySelect');
        const moveTypeSelect = document.getElementById('moveTypeSelect');
        const supplierSelect = document.getElementById('supplierSelect');

        if (!inventorySelect || !moveTypeSelect) return;

        try {
            const [invRes, prodRes, typeRes, supRes] = await Promise.all([
                fetch('/api/Inventories', { headers: { 'Authorization': `Bearer ${token}` } }),
                fetch('/api/Products?IsActive=true', { headers: { 'Authorization': `Bearer ${token}` } }), // Filter for active products
                fetch('/api/MoveTypes', { headers: { 'Authorization': `Bearer ${token}` } }),
                fetch('/api/Suppliers', { headers: { 'Authorization': `Bearer ${token}` } })
            ]);

            if (invRes.ok) allInventories = await invRes.json();
            if (prodRes.ok) allProducts = await prodRes.json();

            fillDropdown(allInventories, 'inventory');

            if (typeRes.ok) {
                const types = await typeRes.json();
                moveTypeSelect.innerHTML = '<option value="" selected disabled>Seçiniz...</option>';
                types.forEach(t => {
                    const opt = document.createElement('option');
                    opt.value = t.id;
                    opt.text = t.moveType;
                    moveTypeSelect.appendChild(opt);
                });
            }

            if (supRes.ok && supplierSelect) {
                const suppliers = await supRes.json();
                supplierSelect.innerHTML = '<option value="">Tedarikçi Seçiniz</option>';
                suppliers.forEach(s => {
                    const opt = document.createElement('option');
                    opt.value = s.id;
                    opt.text = s.supplierName;
                    supplierSelect.appendChild(opt);
                });
            }

        } catch (error) {
            console.error("❌ Dropdown Yükleme Hatası:", error);
        }
    }

    function fillDropdown(data, mode) {
        const select = document.getElementById('inventorySelect');
        if (!select) return;
        select.innerHTML = '<option value="" selected disabled>Seçiniz...</option>';

        if (!data || data.length === 0) {
            const opt = document.createElement('option');
            opt.text = "Veri bulunamadı";
            opt.disabled = true;
            select.appendChild(opt);
            return;
        }

        data.forEach(item => {
            const opt = document.createElement('option');
            if (mode === 'inventory') {
                const pName = item.productName || (item.product ? item.product.productName : "Bilinmeyen Ürün");
                opt.value = item.id;
                opt.text = `${pName} (Mevcut: ${item.quantity})`;
            } else {
                const pName = item.productName;
                opt.value = item.id;
                opt.text = `${pName} ${item.barcode ? ' - ' + item.barcode : ''}`;
            }
            select.appendChild(opt);
        });
    }

    // ==========================================
    // 5. GLOBAL FONKSİYONLAR
    // ==========================================

    window.selectMode = function (mode) {
        const cardExisting = document.getElementById('cardExisting');
        const cardNew = document.getElementById('cardNew');
        const checkBox = document.getElementById('newInventoryCheck');
        const detailsDiv = document.getElementById('newInventoryDetails');
        const label = document.getElementById('productLabel');

        if (!cardExisting || !cardNew) return;

        if (mode === 'existing') {
            cardExisting.classList.add('active');
            cardNew.classList.remove('active');
            if (checkBox) checkBox.checked = false;
            if (detailsDiv) detailsDiv.classList.add('d-none');
            if (label) label.innerText = "ÜRÜN (MEVCUT ENVANTER)";
            fillDropdown(allInventories, 'inventory');
        } else {
            cardNew.classList.add('active');
            cardExisting.classList.remove('active');
            if (checkBox) checkBox.checked = true;
            if (detailsDiv) detailsDiv.classList.remove('d-none');
            if (label) label.innerText = "ÜRÜN KATALOĞUNDAN SEÇİN";
            fillDropdown(allProducts, 'product');
        }
    };

    window.updateColor = function () {
        const select = document.getElementById('moveTypeSelect');
        const input = document.getElementById('quantityInput');
        if (!select || !input) return;
        const text = select.options[select.selectedIndex].text.toLowerCase();
        if (text.includes('giriş') || text.includes('in')) {
            input.style.borderColor = '#198754'; input.style.color = '#198754';
        } else if (text.includes('çıkış') || text.includes('out')) {
            input.style.borderColor = '#dc3545'; input.style.color = '#dc3545';
        } else {
            input.style.borderColor = '#e0e0e0'; input.style.color = '#333';
        }
    };

    window.saveStockMovement = async function () {
        const inventorySelect = document.getElementById('inventorySelect');
        const moveTypeSelect = document.getElementById('moveTypeSelect');
        const supplierSelect = document.getElementById('supplierSelect');
        const quantityInput = document.getElementById('quantityInput');
        const descriptionInput = document.getElementById('descriptionInput');
        const saveButton = document.querySelector('.modal-footer .btn-primary');
        const isNewInventory = document.getElementById('newInventoryCheck')?.checked || false;

        const selectedId = inventorySelect.value;
        const moveTypeId = moveTypeSelect.value;
        const supplierId = supplierSelect.value;
        const quantity = parseInt(quantityInput.value);
        const description = descriptionInput?.value || '';
        const userId = localStorage.getItem('userId');

        if (!selectedId || !moveTypeId || !quantity || !supplierId || quantity <= 0) {
            alert("⚠️ Lütfen zorunlu alanları doldurun ve geçerli bir miktar girin.");
            return;
        }

        if (!userId) {
            alert("❌ Kullanıcı oturumu bulunamadı. Lütfen tekrar giriş yapın.");
            return;
        }

        let payload = {
            moveTypeId: moveTypeId,
            quantity: quantity,
            description: description,
            userId: userId,
            supplierId: supplierId,
            isNewInventory: isNewInventory,
        };

        if (isNewInventory) {
            payload.productId = selectedId;
            payload.inventoryId = null;
            payload.purchasePrice = parseFloat(document.getElementById('purchasePrice')?.value) || 0;
            payload.salePrice = parseFloat(document.getElementById('salePrice')?.value) || 0;
            payload.criticalStockQuantity = parseInt(document.getElementById('criticalStock')?.value) || 10;
            payload.batchNumber = document.getElementById('batchNumber')?.value || "";
            const expDate = document.getElementById('expirationDate')?.value;
            if (expDate) payload.expirationDate = expDate;
        } else {
            payload.inventoryId = selectedId;
            payload.productId = null;
        }

        const originalButtonText = saveButton.innerHTML;
        saveButton.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>Kaydediliyor...';
        saveButton.disabled = true;

        try {
            const response = await fetch('/api/StockMovements', {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(payload)
            });

            const result = await response.json();

            if (response.ok) {
                alert("✅ Stok hareketi başarıyla kaydedildi!");
                const modalEl = document.getElementById('addMovementModal');
                const modal = bootstrap.Modal.getInstance(modalEl);
                if (modal) modal.hide();
                document.getElementById('movementForm').reset();
                await loadStockMovements();
            } else {
                throw new Error(result.errorMessage || result.title || 'İşlem başarısız');
            }

        } catch (error) {
            console.error("❌ Kaydetme Hatası:", error);
            alert(`❌ Hata: ${error.message}`);
        } finally {
            saveButton.innerHTML = originalButtonText;
            saveButton.disabled = false;
        }
    };

})();