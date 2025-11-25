// Prevent back button after logout
(function () {
    if (window.history && window.history.pushState) {
        window.history.pushState(null, null, window.location.href);
        window.addEventListener('popstate', function () {
            window.history.pushState(null, null, window.location.href);
        });
    }
})();

// Hide chemical loader on page load
window.addEventListener('load', function () {
    setTimeout(function () {
        const loader = document.getElementById('chemicalLoader');
        if (loader) {
            loader.classList.add('hidden');
        }
    }, 800);
});

async function Login(event) {
    showLoader();
    removeError();
    event.preventDefault();

    try {
        const form = event.target;
        const submitButton = event.submitter;

        const formData = new FormData(form);
        if (submitButton && submitButton.name) {
            formData.append(submitButton.name, submitButton.value);
        }

        const response = await fetch(`${basePath}/Auth/Login`, {
            method: 'POST',
            body: formData
        });

        const { success, text = "", errors = [] } = await response.json();
        console.log(success, text, errors);
        if (!success) {
            showError(errors);
            hideLoader();
            SwalNG(errors, text);
        }
        else {
            hideLoader();
            window.location.pathname = `${basePath}/Home/vDashboard`;
        }
    } catch ({ message }) {
        alert(`Exception: ${message}`);
        
    }
}