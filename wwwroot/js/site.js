// Auto-dismiss alerts after 5s
document.querySelectorAll('.alert').forEach(el => {
    setTimeout(() => el.remove(), 5000);
});

const checkInInputs = document.querySelectorAll('input[name="CheckIn"], input[name="checkIn"]');
const checkOutInputs = document.querySelectorAll('input[name="CheckOut"], input[name="checkOut"]');
checkInInputs.forEach(ci => {
    ci.addEventListener('change', () => {
        checkOutInputs.forEach(co => {
            if (ci.value) {
                const next = new Date(ci.value);
                next.setDate(next.getDate() + 1);
                co.min = next.toISOString().split('T')[0];
                if (co.value && co.value <= ci.value) co.value = '';
            }
        });
    });
});

document.addEventListener('DOMContentLoaded', function () {
    const avatarBtn = document.querySelector('.nav-avatar-btn');
    const navUser = document.querySelector('.nav-user');

    if (avatarBtn) {
        avatarBtn.addEventListener('click', function (e) {
            e.stopPropagation();
            navUser.classList.toggle('open');
        });

        document.addEventListener('click', function () {
            navUser.classList.remove('open');
        });

        navUser.querySelector('.nav-dropdown').addEventListener('click', function (e) {
            e.stopPropagation();
        });
    }
});