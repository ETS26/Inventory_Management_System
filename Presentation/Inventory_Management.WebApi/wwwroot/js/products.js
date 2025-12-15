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

    // 1. Ürünleri Listele
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

    // 2. Ürün Kartlarını Oluştur (GÜNCELLENEN KISIM)
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
            const unit = p.unitTypeName || "Adet";
            const barcode = p.barcode || "-";

            // --- RESİM MANTIĞI ---
            let imageHtml = '';

            if (p.imageUrl && p.imageUrl.trim() !== "") {
                // Resim Varsa: Şık bir kare kutu içinde göster
                imageHtml = `
                <div class="rounded-3 me-3 overflow-hidden shadow-sm border" style="width: 60px; height: 60px; flex-shrink: 0;">
                    <img src="${p.imageUrl}" alt="${pName}" style="width: 100%; height: 100%; object-fit: cover;" 
                         onerror="this.onerror=null; this.src='https://placehold.co/60x60?text=IMG';"> 
                </div>`;
            } else {
                // Resim Yoksa: Baş Harf (Eski Yöntem)
                imageHtml = `
                <div class="rounded-3 bg-primary-light text-primary d-flex align-items-center justify-content-center me-3 shadow-sm" 
                     style="width: 60px; height: 60px; font-size: 1.5rem; font-weight: bold; flex-shrink: 0;">
                    ${initial}
                </div>`;
            }

            // Kart Tasarımı
            const card = `
            <div class="col-md-6 col-lg-4">
                <div class="card border-0 shadow-sm h-100 p-3 card-hover transition">
                    <div class="d-flex align-items-center mb-3">
                        
                        ${imageHtml} <div class="flex-grow-1 overflow-hidden">
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
                            <button class="btn btn-sm btn-light text-primary rounded-circle me-1" title="Düzenle">
                                <i class="fas fa-pen"></i>
                            </button>
                            <button class="btn btn-sm btn-light text-danger rounded-circle" title="Sil">
                                <i class="fas fa-trash"></i>
                            </button>
                        </div>
                    </div>
                </div>
            </div>`;

            container.innerHTML += card;
        });
    }

    // 3. Dropdownları Doldur
    async function loadProductDropdowns() {
        try {
            const catRes = await fetch('/api/Categories', { headers: { 'Authorization': `Bearer ${token}` } });
            if (catRes.ok) {
                const cats = await catRes.json();
                const select = document.getElementById('categorySelect');
                select.innerHTML = '<option value="" selected disabled>Seçiniz...</option>';
                cats.forEach(c => {
                    const opt = document.createElement('option');
                    opt.value = c.id;
                    opt.text = c.categoryName;
                    select.appendChild(opt);
                });

                // İstatistik Güncelleme
                const countEl = document.getElementById('totalCategoriesCount');
                if (countEl) countEl.innerText = cats.length;
            }

            const unitRes = await fetch('/api/UnitTypes', { headers: { 'Authorization': `Bearer ${token}` } });
            if (unitRes.ok) {
                const units = await unitRes.json();
                const select = document.getElementById('unitTypeSelect');
                select.innerHTML = '<option value="" selected disabled>Seçiniz...</option>';
                units.forEach(u => {
                    const opt = document.createElement('option');
                    opt.value = u.id;
                    opt.text = u.unitName;
                    select.appendChild(opt);
                });
            }
        } catch (e) { console.error("Dropdown hatası:", e); }
    }

    // 4. İstatistikler
    function updateProductStats(data) {
        if (document.getElementById('totalProductsCount')) {
            document.getElementById('totalProductsCount').innerText = data.length;
            document.getElementById('activeProductsCount').innerText = data.filter(x => x.isActive !== false).length;
        }
    }

    // 5. Arama
    window.filterProducts = function () {
        const searchText = document.getElementById('searchInput').value.toLowerCase();
        const filtered = allProducts.filter(p =>
            p.productName.toLowerCase().includes(searchText) ||
            (p.barcode && p.barcode.toLowerCase().includes(searchText)) ||
            (p.categoryName && p.categoryName.toLowerCase().includes(searchText))
        );
        renderProducts(filtered);
    };

    // 6. Yeni Ürün Kaydet (GÜNCELLENEN KISIM)
    window.saveProduct = async function () {
        const name = document.getElementById('productNameInput').value;
        const barcode = document.getElementById('barcodeInput').value;
        const categoryId = document.getElementById('categorySelect').value;
        const unitTypeId = document.getElementById('unitTypeSelect').value;
        const description = document.getElementById('descriptionInput').value;

        // YENİ: Resim URL'sini al
        const imageUrl = document.getElementById('imageUrlInput').value;

        const saveButton = document.querySelector('.modal-footer .btn-primary');

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
            imageUrl: imageUrl, // API'ye gönderiyoruz
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
    // --- YENİ KATEGORİ KAYDETME ---
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
            // Backend'de CategoriesController > Create metodunuz olmalı
            const res = await fetch('/api/Categories', {
                method: 'POST',
                headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });

            if (res.ok) {
                alert("✅ Kategori başarıyla eklendi!");
                location.reload(); // Sayfayı yenile ki yeni kategori dropdown'a gelsin
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