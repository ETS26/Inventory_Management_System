(function () {
    'use strict';

    const token = localStorage.getItem('jwtToken');

    // --- State Management ---
    let allInventories = [];
    let allProducts = [];
    let allMovements = [];
    let allMoveTypes = [];
    let allSuppliers = [];

    // --- Helper Functions ---
    function debounce(func, delay = 300) {
        let timeout;
        return (...args) => {
            clearTimeout(timeout);
            timeout = setTimeout(() => {
                func.apply(this, args);
            }, delay);
        };
    }

    // 1. AUTH GUARD & BAŞLATMA
    document.addEventListener('DOMContentLoaded', function () {
        if (!token) {
            window.location.href = 'login.html';
            return;
        }

        const toggleButton = document.getElementById("menu-toggle");
        if (toggleButton) {
            toggleButton.addEventListener('click', () => {
                document.getElementById("wrapper").classList.toggle("toggled");
            });
        }

        if (document.getElementById('movementsContainer')) {
            console.log("🚀 Stok Hareketleri Sayfası Başlatılıyor...");
            loadStockMovements();
            loadStockDropdowns();
            setupSearch();

            // Filtre Modalı Açıldığında Seçenekleri Doldur
            const filterModal = document.getElementById('filterMovementModal');
            if (filterModal) {
                // Modal her açıldığında dropdownları güncelle (yeni veri gelmiş olabilir)
                filterModal.addEventListener('show.bs.modal', loadFilterOptions);
            }
        }
    });

    // ==========================================
    // 2. ARAMA FONKSİYONLARI (Search Bar)
    // ==========================================
    function setupSearch() {
        const searchInput = document.getElementById('searchInput');
        if (!searchInput) return;

        searchInput.addEventListener('keyup', debounce(() => {
            filterMovements(searchInput.value);
        }, 300));
    }

    function filterMovements(term) {
        const lowerCaseTerm = term.trim().toLowerCase();

        if (!lowerCaseTerm) {
            renderMovements(allMovements, true);
            return;
        }

        const scoredData = allMovements.map(item => {
            let score = 0;
            const fields = [
                item.productName,
                item.moveTypeName,
                item.batchNumber,
                item.userName,
                item.id
            ];

            for (const field of fields) {
                if (!field) continue;
                const lowerCaseField = field.toLowerCase();
                if (lowerCaseField.startsWith(lowerCaseTerm)) score += 3;
                else if (lowerCaseField.includes(lowerCaseTerm)) score += 1;
            }
            return { item, score };
        })
            .filter(x => x.score > 0)
            .sort((a, b) => b.score - a.score);

        const filteredItems = scoredData.map(x => x.item);
        renderMovements(filteredItems, false);
    }

    // ==========================================
    // 3. VERİ YÜKLEME
    // ==========================================
    async function loadStockMovements() {
        const container = document.getElementById('movementsContainer');
        if (!container) return;

        container.innerHTML = `<div class="col-12 text-center py-5"><div class="spinner-border text-primary"></div></div>`;

        try {
            const response = await fetch('/api/StockMovements?IsActive=true', {
                headers: { 'Authorization': `Bearer ${token}` }
            });

            if (!response.ok) throw new Error(`HTTP ${response.status}`);

            const data = await response.json();
            allMovements = data;
            console.log("📦 Veri Yüklendi:", allMovements);

            renderMovements(allMovements, true);

        } catch (error) {
            console.error("❌ Yükleme Hatası:", error);
            container.innerHTML = `<div class="col-12 text-center text-danger py-4">Veriler yüklenemedi.</div>`;
        }
    }

    // ==========================================
    // 4. ARAYÜZ RENDER FONKSİYONLARI
    // ==========================================
    function renderMovements(data, isInitialLoad) {
        const container = document.getElementById('movementsContainer');
        container.innerHTML = '';

        if (!data || data.length === 0) {
            const message = isInitialLoad
                ? 'Henüz Stok Hareketi Yok'
                : 'Arama/Filtre sonucuyla eşleşen kayıt bulunamadı.';

            container.innerHTML = `
                <div class="col-12 text-center py-5">
                    <i class="fas fa-box-open fs-1 text-muted mb-3 d-block"></i>
                    <h5 class="text-muted">${message}</h5>
                </div>`;
            return;
        }

        data.forEach(movement => {
            const card = createMovementCard(movement);
            container.innerHTML += card;
        });
    }

    function createMovementCard(m) {
        const dateObj = new Date(m.createdAt);
        const date = dateObj.toLocaleDateString('tr-TR', { day: 'numeric', month: 'long', year: 'numeric' });
        const time = dateObj.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' });
        const payment = (m.payment || 0).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
        const movementJson = JSON.stringify(m).replace(/"/g, '&quot;');

        let expInfo = `<span class="text-muted small">-</span>`;
        if (m.expirationDate) {
            const expDate = new Date(m.expirationDate);
            const isExpired = expDate < new Date();
            expInfo = `<span class="${isExpired ? 'text-danger fw-bold' : 'text-dark'} small"><i class="far ${isExpired ? 'fa-exclamation-circle' : 'fa-calendar-alt'} me-1"></i>${expDate.toLocaleDateString('tr-TR')}</span>`;
        }

        const batchNo = m.batchNumber ? `<span class="font-monospace fw-bold text-dark small">${m.batchNumber}</span>` : `<span class="text-muted small">Yok</span>`;
        const unitType = m.unitTypeName || '-';
        const moveName = (m.moveTypeName || "").toLowerCase();
        const isIncome = moveName.includes('income') || moveName.includes('giriş') || moveName.includes('in') || moveName.includes('stock in');
        const productName = m.productName || 'Bilinmeyen Ürün';
        const userName = m.userName || 'Sistem';
        const supplierName = m.supplierName || '-';

        return `
        <div class="col-md-6 col-lg-4">
            <div class="card movement-card shadow-sm h-100 ${isIncome ? 'border-left-success' : 'border-left-danger'} p-3">
                <div class="d-flex justify-content-between align-items-start mb-2">
                    <div class="d-flex align-items-center overflow-hidden">
                        <div class="icon-box-sm ${isIncome ? 'bg-success-subtle text-success' : 'bg-danger-subtle text-danger'} me-3 flex-shrink-0">
                            <i class="fas ${isIncome ? 'fa-arrow-down' : 'fa-arrow-up'}"></i>
                        </div>
                        <div class="text-truncate">
                            <h6 class="fw-bold mb-0 text-dark text-truncate" title="${productName}">${productName}</h6>
                            <span class="badge bg-light text-secondary border mt-1 custom-badge">${m.moveTypeName || '-'}</span>
                        </div>
                    </div>
                    <div class="text-end flex-shrink-0">
                        <h4 class="fw-bold mb-0 ${isIncome ? 'text-success' : 'text-danger'}">${isIncome ? '+' : '-'}${m.quantity}</h4>
                        <div class="mt-1 px-2 py-1 bg-light rounded border"><span class="fw-bold text-dark small">₺${payment}</span></div>
                    </div>
                </div>
                <div class="bg-light rounded p-2 mb-3 mt-2 border">
                    <div class="d-flex justify-content-between align-items-center mb-1"><small class="text-muted">ID:</small><span class="fw-bold text-dark small">${m.id}</span></div>
                    <div class="d-flex justify-content-between align-items-center mb-1"><small class="text-muted">Birim:</small><span class="fw-bold text-dark small">${unitType}</span></div>
                    <div class="d-flex justify-content-between align-items-center mb-1"><small class="text-muted">Seri No:</small>${batchNo}</div>
                    <div class="d-flex justify-content-between align-items-center mb-1"><small class="text-muted">SKT:</small>${expInfo}</div>
                    <div class="d-flex justify-content-between align-items-center"><small class="text-muted">Tedarikçi:</small><span class="fw-bold text-dark small">${supplierName}</span></div>
                    ${m.description ? `<div class="mt-2 border-top pt-1 small text-muted fst-italic"><i class="fas fa-info-circle me-1"></i>${m.description}</div>` : ''}
                </div>
                <div class="d-flex justify-content-between align-items-end mt-auto pt-2 border-top">
                    <div class="d-flex align-items-center">
                        <div class="bg-primary text-white rounded-circle d-flex align-items-center justify-content-center me-2" style="width: 24px; height: 24px; font-size: 0.7rem;">${userName.charAt(0).toUpperCase()}</div>
                        <span class="fw-bold small text-dark">${userName}</span>
                    </div>
                    <div class="d-flex align-items-center">
                        <div class="text-end" style="line-height: 1.1;"><small class="text-muted d-block" style="font-size: 0.7rem;">${date}</small><small class="fw-bold text-secondary small">${time}</small></div>
                        <div class="btn-group ms-2">
                            <button class="btn btn-sm btn-light text-primary py-1" title="Düzenle" onclick='openUpdateMovementModal(${movementJson})'><i class="fas fa-pen"></i></button>
                            <button class="btn btn-sm btn-light text-danger py-1" title="Sil" onclick="deleteMovement('${m.id}')"><i class="fas fa-trash"></i></button>
                        </div>
                    </div>
                </div>
            </div>
        </div>`;
    }

    // ==========================================
    // 5. DROPDOWNLARI GÜVENLİ YÜKLE (Kayıt Modalı İçin)
    // ==========================================
    async function loadStockDropdowns() {
        const selects = {
            inventory: document.querySelectorAll('#inventorySelect, #updateInventorySelect'),
            moveType: document.querySelectorAll('#moveTypeSelect, #updateMoveTypeSelect'),
            supplier: document.querySelectorAll('#supplierSelect, #updateSupplierSelect')
        };

        if (!selects.inventory.length || !selects.moveType.length) return;

        try {
            const [invRes, prodRes, typeRes, supRes] = await Promise.all([
                fetch('/api/Inventories', { headers: { 'Authorization': `Bearer ${token}` } }),
                fetch('/api/Products?IsActive=true', { headers: { 'Authorization': `Bearer ${token}` } }),
                fetch('/api/MoveTypes', { headers: { 'Authorization': `Bearer ${token}` } }),
                fetch('/api/Suppliers', { headers: { 'Authorization': `Bearer ${token}` } })
            ]);

            if (invRes.ok) {
                allInventories = await invRes.json();
            }
            if (prodRes.ok) allProducts = await prodRes.json();
            
            if (typeRes.ok) {
                allMoveTypes = await typeRes.json();
                selects.moveType.forEach(select => {
                    select.innerHTML = '<option value="" selected disabled>Seçiniz...</option>';
                    allMoveTypes.forEach(t => select.innerHTML += `<option value="${t.id}">${t.moveType}</option>`);
                });
            }

            if (supRes.ok) {
                const suppliers = await supRes.json();
                allSuppliers = suppliers.filter(s => s.isActive !== false);
                selects.supplier.forEach(select => {
                    select.innerHTML = '<option value="">Tedarikçi Seçiniz</option>';
                    allSuppliers.forEach(s => select.innerHTML += `<option value="${s.id}">${s.supplierName}</option>`);
                });
            }
            
            fillDropdown(document.getElementById('inventorySelect'), allInventories, 'inventory');
            fillDropdown(document.getElementById('updateInventorySelect'), allInventories, 'inventory');

        } catch (error) {
            console.error("❌ Dropdown Yükleme Hatası:", error);
        }
    }

    // ==========================================
    // 6. FİLTRELEME İŞLEMLERİ (GÜNCELLENMİŞ)
    // ==========================================

    function loadFilterOptions() {
        const moveTypeSelect = document.getElementById('filterMoveType');
        const userSelect = document.getElementById('filterUser');
        const unitTypeSelect = document.getElementById('filterUnitType');

        if (!allMovements.length) return;

        if (moveTypeSelect) {
            moveTypeSelect.innerHTML = '<option value="">Tümü</option>';
            const uniqueTypes = [...new Set(allMovements.map(m => m.moveTypeName).filter(x => x))];
            uniqueTypes.sort().forEach(t => {
                const opt = document.createElement('option');
                opt.value = t;
                opt.text = t;
                moveTypeSelect.appendChild(opt);
            });
        }

        if (userSelect) {
            userSelect.innerHTML = '<option value="">Tümü</option>';
            const uniqueUsers = [...new Set(allMovements.map(m => m.userName).filter(x => x))];
            uniqueUsers.sort().forEach(u => {
                const opt = document.createElement('option');
                opt.value = u;
                opt.text = u;
                userSelect.appendChild(opt);
            });
        }

        if (unitTypeSelect) {
            unitTypeSelect.innerHTML = '<option value="">Tümü</option>';
            const uniqueUnits = [...new Set(allMovements.map(m => m.unitTypeName).filter(x => x))];
            uniqueUnits.sort().forEach(u => {
                const opt = document.createElement('option');
                opt.value = u;
                opt.text = u;
                unitTypeSelect.appendChild(opt);
            });
        }
    }

    window.applyMovementFilters = function () {
        const getVal = (id) => document.getElementById(id)?.value?.toLowerCase() || "";
        const getNum = (id) => document.getElementById(id)?.value || "";

        const filters = {
            moveType: getVal('filterMoveType'),
            user: getVal('filterUser'),
            unitType: getVal('filterUnitType'),
            qtyMin: getNum('filterQtyMin'),
            qtyMax: getNum('filterQtyMax'),
            payMin: getNum('filterPaymentMin'),
            payMax: getNum('filterPaymentMax'),
            dateStart: getNum('filterDateStart'),
            dateEnd: getNum('filterDateEnd'),
            expStart: getNum('filterExpDateStart'),
            expEnd: getNum('filterExpDateEnd'),
        };

        const filteredData = allMovements.filter(item => {
            if (filters.moveType && item.moveTypeName?.toLowerCase() !== filters.moveType) return false;
            if (filters.user && item.userName?.toLowerCase() !== filters.user) return false;
            if (filters.unitType && item.unitTypeName?.toLowerCase() !== filters.unitType) return false;
            if (filters.qtyMin && item.quantity < parseFloat(filters.qtyMin)) return false;
            if (filters.qtyMax && item.quantity > parseFloat(filters.qtyMax)) return false;
            if (filters.payMin && (item.payment || 0) < parseFloat(filters.payMin)) return false;
            if (filters.payMax && (item.payment || 0) > parseFloat(filters.payMax)) return false;

            if (filters.dateStart || filters.dateEnd) {
                const itemDate = new Date(item.createdAt).setHours(0, 0, 0, 0);
                if (filters.dateStart && itemDate < new Date(filters.dateStart).setHours(0, 0, 0, 0)) return false;
                if (filters.dateEnd && itemDate > new Date(filters.dateEnd).setHours(0, 0, 0, 0)) return false;
            }

            if (filters.expStart || filters.expEnd) {
                if (!item.expirationDate) return false;
                const itemExp = new Date(item.expirationDate).setHours(0, 0, 0, 0);
                if (filters.expStart && itemExp < new Date(filters.expStart).setHours(0, 0, 0, 0)) return false;
                if (filters.expEnd && itemExp > new Date(filters.expEnd).setHours(0, 0, 0, 0)) return false;
            }

            return true;
        });

        renderMovements(filteredData, false);
        const modalEl = document.getElementById('filterMovementModal');
        if (modalEl) bootstrap.Modal.getInstance(modalEl)?.hide();
    };

    window.clearMovementFilters = function () {
        document.getElementById('movementFilterForm').reset();
        renderMovements(allMovements, true);
    };

    // ==========================================
    // 7. GLOBAL YARDIMCILAR (KAYIT MODALI)
    // ==========================================
    function fillMoveTypeDropdown(selectElement, typesToDisplay) {
        if (!selectElement) return;
        selectElement.innerHTML = '<option value="" selected disabled>Seçiniz...</option>';
        typesToDisplay.forEach(t => {
            const opt = document.createElement('option');
            opt.value = t.id;
            opt.text = t.moveType;
            selectElement.appendChild(opt);
        });
    }

    function fillDropdown(selectElement, data, mode) {
        if (!selectElement) return;
        selectElement.innerHTML = '<option value="" selected disabled>Seçiniz...</option>';
        if (!data || data.length === 0) {
            selectElement.innerHTML += '<option disabled>Veri bulunamadı</option>';
            return;
        }
        data.forEach(item => {
            const opt = document.createElement('option');
            opt.value = item.id;
            if (mode === 'inventory') {
                const barcode = item.barcode ? ` [${item.barcode}]` : '';
                const quantities = item.quantity ? ` - Miktar: ${item.quantity}` : '';
                const batch = item.batchNumber ? ` - Seri: ${item.batchNumber}` : '';
                const unit = item.unitTypeName ? ` - ${item.unitTypeName}` : '';
                const exp = item.expirationDate && !item.expirationDate.startsWith('0001') 
                    ? ` - SKT: ${new Date(item.expirationDate).toLocaleDateString('tr-TR')}` 
                    : '';
                opt.text = `${item.productName || 'Bilinmeyen'}${barcode}${quantities}${unit}${batch}${exp}`;
            } else {
                const barcode = item.barcode ? ` [${item.barcode}]` : '';
                const category = item.categoryName ? ` -  ${item.categoryName}` : '';
                const unit = item.unitTypeName ? ` -  ${item.unitTypeName}` : '';
                opt.text = `${item.productName}${barcode}${category}${unit}`;
            }
            selectElement.appendChild(opt);
        });
    }

    window.selectMode = function (mode) {
        const cardExisting = document.getElementById('cardExisting');
        const cardNew = document.getElementById('cardNew');
        const checkBox = document.getElementById('newInventoryCheck');
        const detailsDiv = document.getElementById('newInventoryDetails');
        const label = document.getElementById('productLabel');
        const inventorySelect = document.getElementById('inventorySelect');
        const moveTypeSelect = document.getElementById('moveTypeSelect');

        if (!cardExisting || !cardNew) return;
        if (mode === 'existing') {
            cardExisting.classList.add('active');
            cardNew.classList.remove('active');
            if (checkBox) checkBox.checked = false;
            if (detailsDiv) detailsDiv.classList.add('d-none');
            if (label) label.innerText = "ÜRÜN (MEVCUT ENVANTER)";
            fillDropdown(inventorySelect, allInventories, 'inventory');
            fillMoveTypeDropdown(moveTypeSelect, allMoveTypes);
        } else {
            cardNew.classList.add('active');
            cardExisting.classList.remove('active');
            if (checkBox) checkBox.checked = true;
            if (detailsDiv) detailsDiv.classList.remove('d-none');
            if (label) label.innerText = "ÜRÜN KATALOĞUNDAN SEÇİN";
            fillDropdown(inventorySelect, allProducts, 'product');
            const stockInMoveTypes = allMoveTypes.filter(type =>
                type.moveType.toLowerCase().includes('giriş') ||
                type.moveType.toLowerCase().includes('in') ||
                type.moveType.toLowerCase().includes('ekleme')
            );
            fillMoveTypeDropdown(moveTypeSelect, stockInMoveTypes);
        }
        updateColor();
    };

    window.updateColor = function () {
        const select = document.getElementById('moveTypeSelect');
        const input = document.getElementById('quantityInput');
        if (!select || !input) return;
        const text = select.options[select.selectedIndex]?.text.toLowerCase() || "";

        if (text.includes('giriş') || text.includes('in')) {
            input.style.borderColor = '#198754'; input.style.color = '#198754';
        } else if (text.includes('çıkış') || text.includes('out')) {
            input.style.borderColor = '#dc3545'; input.style.color = '#dc3545';
        } else {
            input.style.borderColor = '#e0e0e0'; input.style.color = '#333';
        }
    };

    window.saveStockMovement = async function () {
        const form = {
            inventorySelect: document.getElementById('inventorySelect'),
            moveTypeSelect: document.getElementById('moveTypeSelect'),
            supplierSelect: document.getElementById('supplierSelect'),
            quantityInput: document.getElementById('quantityInput'),
            descriptionInput: document.getElementById('descriptionInput'),
            isNewInventory: document.getElementById('newInventoryCheck')?.checked || false
        };
        const saveButton = document.querySelector('#addMovementModal .btn-primary');
        const userId = localStorage.getItem('userId');

        if (!form.inventorySelect.value || !form.moveTypeSelect.value || !form.quantityInput.value || !form.supplierSelect.value || parseInt(form.quantityInput.value) <= 0) {
            alert("⚠️ Lütfen zorunlu alanları doldurun ve geçerli bir miktar girin.");
            return;
        }
        if (!userId) {
            alert("❌ Kullanıcı oturumu bulunamadı. Lütfen tekrar giriş yapın.");
            return;
        }

        let payload = {
            moveTypeId: form.moveTypeSelect.value,
            quantity: parseInt(form.quantityInput.value),
            description: form.descriptionInput?.value || '',
            userId: userId,
            supplierId: form.supplierSelect.value,
            isNewInventory: form.isNewInventory,
        };

        if (form.isNewInventory) {
            Object.assign(payload, {
                productId: form.inventorySelect.value,
                inventoryId: null,
                purchasePrice: parseFloat(document.getElementById('purchasePrice')?.value) || 0,
                salePrice: parseFloat(document.getElementById('salePrice')?.value) || 0,
                criticalStockQuantity: parseInt(document.getElementById('criticalStock')?.value) || 10,
                batchNumber: document.getElementById('batchNumber')?.value || "",
                expirationDate: document.getElementById('expirationDate')?.value || null
            });
        } else {
            payload.inventoryId = form.inventorySelect.value;
            payload.productId = null;
        }

        const originalButtonText = saveButton.innerHTML;
        saveButton.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>Kaydediliyor...';
        saveButton.disabled = true;

        try {
            const response = await fetch('/api/StockMovements', {
                method: 'POST',
                headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });

            let result;
            try { result = await response.json(); } catch { result = { message: await response.text() }; }

            if (!response.ok) {
                let errorMessage = result.message || result.error || result.detail || "İşlem sırasında bir hata oluştu.";
                if (errorMessage.includes("Yetersiz Stok")) {
                    errorMessage = "⚠️ Yetersiz Stok! Çıkış yapmak istediğiniz miktar, mevcut stoktan fazla.";
                }
                throw new Error(errorMessage);
            }

            alert("✅ Stok hareketi başarıyla kaydedildi!");
            bootstrap.Modal.getInstance(document.getElementById('addMovementModal'))?.hide();
            document.getElementById('movementForm').reset();
            await loadStockMovements();
            await loadStockDropdowns(); 

        } catch (error) {
            console.error("❌ Kaydetme Hatası:", error);
            alert(error.message);
        } finally {
            saveButton.innerHTML = originalButtonText;
            saveButton.disabled = false;
        }
    };

    window.openUpdateMovementModal = function (movement) {
        console.log('Güncelleme modalı için gelen hareket verisi:', movement);

        // ID ve basit inputları doğrudan ayarla
        document.getElementById('updateMovementId').value = movement.id;
        document.getElementById('updateQuantityInput').value = movement.quantity;
        document.getElementById('updateDescriptionInput').value = movement.description || '';

        // --- SEÇİM LİSTELERİNİ DOĞRUDAN GÜNCELLE (Yeniden Doldurma!) ---
        
        // Envanter/Ürün
        const inventorySelect = document.getElementById('updateInventorySelect');
        inventorySelect.value = movement.inventoryId;
        if (!inventorySelect.value) {
            // Eğer değer atanamadıysa, listeyi yenileyip tekrar deneyebiliriz (güvenlik önlemi)
            fillDropdown(inventorySelect, allInventories, 'inventory');
            inventorySelect.value = movement.inventoryId;
            console.warn('Envanter seçimi ilk denemede başarısız oldu, liste yenilendi. ID:', movement.inventoryId);
        }

        // Hareket Tipi
        const moveTypeSelect = document.getElementById('updateMoveTypeSelect');
        moveTypeSelect.value = movement.moveTypeId;
        if (!moveTypeSelect.value) {
            console.warn('Hareket tipi seçimi yapılamadı. ID:', movement.moveTypeId);
        }

        // Tedarikçi
        const supplierSelect = document.getElementById('updateSupplierSelect');
        supplierSelect.value = movement.supplierId;
        if (!supplierSelect.value) {
            console.warn('Tedarikçi seçimi yapılamadı. ID:', movement.supplierId);
        }
        
        // Ayarlanan değerleri kontrol et
        console.log(`Modaldaki Değerler -> Miktar: ${document.getElementById('updateQuantityInput').value}, Envanter: ${inventorySelect.value}, Tip: ${moveTypeSelect.value}, Tedarikçi: ${supplierSelect.value}`);

        // Modal'ı göster
        const updateModal = new bootstrap.Modal(document.getElementById('updateMovementModal'));
        updateModal.show();
    };

    window.updateStockMovement = async function() {
        const movementId = document.getElementById('updateMovementId').value;
        const saveButton = document.querySelector('#updateMovementModal .btn-primary');
        const userId = localStorage.getItem('userId');

        const payload = {
            id: movementId,
            inventoryId: document.getElementById('updateInventorySelect').value,
            quantity: parseInt(document.getElementById('updateQuantityInput').value),
            moveTypeId: document.getElementById('updateMoveTypeSelect').value,
            supplierId: document.getElementById('updateSupplierSelect').value,
            description: document.getElementById('updateDescriptionInput').value,
            userId: userId
        };

        if (!payload.inventoryId || !payload.moveTypeId || !payload.quantity || payload.quantity <= 0 || !payload.supplierId) {
            alert("⚠️ Lütfen tüm zorunlu alanları doldurun ve geçerli bir miktar girin.");
            return;
        }

        const originalButtonText = saveButton.innerHTML;
        saveButton.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>Güncelleniyor...';
        saveButton.disabled = true;

        try {
            const response = await fetch(`/api/StockMovements/${movementId}`, {
                method: 'PUT',
                headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });

            if (!response.ok) {
                const errorData = await response.json().catch(() => ({ message: 'Sunucu hatası veya geçersiz yanıt.' }));
                throw new Error(errorData.message || 'Güncelleme işlemi başarısız oldu.');
            }

            alert("✅ Stok hareketi başarıyla güncellendi!");
            bootstrap.Modal.getInstance(document.getElementById('updateMovementModal'))?.hide();
            await loadStockMovements();
            await loadStockDropdowns(); 

        } catch (error) {
            console.error("❌ Güncelleme Hatası:", error);
            alert('Hata: ' + error.message);
        } finally {
            saveButton.innerHTML = originalButtonText;
            saveButton.disabled = false;
        }
    };

    window.deleteMovement = async function(movementId) {
        if (!confirm('Bu stok hareketini silmek istediğinizden emin misiniz? Bu işlem ilgili envanterin stoğunu güncelleyecektir.')) {
            return;
        }

        try {
            const res = await fetch(`/api/StockMovements/${movementId}`, {
                method: 'DELETE',
                headers: { 'Authorization': `Bearer ${token}` }
            });

            if (res.ok) {
                alert('✅ Stok hareketi başarıyla silindi (pasife alındı).');
                await loadStockMovements(); 
                await loadStockDropdowns(); 
            } else {
                const responseText = await res.text();
                let errorMessage = "İşlem başarısız.";

                try {
                    const err = JSON.parse(responseText);
                    errorMessage = err.message || err.error || err.detail || err.title || errorMessage;
                } catch (e) {
                     errorMessage = responseText || `Sunucu Hatası (${res.status})`;
                }
                alert('❌ Hata: ' + errorMessage);
            }
        } catch (e) {
            console.error('❌ Silme hatası:', e);
            alert('Sunucu hatası veya bağlantı problemi.');
        }
    };
})();