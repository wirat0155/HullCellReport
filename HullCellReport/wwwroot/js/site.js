function SwalOK(text) {
    Swal.fire({
        title: "Success",
        text: text,
        icon: "success",
        timer: 2000,
        showConfirmButton: false,
        confirmButtonColor: "#28a745",
        customClass: {
            popup: 'swal-success-popup',
            title: 'swal-success-title'
        },
        showClass: {
            popup: 'animate__animated animate__fadeInDown animate__faster'
        },
        hideClass: {
            popup: 'animate__animated animate__fadeOutUp animate__faster'
        }
    });
}
function SwalNG(errors, text) {
    let message = "";
    let title = "Error";
    
    if (Array.isArray(errors) && errors.length > 0) {
        // Show all error messages in a clean format
        if (errors.length === 1) {
            // Single error - show it directly
            message = errors[0].errorMessage || errors[0].property || "An error occurred.";
        } else {
            // Multiple errors - show as a styled list
            message = '<div style="text-align: left; padding: 0 20px;">';
            errors.forEach((error, index) => {
                const errorMsg = error.errorMessage || error.property || "Unknown error";
                message += `<div style="margin: 8px 0; display: flex; align-items: start;">
                    <span style="color: #dc3545; margin-right: 8px; font-weight: bold;">•</span>
                    <span>${errorMsg}</span>
                </div>`;
            });
            message += '</div>';
        }
    } else if (text) {
        // Use the text parameter if provided
        message = text;
    } else if (typeof errors === 'string') {
        // If errors is a string, use it directly
        message = errors;
    } else {
        // Generic fallback message
        message = "An error occurred. Please try again.";
    }
    
    Swal.fire({
        title: title,
        html: message,
        icon: "error",
        confirmButtonText: "OK",
        confirmButtonColor: "#dc3545",
        customClass: {
            popup: 'swal-error-popup',
            title: 'swal-error-title',
            htmlContainer: 'swal-error-content'
        },
        showClass: {
            popup: 'animate__animated animate__fadeInDown animate__faster'
        },
        hideClass: {
            popup: 'animate__animated animate__fadeOutUp animate__faster'
        }
    });
}

$('form').on('input', 'input, select, textarea', function () {
    const inputName = $(this).attr('name');
    if (inputName) {
        // Escape special characters for the property name in the error object
        const escapedInputName = inputName.replace(/([.#\[\]\\'"])/g, '\\$&');
        $('.error#' + escapedInputName).text('');
    }
});

function removeError() {
    $(".error").text("");
}

function showError(errors) {
    const escapeSelector = (selector) => selector.replace(/([.#\[\]\\'"])/g, '\\$&'); // Properly closed group

    errors.forEach(error => {
        const escapedProperty = escapeSelector(error.property);
        $(".error#" + escapedProperty).text(error.errorMessage);
    });

    const topmostElement = errors
        .map(error => {
            const escapedProperty = escapeSelector(error.property);
            return $(`[name="${escapedProperty}"]`)[0];
        })
        .sort((a, b) => a.getBoundingClientRect().top - b.getBoundingClientRect().top)[0];

    if (topmostElement) {
        $(topmostElement).focus();
    }
}




function renderErrorSpans() {
    $('input[data-form], select[data-form], textarea[data-form]').each(function () {
        const formName = $(this).attr('data-form');
        if (!$('#' + formName).length) {
            $('<p>', { class: 'error text-danger', id: formName }).insertAfter(this);
        }
    });
}

const basePath = (window.location.hostname !== 'localhost' && window.location.hostname !== '127.0.0.1') ? "/" + window.location.pathname.split('/')[1] : ''; // Adjust '/myapp' to your subfolder name

$('form').on('keydown', 'input, select, textarea', function (e) {
    if (e.key === 'Enter') {
        e.preventDefault();
    }
});

    function addEmail(username) {
    const container = document.querySelector('.item-list');

    // สร้าง input ใหม่
    const inputDiv = document.createElement('div');
    inputDiv.classList.add('item-card');

    const input = document.createElement('input');
    input.type = 'email';
    input.placeholder = 'Enter new email';
    input.classList.add('form-control');
    input.autofocus = true;

    inputDiv.appendChild(input);
    container.appendChild(inputDiv);

    // ฟัง enter เพื่อ save
    input.addEventListener('keydown', function (e) {
        if (e.key === 'Enter') {
            const newEmail = input.value.trim();
    if (!newEmail) return alert('Please enter an email.');

    // ยิง AJAX ไปเซฟ
    fetch('/User/AddEmail', {
        method: 'POST',
    headers: {
        'Content-Type': 'application/json'
                },
    body: JSON.stringify({username: username, email: newEmail })
            })
            .then(res => {
                if (!res.ok) throw new Error('Failed to save email');
    return res.json();
            })
            .then(data => {
        // แทน input ด้วย display ปกติ
        inputDiv.innerHTML = `
                    <div class="item-card-details">
                        <p>${newEmail}</p>
                    </div>
                    <button class="btn btn-outline-danger" type="button"
                        onclick="deleteEmail('${data.id}')" title="Delete Email">
                        Remove
                    </button>
                `;
            })
            .catch(err => {
        alert(err.message);
    input.focus();
            });
        }
    });

    // focus ทันที
    input.focus();
}

function showLoader() {
    const loader = document.getElementById('chemicalLoader');
    if (loader) {
        loader.classList.remove('hidden');
    }
}

function hideLoader() {
    const loader = document.getElementById('chemicalLoader');
    if (loader) {
        loader.classList.add('hidden');
    }
}
