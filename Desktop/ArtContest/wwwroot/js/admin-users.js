var currentUserId = null;
var currentAction = null;

function changeRole(userId, newRoleId) {
    newRoleId = parseInt(newRoleId);

    fetch('/Admin/ChangeUserRole', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: 'userId=' + userId + '&newRoleId=' + newRoleId
    })
    .then(function(response) {
        if (response.ok) {
            updateRoleStyle(userId, newRoleId);
        }
    });
}

function updateRoleStyle(userId, roleId) {
    var wrapper = document.getElementById('role-wrapper-' + userId);
    if (!wrapper) return;

    var allClasses = ['size-admin', 'text-admin', 'size-moderator', 'text-moderator', 'size-jury', 'text-jury', 'size-participant', 'text-participant'];
    wrapper.classList.remove.apply(wrapper.classList, allClasses);

    var newClass = roleId === 1 ? 'size-admin text-admin' :
                   roleId === 2 ? 'size-moderator text-moderator' :
                   roleId === 3 ? 'size-jury text-jury' :
                   'size-participant text-participant';

    wrapper.className = 'table-select-wrapper ' + newClass;
}

function confirmDelete(userId, login) {
    currentUserId = userId;
    currentAction = 'delete';
    document.getElementById('modalTitle').textContent = 'Удаление пользователя';
    document.getElementById('modalText').textContent = 'Вы уверены, что хотите удалить пользователя "' + login + '"? Это действие нельзя отменить.';
    document.getElementById('modalConfirmBtn').className = 'modal-btn modal-btn-danger';
    document.getElementById('modalConfirmBtn').textContent = 'Удалить';
    document.getElementById('modalOverlay').style.display = 'flex';
}

function confirmLogout() {
    currentAction = 'logout';
    document.getElementById('modalTitle').textContent = 'Выход';
    document.getElementById('modalText').textContent = 'Вы уверены, что хотите выйти из аккаунта?';
    document.getElementById('modalConfirmBtn').className = 'modal-btn modal-btn-confirm';
    document.getElementById('modalConfirmBtn').textContent = 'Выйти';
    document.getElementById('modalOverlay').style.display = 'flex';
}

function closeModal() {
    document.getElementById('modalOverlay').style.display = 'none';
    currentUserId = null;
    currentAction = null;
}

document.addEventListener('DOMContentLoaded', function() {
    document.getElementById('modalConfirmBtn').onclick = function() {
        if (currentAction === 'delete') {
            fetch('/Admin/DeleteUser', {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: 'userId=' + currentUserId
            })
            .then(function(response) {
                if (response.ok) {
                    var row = document.getElementById('user-row-' + currentUserId);
                    if (row) row.remove();
                }
                closeModal();
            });
        } else if (currentAction === 'logout') {
            window.location.href = '/Auth/Login';
        }
    };

    var logoutBtn = document.getElementById('logoutBtn');
    if (logoutBtn) {
        logoutBtn.onclick = function(e) {
            e.preventDefault();
            confirmLogout();
        };
    }

    document.getElementById('modalOverlay').onclick = function(e) {
        if (e.target === this) closeModal();
    };
});
