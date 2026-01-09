// Service Worker para gerenciar notificações push
const CACHE_NAME = 'lembretes-v1';
const STATIC_CACHE_URLS = [
  '/',
  '/index.html',
  '/manifest.json'
];

// Instalar Service Worker
self.addEventListener('install', (event) => {
  console.log('[Service Worker] Instalando...');
  event.waitUntil(
    caches.open(CACHE_NAME)
      .then((cache) => {
        console.log('[Service Worker] Cache aberto');
        return cache.addAll(STATIC_CACHE_URLS);
      })
      .then(() => self.skipWaiting())
  );
});

// Ativar Service Worker
self.addEventListener('activate', (event) => {
  console.log('[Service Worker] Ativando...');
  event.waitUntil(
    caches.keys().then((cacheNames) => {
      return Promise.all(
        cacheNames.map((cacheName) => {
          if (cacheName !== CACHE_NAME) {
            console.log('[Service Worker] Removendo cache antigo:', cacheName);
            return caches.delete(cacheName);
          }
        })
      );
    }).then(() => self.clients.claim())
  );
});

// Interceptar requisições para cache
self.addEventListener('fetch', (event) => {
  const url = new URL(event.request.url);
  
  // Ignorar requisições do Vite dev server em desenvolvimento
  if (url.pathname.startsWith('/@vite') || 
      url.pathname.startsWith('/@react-refresh') ||
      url.pathname.startsWith('/@id/') ||
      url.pathname.includes('node_modules') ||
      url.pathname.includes('.vite') ||
      url.pathname.includes('?t=')) { // Ignorar arquivos com timestamp do Vite
    // Deixar passar direto sem interceptar
    return;
  }
  
  // Ignorar requisições que não são GET
  if (event.request.method !== 'GET') {
    return;
  }
  
  // Ignorar requisições de API
  if (url.pathname.startsWith('/api/')) {
    return;
  }
  
  event.respondWith(
    caches.match(event.request)
      .then((response) => {
        // Se encontrou no cache, retornar
        if (response) {
          return response;
        }
        // Caso contrário, buscar da rede
        return fetch(event.request).catch((error) => {
          console.error('[Service Worker] Erro ao buscar recurso:', error);
          // Se falhar, retornar undefined para deixar o navegador lidar
          return undefined;
        });
      })
  );
});

// Receber mensagens push
self.addEventListener('push', (event) => {
  console.log('[Service Worker] 📬 Push recebido:', event);
  console.log('[Service Worker] Event.data:', event.data);
  
  let notificationData = {
    title: 'Lembrete',
    body: 'Você tem um lembrete!',
    icon: '/icon-192x192.png',
    badge: '/icon-192x192.png',
    tag: 'lembrete-notification',
    requireInteraction: false,
    data: {}
  };

  if (event.data) {
    try {
      console.log('[Service Worker] Tentando fazer parse do JSON...');
      const data = event.data.json();
      console.log('[Service Worker] ✅ Dados parseados:', data);
      
      notificationData = {
        title: data.title || 'Lembrete',
        body: data.body || 'Você tem um lembrete!',
        icon: data.icon || '/icon-192x192.png',
        badge: data.badge || '/icon-192x192.png',
        tag: data.tag || 'lembrete-notification',
        requireInteraction: data.requireInteraction || false,
        data: data.data || {}
      };
    } catch (e) {
      console.error('[Service Worker] ❌ Erro ao parsear dados do push:', e);
      try {
        const text = event.data.text();
        console.log('[Service Worker] Dados como texto:', text);
        notificationData.body = text || 'Você tem um lembrete!';
      } catch (textError) {
        console.error('[Service Worker] ❌ Erro ao ler dados como texto:', textError);
      }
    }
  } else {
    console.warn('[Service Worker] ⚠️ Event.data está vazio ou null');
  }

  console.log('[Service Worker] 📤 Exibindo notificação:', notificationData);

  event.waitUntil(
    self.registration.showNotification(notificationData.title, {
      body: notificationData.body,
      icon: notificationData.icon,
      badge: notificationData.badge,
      tag: notificationData.tag,
      requireInteraction: notificationData.requireInteraction,
      data: notificationData.data,
      vibrate: [200, 100, 200],
      actions: [
        {
          action: 'open',
          title: 'Abrir',
          icon: undefined
        },
        {
          action: 'close',
          title: 'Fechar'
        }
      ]
    }).then(() => {
      console.log('[Service Worker] ✅ Notificação exibida com sucesso!');
    }).catch((error) => {
      console.error('[Service Worker] ❌ Erro ao exibir notificação:', error);
    })
  );
});

// Clique na notificação
self.addEventListener('notificationclick', (event) => {
  console.log('[Service Worker] Notificação clicada:', event);
  
  event.notification.close();

  if (event.action === 'close') {
    return;
  }

  // Abrir ou focar a aplicação
  event.waitUntil(
    clients.matchAll({ type: 'window', includeUncontrolled: true })
      .then((clientList) => {
        // Se já existe uma janela aberta, focar nela
        for (let i = 0; i < clientList.length; i++) {
          const client = clientList[i];
          if (client.url === '/' && 'focus' in client) {
            return client.focus();
          }
        }
        // Se não existe, abrir nova janela
        if (clients.openWindow) {
          return clients.openWindow('/');
        }
      })
  );
});

// Sincronização em background (para quando o navegador voltar online)
self.addEventListener('sync', (event) => {
  console.log('[Service Worker] Background sync:', event.tag);
  if (event.tag === 'sync-reminders') {
    event.waitUntil(
      // Aqui você pode adicionar lógica para sincronizar dados
      Promise.resolve()
    );
  }
});
