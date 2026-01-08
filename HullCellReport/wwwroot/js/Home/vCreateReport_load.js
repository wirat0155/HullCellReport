// Load report data when page loads with UUID parameter
document.addEventListener('DOMContentLoaded', async function () {
    const urlParams = new URLSearchParams(window.location.search);
    const uuid = urlParams.get('uuid');

    if (uuid) {
        // Only run permission check if check button exists (vViewReport)
        if (document.getElementById('btn-check-report')) {
            await checkPermissionAndSetupButton(uuid);
        }
        await loadReportData(uuid);
    }
});

async function checkPermissionAndSetupButton(uuid) {
    try {
        const response = await fetch(`${basePath}/Home/GetCurrentUserPermissions`);
        const { success, canCheckReport } = await response.json();

        const btnCheck = document.getElementById('btn-check-report');
        const infoText = document.getElementById('check-permission-info');

        if (btnCheck) {
            // Set UUID to button dataset
            btnCheck.dataset.uuid = uuid;

            // Check current report status first
            // Note: We need to wait for report data to load to check status,
            // or pass status to this function. Better to wait or use global state.
            // But since loadReportData calls fetchReportData, we can check there.
            // Let's modify loadReportData to handle this or expose status.

            // Assuming the button state should be updated after data load.
            // So we will just set permission state here, 
            // and additional status check will be done in populateForm or loadReportData.

            btnCheck.dataset.canCheck = (success && canCheckReport) ? "true" : "false";

            // Initial UI state based on permission only
            if (success && canCheckReport) {
                // Enabled by permission, but might be disabled by status later
                btnCheck.disabled = false;
                if (infoText) infoText.style.display = 'none';
            } else {
                btnCheck.disabled = true;
                if (infoText) {
                    infoText.textContent = "โปรดติดต่อ FM ให้ Check รายการนี้";
                    infoText.style.display = 'block';
                }
            }
        }
    } catch (error) {
        console.error('Error checking permissions:', error);
    }
}

async function checkReport() {
    const btnCheck = document.getElementById('btn-check-report');
    if (!btnCheck || !btnCheck.dataset.uuid) return;

    // Disable button to prevent double click
    btnCheck.disabled = true;
    showLoader();

    try {
        const uuid = btnCheck.dataset.uuid;
        const response = await fetch(`${basePath}/Home/CheckReport`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ uuid: uuid })
        });

        const result = await response.json();

        if (result.success) {
            Swal.fire({
                title: 'สำเร็จ',
                text: 'ตรวจสอบรายการเรียบร้อยแล้ว',
                icon: 'success',
                confirmButtonColor: '#4CAF50'
            }).then(() => {
                location.reload();
            });
        } else {
            Swal.fire({
                title: 'เกิดข้อผิดพลาด',
                text: result.message || 'ไม่สามารถตรวจสอบรายการได้',
                icon: 'error',
                confirmButtonColor: '#f44336'
            });
            btnCheck.disabled = false; // Re-enable on failure
        }
    } catch (error) {
        console.error('Error checking report:', error);
        Swal.fire({
            title: 'เกิดข้อผิดพลาด',
            text: 'เกิดข้อผิดพลาดในการเชื่อมต่อ',
            icon: 'error',
            confirmButtonColor: '#f44336'
        });
        btnCheck.disabled = false;
    } finally {
        hideLoader();
    }
}

async function loadReportData(uuid) {
    try {
        showLoader();

        const response = await fetch(`${basePath}/Home/GetReportByUuid?uuid=${uuid}`);
        const { success, data, message } = await response.json();

        if (success && data) {
            // // Check if report is Complete
            // if (data.txt_status === 'C') {
            //     Swal.fire({
            //         title: 'ไม่สามารถแก้ไข',
            //         text: 'รายการนี้ Complete แล้ว ไม่สามารถแก้ไขได้',
            //         icon: 'warning',
            //         confirmButtonColor: '#00bcd4'
            //     }).then(() => {
            //         window.location.href = `${basePath}/Home/vDashboard`;
            //     });
            //     return;
            // }

            populateForm(data);
        } else {
            Swal.fire({
                title: 'ข้อผิดพลาด',
                text: message || 'ไม่พบข้อมูล',
                icon: 'error',
                confirmButtonColor: '#00bcd4'
            }).then(() => {
                window.location.href = `${basePath}/Home/vDashboard`;
            });
        }
    } catch (error) {
        console.error('Error loading report:', error);
        Swal.fire({
            title: 'ข้อผิดพลาด',
            text: 'เกิดข้อผิดพลาดในการโหลดข้อมูล',
            icon: 'error',
            confirmButtonColor: '#00bcd4'
        });
    } finally {
        hideLoader();
    }
}

function populateForm(data) {
    // Section 1: Hull Cell Report
    if (data.txt_line) {
        const radioButton = document.querySelector(`input[name="txt_line"][value="${data.txt_line}"]`);
        if (radioButton) radioButton.checked = true;
    }
    setValueById('analysis-by', data.txt_analysis_by);
    setValueById('sampling-date', data.txt_sampling_date);
    setValueById('time', data.txt_time);

    // Composition Analysis
    setValueById('zinc-metal', data.txt_zinc_metal);
    setValueById('caustic-soda', data.txt_caustic_soda);
    setValueById('sodium-carbonate', data.txt_sodium_carbonate);
    setValueById('nickel', data.txt_nickel);
    setValueById('us-208t', data.txt_us_208t);
    setValueById('ratio', data.txt_ratio);

    // Section 2: Parameter Table
    setValueByName('txt_temp_start', data.txt_temp_start);
    setValueByName('txt_temp_finish', data.txt_temp_finish);
    setValueByName('txt_voltage_start', data.txt_voltage_start);
    setValueByName('txt_voltage_finish', data.txt_voltage_finish);

    // Section 3: X-ray Program
    setValueByName('txt_result_1cm', data.txt_result_1cm);
    setValueByName('txt_zn_1cm', data.txt_zn_1cm);
    setValueByName('txt_ni_1cm', data.txt_ni_1cm);

    setValueByName('txt_result_3cm', data.txt_result_3cm);
    setValueByName('txt_zn_3cm', data.txt_zn_3cm);
    setValueByName('txt_ni_3cm', data.txt_ni_3cm);

    setValueByName('txt_result_5cm', data.txt_result_5cm);
    setValueByName('txt_zn_5cm', data.txt_zn_5cm);
    setValueByName('txt_ni_5cm', data.txt_ni_5cm);

    setValueByName('txt_result_7cm', data.txt_result_7cm);
    setValueByName('txt_zn_7cm', data.txt_zn_7cm);
    setValueByName('txt_ni_7cm', data.txt_ni_7cm);

    setValueByName('txt_result_9cm', data.txt_result_9cm);
    setValueByName('txt_zn_9cm', data.txt_zn_9cm);
    setValueByName('txt_ni_9cm', data.txt_ni_9cm);

    setValueByName('txt_result_19cm', data.txt_result_19cm);
    setValueByName('txt_zn_19cm', data.txt_zn_19cm);
    setValueByName('txt_ni_19cm', data.txt_ni_19cm);

    setValueByName('txt_max_result', data.txt_max_result);
    setValueByName('txt_max_zn', data.txt_max_zn);
    setValueByName('txt_max_ni', data.txt_max_ni);

    setValueByName('txt_min_result', data.txt_min_result);
    setValueByName('txt_min_zn', data.txt_min_zn);
    setValueByName('txt_min_ni', data.txt_min_ni);

    // Section 5: Adjustment Table
    setValueByName('txt_batch_zn', data.txt_batch_zn);
    setValueByName('txt_adjust_zn', data.txt_adjust_zn);
    setValueByName('txt_auto_feed_zn', data.txt_auto_feed_zn);
    setValueByName('txt_remark_zn', data.txt_remark_zn);

    setValueByName('txt_batch_naoh', data.txt_batch_naoh);
    setValueByName('txt_adjust_naoh', data.txt_adjust_naoh);
    setValueByName('txt_auto_feed_naoh', data.txt_auto_feed_naoh);
    setValueByName('txt_remark_naoh', data.txt_remark_naoh);

    setValueByName('txt_batch_208n', data.txt_batch_208n);
    setValueByName('txt_adjust_208n', data.txt_adjust_208n);
    setValueByName('txt_auto_feed_208n', data.txt_auto_feed_208n);
    setValueByName('txt_remark_208n', data.txt_remark_208n);

    setValueByName('txt_batch_208t', data.txt_batch_208t);
    setValueByName('txt_adjust_208t', data.txt_adjust_208t);
    setValueByName('txt_auto_feed_208t', data.txt_auto_feed_208t);
    setValueByName('txt_remark_208t', data.txt_remark_208t);

    setValueByName('txt_batch_208a', data.txt_batch_208a);
    setValueByName('txt_adjust_208a', data.txt_adjust_208a);
    setValueByName('txt_auto_feed_208a', data.txt_auto_feed_208a);
    setValueByName('txt_remark_208a', data.txt_remark_208a);

    setValueByName('txt_batch_208b', data.txt_batch_208b);
    setValueByName('txt_adjust_208b', data.txt_adjust_208b);
    setValueByName('txt_auto_feed_208b', data.txt_auto_feed_208b);
    setValueByName('txt_remark_208b', data.txt_remark_208b);

    // Store UUID in hidden field
    let uuidInput = document.getElementById('txt_uuid_hidden');
    if (!uuidInput) {
        uuidInput = document.createElement('input');
        uuidInput.type = 'hidden';
        uuidInput.id = 'txt_uuid_hidden';
        uuidInput.name = 'txt_uuid';
        document.querySelector('form').appendChild(uuidInput);
    }
    uuidInput.value = data.txt_uuid;

    // Display uploaded images
    if (data.txt_uploaded_images && data.txt_uploaded_images.length > 0) {
        const uploadedImagesContainer = document.getElementById('uploaded-images-list');

        if (uploadedImagesContainer) {
            const uploadedSection = document.getElementById('uploaded-images-container');
            const noImagesMessage = document.getElementById('no-images-message');

            uploadedImagesContainer.innerHTML = '';

            // Show container if exists (vCreateReport)
            if (uploadedSection) {
                uploadedSection.style.display = 'block';
            }

            // Hide no images message if exists (vViewReport)
            if (noImagesMessage) {
                noImagesMessage.style.display = 'none';
            }

            data.txt_uploaded_images.forEach(imageName => {
                const imageItem = document.createElement('div');
                imageItem.className = 'image-item';
                imageItem.setAttribute('data-image-name', imageName);

                // Check if we're in edit mode (has delete button container)
                const hasDeleteButton = document.getElementById('uploaded-images-container') !== null;

                imageItem.innerHTML = `
                    <img src="${basePath}/images/data_log/${imageName}" alt="${imageName}">
                    ${hasDeleteButton ? `
                        <button type="button" class="remove-btn" onclick="deleteUploadedImage('${imageName}')">
                            <i class="fas fa-times"></i>
                        </button>
                    ` : ''}
                    <div style="position: absolute; bottom: 0; left: 0; right: 0; background: rgba(0,0,0,0.7); color: white; padding: 5px; font-size: 11px; text-align: center; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;">
                        ${imageName}
                    </div>
                `;
                uploadedImagesContainer.appendChild(imageItem);
            });
        }
    } else {
        // No images - show message if in view mode
        const noImagesMessage = document.getElementById('no-images-message');
        if (noImagesMessage) {
            noImagesMessage.style.display = 'block';
        }
    }

    // Trigger calculations
    if (typeof calculateRatio === 'function') calculateRatio();
    if (typeof calculateMaxMin === 'function') calculateMaxMin();
    if (typeof updateAutoFeed208T === 'function') updateAutoFeed208T();

    // Fetch and display employee name if analysis_by is filled
    if (data.txt_analysis_by && data.txt_analysis_by.length >= 6) {
        fetchEmployeeName(data.txt_analysis_by);
    }

    // Handle Checked By display
    const checkedByContainer = document.getElementById('checked-by-container');
    const checkedByNameSpan = document.getElementById('checked-by-name');

    if (checkedByContainer && checkedByNameSpan) {
        if (data.txt_checkby && data.txt_checkby !== 'MISSING_KEY') {
            checkedByContainer.style.setProperty('display', 'flex', 'important'); // Force show overriding inline style

            // If checkby is same as analysis_by, use that name (likely fetched already)
            if (data.txt_checkby === data.txt_analysis_by && document.getElementById('employee-name').textContent) {
                // Parse name from "ID (Name)" format if needed, or just fetch again to be safe
                fetchCheckedByEmployeeName(data.txt_checkby);
            } else {
                fetchCheckedByEmployeeName(data.txt_checkby);
            }

            // If already checked, hide the check button if present
            const checkActions = document.querySelector('.check-actions');
            if (checkActions) {
                checkActions.style.display = 'none';
            }
        } else {
            checkedByContainer.style.setProperty('display', 'none', 'important');

            // Handle Check Button State based on Status
            const btnCheck = document.getElementById('btn-check-report');
            const infoText = document.getElementById('check-permission-info');

            if (btnCheck) {
                const canCheckPermission = btnCheck.dataset.canCheck === "true";

                if (data.txt_status !== 'C') {
                    // Not Complete - Disable Check
                    btnCheck.disabled = true;
                    if (infoText) {
                        infoText.textContent = "รายการยังไม่สมบูรณ์ (ต้องเป็น Complete ถึงจะ Check ได้)";
                        infoText.style.display = 'block';
                    }
                } else {
                    // Complete - Check Permission
                    if (canCheckPermission) {
                        btnCheck.disabled = false;
                        if (infoText) infoText.style.display = 'none';
                    } else {
                        btnCheck.disabled = true;
                        // Text already set by checkPermissionAndSetupButton, but ensure it shows
                        if (infoText) {
                            if (infoText.textContent !== "โปรดติดต่อ FM ให้ Check รายการนี้") {
                                infoText.textContent = "โปรดติดต่อ FM ให้ Check รายการนี้";
                            }
                            infoText.style.display = 'block';
                        }
                    }
                }
            }
        }
    }
}

async function fetchCheckedByEmployeeName(empno) {
    try {
        const employeeNameSpan = document.getElementById('checked-by-name');
        if (!employeeNameSpan) return;

        // Use analysis by name logic: empnameengshort1 is handled in PDF, here we show full name or whatever API returns
        // Requirement: "ในช่อง Analysed จะเป็น [empnameengshort1] ของ txt_analysis_by" -> This refers to PDF Template
        // Requirement: " Checked By section display: checked by XXXXXX (Name)" -> This remains as per previous logic?
        // Wait, the requirement "ในช่อง Analysed จะเป็น [empnameengshort1] ของ txt_analysis_by" is for PDF.
        // Let's stick to standard name display here for consistency unless specified.

        const response = await fetch(`${basePath}/Home/GetEmployeeByEmpno?empno=${encodeURIComponent(empno)}`);
        const data = await response.json();

        if (data.success && data.empnameeng) {
            employeeNameSpan.textContent = `${empno} (${data.empnameeng})`;
        } else {
            employeeNameSpan.textContent = `${empno} (ไม่พบข้อมูลพนักงาน)`;
        }
    } catch (error) {
        console.error('Error fetching checked by employee:', error);
    }
}

async function fetchEmployeeName(empno) {
    try {
        const employeeNameSpan = document.getElementById('employee-name');
        if (!employeeNameSpan) return;

        const response = await fetch(`${basePath}/Home/GetEmployeeByEmpno?empno=${encodeURIComponent(empno)}`);
        const data = await response.json();

        if (data.success && data.empnameeng) {
            employeeNameSpan.textContent = `(${data.empnameeng})`;
            employeeNameSpan.style.color = '#00bcd4';
        } else {
            employeeNameSpan.textContent = '(ไม่พบข้อมูลพนักงาน)';
            employeeNameSpan.style.color = '#f44336';
        }
    } catch (error) {
        console.error('Error fetching employee:', error);
    }
}

function setValueById(id, value) {
    const element = document.getElementById(id);
    if (element && value !== null && value !== undefined) {
        element.value = value;
    }
}

function setValueByName(name, value) {
    const elements = document.getElementsByName(name);
    if (elements.length > 0 && value !== null && value !== undefined) {
        elements[0].value = value;
    }
}
