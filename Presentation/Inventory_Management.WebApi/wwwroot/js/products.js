(function () {
    const token = localStorage.getItem('jwtToken');
    let allProducts = [];

    document.addEventListener('DOMContentLoaded', function () {
        if (!token) {
            window.location.href = 'login.html';
            return;
        }

        console.log("🚀 Ürünler Sayfası Yüklendi.");
        loadProducts();
        loadProductDropdowns();
    });

    async function loadProducts() {
        const container = document.getElementById('productsContainer');
        if (!container) return;

        try {
            const response = await fetch('/api/Products', {
                headers: { 'Authorization': `Bearer ${token}` }
            });

            if (!response.ok) throw new Error(`Hata: ${response.status}`);

            const data = await response.json();
            allProducts = data;

            updateProductStats(data);
            renderProducts(data);

        } catch (error) {
            console.error("❌ Ürün yükleme hatası:", error);
            container.innerHTML = `<div class="col-12 text-center text-danger py-5">Veriler yüklenemedi: ${error.message}</div>`;
        }
    }

    function renderProducts(data) {
        const container = document.getElementById('productsContainer');
        container.innerHTML = '';

        if (!data || data.length === 0) {
            container.innerHTML = `
                <div class="col-12 text-center py-5">
                    <i class="fas fa-box-open fs-1 text-muted mb-3"></i>
                    <p class="text-muted">Henüz kayıtlı ürün yok.</p>
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

            let imageHtml = '';
            if (p.imageUrl && p.imageUrl.trim() !== "") {
                imageHtml = `
                <div class="rounded-3 me-3 overflow-hidden shadow-sm border" style="width: 60px; height: 60px; flex-shrink: 0;">
                    <img src="${p.imageUrl}" alt="${pName}" style="width: 100%; height: 100%; object-fit: cover;" 
                         onerror="this.onerror=null; this.src='https://placehold.co/60x60?text=IMG';"> 
                </div>`;
            } else {
                imageHtml = `
                <div class="rounded-3 bg-primary-light text-primary d-flex align-items-center justify-content-center me-3 shadow-sm" 
                     style="width: 60px; height: 60px; font-size: 1.5rem; font-weight: bold; flex-shrink: 0;">
                    ${initial}
                </div>`;
            }

            const buttonsHtml = isInactive
                ? `
                <button class="btn btn-sm btn-light text-success rounded-circle me-1" title="Geri Yükle" onclick="restoreProduct('${p.id}')">
                    <i class="fas fa-undo"></i>
                </button>
                `
                : `
                <button class="btn btn-sm btn-light text-primary rounded-circle me-1" title="Düzenle" onclick='openUpdateModal(${productJson})'>
                    <i class="fas fa-pen"></i>
                </button>
                <button class="btn btn-sm btn-light text-danger rounded-circle" title="Sil" onclick="deleteProduct('${p.id}')">
                    <i class="fas fa-trash"></i>
                </button>
                `;

            const card = `
            <div class="col-md-6 col-lg-4">
                <div class="card border-0 shadow-sm h-100 p-3 card-hover transition ${isInactive ? 'product-inactive' : ''}">
                    <div class="d-flex align-items-center mb-3">
                        ${imageHtml}
                        <div class="flex-grow-1 overflow-hidden">
                            <h6 class="fw-bold text-dark mb-0 text-truncate" title="${pName}">${pName}</h6>
                            <small class="text-muted d-block mt-1 text-truncate">
                                <i class="fas fa-barcode me-1"></i>${barcode}
                            </small>
                        </div>
                        <div class="ms-2">
                            <span class="badge bg-light text-secondary border">${category}</span>
                        </div>
                    </div>
                    <div class="mt-auto pt-3 border-top d-flex justify-content-between align-items-center">
                        <small class="text-muted small text-truncate" style="max-width: 60%;" title="${p.description || ''}">
                            <i class="fas fa-info-circle me-1"></i>${p.description || 'Açıklama mevcut değil.'}
                        </small>
                        <div class="btn-group">
                            ${buttonsHtml}
                        </div>
                    </div>
                </div>
            </div>`;
            container.innerHTML += card;
        });
    }

    async function loadProductDropdowns() {
        try {
            const catRes = await fetch('/api/Categories', { headers: { 'Authorization': `Bearer ${token}` } });
            if (catRes.ok) {
                const cats = await catRes.json();
                const catSelects = document.querySelectorAll('#categorySelect, #updateCategorySelect');
                catSelects.forEach(select => {
                    select.innerHTML = '<option value="" selected disabled>Seçiniz...</option>';
                    cats.forEach(c => {
                        const opt = document.createElement('option');
                        opt.value = c.id;
                        opt.text = c.categoryName;
                        select.appendChild(opt);
                    });
                });
                const countEl = document.getElementById('totalCategoriesCount');
                if (countEl) countEl.innerText = cats.length;
            }

            const unitRes = await fetch('/api/UnitTypes', { headers: { 'Authorization': `Bearer ${token}` } });
            if (unitRes.ok) {
                const units = await unitRes.json();
                const unitSelects = document.querySelectorAll('#unitTypeSelect, #updateUnitTypeSelect');
                unitSelects.forEach(select => {
                    select.innerHTML = '<option value="" selected disabled>Seçiniz...</option>';
                    units.forEach(u => {
                        const opt = document.createElement('option');
                        opt.value = u.id;
                        opt.text = u.unitName;
                        select.appendChild(opt);
                    });
                });
            }
        } catch (e) { console.error("Dropdown hatası:", e); }
    }

    function updateProductStats(data) {
        if (document.getElementById('totalProductsCount')) {
            document.getElementById('totalProductsCount').innerText = data.length;
            document.getElementById('activeProductsCount').innerText = data.filter(x => x.isActive !== false).length;
        }
    }

    window.filterProducts = function () {
        const searchText = document.getElementById('searchInput').value.toLowerCase();
        const filtered = allProducts.filter(p =>
            p.productName.toLowerCase().includes(searchText) ||
            (p.barcode && p.barcode.toLowerCase().includes(searchText)) ||
            (p.categoryName && p.categoryName.toLowerCase().includes(searchText))
        );
        renderProducts(filtered);
    };

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
                // Hata yönetimini güvenli hale getiriyoruz
                // Önce metin olarak okuyoruz, sonra JSON parse deniyoruz
                // Bu sayede "body stream already read" hatası almazsınız
                const responseText = await res.text();
                let errorMessage = "İşlem başarısız.";

                try {
                    const err = JSON.parse(responseText);
                    // Backend'in döndürebileceği farklı hata formatlarını kontrol et
                    errorMessage = err.message || err.error || err.detail || err.title || errorMessage;
                } catch (e) {
                    // JSON değilse (örneğin HTML hata sayfası döndüyse)
                    // Mesaj çok uzunsa kısaltabiliriz veya genel hata verebiliriz
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