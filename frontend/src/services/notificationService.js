// Serviço para gerenciar notificações push
const VAPID_PUBLIC_KEY = import.meta.env.VITE_VAPID_PUBLIC_KEY || '';

// Log da chave VAPID para debug (apenas primeiros caracteres)
if (VAPID_PUBLIC_KEY) {
  console.log('[NotificationService] Chave VAPID carregada:', VAPID_PUBLIC_KEY.substring(0, 20) + '...');
} else {
  console.warn('[NotificationService] ⚠️ Chave VAPID não encontrada! Verifique o arquivo .env');
}

// URL base da API
const API_BASE_URL = import.meta.env.VITE_API_URL 
  ? `${import.meta.env.VITE_API_URL}/api`
  : window.location.hostname === 'localhost'
    ? '/api'
    : 'https://lembretes-api.onrender.com/api';

class NotificationService {
  constructor() {
    this.registration = null;
    this.subscription = null;
  }

  // Verificar se o navegador suporta notificações
  isSupported() {
    return 'Notification' in window && 'serviceWorker' in navigator && 'PushManager' in window;
  }

  // Verificar se já tem permissão
  async getPermission() {
    if (!this.isSupported()) {
      return 'unsupported';
    }
    return Notification.permission;
  }

  // Solicitar permissão
  async requestPermission() {
    if (!this.isSupported()) {
      throw new Error('Notificações não são suportadas neste navegador');
    }

    const permission = await Notification.requestPermission();
    return permission;
  }

  // Registrar Service Worker
  async registerServiceWorker() {
    if (!('serviceWorker' in navigator)) {
      throw new Error('Service Workers não são suportados');
    }

    try {
      const registration = await navigator.serviceWorker.register('/sw.js', {
        scope: '/'
      });
      
      this.registration = registration;
      console.log('[NotificationService] Service Worker registrado:', registration);
      
      return registration;
    } catch (error) {
      console.error('[NotificationService] Erro ao registrar Service Worker:', error);
      throw error;
    }
  }

  // Converter chave VAPID para formato ArrayBuffer
  urlBase64ToUint8Array(base64String) {
    if (!base64String || typeof base64String !== 'string') {
      throw new Error('Chave VAPID inválida: deve ser uma string');
    }

    try {
      const padding = '='.repeat((4 - base64String.length % 4) % 4);
      const base64 = (base64String + padding)
        .replace(/\-/g, '+')
        .replace(/_/g, '/');

      const rawData = window.atob(base64);
      const outputArray = new Uint8Array(rawData.length);

      for (let i = 0; i < rawData.length; ++i) {
        outputArray[i] = rawData.charCodeAt(i);
      }
      return outputArray;
    } catch (error) {
      throw new Error('Erro ao converter chave VAPID. Verifique se a chave está no formato correto.');
    }
  }

  // Criar subscription para push
  async subscribe() {
    if (!this.registration) {
      await this.registerServiceWorker();
    }

    if (!VAPID_PUBLIC_KEY || VAPID_PUBLIC_KEY.trim() === '') {
      throw new Error('Chave VAPID pública não configurada. Verifique a variável VITE_VAPID_PUBLIC_KEY no arquivo .env');
    }

    try {
      // Verificar se o pushManager está disponível
      if (!this.registration.pushManager) {
        throw new Error('PushManager não está disponível. Verifique se o navegador suporta notificações push.');
      }

      // Aguardar o service worker estar ativo
      if (this.registration.waiting) {
        await this.registration.waiting;
      }
      if (this.registration.installing) {
        await this.registration.installing;
      }
      
      // Aguardar um pouco mais para garantir que o service worker está pronto
      await new Promise(resolve => setTimeout(resolve, 1000));

      // Verificar se o service worker está ativo
      if (!this.registration.active) {
        throw new Error('Service Worker não está ativo. Aguarde alguns segundos e tente novamente.');
      }

      // Tentar converter a chave VAPID primeiro para validar
      let applicationServerKey;
      try {
        console.log('[NotificationService] Convertendo chave VAPID:', VAPID_PUBLIC_KEY.substring(0, 30) + '...');
        applicationServerKey = this.urlBase64ToUint8Array(VAPID_PUBLIC_KEY);
        console.log('[NotificationService] ✅ Chave VAPID convertida com sucesso');
      } catch (keyError) {
        console.error('[NotificationService] ❌ Erro ao converter chave VAPID:', keyError);
        throw new Error('Chave VAPID inválida. Verifique se a chave está no formato correto.');
      }

      console.log('[NotificationService] Criando subscription push...');
      const subscription = await this.registration.pushManager.subscribe({
        userVisibleOnly: true,
        applicationServerKey: applicationServerKey
      });

      this.subscription = subscription;
      console.log('[NotificationService] ✅ Subscription criada com sucesso');
      console.log('[NotificationService] Endpoint:', subscription.endpoint);
      
      return subscription;
    } catch (error) {
      console.error('[NotificationService] Erro ao criar subscription:', error);
      console.error('[NotificationService] Detalhes do erro:', {
        name: error.name,
        message: error.message,
        stack: error.stack,
        registration: this.registration ? {
          active: !!this.registration.active,
          waiting: !!this.registration.waiting,
          installing: !!this.registration.installing,
          pushManager: !!this.registration.pushManager
        } : null
      });
      
      // Mensagens de erro mais amigáveis
      if (error.name === 'AbortError' || error.message.includes('push service error') || error.message.includes('Registration failed')) {
        // Verificar se está em HTTP (não HTTPS)
        const isHttp = location.protocol === 'http:';
        const isLocalhost = location.hostname === 'localhost' || location.hostname === '127.0.0.1';
        
        let errorMsg = 'Erro ao conectar com o serviço de notificações push.\n\n';
        
        if (isHttp && isLocalhost) {
          errorMsg += 'Soluções possíveis:\n';
          errorMsg += '1. Alguns navegadores bloqueiam push notifications em HTTP. Tente usar HTTPS.\n';
          errorMsg += '2. Desabilite extensões de privacidade (uBlock Origin, Privacy Badger, etc.)\n';
          errorMsg += '3. Tente em modo anônimo/privado\n';
          errorMsg += '4. Tente em outro navegador (Chrome geralmente funciona melhor)\n';
          errorMsg += '5. Verifique as configurações de notificações do navegador';
        } else {
          errorMsg += 'Possíveis causas:\n';
          errorMsg += '1. Extensões do navegador bloqueando notificações\n';
          errorMsg += '2. Firewall ou antivírus bloqueando a conexão\n';
          errorMsg += '3. Configurações do navegador bloqueando push notifications\n';
          errorMsg += '4. Tente usar outro navegador ou desabilitar extensões';
        }
        
        throw new Error(errorMsg);
      } else if (error.message.includes('Invalid key') || error.message.includes('chave VAPID')) {
        throw new Error('Chave VAPID inválida. Verifique a configuração no arquivo .env');
      } else if (error.message.includes('Permission denied') || error.message.includes('permissão')) {
        throw new Error('Permissão de notificações negada. Por favor, permita notificações nas configurações do navegador.');
      } else if (error.message.includes('Service Worker não está ativo')) {
        throw error;
      }
      
      throw error;
    }
  }

  // Obter subscription atual
  async getSubscription() {
    if (!this.registration) {
      await this.registerServiceWorker();
    }

    try {
      const subscription = await this.registration.pushManager.getSubscription();
      this.subscription = subscription;
      return subscription;
    } catch (error) {
      console.error('[NotificationService] Erro ao obter subscription:', error);
      return null;
    }
  }

  // Cancelar subscription
  async unsubscribe() {
    if (!this.registration) {
      await this.registerServiceWorker();
    }

    try {
      const subscription = await this.getSubscription();
      if (subscription) {
        await subscription.unsubscribe();
        this.subscription = null;
        console.log('[NotificationService] Subscription cancelada');
        return true;
      }
      return false;
    } catch (error) {
      console.error('[NotificationService] Erro ao cancelar subscription:', error);
      throw error;
    }
  }

  // Enviar subscription para o backend
  async sendSubscriptionToBackend(subscription, token) {
    try {
      console.log('[NotificationService] 📤 Enviando subscription para o backend...');
      console.log('[NotificationService] Endpoint:', subscription.endpoint);
      
      const p256dhKey = subscription.getKey('p256dh');
      const authKey = subscription.getKey('auth');
      
      const p256dhBase64 = this.arrayBufferToBase64(p256dhKey);
      const authBase64 = this.arrayBufferToBase64(authKey);
      
      console.log('[NotificationService] P256dh (primeiros 30 chars):', p256dhBase64.substring(0, 30) + '...');
      console.log('[NotificationService] Auth (primeiros 30 chars):', authBase64.substring(0, 30) + '...');

      const subscriptionData = {
        endpoint: subscription.endpoint,
        keys: {
          p256dh: p256dhBase64,
          auth: authBase64
        }
      };

      console.log('[NotificationService] Dados da subscription:', {
        endpoint: subscriptionData.endpoint,
        keysLength: {
          p256dh: subscriptionData.keys.p256dh.length,
          auth: subscriptionData.keys.auth.length
        }
      });

      const response = await fetch(`${API_BASE_URL}/notifications/subscribe`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify(subscriptionData)
      });

      const responseText = await response.text();
      console.log('[NotificationService] Resposta do backend:', response.status, responseText);

      if (!response.ok) {
        let errorMessage = 'Erro ao enviar subscription para o backend';
        try {
          const errorData = JSON.parse(responseText);
          errorMessage = errorData.message || errorData.error || errorMessage;
        } catch (e) {
          errorMessage = responseText || `Erro ${response.status}: ${response.statusText}`;
        }
        throw new Error(errorMessage);
      }

      const data = responseText ? JSON.parse(responseText) : {};
      console.log('[NotificationService] ✅ Subscription enviada com sucesso:', data);
      return data;
    } catch (error) {
      console.error('[NotificationService] ❌ Erro ao enviar subscription:', error);
      throw error;
    }
  }

  // Remover subscription do backend
  async removeSubscriptionFromBackend(token) {
    try {
      const response = await fetch(`${API_BASE_URL}/notifications/unsubscribe`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        }
      });

      if (!response.ok) {
        throw new Error('Erro ao remover subscription do backend');
      }

      return await response.json();
    } catch (error) {
      console.error('[NotificationService] Erro ao remover subscription:', error);
      throw error;
    }
  }

  // Converter ArrayBuffer para Base64
  arrayBufferToBase64(buffer) {
    const bytes = new Uint8Array(buffer);
    let binary = '';
    for (let i = 0; i < bytes.byteLength; i++) {
      binary += String.fromCharCode(bytes[i]);
    }
    return window.btoa(binary);
  }

  // Inicializar notificações (solicitar permissão, registrar SW, criar subscription)
  async initialize(token) {
    try {
      // Verificar suporte
      if (!this.isSupported()) {
        throw new Error('Notificações não são suportadas neste navegador');
      }

      // Verificar se está em HTTPS ou localhost
      if (location.protocol !== 'https:' && location.hostname !== 'localhost' && location.hostname !== '127.0.0.1') {
        throw new Error('Notificações push requerem HTTPS ou localhost');
      }

      // Verificar permissão
      let permission = await this.getPermission();
      if (permission === 'default') {
        permission = await this.requestPermission();
      }

      if (permission !== 'granted') {
        throw new Error('Permissão de notificações negada');
      }

      // Registrar Service Worker
      await this.registerServiceWorker();

      // Aguardar o service worker estar ativo
      if (this.registration.waiting) {
        await new Promise((resolve) => {
          this.registration.waiting.addEventListener('statechange', (e) => {
            if (e.target.state === 'activated') resolve();
          });
        });
      }
      if (this.registration.installing) {
        await new Promise((resolve) => {
          this.registration.installing.addEventListener('statechange', (e) => {
            if (e.target.state === 'activated') resolve();
          });
        });
      }
      
      // Aguardar um pouco mais para garantir que o service worker está pronto
      await new Promise(resolve => setTimeout(resolve, 1000));

      // Verificar se já existe subscription
      let subscription = await this.getSubscription();
      
      if (!subscription) {
        // Criar nova subscription
        subscription = await this.subscribe();
      }

      // Enviar para o backend
      if (subscription && token) {
        await this.sendSubscriptionToBackend(subscription, token);
      }

      return {
        success: true,
        subscription
      };
    } catch (error) {
      console.error('[NotificationService] Erro ao inicializar notificações:', error);
      throw error;
    }
  }

  // Desativar notificações
  async disable(token) {
    try {
      await this.unsubscribe();
      if (token) {
        await this.removeSubscriptionFromBackend(token);
      }
      return { success: true };
    } catch (error) {
      console.error('[NotificationService] Erro ao desativar notificações:', error);
      throw error;
    }
  }
}

export const notificationService = new NotificationService();
