import { addDays, isPast, isThisWeek, isToday, parseISO, startOfToday } from 'date-fns'
import { ptBR } from 'date-fns/locale/pt-BR'
import { Filter, Search, X } from 'lucide-react'

function SearchAndFilters({ searchTerm, onSearchChange, filter, onFilterChange }) {
  const filters = [
    { value: 'all', label: 'Todos' },
    { value: 'today', label: 'Hoje' },
    { value: 'week', label: 'Esta Semana' },
    { value: 'next7days', label: 'Próximos 7 Dias' },
    { value: 'overdue', label: 'Atrasados' },
    { value: 'completed', label: 'Concluídos' },
    { value: 'pending', label: 'Pendentes' },
  ]

  return (
    <div className="glass-effect rounded-2xl p-6 shadow-2xl mb-6">
      <div className="flex flex-col sm:flex-row gap-4">
        {/* Campo de Busca */}
        <div className="flex-1">
          <label htmlFor="search" className="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">
            Buscar Lembretes
          </label>
          <div className="relative">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-5 h-5 text-gray-400" />
            <input
              type="text"
              id="search"
              value={searchTerm}
              onChange={(e) => onSearchChange(e.target.value)}
              placeholder="Digite o nome do lembrete..."
              className="input-field pl-10 pr-10 w-full"
            />
            {searchTerm && (
              <button
                onClick={() => onSearchChange('')}
                className="absolute right-3 top-1/2 transform -translate-y-1/2 text-gray-400 hover:text-gray-600 dark:hover:text-gray-300"
                title="Limpar busca"
              >
                <X className="w-4 h-4" />
              </button>
            )}
          </div>
        </div>

        {/* Filtros */}
        <div className="sm:w-64">
          <label htmlFor="filter" className="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">
            <Filter className="w-4 h-4 inline mr-1" />
            Filtrar
          </label>
          <select
            id="filter"
            value={filter}
            onChange={(e) => onFilterChange(e.target.value)}
            className="input-field w-full"
          >
            {filters.map((f) => (
              <option key={f.value} value={f.value}>
                {f.label}
              </option>
            ))}
          </select>
        </div>
      </div>
    </div>
  )
}

// Função auxiliar para filtrar lembretes
export function filterReminders(reminders, searchTerm, filterType) {
  let filtered = [...reminders]

  // Aplicar busca por nome
  if (searchTerm) {
    const searchLower = searchTerm.toLowerCase()
    filtered = filtered.filter(reminder => 
      reminder.nome?.toLowerCase().includes(searchLower) ||
      reminder.descricao?.toLowerCase().includes(searchLower)
    )
  }

  // Aplicar filtros por data/status
  const today = startOfToday()
  const next7Days = addDays(today, 7)

  switch (filterType) {
    case 'today':
      filtered = filtered.filter(reminder => {
        try {
          const reminderDate = typeof reminder.data === 'string' 
            ? parseISO(reminder.data) 
            : new Date(reminder.data)
          return isToday(reminderDate)
        } catch {
          return false
        }
      })
      break

    case 'week':
      filtered = filtered.filter(reminder => {
        try {
          const reminderDate = typeof reminder.data === 'string' 
            ? parseISO(reminder.data) 
            : new Date(reminder.data)
          return isThisWeek(reminderDate, { locale: ptBR })
        } catch {
          return false
        }
      })
      break

    case 'next7days':
      filtered = filtered.filter(reminder => {
        try {
          const reminderDate = typeof reminder.data === 'string' 
            ? parseISO(reminder.data) 
            : new Date(reminder.data)
          return !isPast(reminderDate) && reminderDate <= next7Days
        } catch {
          return false
        }
      })
      break

    case 'overdue':
      filtered = filtered.filter(reminder => {
        try {
          const reminderDate = typeof reminder.data === 'string' 
            ? parseISO(reminder.data) 
            : new Date(reminder.data)
          return isPast(reminderDate) && !reminder.concluido
        } catch {
          return false
        }
      })
      break

    case 'completed':
      filtered = filtered.filter(reminder => reminder.concluido)
      break

    case 'pending':
      filtered = filtered.filter(reminder => !reminder.concluido)
      break

    case 'all':
    default:
      // Não filtra nada
      break
  }

  return filtered
}

export default SearchAndFilters

