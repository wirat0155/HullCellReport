let currentPage = 1;
const pageSize = 10;
let totalRecords = 0;
let currentSort = 'desc'; // desc = ล่าสุดก่อน, asc = เก่าสุดก่อน
let currentSortColumn = 'createdDate'; // Default sort column
let filters = {
    startDate: '',
    endDate: '',
    status: '',
    checkStatus: ''
};

// Load data when page loads
document.addEventListener('DOMContentLoaded', function () {
    const urlParams = new URLSearchParams(window.location.search);
    if (urlParams.has('startDate') || urlParams.has('status') || urlParams.has('checkStatus')) {
        initializeFiltersFromUrl(urlParams);
    } else {
        setDefaultDates();
    }
    loadData(currentPage);
});

function initializeFiltersFromUrl(params) {
    const startDate = params.get('startDate') || '';
    const endDate = params.get('endDate') || '';
    const status = params.get('status') || '';
    const checkStatus = params.get('checkStatus') || '';
    const dateRange = params.get('dateRange') || '';

    // Set UI elements
    if (startDate) document.getElementById('startDate').value = startDate;
    if (endDate) document.getElementById('endDate').value = endDate;
    if (status) document.getElementById('statusFilter').value = status;
    if (checkStatus) document.getElementById('checkStatusFilter').value = checkStatus;

    if (dateRange) {
        document.getElementById('dateRangeSelect').value = dateRange;
        // If custom/all, we might need to adjust UI if there was one, but standard select is fine
    } else if (startDate && endDate) {
        document.getElementById('dateRangeSelect').value = 'custom';
    }

    // Set filter object
    filters.startDate = startDate;
    filters.endDate = endDate;
    filters.status = status;
    filters.checkStatus = checkStatus;
}

function setDefaultDates() {
    const today = new Date();
    const endDate = today.toISOString().split('T')[0];
    document.getElementById('endDate').value = endDate;

    // Set default to today
    document.getElementById('startDate').value = endDate;
    document.getElementById('dateRangeSelect').value = 'today';

    filters.startDate = endDate;
    filters.endDate = endDate;
}

async function loadData(page) {
    try {
        showLoader();

        const params = new URLSearchParams({
            page: page,
            pageSize: pageSize,
            sortOrder: currentSort,
            sortColumn: currentSortColumn,
            startDate: filters.startDate || '',
            endDate: filters.endDate || '',
            status: filters.status || '',
            checkStatus: filters.checkStatus || ''
        });

        const response = await fetch(`${basePath}/Home/GetHullCellReports?${params}`);
        const { success, data, total, todaySamplingCount, todayDate, uncheckedCount } = await response.json();

        if (success) {
            totalRecords = total;
            renderTable(data, page);
            renderPagination(page);
            updateSortIcon();
            updateSamplingCountCard(todaySamplingCount, todayDate);
            updateUncheckedCountCard(uncheckedCount);
        } else {
            showNoData();
            hideSamplingCountCard();
            hideUncheckedCountCard();
        }
    } catch (error) {
        console.error('Error loading data:', error);
        showNoData();
        hideSamplingCountCard();
        hideUncheckedCountCard();
    } finally {
        hideLoader();
    }
}

function renderTable(data, page) {
    const tbody = document.getElementById('tableBody');
    const noDataMessage = document.getElementById('noDataMessage');
    const paginationContainer = document.getElementById('paginationContainer');

    if (!data || data.length === 0) {
        showNoData();
        return;
    }

    tbody.innerHTML = '';
    noDataMessage.style.display = 'none';
    paginationContainer.style.display = 'flex';

    data.forEach((item, index) => {
        const rowNumber = (page - 1) * pageSize + index + 1;
        const date = new Date(item.createdDate);
        const dateStr = date.toLocaleDateString('th-TH', { year: 'numeric', month: '2-digit', day: '2-digit' });
        const timeStr = date.toLocaleTimeString('th-TH', { hour: '2-digit', minute: '2-digit' });

        // Tank status badges
        const tank208N = item.tank208N || 'Open';
        const tank208T = item.tank208T || 'Open';
        const tank208A = item.tank208A || 'Open';
        const tank208B = item.tank208B || 'Open';

        const badge208N = `<span class="status-badge status-${tank208N.toLowerCase()}">${tank208N}</span>`;
        const badge208T = `<span class="status-badge status-${tank208T.toLowerCase()}">${tank208T}</span>`;
        const badge208A = `<span class="status-badge status-${tank208A.toLowerCase()}">${tank208A}</span>`;
        const badge208B = `<span class="status-badge status-${tank208B.toLowerCase()}">${tank208B}</span>`;

        const statusClass = item.status === 'C' ? 'status-complete' : 'status-draft';
        const statusText = item.status === 'C' ? 'Complete' : 'Draft';

        // Check column content
        let checkContent = '';
        if (item.checkByStatus === 'System') {
            checkContent = 'System';
        } else if (item.checkByStatus === 'Pending') {
            checkContent = '<span class="status-badge" style="background-color: #fff3cd; color: #856404;">ยังไม่ผ่านการตรวจสอบ</span><div style="font-size: 11px; margin-top: 4px; color: #6c757d;"><i class="fas fa-info-circle"></i> กด <i class="fas fa-eye"></i> เพื่อตรวจสอบ</div>';
        } else {
            // Checked
            let checkDateStr = '';
            let checkTimeStr = '';
            if (item.checkDate) {
                const checkDateObj = new Date(item.checkDate);
                checkDateStr = checkDateObj.toLocaleDateString('th-TH', { year: 'numeric', month: '2-digit', day: '2-digit' });
                checkTimeStr = checkDateObj.toLocaleTimeString('th-TH', { hour: '2-digit', minute: '2-digit' });
            }
            checkContent = `<div>${item.checkByName}</div><div style="font-size: 11px; color: #6c757d;">${checkDateStr} ${checkTimeStr}</div>`;
        }

        // Show edit and delete buttons only if status is not "C" (Complete)
        let actionButtons = '';
        if (item.status !== 'C') {
            actionButtons = `
                <div class="action-buttons">
                    <button class="btn-view" onclick="viewReport('${item.id}')">
                        <i class="fas fa-eye"></i> ดู
                    </button>
                    <button class="btn-edit" onclick="editReport('${item.id}')">
                        <i class="fas fa-edit"></i> แก้ไข
                    </button>
                    <button class="btn-delete" onclick="deleteReport('${item.id}')">
                        <i class="fas fa-trash"></i> ลบ
                    </button>
                </div>
            `;
        } else {
            actionButtons = `
                <div class="action-buttons">
                    <button class="btn-view" onclick="viewReport('${item.id}')">
                        <i class="fas fa-eye"></i> ดู
                    </button>
                </div>
            `;
        }

        // Export button - only show for Complete status
        let exportButton = '';
        if (item.status === 'C') {
            exportButton = `
                <button class="btn-export" onclick="exportPDF('${item.id}')">
                    <i class="fas fa-file-pdf"></i> Export
                </button>
            `;
        }

        const row = `
            <tr>
                <td title="${item.id}">${item.id}</td>
                <td>
                    <div>${dateStr}</div>
                    <div style="font-size: 12px; color: #6c757d;">${timeStr}</div>
                </td>
                <td>${item.createdByName || 'Unknown'}</td>
                <td>${badge208N}</td>
                <td>${badge208T}</td>
                <td>${badge208A}</td>
                <td>${badge208B}</td>
                <td><span class="status-badge ${statusClass}">${statusText}</span></td>
                <td>${checkContent}</td>
                <td>
                    ${actionButtons}
                </td>
                <td>
                    ${exportButton}
                </td>
            </tr>
        `;
        tbody.innerHTML += row;
    });
}

function renderPagination(page) {
    const totalPages = Math.ceil(totalRecords / pageSize);
    const pagination = document.getElementById('pagination');
    const pageInfo = document.getElementById('pageInfo');

    if (totalPages <= 1) {
        pagination.innerHTML = '';
        pageInfo.textContent = '';
        return;
    }

    // Page info
    const start = (page - 1) * pageSize + 1;
    const end = Math.min(page * pageSize, totalRecords);
    pageInfo.textContent = `แสดง ${start}-${end} จาก ${totalRecords} รายการ`;

    // Pagination buttons
    let paginationHTML = '';

    // Previous button
    if (page > 1) {
        paginationHTML += `<li><a href="#" onclick="changePage(${page - 1}); return false;">«</a></li>`;
    } else {
        paginationHTML += `<li class="disabled"><span>«</span></li>`;
    }

    // Page numbers
    const maxVisible = 5;
    let startPage = Math.max(1, page - Math.floor(maxVisible / 2));
    let endPage = Math.min(totalPages, startPage + maxVisible - 1);

    if (endPage - startPage < maxVisible - 1) {
        startPage = Math.max(1, endPage - maxVisible + 1);
    }

    if (startPage > 1) {
        paginationHTML += `<li><a href="#" onclick="changePage(1); return false;">1</a></li>`;
        if (startPage > 2) {
            paginationHTML += `<li class="disabled"><span>...</span></li>`;
        }
    }

    for (let i = startPage; i <= endPage; i++) {
        if (i === page) {
            paginationHTML += `<li class="active"><span>${i}</span></li>`;
        } else {
            paginationHTML += `<li><a href="#" onclick="changePage(${i}); return false;">${i}</a></li>`;
        }
    }

    if (endPage < totalPages) {
        if (endPage < totalPages - 1) {
            paginationHTML += `<li class="disabled"><span>...</span></li>`;
        }
        paginationHTML += `<li><a href="#" onclick="changePage(${totalPages}); return false;">${totalPages}</a></li>`;
    }

    // Next button
    if (page < totalPages) {
        paginationHTML += `<li><a href="#" onclick="changePage(${page + 1}); return false;">»</a></li>`;
    } else {
        paginationHTML += `<li class="disabled"><span>»</span></li>`;
    }

    pagination.innerHTML = paginationHTML;
}

function changePage(page) {
    currentPage = page;
    loadData(page);
    window.scrollTo({ top: 0, behavior: 'smooth' });
}

function showNoData() {
    document.getElementById('tableBody').innerHTML = '';
    document.getElementById('noDataMessage').style.display = 'block';
    document.getElementById('paginationContainer').style.display = 'none';
}

// Filter functions
function handleQuickDateRange(value) {
    const today = new Date();
    const endDate = today.toISOString().split('T')[0];
    let startDate = '';

    switch (value) {
        case 'today':
            startDate = endDate;
            break;
        case 'week':
            const weekAgo = new Date(today);
            weekAgo.setDate(weekAgo.getDate() - 7);
            startDate = weekAgo.toISOString().split('T')[0];
            break;
        case 'month':
            const monthAgo = new Date(today);
            monthAgo.setMonth(monthAgo.getMonth() - 1);
            startDate = monthAgo.toISOString().split('T')[0];
            break;
        case 'custom':
            // Don't change dates, just let user pick
            return;
        default:
            startDate = '';
            break;
    }

    document.getElementById('startDate').value = startDate;
    document.getElementById('endDate').value = endDate;
}

function applyFilters() {
    filters.startDate = document.getElementById('startDate').value;
    filters.endDate = document.getElementById('endDate').value;
    filters.status = document.getElementById('statusFilter').value;
    filters.checkStatus = document.getElementById('checkStatusFilter').value;

    currentPage = 1;
    loadData(currentPage);
}

function resetFilters() {
    document.getElementById('dateRangeSelect').value = 'today';
    document.getElementById('statusFilter').value = '';
    document.getElementById('checkStatusFilter').value = '';
    setDefaultDates();

    filters = {
        startDate: document.getElementById('startDate').value,
        endDate: document.getElementById('endDate').value,
        status: '',
        checkStatus: ''
    };

    currentPage = 1;
    loadData(currentPage);
}

// Sort functions
function toggleSort(column) {
    if (currentSortColumn !== column) {
        currentSortColumn = column;
        currentSort = 'desc'; // Reset to desc when changing column
    } else {
        currentSort = currentSort === 'desc' ? 'asc' : 'desc';
    }
    updateSortIcon();
    loadData(currentPage);
}

function updateSortIcon() {
    // Reset all icons
    const icons = document.querySelectorAll('.sort-icon');
    icons.forEach(icon => {
        icon.className = 'fas fa-sort sort-icon';
        icon.classList.remove('active');
    });

    // Set active icon
    let iconId = '';
    if (currentSortColumn === 'createdDate' || currentSortColumn === 'date') { // handle legacy 'date'
        iconId = 'sortIconDate';
    } else if (currentSortColumn === 'checkDate') {
        iconId = 'sortIconCheckDate';
    }

    if (iconId) {
        const icon = document.getElementById(iconId);
        if (icon) {
            icon.classList.add('active');
            if (currentSort === 'asc') {
                icon.classList.remove('fa-sort');
                icon.classList.add('fa-sort-up');
            } else {
                icon.classList.remove('fa-sort');
                icon.classList.add('fa-sort-down');
            }
        }
    }
}

// View report function (read-only)
function viewReport(uuid) {
    window.location.href = `${basePath}/Home/vViewReport?uuid=${uuid}`;
}

// Edit report function
function editReport(uuid) {
    window.location.href = `${basePath}/Home/vCreateReport?uuid=${uuid}`;
}

// Update sampling count card
function updateSamplingCountCard(count, todayDate) {
    const card = document.getElementById('samplingCountCard');
    const countElement = document.getElementById('samplingCount');
    const statusBadge = document.getElementById('samplingStatusBadge');
    const dateElement = document.getElementById('todayDate');

    card.classList.add('show');
    countElement.textContent = count;

    if (todayDate) {
        dateElement.textContent = `(${todayDate})`;
    }

    if (count >= 2) {
        statusBadge.textContent = 'ครบแล้ว ✓';
        statusBadge.classList.remove('incomplete');
        statusBadge.classList.add('complete');
        card.classList.add('complete');
    } else {
        statusBadge.textContent = 'ยังไม่ครบ';
        statusBadge.classList.remove('complete');
        statusBadge.classList.add('incomplete');
        card.classList.remove('complete');
    }
}

function hideSamplingCountCard() {
    const card = document.getElementById('samplingCountCard');
    card.classList.remove('show');
}

function updateUncheckedCountCard(count) {
    const card = document.getElementById('uncheckedCountCard');
    const countElement = document.getElementById('uncheckedCount');

    // Always show the card
    card.classList.add('show');
    countElement.textContent = count;

    // Style update based on count
    if (count === 0) {
        card.classList.add('empty-state');
    } else {
        card.classList.remove('empty-state');
    }
}

function hideUncheckedCountCard() {
    const card = document.getElementById('uncheckedCountCard');
    card.classList.remove('show');
}

function filterUnchecked() {
    // Set filter to "unchecked" and apply
    document.getElementById('checkStatusFilter').value = 'unchecked';

    // Also clear other filters if they conflict? 
    // Maybe keep date range but clear status
    document.getElementById('statusFilter').value = '';

    applyFilters();
}

// Delete report function
async function deleteReport(uuid) {
    const result = await Swal.fire({
        title: 'ยืนยันการลบ',
        text: 'คุณต้องการลบรายการนี้หรือไม่?',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#f44336',
        cancelButtonColor: '#6c757d',
        confirmButtonText: 'ลบ',
        cancelButtonText: 'ยกเลิก'
    });

    if (!result.isConfirmed) {
        return;
    }

    try {
        showLoader();

        const response = await fetch(`${basePath}/Home/DeleteReport?uuid=${uuid}`, {
            method: 'DELETE'
        });

        const { success, message } = await response.json();

        if (success) {
            Swal.fire({
                title: 'สำเร็จ',
                text: message || 'ลบรายการสำเร็จ',
                icon: 'success',
                confirmButtonColor: '#00bcd4'
            }).then(() => {
                loadData(currentPage);
            });
        } else {
            Swal.fire({
                title: 'ข้อผิดพลาด',
                text: message || 'ไม่สามารถลบรายการได้',
                icon: 'error',
                confirmButtonColor: '#00bcd4'
            });
        }
    } catch (error) {
        console.error('Error deleting report:', error);
        Swal.fire({
            title: 'ข้อผิดพลาด',
            text: 'เกิดข้อผิดพลาดในการลบรายการ',
            icon: 'error',
            confirmButtonColor: '#00bcd4'
        });
    } finally {
        hideLoader();
    }
}

// Export PDF function
function exportPDF(uuid) {
    window.open(`${basePath}/Home/ExportPDF?uuid=${uuid}`, '_blank');
}
