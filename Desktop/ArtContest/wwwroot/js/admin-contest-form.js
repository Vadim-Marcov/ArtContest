document.addEventListener('DOMContentLoaded', function() {
    var fileInput = document.getElementById('contest-image-upload');
    var dropzone = document.getElementById('dropzone-label');

    if (fileInput && dropzone) {
        fileInput.addEventListener('change', function() {
            if (this.files && this.files.length > 0) {
                var file = this.files[0];

                var allowedExtensions = ['.jpg', '.jpeg', '.png'];
                var ext = file.name.toLowerCase().substring(file.name.lastIndexOf('.'));
                if (allowedExtensions.indexOf(ext) === -1) {
                    alert('Допустимые форматы: JPG, JPEG, PNG');
                    this.value = '';
                    return;
                }

                if (file.size > 10 * 1024 * 1024) {
                    alert('Размер изображения не может превышать 10 МБ');
                    this.value = '';
                    return;
                }

                var reader = new FileReader();
                reader.onload = function(e) {
                    var img = dropzone.querySelector('img');
                    if (!img) {
                        img = document.createElement('img');
                        dropzone.appendChild(img);
                    }
                    img.src = e.target.result;
                    dropzone.classList.add('has-image');
                };
                reader.readAsDataURL(file);
            }
        });
    }

    var today = new Date();
    var year = today.getFullYear();
    var month = String(today.getMonth() + 1).padStart(2, '0');
    var day = String(today.getDate()).padStart(2, '0');
    var todayStr = year + '-' + month + '-' + day;

    var appStart = document.getElementById('appStartDate');
    var appEnd = document.getElementById('appEndDate');
    var judStart = document.getElementById('judStartDate');
    var judEnd = document.getElementById('judEndDate');

    function setMinDate(el, minVal) {
        if (el && !el.disabled && minVal) {
            el.setAttribute('min', minVal);
        }
    }

    if (appStart && !appStart.disabled) {
        setMinDate(appStart, todayStr);
        appStart.addEventListener('change', function() {
            setMinDate(appEnd, this.value);
            if (appEnd && !appEnd.disabled && appEnd.value && appEnd.value <= this.value) {
                var d = new Date(this.value);
                d.setDate(d.getDate() + 1);
                appEnd.value = d.toISOString().split('T')[0];
            }
            updateJudStartMin();
        });
    }

    if (appEnd && !appEnd.disabled) {
        appEnd.addEventListener('change', function() {
            updateJudStartMin();
            setMinDate(judEnd, judStart && judStart.value ? judStart.value : this.value);
        });
    }

    if (judStart && !judStart.disabled) {
        judStart.addEventListener('change', function() {
            setMinDate(judEnd, this.value);
            if (judEnd && !judEnd.disabled && judEnd.value && judEnd.value <= this.value) {
                var d = new Date(this.value);
                d.setDate(d.getDate() + 1);
                judEnd.value = d.toISOString().split('T')[0];
            }
        });
    }

    function updateJudStartMin() {
        if (judStart && !judStart.disabled) {
            var minVal = appEnd && appEnd.value ? appEnd.value : (appStart && appStart.value ? appStart.value : todayStr);
            setMinDate(judStart, minVal);
            if (judStart.value && judStart.value <= minVal) {
                var d = new Date(minVal);
                d.setDate(d.getDate() + 1);
                judStart.value = d.toISOString().split('T')[0];
            }
        }
    }

    updateJudStartMin();
});

function validateForm() {
    var title = document.querySelector('[name="title"]').value.trim();
    var rules = document.querySelector('[name="rules"]').value.trim();
    var category = document.querySelector('[name="category"]').value;
    var criteria1 = document.querySelector('[name="criteria1"]').value;
    var criteria2 = document.querySelector('[name="criteria2"]').value;

    if (!title || title.length < 3) { alert('Название конкурса должно содержать минимум 3 символа'); return false; }
    if (!rules || rules.length < 10) { alert('Правила и описание должны содержать минимум 10 символов'); return false; }
    if (!category || !criteria1 || !criteria2) { alert('Выберите категорию и критерии оценки'); return false; }

    var appStartEl = document.querySelector('[name="appStartDate"]');
    var appEndEl = document.querySelector('[name="appEndDate"]');
    var judStartEl = document.querySelector('[name="judStartDate"]');
    var judEndEl = document.querySelector('[name="judEndDate"]');

    var appStart = appStartEl && !appStartEl.disabled ? appStartEl.value : null;
    var appEnd = appEndEl && !appEndEl.disabled ? appEndEl.value : null;
    var judStart = judStartEl && !judStartEl.disabled ? judStartEl.value : null;
    var judEnd = judEndEl && !judEndEl.disabled ? judEndEl.value : null;

    if (appStart) {
        var today = new Date(); today.setHours(0, 0, 0, 0);
        var startDate = new Date(appStart);
        if (startDate < today) { alert('Дата начала приёма заявок не может быть в прошлом'); return false; }
    }

    if (appStart && appEnd) {
        if (appEnd <= appStart) { alert('Дата окончания приёма заявок должна быть позже даты начала'); return false; }
        var appDays = (new Date(appEnd) - new Date(appStart)) / 86400000;
        if (appDays < 1) { alert('Период приёма заявок должен быть минимум 1 день'); return false; }
        if (appDays > 365) { alert('Период приёма заявок не может превышать 365 дней'); return false; }
    }

    if (appEnd && judStart) {
        var gapDays = (new Date(judStart) - new Date(appEnd)) / 86400000;
        if (gapDays > 1) { alert('Между окончанием приёма заявок и началом судейства должен быть максимум 1 день'); return false; }
    }

    if (judStart && judEnd) {
        if (judEnd <= judStart) { alert('Дата окончания судейства должна быть позже даты начала'); return false; }
        var judDays = (new Date(judEnd) - new Date(judStart)) / 86400000;
        if (judDays < 1) { alert('Период судейства должен быть минимум 1 день'); return false; }
        if (judDays > 30) { alert('Период судейства не может превышать 30 дней'); return false; }
    }

    var isEditPage = window.location.href.indexOf('/EditContest') !== -1;
    if (!isEditPage) {
        var fileInput = document.getElementById('contest-image-upload');
        if (!fileInput || !fileInput.files || fileInput.files.length === 0) {
            alert('Загрузите изображение конкурса');
            return false;
        }
    }

    return true;
}
