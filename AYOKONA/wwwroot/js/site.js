// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener('DOMContentLoaded', function () {
    // Auto-uppercase and auto-dash for student number
    document.querySelectorAll('.student-number-input').forEach(function (input) {
        input.addEventListener('input', function (e) {
            let value = input.value.toUpperCase().replace(/[^A-Z0-9]/g, '');
            // Format: YYYY-XXXXX-CM-0
            let formatted = '';
            for (let i = 0; i < value.length; i++) {
                if (i === 4 || i === 9 || i === 11) formatted += '-';
                formatted += value[i];
            }
            input.value = formatted;
        });
    });
    // Auto-uppercase for section/class
    document.querySelectorAll('.section-input').forEach(function (input) {
        input.addEventListener('input', function () {
            input.value = input.value.toUpperCase();
        });
    });
});

document.addEventListener('DOMContentLoaded', function () {

    // Gmail-only validation for registration email
    var regForm = document.querySelector('form[asp-action="Register"]');
    if (regForm) {
        regForm.addEventListener('submit', function (e) {
            var emailInput = regForm.querySelector('input[type="email"]');
            if (emailInput && !/^[^@\s]+@gmail\.com$/i.test(emailInput.value.trim())) {
                e.preventDefault();
                alert('Please enter a valid Gmail address (must end with @gmail.com).');
                emailInput.focus();
            }
        });
    }
});
