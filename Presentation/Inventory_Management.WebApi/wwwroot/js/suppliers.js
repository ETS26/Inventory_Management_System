(function () {
    const token = localStorage.getItem('jwtToken');
    let allSuppliers = [];
    let calendarInit = false;
    let currentView = 'list';

    let calendar;
    let selectedEventId = null;
    let selectedEventData = null;

    document.addEventListener('DOMContentLoaded', function () {
        if (!token) { window.location.href = 'login.html'; return; }
        loadSuppliers();
        loadDropdownForSchedule();
    });

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
        if (modalEl) bootstrap.Modal.getInstance(modalEl)?.hide();
    }

    async function loadSuppliers() {
        const container = document.getElementById('suppliersContainer');
        if (!container) return;

        try {
            const response = await fetch('/api/Suppliers', { headers: { 'Authorization': `Bearer ${token}` } });
            if (!response.ok) throw new Error("Hata");
            allSuppliers = await response.json();
            renderSuppliers(allSuppliers);
        } catch (e) { console.error(e); }
    }

    function renderSuppliers(data) {
        const container = document.getElementById('suppliersContainer');
        container.innerHTML = '';
        data.forEach(s => {
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
                                <li><a class="dropdown-item small" href="#" onclick="editSupplier('${s.id}')"><i class="fas fa-pen me-2 text-primary"></i>Düzenle</a></li>
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
    }

    async function initCalendar() {
        const calendarEl = document.getElementById('supplierCalendar');
        if (!calendarEl) return;

        if (calendarInit && calendar) { calendar.render(); return; }

        try {
            const response = await fetch('/api/Suppliers/calendar', {
                headers: { 'Authorization': `Bearer ${token}` },
                cache: 'no-cache' // Tarayıcı önbelleğini devre dışı bırak
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

                    // ✅ 1. Backend hesaplamış tarih varsa (Aylık VEYA Haftalık interval>1)
                    if (item.start) {
                        return [{ ...baseEvent, start: item.start, allDay: false }];
                    }

                    // ✅ 2. Haftalık Recurring (interval=1, Backend daysOfWeek göndermiş)
                    if (item.frequency === 1 && item.daysOfWeek) {
                        return [{
                            ...baseEvent,
                            daysOfWeek: Array.isArray(item.daysOfWeek)
                                ? item.daysOfWeek
                                : item.daysOfWeek.split(',').map(Number),
                            startRecur: item.startDate ? item.startDate.split('T')[0] : null,
                            endRecur: item.endDate ? item.endDate.split('T')[0] : null,
                            startTime: item.arrivalTime ? item.arrivalTime.substring(0, 5) : '09:00'
                        }];
                    }

                    // ✅ 3. Aylık Fallback (Backend hesaplamamışsa - normalde olmamalı)
                    if (item.frequency === 2) {
                        const daysField = item.daysOfMonth || item.DaysOfMonth || item.dayOfMonth;
                        if (!daysField) return [];

                        let daysArray = [];
                        if (typeof daysField === 'string') {
                            daysArray = daysField.split(',').map(d => parseInt(d.trim())).filter(d => !isNaN(d));
                        } else if (Array.isArray(daysField)) {
                            daysArray = daysField;
                        } else if (typeof daysField === 'number') {
                            daysArray = [daysField];
                        }

                        if (daysArray.length === 0) return [];

                        const events = [];
                        const startDate = new Date(item.startDate);
                        const endDate = item.endDate ? new Date(item.endDate) : new Date(new Date().setFullYear(new Date().getFullYear() + 1));
                        const timeStr = item.arrivalTime ? item.arrivalTime.substring(0, 5) : '09:00';
                        const interval = (item.interval && item.interval > 0) ? parseInt(item.interval) : 1;

                        let currentDate = new Date(startDate.getFullYear(), startDate.getMonth(), 1);

                        while (currentDate <= endDate) {
                            const year = currentDate.getFullYear();
                            const month = currentDate.getMonth();
                            const lastDayOfMonth = new Date(year, month + 1, 0).getDate();
                            const processedDays = new Set();

                            daysArray.forEach(targetDay => {
                                const actualDay = Math.min(targetDay, lastDayOfMonth);

                                if (!processedDays.has(actualDay)) {
                                    const eventDate = new Date(year, month, actualDay);

                                    if (eventDate >= startDate && eventDate < endDate) {
                                        const dateString = eventDate.getFullYear() + '-' +
                                            String(eventDate.getMonth() + 1).padStart(2, '0') + '-' +
                                            String(eventDate.getDate()).padStart(2, '0');

                                        events.push({
                                            ...baseEvent,
                                            start: `${dateString}T${timeStr}:00`,
                                            allDay: false
                                        });
                                        processedDays.add(actualDay);
                                    }
                                }
                            });

                            currentDate.setMonth(currentDate.getMonth() + interval);
                        }
                        return events;
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

        if (props.rawStartDate) document.getElementById('startDate').value = props.rawStartDate.split('T')[0];
        else if (event.start) document.getElementById('startDate').value = new Date(event.start.getTime() - (event.start.getTimezoneOffset() * 60000)).toISOString().split('T')[0];
        else if (event.startRecur) document.getElementById('startDate').value = event.startRecur;

        if (props.rawEndDate) document.getElementById('endDate').value = props.rawEndDate.split('T')[0];

        let timeVal = "09:00";
        if (event.start) {
            const h = String(event.start.getHours()).padStart(2, '0');
            const m = String(event.start.getMinutes()).padStart(2, '0');
            timeVal = `${h}:${m}`;
        }
        document.getElementById('arrivalTime').value = timeVal;

        if (props.supplierId) document.getElementById('scheduleSupplierSelect').value = props.supplierId;
        document.getElementById('leadTime').value = props.leadTime || 1;
        document.getElementById('interval').value = props.interval || 1;

        const freq = props.frequency || 1;
        document.getElementById('frequencySelect').value = freq;
        toggleFrequencyOptions();

        document.querySelectorAll('.day-check').forEach(cb => cb.checked = false);

        if (freq === 1) {
            // Haftalık planlar için günler, interval=1 ise (sayı array'i olarak) daysOfWeek, 
            // interval>1 ise (string olarak) daysOfMonth içinde olabilir.
            const days = props.daysOfWeek || props.daysOfMonth || [];
            const daysArr = (typeof days === 'string') ? days.split(',') : days;

            if (Array.isArray(daysArr)) {
                daysArr.forEach(d => {
                    // d bir sayı (interval=1) veya string (interval>1) olabilir.
                    // null, undefined, boş string gibi değerleri atla ama 0'ı (Pazar) atlama.
                    if (d !== null && d !== undefined && d !== '') {
                        const dayStr = String(d).trim();
                        const cb = document.getElementById('day' + dayStr);
                        if (cb) cb.checked = true;
                    }
                });
            }
        } else if (freq === 2) {
            let val = props.daysOfMonth || "";
            if (Array.isArray(val)) val = val.join(',');
            document.getElementById('daysOfMonth').value = val;
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

        if (freq === 1) {
            weeklyDiv.classList.remove('d-none');
            monthlyDiv.classList.add('d-none');
        } else {
            weeklyDiv.classList.add('d-none');
            monthlyDiv.classList.remove('d-none');
        }
    };

    window.saveSchedule = async function () {
        const editId = document.getElementById('editRuleId').value;
        const isUpdate = !!editId;

        const ruleName = document.getElementById('ruleName').value;
        const startDate = document.getElementById('startDate').value;
        const supplierId = document.getElementById('scheduleSupplierSelect').value;
        const timeVal = document.getElementById('arrivalTime').value;
        const frequency = parseInt(document.getElementById('frequencySelect').value);
        const interval = parseInt(document.getElementById('interval').value) || 1;

        if (!ruleName || !startDate || !supplierId) {
            alert("⚠️ Lütfen zorunlu alanları doldurun.");
            return;
        }

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

        if (isUpdate) payload.id = editId;

        if (frequency === 1) {
            const selectedDays = Array.from(document.querySelectorAll('.day-check:checked')).map(cb => cb.value);
            payload.daysOfWeek = selectedDays.join(',');
            payload.daysOfMonth = null;
        } else if (frequency === 2) {
            const domInput = document.getElementById('daysOfMonth');
            const val = domInput ? domInput.value.trim() : "";

            if (!val) { alert("Lütfen ayın günlerini girin."); return; }
            payload.daysOfMonth = val;
            payload.daysOfWeek = null;
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
                alert(`✅ İşlem Başarılı!`);
                hideModal('addScheduleModal');
                calendarInit = false;
                initCalendar();
            } else {
                const err = await res.json();
                alert("❌ Hata: " + (err.message || "İşlem başarısız."));
            }
        } catch (e) {
            console.error("Hata:", e);
            alert("Sunucu ile iletişim kurulamadı.");
        } finally {
            btn.innerHTML = originalText;
            btn.disabled = false;
        }
    };

    window.confirmDelete = function () {
        hideModal('actionChoiceModal');
        if (confirm("Bu planı silmek istediğinize emin misiniz?")) {
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
                initCalendar();
            } else {
                alert("Silme başarısız.");
            }
        } catch (e) { console.error(e); }
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
        const payload = {
            id: isUpdate ? id : undefined,
            supplierName: document.getElementById('supplierName').value,
            contactPerson: document.getElementById('contactPerson').value,
            email: document.getElementById('email').value,
            phoneNumber: document.getElementById('phoneNumber').value,
            address: document.getElementById('address').value
        };

        if (!payload.supplierName) { alert("Şirket adı zorunludur."); return; }

        const method = isUpdate ? 'PUT' : 'POST';
        let url = '/api/Suppliers';

        try {
            const res = await fetch(url, {
                method: method,
                headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });
            if (res.ok) {
                alert("Başarılı!");
                hideModal('addSupplierModal');
                loadSuppliers();
            }
        } catch (e) { console.error(e); }
    };

    window.deleteSupplier = async function (id) {
        if (!confirm("Silmek istediğinize emin misiniz?")) return;
        try {
            await fetch(`/api/Suppliers/${id}`, { method: 'DELETE', headers: { 'Authorization': `Bearer ${token}` } });
            loadSuppliers();
        } catch (e) { console.error(e); }
    };

    window.filterSuppliers = function () {
        const text = document.getElementById('searchInput').value.toLowerCase();
        const filtered = allSuppliers.filter(s => s.supplierName.toLowerCase().includes(text));
        renderSuppliers(filtered);
    };

})();