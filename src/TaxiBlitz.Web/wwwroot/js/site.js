// ── Theme toggle ─────────────────────────────────────────────────────────────
(function () {
    var saved = localStorage.getItem('tb-theme') || 'dark';
    if (saved === 'light') document.documentElement.setAttribute('data-theme', 'light');
})();

function tbToggleTheme() {
    var current = document.documentElement.getAttribute('data-theme') || 'dark';
    var next = current === 'light' ? 'dark' : 'light';
    if (next === 'dark') {
        document.documentElement.removeAttribute('data-theme');
    } else {
        document.documentElement.setAttribute('data-theme', 'light');
    }
    localStorage.setItem('tb-theme', next);
}

// ── Favourite / wishlist AJAX ─────────────────────────────────────────────────
function tbToggleFavourite(tourId, btn) {
    var token = document.querySelector('input[name="__RequestVerificationToken"]');
    fetch('/Favourites/Toggle/' + tourId, {
        method: 'POST',
        headers: {
            'X-Requested-With': 'XMLHttpRequest',
            'RequestVerificationToken': token ? token.value : ''
        }
    })
    .then(function (r) { return r.ok ? r.json() : null; })
    .then(function (data) {
        if (!data) return;
        var icon = btn.querySelector('.fav-icon');
        if (icon) icon.setAttribute('fill', data.isFavourite ? 'currentColor' : 'none');
        btn.title = data.isFavourite ? 'Remove from wishlist' : 'Add to wishlist';
        btn.classList.toggle('fav-active', data.isFavourite);
    })
    .catch(function () {});
}

// ── Copy to clipboard ─────────────────────────────────────────────────────────
function tbCopyToClipboard(text, btn) {
    if (!navigator.clipboard) { return; }
    navigator.clipboard.writeText(text).then(function () {
        var orig = btn.textContent;
        btn.textContent = 'Copied!';
        setTimeout(function () { btn.textContent = orig; }, 1800);
    });
}
