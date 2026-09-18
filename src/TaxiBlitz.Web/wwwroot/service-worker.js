const CACHE = 'taxiblitz-v2';
const SHELL = [
    '/',
    '/css/taxiblitz-design.css',
    '/js/site.js',
    '/lib/jquery/jquery.min.js',
    '/lib/bootstrap/bootstrap.bundle.min.js',
    '/img/background.jpg',
    '/manifest.json'
];

self.addEventListener('install', function (e) {
    e.waitUntil(
        caches.open(CACHE).then(function (c) { return c.addAll(SHELL); })
    );
    self.skipWaiting();
});

self.addEventListener('activate', function (e) {
    e.waitUntil(
        caches.keys().then(function (keys) {
            return Promise.all(keys.filter(function (k) { return k !== CACHE; }).map(function (k) { return caches.delete(k); }));
        })
    );
    self.clients.claim();
});

self.addEventListener('fetch', function (e) {
    var req = e.request;
    if (req.method !== 'GET') return;
    var url = new URL(req.url);

    // Static assets: cache-first
    if (url.pathname.match(/\.(css|js|woff2?|png|jpg|jpeg|webp|svg|ico)$/)) {
        e.respondWith(
            caches.match(req).then(function (cached) {
                return cached || fetch(req).then(function (res) {
                    if (!res || res.status !== 200) return res;
                    var clone = res.clone();
                    caches.open(CACHE).then(function (c) { c.put(req, clone); });
                    return res;
                });
            })
        );
        return;
    }

    // HTML pages: network-first, fall back to cache
    if (req.headers.get('accept') && req.headers.get('accept').includes('text/html')) {
        e.respondWith(
            fetch(req).then(function (res) {
                var clone = res.clone();
                caches.open(CACHE).then(function (c) { c.put(req, clone); });
                return res;
            }).catch(function () {
                return caches.match(req).then(function (c) { return c || caches.match('/'); });
            })
        );
    }
});
