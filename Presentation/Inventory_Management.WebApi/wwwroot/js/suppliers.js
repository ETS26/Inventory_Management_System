(function () {
    const token = localStorage.getItem('jwtToken');
    let allSuppliers = [];
    let calendarInit = false;
    let currentView = 'list';

    // Global değişkenler
    let calendar;
    let selectedEventId = null;
    let selectedEventData = null;

    document.addEventListener('DOMContentLoaded', function () {
        if (!token) { window.location.href = 'login.html'; return; }

        loadSuppliers();
        loadDropdownForSchedule();
    });

    // 1. Görünüm Değiştir
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
        if (modalEl) bootstrap.Modal.getInstance(modalId)?.hide();
    }

    // 2. Tedarikçileri Listele
    async function loadSuppliers() {
        const container = document.getElementById('suppliersContainer');
        if (!container) return;

        try {
            const response = await fetch('/api/Suppliers', { headers: { 'Authorization': `Bearer ${token}` } });
            if (!response.ok) throw new Error("Hata");
            allSuppliers = await response.json();

            container.innerHTML = '';
            if (allSuppliers.length === 0) {
                container.innerHTML = '<div class="col-12 text-center text-muted py-5">Henüz tedarikçi yok.</div>';
                return;
            }

            allSuppliers.forEach(s => {
                const initial = s.supplierName ? s.supplierName.charAt(0).toUpperCase() : "?";
                container.innerHTML += `
                <div class="col-md-6 col-lg-4">
                    <div class="card border-0 bg-white shadow-sm p-4 h-100 supplier-card">
                        <div class="d-flex justify-content-between align-items-start mb-3">
                            <div class="d-flex align-items-center">
                                <div class="rounded-circle bg-primary text-white d-flex align-items-center justify-content-center me-3 shadow-sm" style="width: 50px; height: 50px; font-size: 1.2rem; font-weight: bold;">${initial}</div>
                                <div><h6 class="fw-bold text-dark mb-0">${s.supplierName}</h6><small class="text-muted">${s.contactPerson || '-'}</small></div>
                            </div>
                            <div class="dropdown">
                                <button class="btn btn-light btn-sm rounded-circle" type="button" data-bs-toggle="dropdown"><i class="fas fa-ellipsis-v text-muted"></i></button>
                                <ul class="dropdown-menu border-0 shadow">
                                    <li><a class="dropdown-item small" href="#"><i class="fas fa-pen me-2 text-primary"></i>Düzenle</a></li>
                                    <li><a class="dropdown-item small text-danger" href="#" onclick="deleteSupplier('${s.id}')"><i class="fas fa-trash me-2"></i>Sil</a></li>
                                </ul>
                            </div>
                        </div>
                        <div class="mt-2">
                            <div class="d-flex align-items-center mb-2"><span class="contact-icon me-2"><i class="fas fa-phone-alt small"></i></span><span class="small text-muted">${s.phoneNumber || '-'}</span></div>
                            <div class="d-flex align-items-center"><span class="contact-icon me-2"><i class="fas fa-envelope small"></i></span><span class="small text-muted text-truncate" style="max-width: 200px;">${s.email || '-'}</span></div>
                        </div>
                        <div class="mt-3 pt-3 border-top"><small class="text-muted d-block mb-1" style="font-size: 0.7rem;">ADRES</small><p class="small text-dark mb-0 text-truncate">${s.address || '-'}</p></div>
                    </div>
                </div>`;
            });
        } catch (e) { console.error(e); }
    }

    // 3. Takvim ve Planlama
    async function initCalendar() {
        const calendarEl = document.getElementById('supplierCalendar');
        if (!calendarEl) return;

        if (calendarInit && calendar) { calendar.render(); return; }

        try {
            const response = await fetch('/api/DeliveryRules', { headers: { 'Authorization': `Bearer ${token}` } });
            // Veya /api/Suppliers/calendar

            let eventsData = [];

            if (response.ok) {
                const apiData = await response.json();

                apiData.forEach(item => {
                    // Ortak Özellikler
                    const baseEvent = {
                        id: item.id,
                        title: item.title || item.ruleName,
                        backgroundColor: item.calendarColor || item.color || '#0d6efd',
                        borderColor: item.calendarColor || item.color || '#0d6efd',
                        textColor: '#fff',
                        extendedProps: {
                            description: item.description,
                            supplierId: item.supplierId,
                            leadTime: item.leadTime || item.leadTimeDays,
                            frequency: item.frequency,
                            interval: item.interval,
                            dayOfMonth: item.dayOfMonth,
                            rawStartDate: item.startDate,
                            rawEndDate: item.endDate
                        }
                    };

                    // SENARYO A: Backend'den Kesin Tarih Gelmişse (Aylık Planlar İçin)
                    // (Backend Handler'ında "while" döngüsü ile tek tek tarih üretmiştik)
                    if (item.start) {
                        eventsData.push({
                            ...baseEvent,
                            start: item.start, // Örn: "2025-05-15T14:30:00"
                            allDay: false
                        });
                    }
                    // SENARYO B: Haftalık Tekrar (FullCalendar recurring özelliği)
                    else if (item.daysOfWeek) {
                        eventsData.push({
                            ...baseEvent,
                            daysOfWeek: Array.isArray(item.daysOfWeek) ? item.daysOfWeek : item.daysOfWeek.split(',').map(Number),
                            startTime: item.startTime || (item.arrivalTime ? item.arrivalTime.substring(0, 5) : '09:00'),
                            startRecur: item.startRecur || (item.startDate ? item.startDate.split('T')[0] : null),
                            endRecur: item.endRecur || (item.endDate ? new Date(new Date(item.endDate).getTime() + 86400000).toISOString().split('T')[0] : null)
                        });
                    }
                    // SENARYO C: Tek Seferlik Plan
                    else if (item.startDate) {
                        eventsData.push({
                            ...baseEvent,
                            start: item.startDate,
                            allDay: true // Veya saat varsa false
                        });
                    }
                });
            }

            calendar = new FullCalendar.Calendar(calendarEl, {
                initialView: 'dayGridMonth',
                height: 600,
                headerToolbar: { left: 'prev,next today', center: 'title', right: 'dayGridMonth,listWeek' },
                locale: 'tr',
                events: eventsData,

                eventClick: function (info) {
                    selectedEventId = info.event.id;
                    selectedEventData = info.event;
                    openModal('actionChoiceModal');
                },

                eventDidMount: function (info) {
                    info.el.title = info.event.title;
                }
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
                data.forEach(s => {
                    const opt = document.createElement('option');
                    opt.value = s.id;
                    opt.text = s.supplierName;
                    select.appendChild(opt);
                });
            }
        } catch (e) { console.error(e); }
    }

    // --- FORM İŞLEMLERİ ---

    window.openEditModal = function () {
        hideModal('actionChoiceModal');
        resetScheduleForm();
        document.getElementById('editRuleId').value = selectedEventId;

        document.querySelector('#addScheduleModal .modal-title').innerText = 'Planı Düzenle';
        const saveBtn = document.querySelector('#addScheduleModal .btn-primary');
        saveBtn.innerText = "Güncelle";

        const event = selectedEventData;
        const props = event.extendedProps;

        document.getElementById('ruleName').value = event.title;
        document.getElementById('calendarColor').value = event.backgroundColor;

        // Tarih - RAW verilerden al
        if (props.rawStartDate) {
            document.getElementById('startDate').value = props.rawStartDate.split('T')[0];
        } else if (event.start) {
            document.getElementById('startDate').value = event.start.toISOString().split('T')[0];
        }

        if (props.rawEndDate) {
            document.getElementById('endDate').value = props.rawEndDate.split('T')[0];
        } else if (event.end) {
            document.getElementById('endDate').value = event.end.toISOString().split('T')[0];
        }

        // Saat
        let timeVal = "09:00";
        if (event.start) {
            timeVal = event.start.toTimeString().substring(0, 5);
        }
        document.getElementById('arrivalTime').value = timeVal;

        if (props.supplierId) document.getElementById('scheduleSupplierSelect').value = props.supplierId;
        document.getElementById('leadTime').value = props.leadTime || 1;
        document.getElementById('interval').value = props.interval || 1;

        // Sıklık Ayarı
        const freq = props.frequency || 1;
        document.getElementById('frequencySelect').value = freq;
        toggleFrequencyOptions();

        // Checkboxları Temizle
        document.querySelectorAll('.day-check').forEach(cb => cb.checked = false);

        if (freq === 1) { // Haftalık
            const daysOfWeek = props.daysOfWeek;
            let days = [];

            if (typeof daysOfWeek === 'string') {
                days = daysOfWeek.split(',').map(d => parseInt(d.trim()));
            } else if (Array.isArray(daysOfWeek)) {
                days = daysOfWeek.map(d => parseInt(d));
            }

            days.forEach(day => {
                const cb = document.getElementById('day' + day);
                if (cb) cb.checked = true;
            });
        } else if (freq === 2) { // Aylık - ÇOK GÜN
            const daysOfMonth = props.daysOfMonth;
            let daysStr = '';

            if (typeof daysOfMonth === 'string') {
                daysStr = daysOfMonth;
            } else if (Array.isArray(daysOfMonth)) {
                daysStr = daysOfMonth.join(',');
            } else if (typeof daysOfMonth === 'number') {
                daysStr = daysOfMonth.toString();
            }

            document.getElementById('daysOfMonth').value = daysStr;
        }

        setTimeout(() => { openModal('addScheduleModal'); }, 200);
    };

    window.resetScheduleForm = function () {
        document.getElementById('scheduleForm').reset();
        document.getElementById('editRuleId').value = "";
        document.querySelector('#addScheduleModal .modal-title').innerText = 'Yeni Teslimat Planı';
        const saveBtn = document.querySelector('#addScheduleModal .btn-primary');
        saveBtn.innerText = "Planı Kaydet";
        document.getElementById('frequencySelect').value = "1";
        toggleFrequencyOptions();
    };

    window.toggleFrequencyOptions = function () {
        const freq = parseInt(document.getElementById('frequencySelect').value);
        const weeklyDiv = document.getElementById('weeklyOptions');
        const monthlyDiv = document.getElementById('monthlyOptions');

        if (freq === 1) { // Weekly
            weeklyDiv.classList.remove('d-none');
            monthlyDiv.classList.add('d-none');
        } else { // Monthly
            weeklyDiv.classList.add('d-none');
            monthlyDiv.classList.remove('d-none');
        }
    };

    // --- KAYDETME (ÇOK GÜN DESTEĞİ) ---
    window.saveSchedule = async function () {
        const editId = document.getElementById('editRuleId').value;
        const isUpdate = !!editId;

        // Verileri Al
        const ruleName = document.getElementById('ruleName').value;
        const startDate = document.getElementById('startDate').value;
        const supplierId = document.getElementById('scheduleSupplierSelect').value;
        const timeVal = document.getElementById('arrivalTime').value;
        const frequency = parseInt(document.getElementById('frequencySelect').value);
        const interval = parseInt(document.getElementById('interval').value) || 1;

        if (!ruleName || !startDate || !supplierId) {
            alert("⚠️ Lütfen Plan Adı, Başlangıç Tarihi ve Tedarikçi alanlarını doldurun.");
            return;
        }

        // Sıklık kontrolü
        if (frequency === 1) { // Haftalık
            const selectedDays = Array.from(document.querySelectorAll('.day-check:checked'));
            if (selectedDays.length === 0) {
                alert("⚠️ Haftalık planlama için en az bir gün seçmelisiniz.");
                return;
            }
        } else if (frequency === 2) { // Aylık - ÇOK GÜN KONTROLÜ
            const daysOfMonthInput = document.getElementById('daysOfMonth').value.trim();
            if (!daysOfMonthInput) {
                alert("⚠️ Lütfen ayın günlerini girin (örn: 4,7,31).");
                return;
            }

            // Virgülle ayrılmış günleri kontrol et
            const days = daysOfMonthInput.split(',').map(d => parseInt(d.trim())).filter(d => !isNaN(d));
            if (days.length === 0) {
                alert("⚠️ Lütfen geçerli gün numaraları girin (1-31 arası).");
                return;
            }

            const invalidDays = days.filter(d => d < 1 || d > 31);
            if (invalidDays.length > 0) {
                alert("⚠️ Geçersiz gün numaraları: " + invalidDays.join(', ') + ". Gün numaraları 1-31 arasında olmalıdır.");
                return;
            }
        }

        // Payload Oluştur
        const payload = {
            ruleName: ruleName,
            startDate: startDate,
            endDate: document.getElementById('endDate').value || null,
            frequency: frequency,
            interval: interval,
            leadTimeDays: parseInt(document.getElementById('leadTime').value) || 0,
            calendarColor: document.getElementById('calendarColor').value,
            arrivalTime: (timeVal || "09:00") + ":00",
            supplierId: supplierId
        };

        if (isUpdate) {
            payload.id = editId;
        }

        // --- ÇOK GÜN DESTEĞİ: Sıklığa Göre Veri Gönder ---
        if (frequency === 1) { // Haftalık
            const selectedDays = Array.from(document.querySelectorAll('.day-check:checked')).map(cb => cb.value);
            payload.daysOfWeek = selectedDays.join(',');
            payload.daysOfMonth = null; // Aylık verisini temizle
        } else if (frequency === 2) { // Aylık - ÇOK GÜN
            const daysOfMonthInput = document.getElementById('daysOfMonth').value.trim();
            // Boşlukları temizle ve virgülle ayır
            const cleanedDays = daysOfMonthInput.split(',')
                .map(d => parseInt(d.trim()))
                .filter(d => !isNaN(d) && d >= 1 && d <= 31)
                .join(',');

            payload.daysOfMonth = cleanedDays;
            payload.daysOfWeek = null; // Haftalık verisini temizle
        }

        const method = isUpdate ? 'PUT' : 'POST';
        const url = '/api/DeliveryRules';
        const btn = document.querySelector('#addScheduleModal .btn-primary');
        const originalText = btn.innerHTML;

        btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> İşleniyor...';
        btn.disabled = true;

        try {
            const res = await fetch(url, {
                method: method,
                headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });

            if (res.ok) {
                alert(`✅ Plan başarıyla ${isUpdate ? "güncellendi" : "oluşturuldu"}!`);
                hideModal('addScheduleModal');
                calendarInit = false;
                await initCalendar();
            } else {
                const err = await res.json();
                console.error("Backend Hatası:", err);
                alert("❌ Hata: " + (err.message || err.title || "İşlem başarısız."));
            }
        } catch (e) {
            console.error("Fetch Hatası:", e);
            alert("❌ Sunucu ile iletişim kurulamadı: " + e.message);
        } finally {
            btn.innerHTML = originalText;
            btn.disabled = false;
        }
    };

    // --- SİLME ---
    window.confirmDelete = function () {
        hideModal('actionChoiceModal');
        if (confirm("Bu planı kalıcı olarak silmek istediğinize emin misiniz?")) {
            deleteSchedule(selectedEventId);
        }
    };

    async function deleteSchedule(id) {
        try {
            const res = await fetch(`/api/DeliveryRules/${id}`, {
                method: 'DELETE',
                headers: { 'Authorization': `Bearer ${token}` }
            });

            if (res.ok) {
                alert("🗑️ Plan silindi.");
                calendarInit = false;
                await initCalendar();
            } else {
                alert("❌ Silme işlemi başarısız.");
            }
        } catch (e) {
            console.error(e);
            alert("❌ Silme sırasında hata: " + e.message);
        }
    }

    // Tedarikçi Kaydet
    window.saveSupplier = async function () {
        const payload = {
            supplierName: document.getElementById('supplierName').value,
            contactPerson: document.getElementById('contactPerson').value,
            email: document.getElementById('email').value,
            phoneNumber: document.getElementById('phoneNumber').value,
            address: document.getElementById('address').value
        };
        if (!payload.supplierName) { alert("⚠️ Şirket adı zorunludur."); return; }
        try {
            const res = await fetch('/api/Suppliers', {
                method: 'POST',
                headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });
            if (res.ok) {
                alert("✅ Tedarikçi başarıyla eklendi!");
                hideModal('addSupplierModal');
                await loadSuppliers();
            } else {
                const err = await res.json();
                alert("❌ Hata: " + (err.message || "İşlem başarısız."));
            }
        } catch (e) {
            console.error(e);
            alert("❌ Sunucu ile iletişim kurulamadı.");
        }
    };

    window.filterSuppliers = function () {
        const text = document.getElementById('searchInput').value.toLowerCase();
        const container = document.getElementById('suppliersContainer');
        if (!container) return;

        const filtered = allSuppliers.filter(s =>
            s.supplierName.toLowerCase().includes(text) ||
            (s.contactPerson && s.contactPerson.toLowerCase().includes(text)) ||
            (s.email && s.email.toLowerCase().includes(text))
        );

        container.innerHTML = '';
        if (filtered.length === 0) {
            container.innerHTML = '<div class="col-12 text-center text-muted py-5">Arama sonucu bulunamadı.</div>';
            return;
        }

        filtered.forEach(s => {
            const initial = s.supplierName ? s.supplierName.charAt(0).toUpperCase() : "?";
            container.innerHTML += `
            <div class="col-md-6 col-lg-4">
                <div class="card border-0 bg-white shadow-sm p-4 h-100 supplier-card">
                    <div class="d-flex justify-content-between align-items-start mb-3">
                        <div class="d-flex align-items-center">
                            <div class="rounded-circle bg-primary text-white d-flex align-items-center justify-content-center me-3 shadow-sm" style="width: 50px; height: 50px; font-size: 1.2rem; font-weight: bold;">${initial}</div>
                            <div><h6 class="fw-bold text-dark mb-0">${s.supplierName}</h6><small class="text-muted">${s.contactPerson || '-'}</small></div>
                        </div>
                        <div class="dropdown">
                            <button class="btn btn-light btn-sm rounded-circle" type="button" data-bs-toggle="dropdown"><i class="fas fa-ellipsis-v text-muted"></i></button>
                            <ul class="dropdown-menu border-0 shadow">
                                <li><a class="dropdown-item small" href="#"><i class="fas fa-pen me-2 text-primary"></i>Düzenle</a></li>
                                <li><a class="dropdown-item small text-danger" href="#" onclick="deleteSupplier('${s.id}')"><i class="fas fa-trash me-2"></i>Sil</a></li>
                            </ul>
                        </div>
                    </div>
                    <div class="mt-2">
                        <div class="d-flex align-items-center mb-2"><span class="contact-icon me-2"><i class="fas fa-phone-alt small"></i></span><span class="small text-muted">${s.phoneNumber || '-'}</span></div>
                        <div class="d-flex align-items-center"><span class="contact-icon me-2"><i class="fas fa-envelope small"></i></span><span class="small text-muted text-truncate" style="max-width: 200px;">${s.email || '-'}</span></div>
                    </div>
                    <div class="mt-3 pt-3 border-top"><small class="text-muted d-block mb-1" style="font-size: 0.7rem;">ADRES</small><p class="small text-dark mb-0 text-truncate">${s.address || '-'}</p></div>
                </div>
            </div>`;
        });
    };

})();