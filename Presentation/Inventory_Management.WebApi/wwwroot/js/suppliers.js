(function () {
    const token = localStorage.getItem('jwtToken');
    let allSuppliers = [];
    let calendarInit = false;
    let currentView = 'list';

    let calendar;
    let selectedEventId = null;
    let selectedEventData = null;

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
        if (!token) { window.location.href = 'login.html'; return; }
        loadSuppliers();
        loadDropdownForSchedule();
        setupSearch(); // Arama dinleyicisini başlat
    });

    // --- Search ---
    function setupSearch() {
        const searchInput = document.getElementById('searchInput');
        if (searchInput) {
            searchInput.addEventListener('keyup', debounce(() => {
                filterSuppliers(searchInput.value);
            }, 300));
        }
    }

    function filterSuppliers(term) {
        const lowerCaseTerm = term.trim().toLowerCase();
        if (!lowerCaseTerm) {
            renderSuppliers(allSuppliers, true);
            return;
        }

        const scoredData = allSuppliers.map(item => {
            let score = 0;
            const fields = [
                item.supplierName,
                item.contactPerson,
                item.email,
                item.phoneNumber
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
        renderSuppliers(filteredItems, false);
    }

    // --- Main Functions ---
    window.toggleView = function (view) {
        currentView = view;
        const list = document.getElementById('listView');
        const cal = document.getElementById('calendarView');
        const btn = document.getElementById('mainActionButton');

        if (view === 'list') {
            list.classList.remove('d-none');
            cal.classList.add('d-none');
            btn.innerHTML = '<i class="fas fa-plus me-2"></i>Yeni Tedarikçi';
            btn.className = 'btn btn-primary rounded-pill px-4 shadow-sm';
        } else {
            list.classList.add('d-none');
            cal.classList.remove('d-none');
            btn.innerHTML = '<i class="far fa-calendar-plus me-2"></i>Yeni Planlama';
            btn.className = 'btn btn-info text-white rounded-pill px-4 shadow-sm';
            setTimeout(() => { initCalendar(); }, 100);
        }
    };

    window.handleMainAction = function () {
        if (currentView === 'list') {
            document.getElementById('supplierForm').reset();
            document.getElementById('supplierId').value = '';
            document.querySelector('#addSupplierModal .modal-title').innerText = 'Yeni Tedarikçi Ekle';
            openModal('addSupplierModal');
        } else {
            resetScheduleForm();
            openModal('addScheduleModal');
        }
    };

    function openModal(modalId) {
        const modalEl = document.getElementById(modalId);
        if (modalEl) bootstrap.Modal.getOrCreateInstance(modalEl).show();
    }

    function hideModal(modalId) {
        const modalEl = document.getElementById(modalId);
        if (modalEl) bootstrap.Modal.getInstance(modalEl)?.hide();
    }

    async function loadSuppliers() {
        const container = document.getElementById('suppliersContainer');
        if (!container) return;
        container.innerHTML = `<div class="col-12 text-center py-5"><div class="spinner-border text-primary"></div></div>`;

        try {
            const response = await fetch('/api/Suppliers', { headers: { 'Authorization': `Bearer ${token}` } });
            if (!response.ok) throw new Error("Veri yüklenemedi");
            allSuppliers = await response.json();
            renderSuppliers(allSuppliers, true);
        } catch (e) { 
            console.error(e);
            container.innerHTML = `<div class="col-12 text-center py-5 text-danger">Tedarikçiler yüklenemedi.</div>`;
        }
    }

    function renderSuppliers(data, isInitialLoad = false) {
        const container = document.getElementById('suppliersContainer');
        container.innerHTML = '';
        if (!data || data.length === 0) {
            const message = isInitialLoad ? "Kayıtlı tedarikçi bulunamadı." : "Arama sonucuyla eşleşen tedarikçi bulunamadı.";
            container.innerHTML = `<div class="col-12 text-center py-5"><i class="fas fa-users-slash fs-1 text-muted mb-3"></i><p class="text-muted">${message}</p></div>`;
            return;
        }

        data.forEach(s => {
            const initial = s.supplierName ? s.supplierName.charAt(0).toUpperCase() : "?";
            const isInactive = s.isActive === false;

            const buttonsHtml = isInactive
                ? `<button class="btn btn-sm btn-light text-success rounded-circle" title="Geri Yükle" onclick="restoreSupplier('${s.id}')"><i class="fas fa-undo"></i></button>`
                : `<div class="dropdown">
                       <button class="btn btn-light btn-sm rounded-circle" type="button" data-bs-toggle="dropdown"><i class="fas fa-ellipsis-v text-muted"></i></button>
                       <ul class="dropdown-menu border-0 shadow">
                           <li><a class="dropdown-item small" href="#" onclick="editSupplier('${s.id}')"><i class="fas fa-pen me-2 text-primary"></i>Düzenle</a></li>
                           <li><a class="dropdown-item small text-danger" href="#" onclick="deleteSupplier('${s.id}')"><i class="fas fa-trash me-2"></i>Sil</a></li>
                       </ul>
                   </div>`;
            
            container.innerHTML += `
            <div class="col-md-6 col-lg-4">
                <div class="card border-0 bg-white shadow-sm p-4 h-100 supplier-card ${isInactive ? 'supplier-inactive' : ''}">
                    <div class="d-flex justify-content-between align-items-start mb-3">
                        <div class="d-flex align-items-center">
                            <div class="rounded-circle bg-primary text-white d-flex align-items-center justify-content-center me-3 shadow-sm" style="width: 50px; height: 50px; font-size: 1.2rem; font-weight: bold;">${initial}</div>
                            <div><h6 class="fw-bold text-dark mb-0">${s.supplierName}</h6><small class="text-muted">${s.contactPerson || '-'}</small></div>
                        </div>
                        ${buttonsHtml}
                    </div>
                    <div class="mt-2">
                        <div class="d-flex align-items-center mb-2"><span class="contact-icon me-2"><i class="fas fa-phone-alt small"></i></span><span class="small text-muted">${s.phoneNumber || '-'}</span></div>
                        <div class="d-flex align-items-center"><span class="contact-icon me-2"><i class="fas fa-envelope small"></i></span><span class="small text-muted text-truncate" style="max-width: 200px;">${s.email || '-'}</span></div>
                    </div>
                    <div class="mt-3 pt-3 border-top"><small class="text-muted d-block mb-1" style="font-size: 0.7rem;">ADRES</small><p class="small text-dark mb-0 text-truncate">${s.address || '-'}</p></div>
                </div>
            </div>`;
        });
    }

    window.editSupplier = function (id) {
        const supplier = allSuppliers.find(s => s.id === id);
        if (!supplier) return;
        document.getElementById('supplierForm').reset();
        document.getElementById('supplierId').value = supplier.id;
        document.getElementById('supplierName').value = supplier.supplierName;
        document.getElementById('contactPerson').value = supplier.contactPerson;
        document.getElementById('email').value = supplier.email;
        document.getElementById('phoneNumber').value = supplier.phoneNumber;
        document.getElementById('address').value = supplier.address;
        document.querySelector('#addSupplierModal .modal-title').innerText = 'Tedarikçiyi Düzenle';
        openModal('addSupplierModal');
    };

    window.saveSupplier = async function () {
        const id = document.getElementById('supplierId').value;
        const isUpdate = !!id;
        
        let payload = {
            id: isUpdate ? id : undefined,
            supplierName: document.getElementById('supplierName').value,
            contactPerson: document.getElementById('contactPerson').value,
            email: document.getElementById('email').value,
            phoneNumber: document.getElementById('phoneNumber').value,
            address: document.getElementById('address').value,
            isActive: true // Create or update always sets to active
        };

        if (!payload.supplierName) { alert("Şirket adı zorunludur."); return; }
        
        if (isUpdate) {
            const originalSupplier = allSuppliers.find(s => s.id === id);
            if (originalSupplier) {
                payload.companyId = originalSupplier.companyId;
            } else {
                alert("Güncellenecek tedarikçi bulunamadı. Lütfen sayfayı yenileyin.");
                return;
            }
        }

        const method = isUpdate ? 'PUT' : 'POST';
        const url = '/api/Suppliers';

        try {
            const res = await fetch(url, {
                method: method,
                headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });
            if (res.ok) {
                alert("✅ Başarılı!");
                hideModal('addSupplierModal');
                loadSuppliers();
            } else {
                const errorText = await res.text();
                console.error("Kaydetme Hatası:", errorText);
                throw new Error("Kaydetme işlemi başarısız. Sunucu detayları için konsolu kontrol edin.");
            }
        } catch (e) { 
            console.error(e); 
            alert("❌ " + e.message);
        }
    };

    window.deleteSupplier = async function (id) {
        if (!confirm("Bu tedarikçiyi silmek istediğinize emin misiniz? Bu işlem tedarikçiyi pasif hale getirecektir.")) return;
        try {
            const res = await fetch(`/api/Suppliers/${id}`, { 
                method: 'DELETE', 
                headers: { 'Authorization': `Bearer ${token}` } 
            });
            if(res.ok) {
                alert('✅ Tedarikçi başarıyla pasife alındı.');
                loadSuppliers();
            } else {
                throw new Error("Silme işlemi başarısız.");
            }
        } catch (e) { 
            console.error(e); 
            alert("❌ " + e.message);
        }
    };

    window.restoreSupplier = async function (id) {
        if (!confirm("Bu tedarikçiyi yeniden aktif etmek istediğinize emin misiniz?")) return;
        try {
            const res = await fetch(`/api/Suppliers/activate/${id}`, { 
                method: 'PUT', 
                headers: { 'Authorization': `Bearer ${token}` } 
            });
            if(res.ok) {
                alert('✅ Tedarikçi başarıyla aktif edildi.');
                loadSuppliers();
            } else {
                throw new Error("Aktif etme işlemi başarısız.");
            }
        } catch (e) { 
            console.error(e); 
            alert("❌ " + e.message);
        }
    };

    // (Calendar and schedule functions remain unchanged)
    async function initCalendar() {
        const calendarEl = document.getElementById('supplierCalendar');
        if (!calendarEl) return;
        if (calendarInit && calendar) { calendar.render(); return; }

        try {
            const response = await fetch('/api/Suppliers/calendar', {
                headers: { 'Authorization': `Bearer ${token}` },
                cache: 'no-cache'
            });
            let eventsData = [];
            if (response.ok) {
                const apiData = await response.json();
                eventsData = apiData.flatMap(item => {
                    const baseEvent = {
                        id: item.id,
                        title: item.title || item.ruleName,
                        backgroundColor: item.calendarColor || '#0d6efd',
                        borderColor: item.calendarColor || '#0d6efd',
                        textColor: '#fff',
                        extendedProps: {
                            supplierId: item.supplierId,
                            leadTime: item.leadTime || item.leadTimeDays,
                            frequency: item.frequency,
                            interval: item.interval,
                            daysOfMonth: item.daysOfMonth || item.DaysOfMonth || item.dayOfMonth,
                            daysOfWeek: item.daysOfWeek,
                            rawStartDate: item.startDate,
                            rawEndDate: item.endDate
                        }
                    };

                    if (item.start) return [{ ...baseEvent, start: item.start, allDay: false }];
                    if (item.frequency === 1 && item.daysOfWeek) {
                        return [{
                            ...baseEvent,
                            daysOfWeek: Array.isArray(item.daysOfWeek) ? item.daysOfWeek : item.daysOfWeek.split(',').map(Number),
                            startRecur: item.startDate ? item.startDate.split('T')[0] : null,
                            endRecur: item.endDate ? item.endDate.split('T')[0] : null,
                            startTime: item.arrivalTime ? item.arrivalTime.substring(0, 5) : '09:00'
                        }];
                    }
                    return [];
                });
            }

            calendar = new FullCalendar.Calendar(calendarEl, {
                initialView: 'dayGridMonth',
                height: 600,
                headerToolbar: { left: 'prev,next today', center: 'title', right: 'dayGridMonth,listWeek' },
                locale: 'tr',
                events: eventsData,
                eventClick: info => {
                    selectedEventId = info.event.id;
                    selectedEventData = info.event;
                    openModal('actionChoiceModal');
                },
                eventDidMount: info => { info.el.title = info.event.title; }
            });
            calendar.render();
            calendarInit = true;

        } catch (e) { console.error("Takvim Hatası:", e); }
    }

    async function loadDropdownForSchedule() {
        const select = document.getElementById('scheduleSupplierSelect');
        if (!select) return;
        try {
            const res = await fetch('/api/Suppliers', { headers: { 'Authorization': `Bearer ${token}` } });
            if (res.ok) {
                const data = await res.json();
                select.innerHTML = '<option selected disabled>Seçiniz...</option>';
                data.forEach(s => select.innerHTML += `<option value="${s.id}">${s.supplierName}</option>`);
            }
        } catch (e) { console.error(e); }
    }

    window.openEditModal = function () {
        hideModal('actionChoiceModal');
        resetScheduleForm();
        document.getElementById('editRuleId').value = selectedEventId;
        document.querySelector('#addScheduleModal .modal-title').innerText = 'Planı Düzenle';
        document.querySelector('#addScheduleModal .btn-primary').innerText = "Güncelle";
        const event = selectedEventData;
        const props = event.extendedProps;
        document.getElementById('ruleName').value = event.title;
        document.getElementById('calendarColor').value = event.backgroundColor;
        if (props.rawStartDate) document.getElementById('startDate').value = props.rawStartDate.split('T')[0];
        else if (event.start) document.getElementById('startDate').value = new Date(event.start.getTime() - (event.start.getTimezoneOffset() * 60000)).toISOString().split('T')[0];
        else if (event.startRecur) document.getElementById('startDate').value = event.startRecur;
        if (props.rawEndDate) document.getElementById('endDate').value = props.rawEndDate.split('T')[0];
        let timeVal = "09:00";
        if (event.start) timeVal = `${String(event.start.getHours()).padStart(2, '0')}:${String(event.start.getMinutes()).padStart(2, '0')}`;
        document.getElementById('arrivalTime').value = timeVal;
        if (props.supplierId) document.getElementById('scheduleSupplierSelect').value = props.supplierId;
        document.getElementById('leadTime').value = props.leadTime || 1;
        document.getElementById('interval').value = props.interval || 1;
        const freq = props.frequency || 1;
        document.getElementById('frequencySelect').value = freq;
        toggleFrequencyOptions();
        document.querySelectorAll('.day-check').forEach(cb => cb.checked = false);
        if (freq === 1) {
            const days = props.daysOfWeek || props.daysOfMonth || [];
            const daysArr = (typeof days === 'string') ? days.split(',') : days;
            if (Array.isArray(daysArr)) daysArr.forEach(d => { if (d !== null && d !== undefined && d !== '') { const cb = document.getElementById('day' + String(d).trim()); if (cb) cb.checked = true; } });
        } else if (freq === 2) {
            let val = props.daysOfMonth || "";
            if (Array.isArray(val)) val = val.join(',');
            document.getElementById('daysOfMonth').value = val;
        }
        setTimeout(() => openModal('addScheduleModal'), 200);
    };

    window.resetScheduleForm = function () {
        document.getElementById('scheduleForm').reset();
        document.getElementById('editRuleId').value = "";
        document.querySelector('#addScheduleModal .modal-title').innerText = 'Yeni Teslimat Planı';
        document.querySelector('#addScheduleModal .btn-primary').innerText = "Planı Kaydet";
        document.getElementById('frequencySelect').value = "1";
        toggleFrequencyOptions();
    };

    window.toggleFrequencyOptions = function () {
        const freq = parseInt(document.getElementById('frequencySelect').value);
        document.getElementById('weeklyOptions').classList.toggle('d-none', freq !== 1);
        document.getElementById('monthlyOptions').classList.toggle('d-none', freq !== 2);
    };

    window.saveSchedule = async function () {
        const editId = document.getElementById('editRuleId').value;
        const isUpdate = !!editId;
        const ruleName = document.getElementById('ruleName').value;
        const startDate = document.getElementById('startDate').value;
        const supplierId = document.getElementById('scheduleSupplierSelect').value;
        if (!ruleName || !startDate || !supplierId) { alert("⚠️ Lütfen zorunlu alanları doldurun."); return; }
        const payload = {
            ruleName,
            startDate,
            supplierId,
            endDate: document.getElementById('endDate').value || null,
            frequency: parseInt(document.getElementById('frequencySelect').value),
            interval: parseInt(document.getElementById('interval').value) || 1,
            leadTimeDays: parseInt(document.getElementById('leadTime').value) || 0,
            calendarColor: document.getElementById('calendarColor').value,
            arrivalTime: (document.getElementById('arrivalTime').value || "09:00") + ":00",
        };
        if (isUpdate) payload.id = editId;
        if (payload.frequency === 1) {
            payload.daysOfWeek = Array.from(document.querySelectorAll('.day-check:checked')).map(cb => cb.value).join(',');
        } else {
            const domInput = document.getElementById('daysOfMonth');
            if (!domInput.value.trim()) { alert("Lütfen ayın günlerini girin."); return; }
            payload.daysOfMonth = domInput.value.trim();
        }
        const btn = document.querySelector('#addScheduleModal .btn-primary');
        const originalText = btn.innerHTML;
        btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> İşleniyor...';
        btn.disabled = true;
        try {
            const res = await fetch('/api/DeliveryRules', {
                method: isUpdate ? 'PUT' : 'POST',
                headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });
            if (!res.ok) { const err = await res.json(); throw new Error(err.message || "İşlem başarısız."); }
            alert(`✅ İşlem Başarılı!`);
            hideModal('addScheduleModal');
            calendarInit = false;
            initCalendar();
        } catch (e) {
            console.error("Hata:", e);
            alert("❌ Hata: " + e.message);
        } finally {
            btn.innerHTML = originalText;
            btn.disabled = false;
        }
    };

    window.confirmDelete = function () {
        hideModal('actionChoiceModal');
        if (confirm("Bu planı silmek istediğinize emin misiniz?")) deleteSchedule(selectedEventId);
    };

    async function deleteSchedule(id) {
        try {
            const res = await fetch(`/api/DeliveryRules/${id}`, { method: 'DELETE', headers: { 'Authorization': `Bearer ${token}` } });
            if (res.ok) { alert("🗑️ Plan silindi."); calendarInit = false; initCalendar(); }
            else alert("Silme başarısız.");
        } catch (e) { console.error(e); }
    }
})();