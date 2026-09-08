(() => {
    const button = document.getElementById('toggle-password');
    const password = document.getElementById('Input_Password');
    if (!button || !password) return;
    button.hidden = false;
    button.addEventListener('click', () => {
        const show = password.type === 'password';
        password.type = show ? 'text' : 'password';
        button.textContent = show ? 'লুকান' : 'দেখুন';
        button.setAttribute('aria-pressed', String(show));
    });
})();
