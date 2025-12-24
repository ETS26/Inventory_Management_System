(function () {
    const token = localStorage.getItem('jwtToken');
    let allProducts = [];
    let allCategories = []; // Global
    let allUnitTypes = []; // Global

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

    // --- Initial Setup ---
    document.addEventListener('DOMContentLoaded', function () {
        if (!token) {
            window.location.href = 'login.html';
            return;
        }

        console.log("🚀 Ürünler Sayfası Yüklendi.");
        loadProducts();
        loadProductDropdowns();
        setupSearch();
    });
    
    // --- Search Functionality ---
    function setupSearch() {
        const searchInput = document.getElementById('searchInput');
        if (!searchInput) return;

        searchInput.addEventListener('keyup', debounce(() => {
            filterAndRenderProducts(searchInput.value);
        }, 300));
    }

    function filterAndRenderProducts(term) {
        const lowerCaseTerm = term.trim().toLowerCase();

        if (!lowerCaseTerm) {
            renderProducts(allProducts, true); // Show all if search is empty
            return;
        }

        const scoredData = allProducts.map(item => {
            let score = 0;
            const fields = [
                item.productName,
                item.barcode,
                item.categoryName
            ];

            for (const field of fields) {
                if (!field) continue;
                const lowerCaseField = field.toLowerCase();

                if (lowerCaseField.startsWith(lowerCaseTerm)) {
                    score += 3;
                } else if (lowerCaseField.includes(lowerCaseTerm)) {
                    score += 1;
                }
            }
            return { item, score };
        })
        .filter(x => x.score > 0)
        .sort((a, b) => b.score - a.score);

        const filteredItems = scoredData.map(x => x.item);
        renderProducts(filteredItems, false); // Render search results
    }

    // --- Data Loading ---
    async function loadProducts() {
        const container = document.getElementById('productsContainer');
        if (!container) return;
        container.innerHTML = `<div class="col-12 text-center py-5"><div class="spinner-border text-primary"></div></div>`;

        try {
            const response = await fetch('/api/Products', {
                headers: { 'Authorization': `Bearer ${token}` }
            });
            if (!response.ok) throw new Error(`Hata: ${response.status}`);

            allProducts = await response.json();
            updateProductStats(allProducts);
            renderProducts(allProducts, true);

        } catch (error) {
            console.error("❌ Ürün yükleme hatası:", error);
            container.innerHTML = `<div class="col-12 text-center text-danger py-5">Veriler yüklenemedi.</div>`;
        }
    }

    // --- UI Rendering ---
    function renderProducts(data, isInitialLoad = false) {
        const container = document.getElementById('productsContainer');
        container.innerHTML = '';

        if (!data || data.length === 0) {
            const message = isInitialLoad ? "Henüz kayıtlı ürün yok." : "Arama sonucuyla eşleşen ürün bulunamadı.";
            container.innerHTML = `
                <div class="col-12 text-center py-5">
                    <i class="fas fa-box-open fs-1 text-muted mb-3"></i>
                    <p class="text-muted">${message}</p>
                </div>`;
            return;
        }

        data.forEach(p => {
            const pName = p.productName || "İsimsiz Ürün";
            const initial = pName.charAt(0).toUpperCase();
            const category = p.categoryName || "Genel";
            const barcode = p.barcode || "-";
            const productJson = JSON.stringify(p).replace(/"/g, '&quot;');
            const isInactive = p.isActive === false;

            // Check both casing possibilities for ImageURL
            const imgUrl = p.imageURL || p.imageUrl;

            let imageHtml;
            if (imgUrl && imgUrl.trim() !== "") {
                const rawUrl = imgUrl.trim();
                
                imageHtml = `
                <a href="${rawUrl}" target="_blank" rel="noopener noreferrer" class="rounded-3 me-3 overflow-hidden shadow-sm border d-block" 
                     style="width: 60px; height: 60px; flex-shrink: 0;"
                     title="Açılacak Link: ${rawUrl}">
                       <img src="${rawUrl}" alt="${pName}" style="width: 100%; height: 100%; object-fit: cover;" onerror="this.onerror=null; this.src='https://placehold.co/60x60?text=IMG';"> 
                   </a>`;
            } else {
                imageHtml = `<div class="rounded-3 bg-primary-light text-primary d-flex align-items-center justify-content-center me-3 shadow-sm" 
                        style="width: 60px; height: 60px; font-size: 1.5rem; font-weight: bold; flex-shrink: 0;">
                       ${initial}
                   </div>`;
            }

            const buttonsHtml = isInactive
                ? `<button class="btn btn-sm btn-light text-success rounded-circle me-1" title="Geri Yükle" onclick="restoreProduct('${p.id}')"><i class="fas fa-undo"></i></button>`
                : `<button class="btn btn-sm btn-light text-warning rounded-circle me-1" title="Sipariş Ver" onclick='openProductOrderModal(${productJson})'><i class="fas fa-shopping-cart"></i></button>
                   <button class="btn btn-sm btn-light text-primary rounded-circle me-1" title="Düzenle" onclick='openUpdateModal(${productJson})'><i class="fas fa-pen"></i></button>
                   <button class="btn btn-sm btn-light text-danger rounded-circle" title="Sil" onclick="deleteProduct('${p.id}')"><i class="fas fa-trash"></i></button>`;

            container.innerHTML += `
                <div class="col-md-6 col-lg-4">
                    <div class="card border-0 shadow-sm h-100 p-3 card-hover transition ${isInactive ? 'product-inactive' : ''}">
                        <div class="d-flex align-items-center mb-3">
                            ${imageHtml}
                            <div class="flex-grow-1 overflow-hidden">
                                <h6 class="fw-bold text-dark mb-0 text-truncate" title="${pName}">${pName}</h6>
                                <small class="text-muted d-block mt-1 text-truncate"><i class="fas fa-barcode me-1"></i>${barcode}</small>
                                <small class="text-secondary d-block mt-1" style="font-size: 0.75rem;">${p.categoryName || '-'} • ${p.unitTypeName || '-'}</small>
                            </div>
                            <div class="ms-2"><span class="badge bg-light text-secondary border">${category}</span></div>
                        </div>
                        <div class="mt-auto pt-3 border-top d-flex justify-content-between align-items-center">
                            <small class="text-muted small text-truncate" style="max-width: 60%;" title="${p.description || ''}"><i class="fas fa-info-circle me-1"></i>${p.description || 'Açıklama mevcut değil.'}</small>
                            <div class="btn-group">${buttonsHtml}</div>
                        </div>
                    </div>
                </div>`;
        });
    }

    async function loadProductDropdowns() {
        try {
            const [catRes, unitRes] = await Promise.all([
                fetch('/api/Categories', { headers: { 'Authorization': `Bearer ${token}` } }),
                fetch('/api/UnitTypes', { headers: { 'Authorization': `Bearer ${token}` } })
            ]);

            if (catRes.ok) {
                allCategories = await catRes.json();
                document.querySelectorAll('#categorySelect, #updateCategorySelect, #filterCategory').forEach(select => {
                    const isFilter = select.id.startsWith('filter');
                    select.innerHTML = isFilter ? '<option value="">Tümü</option>' : '<option value="" selected disabled>Seçiniz...</option>';
                    allCategories.forEach(c => {
                        if (isFilter || c.isActive !== false) {
                            select.innerHTML += `<option value="${c.id}">${c.categoryName}</option>`;
                        }
                    });
                });
                const countEl = document.getElementById('totalCategoriesCount');
                if (countEl) countEl.innerText = allCategories.filter(c => c.isActive !== false).length;
            }

            if (unitRes.ok) {
                allUnitTypes = await unitRes.json();
                document.querySelectorAll('#unitTypeSelect, #updateUnitTypeSelect, #filterUnitType').forEach(select => {
                    const isFilter = select.id.startsWith('filter');
                    select.innerHTML = isFilter ? '<option value="">Tümü</option>' : '<option value="" selected disabled>Seçiniz...</option>';
                    allUnitTypes.forEach(u => {
                        if (isFilter || u.isActive !== false) { // Sadece aktifleri dropdowna ekle (opsiyonel ama mantıklı)
                            select.innerHTML += `<option value="${u.id}">${u.unitName}</option>`;
                        }
                    });
                });
                const unitCountEl = document.getElementById('totalUnitTypesCount');
                if (unitCountEl) unitCountEl.innerText = allUnitTypes.filter(u => u.isActive !== false).length;
            }
        } catch (e) { console.error("Dropdown hatası:", e); }
    }

    // --- Filter Functions ---
    window.applyProductFilters = function() {
        const categoryId = document.getElementById('filterCategory').value;
        const unitTypeId = document.getElementById('filterUnitType').value;
        const status = document.getElementById('filterStatus').value;

        const filtered = allProducts.filter(p => {
            // Category Filter
            if (categoryId && p.categoryId !== categoryId) return false;
            // Unit Type Filter
            if (unitTypeId && p.unitTypeId !== unitTypeId) return false;
            // Status Filter
            if (status !== "") {
                const isActive = status === "true";
                if (p.isActive !== isActive) return false;
            }
            return true;
        });

        // Close Modal
        const modalEl = document.getElementById('filterProductModal');
        const modal = bootstrap.Modal.getInstance(modalEl);
        if (modal) modal.hide();

        renderProducts(filtered, false);
    };

    window.clearProductFilters = function() {
        document.getElementById('filterForm').reset();
        renderProducts(allProducts, true);
        
        // Close Modal
        const modalEl = document.getElementById('filterProductModal');
        const modal = bootstrap.Modal.getInstance(modalEl);
        if (modal) modal.hide();
    };

    function updateProductStats(data) {
        const totalEl = document.getElementById('totalProductsCount');
        const activeEl = document.getElementById('activeProductsCount');
        if (totalEl) totalEl.innerText = data.length;
        if (activeEl) activeEl.innerText = data.filter(x => x.isActive !== false).length;
    }

    // --- CRUD and other window functions ---
    
    // (The rest of the functions: saveProduct, openUpdateModal, updateProduct, deleteProduct, restoreProduct, saveCategory remain unchanged)
    
    window.saveProduct = async function () {
        const name = document.getElementById('productNameInput').value;
        const barcode = document.getElementById('barcodeInput').value;
        const categoryId = document.getElementById('categorySelect').value;
        const unitTypeId = document.getElementById('unitTypeSelect').value;
        const description = document.getElementById('descriptionInput').value;
        const imageUrl = document.getElementById('imageUrlInput').value;

        const saveButton = document.querySelector('#addProductModal .btn-primary');

        if (!name || !barcode || !categoryId || !unitTypeId) {
            alert("⚠️ Lütfen zorunlu alanları doldurun.");
            return;
        }

        const payload = {
            productName: name,
            barcode: barcode,
            categoryId: categoryId,
            unitTypeId: unitTypeId,
            description: description,
            imageUrl: imageUrl,
            isActive: true
        };

        const originalText = saveButton.innerHTML;
        saveButton.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Kaydediliyor...';
        saveButton.disabled = true;

        try {
            const res = await fetch('/api/Products', {
                method: 'POST',
                headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });

            if (res.ok) {
                alert("✅ Ürün başarıyla tanımlandı!");
                location.reload();
            } else {
                const err = await res.json();
                alert("Hata: " + (err.message || "Kaydedilemedi."));
            }
        } catch (e) {
            console.error(e);
            alert("Sunucu hatası.");
        } finally {
            saveButton.innerHTML = originalText;
            saveButton.disabled = false;
        }
    };
    window.openUpdateModal = function (product) {
        document.getElementById('updateProductId').value = product.id;
        document.getElementById('updateProductNameInput').value = product.productName;
        document.getElementById('updateBarcodeInput').value = product.barcode;
        document.getElementById('updateCategorySelect').value = product.categoryId;
        document.getElementById('updateUnitTypeSelect').value = product.unitTypeId;
        document.getElementById('updateDescriptionInput').value = product.description;
        document.getElementById('updateImageUrlInput').value = product.imageUrl;
        const updateModal = new bootstrap.Modal(document.getElementById('updateProductModal'));
        updateModal.show();
    }

    window.updateProduct = async function () {
        const productId = document.getElementById('updateProductId').value;
        const name = document.getElementById('updateProductNameInput').value;
        const barcode = document.getElementById('updateBarcodeInput').value;
        const categoryId = document.getElementById('updateCategorySelect').value;
        const unitTypeId = document.getElementById('updateUnitTypeSelect').value;
        const description = document.getElementById('updateDescriptionInput').value;
        const imageUrl = document.getElementById('updateImageUrlInput').value;
    
        const updateButton = document.querySelector('#updateProductModal .btn-primary');
    
        if (!name || !barcode || !categoryId || !unitTypeId) {
            alert("⚠️ Lütfen zorunlu alanları doldurun.");
            return;
        }
    
        const payload = {
            id: productId,
            productName: name,
            barcode: barcode,
            categoryId: categoryId,
            unitTypeId: unitTypeId,
            description: description,
            imageURL: imageUrl,
            isActive: true 
        };
    
        const originalText = updateButton.innerHTML;
        updateButton.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Güncelleniyor...';
        updateButton.disabled = true;
    
        try {
            const res = await fetch(`/api/Products/${productId}`, {
                method: 'PUT',
                headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });
    
            if (res.ok) {
                alert("✅ Ürün başarıyla güncellendi!");
                location.reload();
            } else {

                const errorBody = await res.text();
                console.error('Sunucudan gelen hata:', errorBody);
                try {
                    const err = JSON.parse(errorBody);
                    alert("Hata: " + (err.message || "Güncelleme yapılamadı."));
                } catch (e) {
                    alert("Hata: " + errorBody);
                }
            }
        } catch (e) {
            console.error(e);
            alert("Sunucu hatası. Lütfen konsolu kontrol edin.");
        } finally {
            updateButton.innerHTML = originalText;
            updateButton.disabled = false;
        }
    };

    window.deleteProduct = async function (productId) {
        if (!confirm('Bu ürünü silmek/pasife almak istediğinizden emin misiniz?')) {
            return;
        }

        try {
            const res = await fetch(`/api/Products/${productId}`, {
                method: 'DELETE',
                headers: { 'Authorization': `Bearer ${token}` }
            });

            if (res.ok) {
                alert('✅ Ürün başarıyla silindi/pasife alındı.');
                loadProducts();
            } else {
                const responseText = await res.text();
                let errorMessage = "İşlem başarısız.";

                try {
                    const err = JSON.parse(responseText);
                    errorMessage = err.message || err.error || err.detail || err.title || errorMessage;
                } catch (e) {
                    if (responseText && responseText.length < 500) {
                        errorMessage = responseText;
                    } else {
                        errorMessage = `Sunucu Hatası (${res.status})`;
                    }
                }
                alert('❌ Hata: ' + errorMessage);
            }
        } catch (e) {
            console.error(e);
            alert('Sunucu hatası veya bağlantı problemi.');
        }
    };

    window.restoreProduct = async function (productId) {
        if (!confirm('Bu ürünü yeniden aktif etmek istediğinizden emin misiniz?')) {
            return;
        }

        try {
            const res = await fetch(`/api/Products/activate/${productId}`, {
                method: 'PUT',
                headers: { 'Authorization': `Bearer ${token}` }
            });

            if (res.ok) {
                alert('✅ Ürün başarıyla aktif edildi.');
                loadProducts();
            } else {
                const err = await res.json();
                alert('Hata: ' + (err.message || 'İşlem başarısız.'));
            }
        } catch (e) {
            console.error(e);
            alert('Sunucu hatası.');
        }
    };

    // --- CATEGORY MANAGEMENT ---
    window.openCategoryManager = function() {
        renderCategoryManagerList();
        const modal = new bootstrap.Modal(document.getElementById('categoryManagerModal'));
        modal.show();
    };

    window.renderCategoryManagerList = function() {
        const tbody = document.getElementById('categoryListBody');
        tbody.innerHTML = '';
        
        // Sort: Active first, then by name
        const sorted = [...allCategories].sort((a, b) => {
            if (a.isActive === b.isActive) return a.categoryName.localeCompare(b.categoryName);
            return a.isActive ? -1 : 1;
        });

        sorted.forEach(c => {
            const isPassive = c.isActive === false;
            const badge = isPassive ? '<span class="badge bg-danger">Pasif</span>' : '<span class="badge bg-success">Aktif</span>';
            const rowClass = isPassive ? 'table-secondary text-muted' : '';
            const desc = c.description || '-';
            const catJson = JSON.stringify(c).replace(/"/g, '&quot;');

            const actionBtn = isPassive 
                ? `<button class="btn btn-sm btn-outline-success" onclick="toggleCategoryStatus('${c.id}', true)" title="Aktif Et"><i class="fas fa-undo"></i></button>`
                : `<button class="btn btn-sm btn-outline-primary me-1" onclick='editCategory(${catJson})' title="Düzenle"><i class="fas fa-pen"></i></button>
                   <button class="btn btn-sm btn-outline-danger" onclick="toggleCategoryStatus('${c.id}', false)" title="Pasife Al"><i class="fas fa-trash"></i></button>`;

            tbody.innerHTML += `
                <tr class="${rowClass}">
                    <td class="fw-bold">${c.categoryName}</td>
                    <td class="small text-truncate" style="max-width: 150px;">${desc}</td>
                    <td>${badge}</td>
                    <td class="text-end">${actionBtn}</td>
                </tr>
            `;
        });
    };

    window.handleCategorySubmit = async function() {
        const id = document.getElementById('catManId').value;
        const name = document.getElementById('catManName').value;
        const desc = document.getElementById('catManDesc').value;
        const btn = document.getElementById('catManSubmitBtn');

        if (!name) { alert("Lütfen kategori adını giriniz."); return; }

        const payload = {
            categoryName: name,
            description: desc,
            isActive: true
        };

        if (id) payload.id = id;

        const url = '/api/Categories';
        const method = id ? 'PUT' : 'POST';

        btn.disabled = true;
        const originalText = btn.innerHTML;
        btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i>';

        try {
            const res = await fetch(url, {
                method: method,
                headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });

            if (res.ok) {
                alert(`Kategori başarıyla ${id ? 'güncellendi' : 'eklendi'}!`);
                resetCategoryForm();
                await loadProductDropdowns(); // Reload data
                renderCategoryManagerList();
            } else {
                const err = await res.json();
                alert("Hata: " + (err.message || "İşlem başarısız."));
            }
        } catch (e) { console.error(e); alert("Sunucu hatası."); }
        finally { btn.disabled = false; btn.innerHTML = originalText; }
    };

    window.editCategory = function(cat) {
        document.getElementById('catManId').value = cat.id;
        document.getElementById('catManName').value = cat.categoryName;
        document.getElementById('catManDesc').value = cat.description || '';
        
        document.getElementById('catFormTitle').innerHTML = '<i class="fas fa-edit me-1"></i>KATEGORİ DÜZENLE';
        document.getElementById('catManSubmitBtn').innerText = 'Güncelle';
        document.getElementById('catManSubmitBtn').classList.replace('btn-primary', 'btn-warning');
        document.getElementById('catManCancelBtn').classList.remove('d-none');
    };

    window.resetCategoryForm = function() {
        document.getElementById('categoryManagerForm').reset();
        document.getElementById('catManId').value = '';
        
        document.getElementById('catFormTitle').innerHTML = '<i class="fas fa-plus-circle me-1"></i>YENİ KATEGORİ EKLE';
        const btn = document.getElementById('catManSubmitBtn');
        btn.innerText = 'Ekle';
        btn.classList.replace('btn-warning', 'btn-primary');
        document.getElementById('catManCancelBtn').classList.add('d-none');
    };

    window.toggleCategoryStatus = async function(id, isActive) {
        if (!confirm(`Bu kategoriyi ${isActive ? 'aktif etmek' : 'pasife almak'} istediğinize emin misiniz?`)) return;

        try {
            // Assuming the API supports toggle via PUT or a specific endpoint. 
            // If standard PUT is used, we need the full object. 
            // Let's find the object locally first.
            const cat = allCategories.find(c => c.id === id);
            if (!cat) return;

            const payload = { ...cat, isActive: isActive };
            
            const res = await fetch(`/api/Categories`, {
                method: 'PUT',
                headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });

            if (res.ok) {
                await loadProductDropdowns();
                renderCategoryManagerList();
            } else {
                alert("Durum değiştirilemedi.");
            }
        } catch (e) { console.error(e); alert("Hata oluştu."); }
    };

    // --- UNIT TYPE MANAGEMENT ---
    window.openUnitTypeManager = function() {
        renderUnitTypeManagerList();
        const modal = new bootstrap.Modal(document.getElementById('unitTypeManagerModal'));
        modal.show();
    };

    window.renderUnitTypeManagerList = function() {
        const tbody = document.getElementById('unitTypeListBody');
        tbody.innerHTML = '';
        
        const sorted = [...allUnitTypes].sort((a, b) => {
            if (a.isActive === b.isActive) return a.unitName.localeCompare(b.unitName);
            return a.isActive ? -1 : 1;
        });

        sorted.forEach(u => {
            const isPassive = u.isActive === false;
            const badge = isPassive ? '<span class="badge bg-danger">Pasif</span>' : '<span class="badge bg-success">Aktif</span>';
            const rowClass = isPassive ? 'table-secondary text-muted' : '';
            const desc = u.description || '-';
            const unitJson = JSON.stringify(u).replace(/"/g, '&quot;');

            const actionBtn = isPassive 
                ? `<button class="btn btn-sm btn-outline-success" onclick="toggleUnitTypeStatus('${u.id}', true)" title="Aktif Et"><i class="fas fa-undo"></i></button>`
                : `<button class="btn btn-sm btn-outline-primary me-1" onclick='editUnitType(${unitJson})' title="Düzenle"><i class="fas fa-pen"></i></button>
                   <button class="btn btn-sm btn-outline-danger" onclick="toggleUnitTypeStatus('${u.id}', false)" title="Pasife Al"><i class="fas fa-trash"></i></button>`;

            tbody.innerHTML += `
                <tr class="${rowClass}">
                    <td class="fw-bold">${u.unitName}</td>
                    <td class="small text-truncate" style="max-width: 150px;">${desc}</td>
                    <td>${badge}</td>
                    <td class="text-end">${actionBtn}</td>
                </tr>
            `;
        });
    };

    window.handleUnitTypeSubmit = async function() {
        const id = document.getElementById('unitManId').value;
        const name = document.getElementById('unitManName').value;
        const desc = document.getElementById('unitManDesc').value;
        const btn = document.getElementById('unitManSubmitBtn');

        if (!name) { alert("Lütfen birim adını giriniz."); return; }

        const payload = { unitName: name, description: desc, isActive: true };
        if (id) payload.id = id;

        const url = '/api/UnitTypes';
        const method = id ? 'PUT' : 'POST';

        btn.disabled = true;
        const originalText = btn.innerHTML;
        btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i>';

        try {
            const res = await fetch(url, {
                method: method,
                headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });

            if (res.ok) {
                alert(`Birim tipi başarıyla ${id ? 'güncellendi' : 'eklendi'}!`);
                resetUnitTypeForm();
                await loadProductDropdowns();
                renderUnitTypeManagerList();
            } else {
                const err = await res.json();
                alert("Hata: " + (err.message || "İşlem başarısız."));
            }
        } catch (e) { console.error(e); alert("Sunucu hatası."); }
        finally { btn.disabled = false; btn.innerHTML = originalText; }
    };

    window.editUnitType = function(unit) {
        document.getElementById('unitManId').value = unit.id;
        document.getElementById('unitManName').value = unit.unitName;
        document.getElementById('unitManDesc').value = unit.description || '';
        
        document.getElementById('unitFormTitle').innerHTML = '<i class="fas fa-edit me-1"></i>BİRİM DÜZENLE';
        document.getElementById('unitManSubmitBtn').innerText = 'Güncelle';
        document.getElementById('unitManSubmitBtn').classList.replace('btn-primary', 'btn-warning');
        document.getElementById('unitManCancelBtn').classList.remove('d-none');
    };

    window.resetUnitTypeForm = function() {
        document.getElementById('unitManagerForm').reset();
        document.getElementById('unitManId').value = '';
        
        document.getElementById('unitFormTitle').innerHTML = '<i class="fas fa-plus-circle me-1"></i>YENİ BİRİM TİPİ EKLE';
        const btn = document.getElementById('unitManSubmitBtn');
        btn.innerText = 'Ekle';
        btn.classList.replace('btn-warning', 'btn-primary');
        document.getElementById('unitManCancelBtn').classList.add('d-none');
    };

    window.toggleUnitTypeStatus = async function(id, isActive) {
        if (!confirm(`Bu birim tipini ${isActive ? 'aktif etmek' : 'pasife almak'} istediğinize emin misiniz?`)) return;

        try {
            const unit = allUnitTypes.find(u => u.id === id);
            if (!unit) return;

            const payload = { ...unit, isActive: isActive };
            
            const res = await fetch(`/api/UnitTypes`, {
                method: 'PUT',
                headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });

            if (res.ok) {
                await loadProductDropdowns();
                renderUnitTypeManagerList();
            } else {
                alert("Durum değiştirilemedi.");
            }
        } catch (e) { console.error(e); alert("Hata oluştu."); }
    };

    window.openProductOrderModal = async function(product) {
        document.getElementById('orderProductId').value = product.id; 
        document.getElementById('orderProductName').value = product.productName;
        document.getElementById('orderQuantity').value = 100;
        document.getElementById('orderDescription').value = '';

        const userName = localStorage.getItem('userName') || 'Misafir';
        const userCompany = localStorage.getItem('userCompany') || '';
        document.getElementById('orderUserName').innerText = userName;
        document.getElementById('orderUserCompany').innerText = userCompany;
        const initials = userName === 'Misafir' ? 'U' : userName.match(/\b(\w)/g).join('').substring(0, 2).toUpperCase();
        document.getElementById('orderUserInitials').innerText = initials;

        const supplierSelect = document.getElementById('orderSupplierSelect');
        supplierSelect.innerHTML = '<option value="" selected disabled>Yükleniyor...</option>';
        
        try {
            const res = await fetch('/api/Suppliers?IsActive=true', {
                headers: { 'Authorization': `Bearer ${localStorage.getItem('jwtToken')}` }
            });
            if(res.ok) {
                const suppliers = await res.json();
                supplierSelect.innerHTML = '<option value="" selected disabled>Tedarikçi Seçiniz...</option>';
                suppliers.forEach(s => {
                    const comp = s.companyName || s.contactPerson || 'Genel';
                    supplierSelect.innerHTML += `<option value="${s.id}">${s.supplierName} (${comp})</option>`;
                });
            } else {
                supplierSelect.innerHTML = '<option value="" disabled>Tedarikçiler yüklenemedi</option>';
            }
        } catch(e) {
            console.error(e);
            supplierSelect.innerHTML = '<option value="" disabled>Hata oluştu</option>';
        }

        const modal = new bootstrap.Modal(document.getElementById('orderModal'));
        modal.show();
    };

})();