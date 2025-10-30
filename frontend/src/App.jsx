import { Bell, LogOut, Sparkles, User } from 'lucide-react'
import { useEffect, useState } from 'react'
import DarkModeToggle from './components/DarkModeToggle'
import Dashboard from './components/Dashboard'
import ExportImport from './components/ExportImport'
import Login from './components/Login'
import Register from './components/Register'
import ReminderForm from './components/ReminderForm'
import ReminderList from './components/ReminderList'
import SearchAndFilters, { filterReminders } from './components/SearchAndFilters'
import { useDarkMode } from './hooks/useDarkMode'
import { api } from './services/api'
import { authService } from './services/authService'

function App() {
  const { isDarkMode, toggleDarkMode } = useDarkMode()
  const [isAuthenticated, setIsAuthenticated] = useState(false)
  const [showRegister, setShowRegister] = useState(false)
  const [reminders, setReminders] = useState([])
  const [allReminders, setAllReminders] = useState([]) // Todos os lembretes (sem filtros)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [user, setUser] = useState(null)
  const [searchTerm, setSearchTerm] = useState('')
  const [filter, setFilter] = useState('all')

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
      
      // Ordenar por data e horário (mais próximo primeiro)
      const sortedData = data.sort((a, b) => {
        // Comparar datas primeiro
        const dateA = new Date(a.data)
        const dateB = new Date(b.data)
        const dateDiff = dateA.getTime() - dateB.getTime()
        
        if (dateDiff !== 0) {
          return dateDiff
        }
        
        // Se a data for a mesma, comparar horários
        const horarioA = a.horario ? (typeof a.horario === 'string' ? a.horario : a.horario.toString()) : null
        const horarioB = b.horario ? (typeof b.horario === 'string' ? b.horario : b.horario.toString()) : null
        
        // Se nenhum tem horário, mantém ordem
        if (!horarioA && !horarioB) return 0
        
        // Se só um tem horário, o sem horário vai primeiro (ou pode ajustar conforme preferência)
        if (!horarioA) return -1
        if (!horarioB) return 1
        
        // Comparar horários (formato HH:mm ou HH:mm:ss)
        const timeA = horarioA.substring(0, 5) // Pega HH:mm
        const timeB = horarioB.substring(0, 5)
        
        return timeA.localeCompare(timeB)
      })
      
      setAllReminders(sortedData)
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
    setAllReminders([])
    setSearchTerm('')
    setFilter('all')
    setEditingReminder(null)
  }

  const [editingReminder, setEditingReminder] = useState(null)

  const handleAddReminder = async (nome, data, horario, descricao, categoria, id = null) => {
    try {
      if (id) {
        // Atualizar lembrete existente
        await api.updateReminder(id, nome, data, horario, descricao, categoria)
        setEditingReminder(null) // Limpar modo de edição
        await loadReminders()
      } else {
        // Criar novo lembrete
        await api.createReminder(nome, data, horario, descricao, categoria)
        await loadReminders()
      }
      return true
    } catch (err) {
      console.error('Erro ao adicionar/atualizar lembrete:', err)
      setError(err.message || 'Erro ao adicionar/atualizar lembrete')
      return false
    }
  }

  const handleEditReminder = (reminder) => {
    setEditingReminder(reminder)
    // Scroll suave até o formulário
    document.querySelector('.glass-effect')?.scrollIntoView({ behavior: 'smooth', block: 'start' })
  }

  const handleCancelEdit = () => {
    setEditingReminder(null)
  }

  const handleToggleComplete = async (id, isCompleted) => {
    try {
      if (isCompleted) {
        await api.markAsCompleted(id)
      } else {
        await api.markAsIncomplete(id)
      }
      // Atualizar o lembrete localmente
      const updated = allReminders.map(r => 
        r.id === id ? { ...r, concluido: isCompleted } : r
      )
      setAllReminders(updated)
      applyFilters(updated, searchTerm, filter)
    } catch (err) {
      console.error('Erro ao alterar status do lembrete:', err)
      setError(err.message || 'Erro ao alterar status do lembrete')
      // Recarregar em caso de erro
      await loadReminders()
    }
  }

  const handleDeleteReminder = async (id) => {
    try {
      await api.deleteReminder(id)
      const updated = allReminders.filter(r => r.id !== id)
      setAllReminders(updated)
      applyFilters(updated, searchTerm, filter)
      
      // Se estava editando o lembrete deletado, limpar edição
      if (editingReminder?.id === id) {
        setEditingReminder(null)
      }
    } catch (err) {
      console.error('Erro ao deletar lembrete:', err)
      setError(err.message || 'Erro ao deletar lembrete')
      // Recarregar em caso de erro
      await loadReminders()
    }
  }

  // Aplicar filtros aos lembretes
  const applyFilters = (reminderList, search, filterType) => {
    const filtered = filterReminders(reminderList, search, filterType)
    setReminders(filtered)
  }

  // Atualizar quando busca ou filtro mudar
  useEffect(() => {
    applyFilters(allReminders, searchTerm, filter)
  }, [searchTerm, filter, allReminders])

  // Importar lembretes
  const handleImportReminders = async (importedData) => {
    try {
      setLoading(true)
      setError(null)
      
      // Criar cada lembrete importado
      let successCount = 0
      let errorCount = 0
      
      for (const item of importedData) {
        try {
          // Converter data para formato correto
          let dataValue = item.data
          if (typeof dataValue === 'string') {
            // Se for string ISO completa, extrair apenas a data
            if (dataValue.includes('T')) {
              dataValue = dataValue.split('T')[0]
            }
          } else if (dataValue instanceof Date) {
            dataValue = dataValue.toISOString().split('T')[0]
          }
          
          // Converter horário
          let horarioValue = item.horario || null
          if (horarioValue && typeof horarioValue === 'string') {
            horarioValue = horarioValue.substring(0, 5) // HH:mm
          }
          
          await api.createReminder(
            item.nome,
            dataValue,
            horarioValue,
            item.descricao || null,
            item.categoria || null
          )
          successCount++
        } catch (err) {
          console.error(`Erro ao importar lembrete "${item.nome}":`, err)
          errorCount++
        }
      }
      
      // Recarregar lembretes
      await loadReminders()
      
      if (errorCount > 0) {
        setError(`${successCount} lembretes importados com sucesso, ${errorCount} falharam.`)
      } else {
        setError(null)
      }
    } catch (err) {
      console.error('Erro ao importar lembretes:', err)
      setError('Erro ao importar lembretes: ' + (err.message || 'Erro desconhecido'))
    } finally {
      setLoading(false)
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
      <div className="max-w-7xl mx-auto">
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

        {/* Formulário e Dashboard lado a lado */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-8 animate-slide-up">
          <div>
            <ReminderForm 
              onAddReminder={handleAddReminder} 
              editingReminder={editingReminder}
              onCancelEdit={handleCancelEdit}
            />
          </div>
          {!loading && allReminders.length > 0 && (
            <div>
              <Dashboard reminders={allReminders} />
            </div>
          )}
        </div>

        {/* Importar */}
        {!loading && (
          <div className="mb-6 animate-slide-up">
            <ExportImport 
              onImportSuccess={handleImportReminders}
            />
          </div>
        )}

        {/* Busca e Filtros */}
        <div className="mb-6 animate-slide-up">
          <SearchAndFilters
            searchTerm={searchTerm}
            onSearchChange={setSearchTerm}
            filter={filter}
            onFilterChange={setFilter}
          />
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
              onEditReminder={handleEditReminder}
              onToggleComplete={handleToggleComplete}
            />
          )}
        </div>

        {/* Footer */}
      </div>
    </div>
  )
}

export default App

