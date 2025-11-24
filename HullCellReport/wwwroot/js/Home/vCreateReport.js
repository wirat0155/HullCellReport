/* ---------------- ปุ่มบันทึก & Export PDF ---------------- */
async function Save(event) {
    // 1. หยุดการ Submit ปกติ
    event.preventDefault();

    // 2. เตรียมข้อมูล Form ทันที (ควรทำก่อน Swal เพื่อกัน event loss)
    const form = event.target;
    const formData = new FormData(form);
    const submitButton = event.submitter;

    if (submitButton && submitButton.name) {
        formData.append(submitButton.name, submitButton.value);
    }

    // 3. แสดง Popup ยืนยัน
    const result = await Swal.fire({
        title: 'บันทึกข้อมูล',
        text: 'คุณต้องการบันทึกข้อมูลหรือไม่?',
        icon: 'question',
        showCancelButton: true,
        confirmButtonColor: '#00bcd4',
        cancelButtonColor: '#6c757d',
        confirmButtonText: 'บันทึก',
        cancelButtonText: 'ยกเลิก'
    });

    // 4. ตรวจสอบว่าผู้ใช้กด "ตกลง" หรือไม่
    if (!result.isConfirmed) {
        return; // ถ้ากดยกเลิก ให้จบการทำงานตรงนี้
    }

    // 5. ส่งข้อมูลไป Backend
    try {
        // แสดง Loader (ถ้ามีฟังก์ชันนี้)
        // showLoader(); 

        const response = await fetch(`${basePath}/Home/CreateReport`, {
            method: 'POST',
            body: formData
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const { success, text = "", errors = [], redirectToEdit = false, uuid = "" } = await response.json();

        if (!success) {
            // Check if need to redirect to edit incomplete report
            if (redirectToEdit && uuid) {
                Swal.fire({
                    title: 'แจ้งเตือน',
                    text: errors[0] || 'มีรายการที่ยังไม่ Complete',
                    icon: 'warning',
                    confirmButtonColor: '#00bcd4',
                    confirmButtonText: 'ไปแก้ไขรายการเดิม'
                }).then(() => {
                    window.location.href = `${basePath}/Home/vCreateReport?uuid=${uuid}`;
                });
                return;
            }

            // กรณี Backend ตอบกลับว่าไม่สำเร็จ
            if (typeof showError === 'function') showError(errors);
            if (typeof SwalNG === 'function') SwalNG(errors);
        } else {
            // กรณีสำเร็จ - แสดง alert แล้ว redirect ไป dashboard
            Swal.fire({
                title: 'สำเร็จ',
                text: text || 'บันทึกข้อมูลสำเร็จ',
                icon: 'success',
                confirmButtonColor: '#00bcd4',
                confirmButtonText: 'ตกลง'
            }).then(() => {
                window.location.href = `${basePath}/Home/vDashboard`;
            });
        }

    } catch (error) {
        // จัดการ Error ทาง Network หรือ Exception อื่นๆ
        console.error(error);
        alert(`Exception: ${error.message}`);
    } finally {
        // ซ่อน Loader เสมอไม่ว่าจะสำเร็จหรือล้มเหลว
        if (typeof hideLoader === 'function') hideLoader();
    }
}

function printReport() {
    window.print();
}

/* ---------------- (C) แสดง Preview ภาพที่เลือก ---------------- */
document.getElementById('file_upload').addEventListener('change', function () {
    const previewContainer = document.getElementById('image-preview');
    const previewSection = document.getElementById('preview-container');
    previewContainer.innerHTML = "";
    const files = this.files;
    
    if (files.length > 0) {
        previewSection.style.display = 'block';
    } else {
        previewSection.style.display = 'none';
    }
    
    for (let i = 0; i < files.length; i++) {
        const file = files[i];
        
        // Check if file is an image
        if (file.type.startsWith('image/')) {
            const reader = new FileReader();
            
            reader.onload = function(e) {
                const imageItem = document.createElement('div');
                imageItem.className = 'image-item';
                imageItem.innerHTML = `
                    <img src="${e.target.result}" alt="${file.name}">
                    <div style="position: absolute; bottom: 0; left: 0; right: 0; background: rgba(0,0,0,0.7); color: white; padding: 5px; font-size: 11px; text-align: center; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;">
                        ${file.name}
                    </div>
                `;
                previewContainer.appendChild(imageItem);
            };
            
            reader.readAsDataURL(file);
        }
    }
});

/* ---------------- Delete uploaded image ---------------- */
function deleteUploadedImage(imageName) {
    Swal.fire({
        title: 'ยืนยันการลบ',
        text: 'คุณต้องการลบภาพนี้หรือไม่?',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#f44336',
        cancelButtonColor: '#6c757d',
        confirmButtonText: 'ลบ',
        cancelButtonText: 'ยกเลิก'
    }).then((result) => {
        if (result.isConfirmed) {
            // Add to deleted list
            const deletedInput = document.getElementById('deleted-images');
            const deletedList = deletedInput.value ? deletedInput.value.split(',') : [];
            deletedList.push(imageName);
            deletedInput.value = deletedList.join(',');
            
            // Remove from display
            const imageElement = document.querySelector(`[data-image-name="${imageName}"]`);
            if (imageElement) {
                imageElement.remove();
            }
            
            // Check if no more images
            const uploadedList = document.getElementById('uploaded-images-list');
            if (uploadedList.children.length === 0) {
                document.getElementById('uploaded-images-container').style.display = 'none';
            }
            
            Swal.fire({
                title: 'ลบแล้ว',
                text: 'ภาพจะถูกลบเมื่อบันทึกข้อมูล',
                icon: 'success',
                timer: 2000,
                showConfirmButton: false
            });
        }
    });
}

/* ---------------- (D) Ratio Zn/Ni ---------------- */
function calculateRatio() {
    const zn = parseFloat(document.getElementById("zinc-metal").value);
    const ni = parseFloat(document.getElementById("nickel").value);
    if (!isNaN(zn) && !isNaN(ni) && ni !== 0) {
        document.getElementById("ratio").value = (zn / ni).toFixed(2);
    } else {
        document.getElementById("ratio").value = "";
    }
}

/* ---------------- Get Employee Name by Empno ---------------- */
let empnoTimeout = null;
document.getElementById('analysis-by').addEventListener('input', function() {
    const empno = this.value.trim();
    const employeeNameSpan = document.getElementById('employee-name');
    
    // Clear previous timeout
    if (empnoTimeout) {
        clearTimeout(empnoTimeout);
    }
    
    // Clear employee name if less than 6 characters
    if (empno.length < 6) {
        employeeNameSpan.textContent = '';
        return;
    }
    
    // Set timeout to call API after user stops typing
    empnoTimeout = setTimeout(async () => {
        try {
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
            employeeNameSpan.textContent = '';
        }
    }, 500); // Wait 500ms after user stops typing
});

/* ---------------- (E) NaOH < 115 → 25 kg ---------------- */
document.getElementById('caustic-soda').addEventListener('input', function () {
    const value = parseFloat(this.value);
    const adjust = document.getElementsByName("txt_adjust_naoh")[0];
    if (!isNaN(value) && value < 115) {
        adjust.value = "25 kg";
    } else {
        adjust.value = "";
    }
});

/* ---------------- (F) X-ray Max/Min + เรียก Logic Ni ---------------- */
function calculateMaxMin() {
    const resultInputs = document.querySelectorAll('input[name^="txt_result_"]');
    let maxR = -Infinity, minR = Infinity;
    resultInputs.forEach(x => {
        const v = parseFloat(x.value);
        if (!isNaN(v)) {
            if (v > maxR) maxR = v;
            if (v < minR) minR = v;
        }
    });
    document.getElementsByName("txt_max_result")[0].value = maxR !== -Infinity ? maxR : "";
    document.getElementsByName("txt_min_result")[0].value = minR !== Infinity ? minR : "";

    // Zn
    const znInputs = document.querySelectorAll('input[name^="txt_zn_"]');
    let maxZn = -Infinity, minZn = Infinity;
    znInputs.forEach(x => {
        const v = parseFloat(x.value);
        if (!isNaN(v)) {
            if (v > maxZn) maxZn = v;
            if (v < minZn) minZn = v;
        }
    });
    document.getElementsByName("txt_max_zn")[0].value = maxZn !== -Infinity ? maxZn : "";
    document.getElementsByName("txt_min_zn")[0].value = minZn !== Infinity ? minZn : "";

    // Ni
    const niInputs = document.querySelectorAll('input[name^="txt_ni_"]');
    let maxNi = -Infinity, minNi = Infinity;
    niInputs.forEach(x => {
        const v = parseFloat(x.value);
        if (!isNaN(v)) {
            if (v > maxNi) maxNi = v;
            if (v < minNi) minNi = v;
        }
    });
    document.getElementsByName("txt_max_ni")[0].value = maxNi !== -Infinity ? maxNi : "";
    document.getElementsByName("txt_min_ni")[0].value = minNi !== Infinity ? minNi : "";

    checkNi1cm();
    checkNi9cm();
}

/* ---------------- (G) %Ni at 1 cm – Logic รวม ---------------- */
function checkNi1cm() {
    const ni = parseFloat(document.getElementsByName("txt_ni_1cm")[0].value);
    const autoN = document.getElementsByName("txt_auto_feed_208n")[0];
    const autoA = document.getElementsByName("txt_auto_feed_208a")[0];
    const autoB = document.getElementsByName("txt_auto_feed_208b")[0];
    const adjN = document.getElementsByName("txt_adjust_208n")[0];
    const remarkN = document.getElementsByName("txt_remark_208n")[0];
    const batchZn = document.getElementsByName("txt_batch_zn")[0];

    // เคลียร์ค่าเริ่มต้น
    autoN.value = autoA.value = autoB.value = "";
    adjN.value = remarkN.value = "";
    batchZn.value = "";

    if (isNaN(ni)) return;

    // 1) 14.3 – 15.0 → Auto feed Open ทุกตัว
    if (ni >= 14.3 && ni <= 15.0) {
        autoN.value = "Open";
        autoA.value = "Open";
        autoB.value = "Open";
        return;
    }

    // 2) 13.9 – 14.2 → ปรับเติม 208N 3 ลิตร + Auto feed Open
    if (ni >= 13.9 && ni < 14.3) {
        autoN.value = autoA.value = autoB.value = "Open";
        adjN.value = "ปรับเติม 208N 3 ลิตร";
        return;
    }

    // 3) Ni < 13.9 → ปรับเติม 5 ลิตร + Request PD calibration + ลดแช่ซิงค์ 1 ตะแกรง
    if (ni < 13.9) {
        autoN.value = autoA.value = autoB.value = "Open";
        adjN.value = "ปรับเติม 208N 5 ลิตร";
        remarkN.value = "Request PD calibration feed";
        batchZn.value = "ลดการแช่ซิงค์ลงจากมาตรฐานเดิม 1 ตะแกรง โดยดูชิ้นงานประกอบ";
        return;
    }

    // Ni สูงฝั่งบน
    if (ni >= 16) {
        autoN.value = "Stop feed 208N 4 ชั่วโมง";
        autoA.value = autoB.value = "Open";
        batchZn.value = "แช่ซิงค์มากกว่ามาตรฐานเดิม 2 ตะแกรง โดยดูชิ้นงานประกอบ";
        return;
    }

    if (ni > 15 && ni < 16) {
        autoN.value = "Stop feed 208N 2 ชั่วโมง";
        autoA.value = autoB.value = "Open";
        batchZn.value = "แช่ซิงค์มากกว่ามาตรฐานเดิม 1 ตะแกรง โดยดูชิ้นงานประกอบ";
        return;
    }

    // ค่าอื่น ๆ → เปิด auto feed ทั้งหมด
    autoN.value = autoA.value = autoB.value = "Open";
}

/* ---------------- (H) %Ni at 9 cm ---------------- */
function checkNi9cm() {
    const ni = parseFloat(document.getElementsByName("txt_ni_9cm")[0].value);
    const adjB = document.getElementsByName("txt_adjust_208b")[0];
    const adjA = document.getElementsByName("txt_adjust_208a")[0];
    const remarkA = document.getElementsByName("txt_remark_208a")[0];
    const remarkB = document.getElementsByName("txt_remark_208b")[0];

    adjA.value = adjB.value = "";
    remarkA.value = remarkB.value = "";

    if (isNaN(ni)) return;

    if (ni < 13.3 && ni >= 12.5) {
        adjB.value = "ปรับเติม 208B 1 ลิตร";
        adjA.value = "ปรับเติม 208A 0.5 ลิตร";
        remarkA.value = "ตามเงื่อนไข %Ni < 13.5";
        remarkB.value = "ตามเงื่อนไข %Ni < 13.5";
    } else if (ni < 12.5) {
        adjB.value = "ปรับเติม 208B 1.5 ลิตร";
        adjA.value = "ปรับเติม 208A 0.8 ลิตร";
        remarkA.value = "ตามเงื่อนไข %Ni < 12.5";
        remarkB.value = "ตามเงื่อนไข %Ni < 12.5";
    } else if (ni >= 13.3 && ni <= 14.5) {
        remarkA.value = "อยู่ในเกณฑ์ ไม่ต้องปรับ";
        remarkB.value = "อยู่ในเกณฑ์ ไม่ต้องปรับ";
    }
}

/* ---------------- (I) US-208T Condition ---------------- */
function updateAutoFeed208T() {
    const v = parseFloat(document.getElementById("us-208t").value);
    const auto = document.getElementsByName("txt_auto_feed_208t")[0];
    const adj = document.getElementsByName("txt_adjust_208t")[0];
    const remark = document.getElementsByName("txt_remark_208t")[0];

    auto.value = adj.value = remark.value = "";

    if (isNaN(v)) return;

    if (v > 153) {
        auto.value = "Stop feed 2 ชั่วโมง";
        return;
    }

    if (v >= 148 && v <= 153) {
        auto.value = "Open";
        return;
    }

    if (v < 148) {
        auto.value = "Open";
        adj.value = "Refill 208T";
        remark.value = "Request PD calibration feed";
        return;
    }
}

document.getElementById("us-208t").addEventListener("input", updateAutoFeed208T);

/* ---------------- (J) Global Enter → ไปช่องถัดไป (ยกเว้น X-ray) ---------------- */
document.addEventListener("keydown", function (event) {
    if (event.key !== "Enter" || event.shiftKey) return;

    const target = event.target;
    const tag = target.tagName.toLowerCase();
    if (tag !== "input" && tag !== "textarea") return;

    const name = target.getAttribute("name") || "";
    if (name.startsWith("txt_result_") || name.startsWith("txt_zn_") || name.startsWith("txt_ni_")) {
        return;
    }

    event.preventDefault();
    const fields = Array.from(document.querySelectorAll("input, textarea"));
    const index = fields.indexOf(target);
    if (index >= 0 && index < fields.length - 1) {
        fields[index + 1].focus();
    }
});

/* ---------------- (K) X-ray Column Navigation ---------------- */
document.addEventListener("keydown", function (event) {
    const key = event.key;
    if (!["Enter", "ArrowDown", "ArrowUp"].includes(key)) return;

    const target = event.target;
    const name = target.getAttribute("name") || "";
    if (!name) return;

    let prefix = null;
    if (name.startsWith("txt_result_")) prefix = "txt_result_";
    if (name.startsWith("txt_zn_")) prefix = "txt_zn_";
    if (name.startsWith("txt_ni_")) prefix = "txt_ni_";

    if (!prefix) return;

    event.preventDefault();

    const columns = ["txt_result_", "txt_zn_", "txt_ni_"];
    const currentColumnIndex = columns.indexOf(prefix);
    const currentColumnInputs = Array.from(document.querySelectorAll(`input[name^="${prefix}"]`));
    const rowIndex = currentColumnInputs.indexOf(target);

    if (key === "Enter" || key === "ArrowDown") {
        if (rowIndex < currentColumnInputs.length - 1) {
            currentColumnInputs[rowIndex + 1].focus();
            return;
        }

        if (currentColumnIndex < columns.length - 1) {
            const nextColumn = columns[currentColumnIndex + 1];
            const nextInputs = Array.from(document.querySelectorAll(`input[name^="${nextColumn}"]`));
            if (nextInputs[rowIndex]) nextInputs[rowIndex].focus();
        }
    }

    if (key === "ArrowUp") {
        if (rowIndex > 0) {
            currentColumnInputs[rowIndex - 1].focus();
            return;
        }

        if (rowIndex === 0 && currentColumnIndex > 0) {
            const prevColumn = columns[currentColumnIndex - 1];
            const prevInputs = Array.from(document.querySelectorAll(`input[name^="${prevColumn}"]`));
            if (prevInputs[rowIndex]) prevInputs[rowIndex].focus();
        }
    }
});
