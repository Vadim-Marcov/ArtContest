function validateLoginForm() {
    var login = document.getElementById('loginInput').value.trim();
    var password = document.getElementById('passwordInput').value;

    if (login.length < 3) {
        showFieldError('loginInput', 'Логин должен содержать минимум 3 символа');
        return false;
    }
    if (login.length > 50) {
        showFieldError('loginInput', 'Логин не может превышать 50 символов');
        return false;
    }
    if (password.length < 6) {
        showFieldError('passwordInput', 'Пароль должен содержать минимум 6 символов');
        return false;
    }
    if (password.length > 100) {
        showFieldError('passwordInput', 'Пароль не может превышать 100 символов');
        return false;
    }
    clearFieldErrors();
    return true;
}

function validateRegisterForm() {
    var login = document.getElementById('regLogin').value.trim();
    var email = document.getElementById('regEmail').value.trim();
    var birthDate = document.getElementById('regBirthDate').value;
    var region = document.getElementById('regRegion').value;
    var password = document.getElementById('regPassword').value;
    var confirmPassword = document.getElementById('regConfirmPassword').value;

    clearFieldErrors();

    if (login.length < 3) {
        showFieldError('regLogin', 'Логин должен содержать минимум 3 символа');
        return false;
    }
    if (login.length > 50) {
        showFieldError('regLogin', 'Логин не может превышать 50 символов');
        return false;
    }
    if (!/^[a-zA-Z0-9_]+$/.test(login)) {
        showFieldError('regLogin', 'Логин может содержать только латинские буквы, цифры и _');
        return false;
    }

    var emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(email)) {
        showFieldError('regEmail', 'Введите корректный email адрес');
        return false;
    }
    if (email.length > 100) {
        showFieldError('regEmail', 'Email не может превышать 100 символов');
        return false;
    }

    if (!birthDate) {
        showFieldError('regBirthDate', 'Укажите дату рождения');
        return false;
    } else {
        var birth = new Date(birthDate);
        var today = new Date();
        var age = today.getFullYear() - birth.getFullYear();
        if (age < 5 || age > 120) {
            showFieldError('regBirthDate', 'Некорректная дата рождения');
            return false;
        }
    }

    if (!region) {
        showFieldError('regRegion', 'Выберите регион');
        return false;
    }

    if (password.length < 6) {
        showFieldError('regPassword', 'Пароль должен содержать минимум 6 символов');
        return false;
    }
    if (password.length > 100) {
        showFieldError('regPassword', 'Пароль не может превышать 100 символов');
        return false;
    }

    if (password !== confirmPassword) {
        showFieldError('regConfirmPassword', 'Пароли не совпадают');
        return false;
    }

    return true;
}

function showFieldError(fieldId, message) {
    var field = document.getElementById(fieldId);
    if (!field) return;

    field.classList.add('input-error');

    var existing = field.parentNode.querySelector('.field-error-text');
    if (existing) existing.remove();

    var errorSpan = document.createElement('span');
    errorSpan.className = 'field-error-text';
    errorSpan.textContent = message;
    field.parentNode.appendChild(errorSpan);
}

function clearFieldErrors() {
    document.querySelectorAll('.input-error').forEach(function(el) {
        el.classList.remove('input-error');
    });
    document.querySelectorAll('.field-error-text').forEach(function(el) {
        el.remove();
    });
}
