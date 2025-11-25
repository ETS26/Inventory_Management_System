(function () {
    'use strict';

    // Token kontrolü
    const token = localStorage.getItem('jwtToken');
    if (!token) {
        window.location.href = 'login.html';
        return;
    }

    // Menü Toggle
    const initMenuToggle = () => {
        const toggleButton = document.getElementById("menu-toggle");
        const wrapper = document.getElementById("wrapper");

        if (toggleButton && wrapper) {
            toggleButton.addEventListener('click', () => {
                wrapper.classList.toggle("toggled");
            });
        }
    };

    // Logout Fonksiyonu
    window.logout = () => {
        localStorage.clear();
        window.location.href = 'login.html';
    };

    // DOM Hazır Olduğunda
    document.addEventListener('DOMContentLoaded', async () => {
        console.log("🚀 Stok Hareketleri Sayfası Yükleniyor...");

        initMenuToggle();

        // Sayfa elementlerinin varlığını kontrol et
        const movementsContainer = document.getElementById('movementsContainer');

        if (movementsContainer) {
            await loadStockMovements();
            await loadStockDropdowns();
        } else {
            console.warn("⚠️ Stok hareketleri container'ı bulunamadı!");
        }
    });

    // ==========================================
    // 1. STOK HAREKETLERİNİ YÜKLE
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
            const response = await fetch('/api/StockMovements', {
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
                        <p class="text-muted small">İlk kaydınızı eklemek için "Yeni Hareket Ekle" butonuna tıklayın.</p>
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
    // 2. HAREKET KARTINI OLUŞTUR
    // ==========================================
    function createMovementCard(movement) {
        // Null güvenli veri çekme
        const productName = movement.productName || 'Silinmiş Ürün';
        const moveTypeName = movement.moveTypeName || 'Bilinmeyen';
        const quantity = movement.quantity || 0;
        const userName = movement.userName || 'Sistem';
        const description = movement.description || '';

        // Tarih formatı
        const dateObj = new Date(movement.createdAt);
        const date = dateObj.toLocaleDateString('tr-TR', {
            day: 'numeric',
            month: 'long',
            year: 'numeric'
        });
        const time = dateObj.toLocaleTimeString('tr-TR', {
            hour: '2-digit',
            minute: '2-digit'
        });

        // Hareket tipi kontrolü (giriş mi çıkış mı?)
        const moveName = moveTypeName.toLowerCase();
        const isIncome = moveName.includes('stock in') ||
            moveName.includes('giriş') ||
            moveName.includes('in') ||
            moveName.includes('income');

        // Stil değişkenleri
        const borderClass = isIncome ? 'border-start border-success border-3' : 'border-start border-danger border-3';
        const iconBg = isIncome ? 'bg-success-subtle text-success' : 'bg-danger-subtle text-danger';
        const icon = isIncome ? 'fa-arrow-down' : 'fa-arrow-up';
        const amountColor = isIncome ? 'text-success' : 'text-danger';
        const amountPrefix = isIncome ? '+' : '-';
        const badgeClass = isIncome ? 'bg-success-subtle text-success' : 'bg-danger-subtle text-danger';

        // Kullanıcı baş harfi
        const userInitial = userName.charAt(0).toUpperCase();

        return `
            <div class="col-md-6 col-lg-4 mb-4">
                <div class="card shadow-sm h-100 ${borderClass} hover-shadow transition">
                    <div class="card-body">
                        <!-- Üst Kısım: Ürün ve Miktar -->
                        <div class="d-flex justify-content-between align-items-start mb-3">
                            <div class="d-flex align-items-center flex-grow-1">
                                <div class="rounded-circle ${iconBg} d-flex align-items-center justify-content-center me-3" 
                                     style="width: 40px; height: 40px;">
                                    <i class="fas ${icon}"></i>
                                </div>
                                <div class="flex-grow-1">
                                    <h6 class="fw-bold mb-1 text-dark text-truncate">${productName}</h6>
                                    <span class="badge ${badgeClass} border px-2 py-1">
                                        ${moveTypeName}
                                    </span>
                                </div>
                            </div>
                            <div class="text-end ms-2">
                                <h4 class="fw-bold mb-0 ${amountColor}">${amountPrefix}${quantity}</h4>
                                <small class="text-muted">Adet</small>
                            </div>
                        </div>

                        ${description ? `
                        <!-- Açıklama -->
                        <div class="alert alert-light border-0 py-2 px-3 mb-3 small">
                            <i class="fas fa-info-circle me-1 text-muted"></i>
                            <span class="text-muted fst-italic">${description}</span>
                        </div>
                        ` : ''}

                        <!-- Alt Kısım: Kullanıcı ve Tarih -->
                        <div class="d-flex justify-content-between align-items-center pt-3 border-top">
                            <div class="d-flex align-items-center">
                                <div class="bg-primary text-white rounded-circle d-flex align-items-center justify-content-center me-2" 
                                     style="width: 28px; height: 28px; font-size: 0.75rem; font-weight: 600;">
                                    ${userInitial}
                                </div>
                                <div>
                                    <small class="text-muted d-block" style="font-size: 0.7rem;">İşlem Yapan</small>
                                    <span class="fw-bold small text-dark">${userName}</span>
                                </div>
                            </div>
                            <div class="text-end">
                                <small class="text-muted d-block" style="font-size: 0.7rem;">${date}</small>
                                <span class="fw-bold text-secondary small">${time}</span>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        `;
    }

    // ==========================================
    // 3. DROPDOWN'LARI DOLDUR
    // ==========================================
    async function loadStockDropdowns() {
        const inventorySelect = document.getElementById('inventorySelect');
        const moveTypeSelect = document.getElementById('moveTypeSelect');
        const supplierSelect = document.getElementById('supplierSelect');

        if (!inventorySelect || !moveTypeSelect || !supplierSelect) {
            console.warn("⚠️ Dropdown elementleri bulunamadı!");
            return;
        }

        try {
            // Paralel istekler
            const [invResponse, typeResponse, supplierResponse] = await Promise.all([
                fetch('/api/Inventories', { headers: { 'Authorization': `Bearer ${token}` } }),
                fetch('/api/MoveTypes', { headers: { 'Authorization': `Bearer ${token}` } }),
                fetch('/api/Suppliers', { headers: { 'Authorization': `Bearer ${token}` } })
            ]);

            // Inventories
            if (invResponse.ok) {
                const inventories = await invResponse.json();
                inventorySelect.innerHTML = '<option value="" selected disabled>Ürün seçiniz...</option>';

                inventories.forEach(item => {
                    const productName = item.productName || item.product?.productName || 'Bilinmeyen';
                    const option = document.createElement('option');
                    option.value = item.id;
                    option.textContent = `${productName} (Stok: ${item.quantity})`;
                    if (item.quantity === 0) {
                        option.disabled = true;
                        option.textContent += ' - Tükendi';
                    }
                    inventorySelect.appendChild(option);
                });
            }

            // Move Types
            if (typeResponse.ok) {
                const moveTypes = await typeResponse.json();
                moveTypeSelect.innerHTML = '<option value="" selected disabled>İşlem tipi seçiniz...</option>';

                moveTypes.forEach(type => {
                    const option = document.createElement('option');
                    option.value = type.id;
                    option.textContent = type.moveType || 'Tanımsız';
                    moveTypeSelect.appendChild(option);
                });
            }

            // Suppliers (opsiyonel)
            if (supplierResponse.ok && supplierSelect) {
                const suppliers = await supplierResponse.json();
                supplierSelect.innerHTML = '<option value="">Tedarikçi yok</option>';

                suppliers.forEach(supplier => {
                    const option = document.createElement('option');
                    option.value = supplier.id;
                    option.textContent = supplier.supplierName || 'İsimsiz Tedarikçi';
                    supplierSelect.appendChild(option);
                });
            }

        } catch (error) {
            console.error("❌ Dropdown Yükleme Hatası:", error);
        }
    }

    // ==========================================
    // 4. YENİ HAREKET KAYDETME
    // ==========================================
    window.saveStockMovement = async function () {
        // Form elemanlarını al
        const inventorySelect = document.getElementById('inventorySelect');
        const moveTypeSelect = document.getElementById('moveTypeSelect');
        const supplierSelect = document.getElementById('supplierSelect');
        const quantityInput = document.getElementById('quantityInput');
        const descriptionInput = document.getElementById('descriptionInput');
        const saveButton = document.querySelector('.modal-footer .btn-primary');

        // Validasyon
        const inventoryId = inventorySelect.value;
        const moveTypeId = moveTypeSelect.value;
        const supplierId = supplierSelect.value;
        const quantity = parseInt(quantityInput.value);
        const description = descriptionInput?.value || '';
        const userId = localStorage.getItem('userId');

        if (!inventoryId || !moveTypeId || !quantity || !supplierId || quantity <= 0) {
            alert("⚠️ Lütfen tüm zorunlu alanları doldurun ve geçerli bir miktar girin.");
            return;
        }

        if (!userId) {
            alert("❌ Kullanıcı ID bulunamadı. Lütfen çıkış yapıp tekrar giriş yapın.");
            return;
        }

        // Payload hazırla
        const payload = {
            inventoryId: inventoryId,
            moveTypeId: moveTypeId,
            supplierId: supplierId,
            userId: userId,
            quantity: quantity,
            description: description    
        };

        console.log("📤 Gönderilen Veri:", payload);

        // Button durumunu güncelle
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

                // Modal'ı kapat
                const modal = bootstrap.Modal.getInstance(document.getElementById('addMovementModal'));
                if (modal) modal.hide();

                // Formu temizle
                document.getElementById('movementForm')?.reset();

                // Sayfayı yenile
                await loadStockMovements();

            } else {
                throw new Error(result.errorMessage || result.title || 'İşlem başarısız');
            }

        } catch (error) {
            console.error("❌ Kaydetme Hatası:", error);
            alert(`❌ Hata: ${error.message}`);
        } finally {
            // Button'u eski haline getir
            saveButton.innerHTML = originalButtonText;
            saveButton.disabled = false;
        }
    };

})();