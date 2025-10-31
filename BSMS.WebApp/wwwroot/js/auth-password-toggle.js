// Password visibility toggle for auth pages
(function () {
    function toggle(btn) {
        const wrapper = btn.closest('.input-wrapper');
        if (!wrapper) return;
        const input = wrapper.querySelector('input.form-input');
        if (!input) return;
        const show = input.type === 'password';
        input.type = show ? 'text' : 'password';
        btn.classList.toggle('is-on', show);
        btn.setAttribute('aria-pressed', show ? 'true' : 'false');
    }

    document.addEventListener('click', function (e) {
        const btn = e.target.closest('.toggle-password');
        if (btn) {
            e.preventDefault();
            toggle(btn);
        }
    });
})();