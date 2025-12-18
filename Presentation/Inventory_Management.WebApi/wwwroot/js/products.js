(function () {
    const token = localStorage.getItem('jwtToken');
    let allProducts = [];

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

            let imageHtml = p.imageUrl && p.imageUrl.trim() !== ""
                ? `<div class="rounded-3 me-3 overflow-hidden shadow-sm border" style="width: 60px; height: 60px; flex-shrink: 0;">
                       <img src="${p.imageUrl}" alt="${pName}" style="width: 100%; height: 100%; object-fit: cover;" onerror="this.onerror=null; this.src='https://placehold.co/60x60?text=IMG';"> 
                   </div>`
                : `<div class="rounded-3 bg-primary-light text-primary d-flex align-items-center justify-content-center me-3 shadow-sm" 
                        style="width: 60px; height: 60px; font-size: 1.5rem; font-weight: bold; flex-shrink: 0;">
                       ${initial}
                   </div>`;

            const buttonsHtml = isInactive
                ? `<button class="btn btn-sm btn-light text-success rounded-circle me-1" title="Geri Yükle" onclick="restoreProduct('${p.id}')"><i class="fas fa-undo"></i></button>`
                : `<button class="btn btn-sm btn-light text-primary rounded-circle me-1" title="Düzenle" onclick='openUpdateModal(${productJson})'><i class="fas fa-pen"></i></button>
                   <button class="btn btn-sm btn-light text-danger rounded-circle" title="Sil" onclick="deleteProduct('${p.id}')"><i class="fas fa-trash"></i></button>`;

            container.innerHTML += `
                <div class="col-md-6 col-lg-4">
                    <div class="card border-0 shadow-sm h-100 p-3 card-hover transition ${isInactive ? 'product-inactive' : ''}">
                        <div class="d-flex align-items-center mb-3">
                            ${imageHtml}
                            <div class="flex-grow-1 overflow-hidden">
                                <h6 class="fw-bold text-dark mb-0 text-truncate" title="${pName}">${pName}</h6>
                                <small class="text-muted d-block mt-1 text-truncate"><i class="fas fa-barcode me-1"></i>${barcode}</small>
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
                const cats = await catRes.json();
                document.querySelectorAll('#categorySelect, #updateCategorySelect').forEach(select => {
                    select.innerHTML = '<option value="" selected disabled>Seçiniz...</option>';
                    cats.forEach(c => select.innerHTML += `<option value="${c.id}">${c.categoryName}</option>`);
                });
                const countEl = document.getElementById('totalCategoriesCount');
                if (countEl) countEl.innerText = cats.length;
            }

            if (unitRes.ok) {
                const units = await unitRes.json();
                document.querySelectorAll('#unitTypeSelect, #updateUnitTypeSelect').forEach(select => {
                    select.innerHTML = '<option value="" selected disabled>Seçiniz...</option>';
                    units.forEach(u => select.innerHTML += `<option value="${u.id}">${u.unitName}</option>`);
                });
            }
        } catch (e) { console.error("Dropdown hatası:", e); }
    }

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

    window.saveCategory = async function () {
        const name = document.getElementById('catNameInput').value;
        const description = document.getElementById('catDescInput').value;
        const saveButton = document.querySelector('#addCategoryModal .btn-primary');

        if (!name) {
            alert("⚠️ Lütfen kategori adını giriniz.");
            return;
        }

        const payload = {
            categoryName: name,
            description: description,
            isActive: true
        };

        const originalText = saveButton.innerHTML;
        saveButton.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Kaydediliyor...';
        saveButton.disabled = true;

        try {
            const res = await fetch('/api/Categories', {
                method: 'POST',
                headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });

            if (res.ok) {
                alert("✅ Kategori başarıyla eklendi!");
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

})();