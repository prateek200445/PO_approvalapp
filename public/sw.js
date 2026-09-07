const CACHE_NAME = "po-approval-cache-v2";
const PRECACHE_ASSETS = [
  "/manifest.json",
  "/favicon.png",
  "/icon-192.png",
  "/icon-512.png",
  "/apple-touch-icon.png",
];

// Install: cache only stable shell icons (never hashed JS/CSS or HTML)
self.addEventListener("install", (event) => {
  event.waitUntil(
    caches
      .open(CACHE_NAME)
      .then((cache) => cache.addAll(PRECACHE_ASSETS))
      .then(() => self.skipWaiting()),
  );
});

// Activate: drop every old cache so deploys cannot serve stale hashed assets
self.addEventListener("activate", (event) => {
  event.waitUntil(
    caches
      .keys()
      .then((names) =>
        Promise.all(names.filter((name) => name !== CACHE_NAME).map((name) => caches.delete(name))),
      )
      .then(() => self.clients.claim()),
  );
});

self.addEventListener("fetch", (event) => {
  const url = new URL(event.request.url);

  // Never intercept API, Vite HMR, or hashed build assets — always network.
  // Cache-first on /assets/*.js was breaking desktop after each Vercel deploy.
  if (
    url.pathname.startsWith("/api/") ||
    url.pathname.startsWith("/assets/") ||
    url.pathname.includes("__vite_") ||
    url.pathname.includes("hot-update")
  ) {
    return;
  }

  // Navigations: always prefer network (never serve stale HTML that points at deleted JS hashes)
  if (event.request.mode === "navigate") {
    event.respondWith(fetch(event.request));
    return;
  }

  // Other same-origin static files (icons, etc.): stale-while-revalidate
  if (event.request.url.startsWith(self.location.origin)) {
    event.respondWith(
      caches.match(event.request).then((cached) => {
        const network = fetch(event.request)
          .then((response) => {
            if (response && response.status === 200) {
              const clone = response.clone();
              caches.open(CACHE_NAME).then((cache) => cache.put(event.request, clone));
            }
            return response;
          })
          .catch(() => cached);
        return cached || network;
      }),
    );
  }
});
