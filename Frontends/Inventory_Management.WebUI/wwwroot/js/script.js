// Menü Toggle İşlemi
var el = document.getElementById("wrapper");
var toggleButton = document.getElementById("menu-toggle");

toggleButton.onclick = function () {
    el.classList.toggle("toggled");
};

// ÇIKIŞ YAP (LOGOUT) FONKSİYONU
function logout() {
    localStorage.removeItem('jwtToken');
    localStorage.removeItem('userName');
    window.location.href = 'login.html';
}

// --- 1. GRAFİK (CHART.JS) ---
var ctx = document.getElementById('myChart').getContext('2d');
var myChart = new Chart(ctx, {
    type: 'line',
    data: {
        labels: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov'],
        datasets: [{
            label: 'Income',
            data: [450, 520, 480, 590, 610, 680, 720, 690, 750, 810, 780],
            borderColor: '#0d6efd',
            backgroundColor: 'rgba(13, 110, 253, 0.1)',
            borderWidth: 2,
            tension: 0.4,
            pointBackgroundColor: '#ffffff',
            pointBorderColor: '#0d6efd',
            pointRadius: 4
        },
        {
            label: 'Outcome',
            data: [320, 380, 420, 390, 450, 480, 520, 490, 540, 580, 620],
            borderColor: '#dc3545',
            backgroundColor: 'rgba(220, 53, 69, 0.1)',
            borderWidth: 2,
            tension: 0.4,
            pointBackgroundColor: '#ffffff',
            pointBorderColor: '#dc3545',
            pointRadius: 4
        }]
    },
    options: {
        responsive: true,
        plugins: {
            legend: { position: 'top', align: 'start', labels: { usePointStyle: true, boxWidth: 8 } }
        },
        scales: {
            y: { beginAtZero: true, grid: { borderDash: [2, 2], drawBorder: false } },
            x: { grid: { display: false } }
        }
    }
});

// --- 2. TAKVİM (FULLCALENDAR) ---
document.addEventListener('DOMContentLoaded', function() {
    var calendarEl = document.getElementById('calendar');

    var calendar = new FullCalendar.Calendar(calendarEl, {
        initialView: 'dayGridMonth',
        headerToolbar: {
            left: 'prev,next today',
            center: 'title',
            right: 'dayGridMonth,timeGridWeek,listWeek'
        },
        height: 650,
        themeSystem: 'bootstrap5',
        
        events: [
            {
                title: 'TechSupply Co. (50 Mouse)',
                start: '2025-11-20',
                backgroundColor: '#0d6efd',
                borderColor: '#0d6efd'
            },
            {
                title: 'Cable World (100 USB-C)',
                start: '2025-11-22',
                backgroundColor: '#198754',
                borderColor: '#198754'
            },
            {
                title: 'Vision Tech (Webcams)',
                start: '2025-11-25',
                backgroundColor: '#ffc107',
                borderColor: '#ffc107',
                textColor: '#000'
            }
        ],
        eventClick: function(info) {
            alert('Teslimat Detayı:\n' + info.event.title);
        }
    });

    calendar.render();
});