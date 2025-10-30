import { Bell, LogOut, Sparkles, User } from 'lucide-react'
import { useEffect, useState } from 'react'
import DarkModeToggle from './components/DarkModeToggle'
import Login from './components/Login'
import Register from './components/Register'
import ReminderForm from './components/ReminderForm'
import ReminderList from './components/ReminderList'
import { useDarkMode } from './hooks/useDarkMode'
import { api } from './services/api'
import { authService } from './services/authService'

function App() {
  const { isDarkMode, toggleDarkMode } = useDarkMode()
  const [isAuthenticated, setIsAuthenticated] = useState(false)
  const [showRegister, setShowRegister] = useState(false)
  const [reminders, setReminders] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [user, setUser] = useState(null)

  // Verificar autenticação ao iniciar
  useEffect(() => {
    const authenticated = authService.isAuthenticated()
    setIsAuthenticated(authenticated)
    
    if (authenticated) {
      const userData = authService.getUser()
      setUser(userData)
      loadReminders()
    } else {
      setLoading(false)
    }
  }, [])

  const loadReminders = async () => {
    try {
      setLoading(true)
      setError(null)
      const data = await api.getAllReminders()
      const sortedData = data.sort((a, b) => new Date(a.data) - new Date(b.data))
      setReminders(sortedData)
    } catch (err) {
      setError('Erro ao carregar lembretes.')
      console.error(err)
    } finally {
      setLoading(false)
    }
  }

  const handleLogin = () => {
    setIsAuthenticated(true)
    const userData = authService.getUser()
    setUser(userData)
    loadReminders()
  }

  const handleRegister = () => {
    setIsAuthenticated(true)
    const userData = authService.getUser()
    setUser(userData)
    loadReminders()
  }

  const handleLogout = () => {
    authService.logout()
    setIsAuthenticated(false)
    setUser(null)
    setReminders([])
  }

  const handleAddReminder = async (nome, data, horario) => {
    try {
      await api.createReminder(nome, data, horario)
      await loadReminders()
      return true
    } catch (err) {
      console.error('Erro ao adicionar lembrete:', err)
      setError(err.message || 'Erro ao adicionar lembrete')
      return false
    }
  }

  const handleDeleteReminder = async (id) => {
    try {
      await api.deleteReminder(id)
      setReminders(reminders.filter(r => r.id !== id))
    } catch (err) {
      console.error('Erro ao deletar lembrete:', err)
    }
  }

  // Se não estiver autenticado, mostrar login/registro
  if (!isAuthenticated) {
    return (
      <>
        {showRegister ? (
          <Register 
            onRegister={handleRegister} 
            onToggleMode={() => setShowRegister(false)}
          />
        ) : (
          <Login 
            onLogin={handleLogin} 
            onToggleMode={() => setShowRegister(true)}
          />
        )}
      </>
    )
  }

  // Interface principal (autenticado)
  return (
    <div className="min-h-screen py-8 px-4 sm:px-6 lg:px-8">
      <div className="max-w-4xl mx-auto">
        {/* Header */}
        <header className="text-center mb-12 animate-fade-in">
          <div className="flex items-center justify-between mb-4">
            <div className="flex items-center gap-2 text-gray-700 dark:text-gray-300">
              <User className="w-5 h-5" />
              <span className="font-medium">Olá, {user?.nome || 'Usuário'}!</span>
            </div>
            <div className="flex items-center gap-2">
              <DarkModeToggle isDarkMode={isDarkMode} onToggle={toggleDarkMode} />
              <button
                onClick={handleLogout}
                className="flex items-center gap-2 px-4 py-2 text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/30 rounded-lg transition-colors"
              >
                <LogOut className="w-4 h-4" />
                Sair
              </button>
            </div>
          </div>

          <div className="flex items-center justify-center gap-3 mb-4">
            <div className="relative">
              <Bell className="w-12 h-12 text-blue-600 dark:text-blue-400 animate-bounce-subtle" />
              <Sparkles className="w-5 h-5 text-yellow-400 dark:text-yellow-300 absolute -top-1 -right-1 animate-pulse" />
            </div>
          </div>
          <h1 className="text-5xl font-bold bg-gradient-to-r from-blue-600 via-indigo-600 to-purple-600 dark:from-blue-400 dark:via-indigo-400 dark:to-purple-400 bg-clip-text text-transparent mb-2">
            Sistema de Lembretes
          </h1>
          <p className="text-gray-600 dark:text-gray-400 text-lg">
            Organize suas tarefas de forma elegante e eficiente ✨
          </p>
        </header>

        {/* Error Message */}
        {error && (
          <div className="mb-6 p-4 bg-red-100 dark:bg-red-900/30 border-l-4 border-red-500 dark:border-red-400 text-red-700 dark:text-red-400 rounded-lg animate-slide-up">
            <p className="font-medium">⚠️ {error}</p>
          </div>
        )}

        {/* Formulário */}
        <div className="mb-8 animate-slide-up">
          <ReminderForm onAddReminder={handleAddReminder} />
        </div>

        {/* Lista de Lembretes */}
        <div className="animate-fade-in">
          {loading ? (
            <div className="glass-effect rounded-2xl p-12 text-center">
              <div className="inline-block animate-spin rounded-full h-12 w-12 border-4 border-blue-500 dark:border-blue-400 border-t-transparent"></div>
              <p className="mt-4 text-gray-600 dark:text-gray-400">Carregando lembretes...</p>
            </div>
          ) : (
            <ReminderList 
              reminders={reminders} 
              onDeleteReminder={handleDeleteReminder} 
            />
          )}
        </div>

        {/* Footer */}
      </div>
    </div>
  )
}

export default App

