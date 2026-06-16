const CACHE_NAME = "po-approval-cache-v1";
const PRECACHE_ASSETS = [
  "/",
  "/manifest.json",
  "/favicon.png",
  "/icon-192.png",
  "/icon-512.png",
  "/apple-touch-icon.png"
];

// Install Event: cache static shell assets
self.addEventListener("install", (event) => {
  event.waitUntil(
    caches.open(CACHE_NAME).then((cache) => {
      console.log("[Service Worker] Pre-caching offline assets");
      return cache.addAll(PRECACHE_ASSETS);
    }).then(() => self.skipWaiting())
  );
});

// Activate Event: clear old caches
self.addEventListener("activate", (event) => {
  event.waitUntil(
    caches.keys().then((cacheNames) => {
      return Promise.all(
        cacheNames.map((cache) => {
          if (cache !== CACHE_NAME) {
            console.log("[Service Worker] Clearing old cache", cache);
            return caches.delete(cache);
          }
        })
      );
    }).then(() => self.clients.claim())
  );
});

// Fetch Event: handle offline strategies
self.addEventListener("fetch", (event) => {
  const url = new URL(event.request.url);

  // 1. Bypass API requests (always network-only)
  if (url.pathname.startsWith("/api/")) {
    return;
  }

  // 2. Bypass hot-reload or DevServer requests
  if (url.pathname.includes("__vite_") || url.pathname.includes("hot-update")) {
    return;
  }

  // 3. Navigation requests (HTML pages)
  if (event.request.mode === "navigate") {
    event.respondWith(
      fetch(event.request)
        .then((response) => {
          // Cache the latest index page for offline shell
          const responseClone = response.clone();
          caches.open(CACHE_NAME).then((cache) => {
            cache.put("/", responseClone);
          });
          return response;
        })
        .catch(() => {
          // If offline, return the cached index page
          return caches.match("/");
        })
    );
    return;
  }

  // 4. Static assets (JS, CSS, images, fonts)
  // Use Stale-While-Revalidate/Cache-First strategy
  event.respondWith(
    caches.match(event.request).then((cachedResponse) => {
      if (cachedResponse) {
        // Return cached version, and update in background if it's stylesheet/script
        if (event.request.url.startsWith(self.location.origin)) {
          fetch(event.request).then((networkResponse) => {
            if (networkResponse.status === 200) {
              caches.open(CACHE_NAME).then((cache) => {
                cache.put(event.request, networkResponse);
              });
            }
          }).catch(() => {/* ignore background update errors */});
        }
        return cachedResponse;
      }

      return fetch(event.request).then((networkResponse) => {
        // Cache newly fetched assets
        if (
          networkResponse &&
          networkResponse.status === 200 &&
          event.request.url.startsWith(self.location.origin)
        ) {
          const responseClone = networkResponse.clone();
          caches.open(CACHE_NAME).then((cache) => {
            cache.put(event.request, responseClone);
          });
        }
        return networkResponse;
      });
    })
  );
});
