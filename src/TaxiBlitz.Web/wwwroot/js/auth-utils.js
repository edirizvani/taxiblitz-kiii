/* TaxiBlitz Ohrid — Auth UI utilities
   Password toggle, strength meter, confirm match, submit loading state */

// ── Password show/hide toggle ─────────────────────────────────
function tbTogglePassword(btn) {
    var wrap  = btn.closest('.dt-pw-wrap');
    var input = wrap.querySelector('input');
    var isHidden = input.type === 'password';
    input.type = isHidden ? 'text' : 'password';
    var eyeOpen = btn.querySelector('.icon-eye');
    var eyeOff  = btn.querySelector('.icon-eye-off');
    if (eyeOpen)  eyeOpen.style.display  = isHidden ? 'none' : '';
    if (eyeOff)   eyeOff.style.display   = isHidden ? ''     : 'none';
    btn.setAttribute('aria-label', isHidden ? 'Hide password' : 'Show password');
}

// ── Password strength meter ───────────────────────────────────
function tbPasswordStrength(inputEl, strengthBarId) {
    var container = document.getElementById(strengthBarId);
    if (!container) return;
    var bars  = container.querySelectorAll('.dt-pw-strength-bar');
    var label = container.querySelector('.dt-pw-strength-label');
    var val   = inputEl.value;

    if (!val) {
        container.style.display = 'none';
        bars.forEach(function(b) { b.className = 'dt-pw-strength-bar'; });
        if (label) label.textContent = '';
        return;
    }

    container.style.display = 'flex';

    var score = 0;
    if (val.length >= 6)  score++;
    if (val.length >= 10) score++;
    if (/[A-Z]/.test(val) && /[a-z]/.test(val) && /[0-9]/.test(val)) score++;
    if (/[^A-Za-z0-9]/.test(val)) score++;
    score = Math.max(1, score);

    var labels = ['', 'Weak', 'Fair', 'Strong', 'Very strong'];
    bars.forEach(function(b, i) {
        b.className = 'dt-pw-strength-bar' + (i < score ? ' active-' + score : '');
    });
    if (label) label.textContent = labels[score];
}

// ── Confirm-password live match ───────────────────────────────
function tbConfirmMatch(confirmInput, passwordInputId, okId, errId) {
    var pwInput = document.getElementById(passwordInputId);
    var okEl    = document.getElementById(okId);
    var errEl   = document.getElementById(errId);
    if (!pwInput || !okEl || !errEl) return;

    var match = confirmInput.value === pwInput.value && confirmInput.value !== '';
    okEl.style.display  = match ? 'block' : 'none';
    errEl.style.display = (!match && confirmInput.value !== '') ? 'block' : 'none';
}

// ── Submit button loading state (prevent double-submit) ───────
document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('form').forEach(function (form) {
        var submitBtn = form.querySelector('.dt-auth-submit');
        if (!submitBtn) return;
        form.addEventListener('submit', function () {
            submitBtn.disabled = true;
            submitBtn.style.opacity = '0.65';
            submitBtn.style.cursor  = 'wait';
        });
    });

    // VerifyCode: auto-focus + auto-submit on 6 digits
    var codeInput = document.getElementById('Code');
    if (codeInput) {
        codeInput.focus();
        codeInput.addEventListener('input', function () {
            this.value = this.value.replace(/\D/g, '').slice(0, 6);
            if (this.value.length === 6) {
                var form = this.closest('form');
                if (form) form.submit();
            }
        });
    }
});
