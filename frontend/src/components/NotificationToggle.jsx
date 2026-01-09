import { Bell, BellOff } from 'lucide-react'
import { useEffect, useState } from 'react'
import { notificationService } from '../services/notificationService'
import { authService } from '../services/authService'

function NotificationToggle() {
  const [isEnabled, setIsEnabled] = useState(false)
  const [isLoading, setIsLoading] = useState(true)
  const [isSupported, setIsSupported] = useState(false)
  const [error, setError] = useState(null)

  useEffect(() => {
    checkNotificationStatus()
  }, [])

  const checkNotificationStatus = async () => {
    try {
      setIsLoading(true)
      setError(null)

      const supported = notificationService.isSupported()
      setIsSupported(supported)

      if (!supported) {
        setIsLoading(false)
        return
      }

      const permission = await notificationService.getPermission()
      const subscription = await notificationService.getSubscription()

      setIsEnabled(permission === 'granted' && subscription !== null)
    } catch (err) {
      console.error('Erro ao verificar status de notificações:', err)
      setError('Erro ao verificar notificações')
    } finally {
      setIsLoading(false)
    }
  }

  const handleToggle = async () => {
    try {
      setIsLoading(true)
      setError(null)

      const token = authService.getToken()
      if (!token) {
        setError('Você precisa estar autenticado')
        return
      }

      if (isEnabled) {
        // Desativar notificações
        await notificationService.disable(token)
        setIsEnabled(false)
      } else {
        // Ativar notificações
        await notificationService.initialize(token)
        setIsEnabled(true)
      }
    } catch (err) {
      console.error('Erro ao alternar notificações:', err)
      
      // Mensagens de erro mais amigáveis
      let errorMessage = 'Erro ao alternar notificações';
      if (err.message) {
        errorMessage = err.message;
      } else if (err.name === 'AbortError') {
        errorMessage = 'Erro ao conectar com o serviço de notificações. Tente novamente ou verifique sua conexão.';
      } else if (err.message?.includes('VAPID')) {
        errorMessage = 'Chave de notificações não configurada. Contate o administrador.';
      } else if (err.message?.includes('HTTPS')) {
        errorMessage = 'Notificações push requerem HTTPS ou localhost.';
      } else if (err.message?.includes('permissão') || err.message?.includes('Permission')) {
        errorMessage = 'Permissão de notificações negada. Permita nas configurações do navegador.';
      }
      
      setError(errorMessage)
    } finally {
      setIsLoading(false)
    }
  }

  if (!isSupported) {
    return null // Não mostrar se não for suportado
  }

  const handleTestNotification = async () => {
    try {
      setIsLoading(true)
      setError(null)

      const token = authService.getToken()
      if (!token) {
        setError('Você precisa estar autenticado')
        return
      }

      if (!isEnabled) {
        setError('Ative as notificações primeiro antes de testar')
        return
      }

      const response = await fetch('/api/notifications/test', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify({
          title: '🔔 Teste de Notificação',
          body: 'Esta é uma notificação de teste! Se você está vendo isso, as notificações estão funcionando corretamente.'
        })
      })

      if (!response.ok) {
        let errorMessage = 'Erro ao enviar notificação de teste'
        try {
          const contentType = response.headers.get('content-type')
          if (contentType && contentType.includes('application/json')) {
            const data = await response.json()
            errorMessage = data.message || data.error || errorMessage
          } else {
            const text = await response.text()
            errorMessage = `Erro ${response.status}: ${text.substring(0, 200)}`
          }
        } catch (parseError) {
          errorMessage = `Erro ${response.status}: Não foi possível processar a resposta do servidor`
        }
        throw new Error(errorMessage)
      }

      // Verificar se há conteúdo antes de tentar fazer parse do JSON
      const contentType = response.headers.get('content-type')
      const text = await response.text()
      
      if (!text || text.trim().length === 0) {
        // Se não houver conteúdo, considerar como sucesso
        setError(null)
        const successMsg = '✅ Notificação de teste enviada! Verifique se apareceu no seu navegador.'
        setError(successMsg)
        setTimeout(() => setError(null), 5000)
        return
      }

      // Tentar fazer parse do JSON apenas se houver conteúdo
      let data = null
      if (contentType && contentType.includes('application/json')) {
        try {
          data = JSON.parse(text)
        } catch (parseError) {
          console.warn('Resposta não é um JSON válido:', text)
          // Mesmo assim, considerar como sucesso se a resposta foi OK
          setError(null)
          const successMsg = '✅ Notificação de teste enviada! Verifique se apareceu no seu navegador.'
          setError(successMsg)
          setTimeout(() => setError(null), 5000)
          return
        }
      }

      setError(null)
      // Mostrar mensagem de sucesso temporariamente
      const successMsg = data?.message || '✅ Notificação de teste enviada! Verifique se apareceu no seu navegador.'
      setError(successMsg)
      setTimeout(() => setError(null), 5000)
    } catch (err) {
      console.error('Erro ao testar notificação:', err)
      setError(err.message || 'Erro ao testar notificação')
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <div className="flex flex-col gap-2">
      <div className="flex items-center gap-2">
        {error && (
          <div className={`text-xs max-w-md whitespace-pre-line ${
            error.startsWith('✅') 
              ? 'text-green-600 dark:text-green-400' 
              : 'text-red-600 dark:text-red-400'
          }`}>
            {error}
          </div>
        )}
        <button
          onClick={handleToggle}
          disabled={isLoading}
          className={`flex items-center gap-2 px-4 py-2 rounded-lg transition-colors ${
            isEnabled
              ? 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400 hover:bg-green-200 dark:hover:bg-green-900/50'
              : 'bg-gray-100 dark:bg-gray-800 text-gray-700 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-700'
          } ${isLoading ? 'opacity-50 cursor-not-allowed' : ''}`}
          title={isEnabled ? 'Desativar notificações' : 'Ativar notificações'}
        >
          {isLoading ? (
            <div className="w-4 h-4 border-2 border-current border-t-transparent rounded-full animate-spin"></div>
          ) : isEnabled ? (
            <Bell className="w-4 h-4" />
          ) : (
            <BellOff className="w-4 h-4" />
          )}
          <span className="text-sm font-medium">
            {isEnabled ? 'Notificações Ativas' : 'Ativar Notificações'}
          </span>
        </button>
        {isEnabled && (
          <button
            onClick={handleTestNotification}
            disabled={isLoading}
            className="flex items-center gap-2 px-3 py-2 rounded-lg bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400 hover:bg-blue-200 dark:hover:bg-blue-900/50 transition-colors text-sm font-medium"
            title="Enviar notificação de teste"
          >
            🧪 Testar
          </button>
        )}
      </div>
    </div>
  )
}

export default NotificationToggle
