import { format, parseISO } from 'date-fns'
import { ptBR } from 'date-fns/locale/pt-BR'
import { Inbox, ListTodo } from 'lucide-react'
import { useMemo } from 'react'
import ReminderItem from './ReminderItem'

function ReminderList({ reminders, onDeleteReminder, onEditReminder, onToggleComplete }) {
  // Agrupar lembretes por data e ordenar por horário dentro de cada grupo
  const groupedReminders = useMemo(() => {
    const groups = {}
    
    reminders.forEach(reminder => {
      const date = format(parseISO(reminder.data), 'yyyy-MM-dd')
      if (!groups[date]) {
        groups[date] = []
      }
      groups[date].push(reminder)
    })
    
    // Ordenar lembretes dentro de cada grupo por horário
    Object.keys(groups).forEach(date => {
      groups[date].sort((a, b) => {
        const horarioA = a.horario ? (typeof a.horario === 'string' ? a.horario : a.horario.toString()) : null
        const horarioB = b.horario ? (typeof b.horario === 'string' ? b.horario : b.horario.toString()) : null
        
        // Se nenhum tem horário, mantém ordem
        if (!horarioA && !horarioB) return 0
        
        // Se só um tem horário, o sem horário vai primeiro
        if (!horarioA) return -1
        if (!horarioB) return 1
        
        // Comparar horários (formato HH:mm ou HH:mm:ss)
        const timeA = horarioA.substring(0, 5) // Pega HH:mm
        const timeB = horarioB.substring(0, 5)
        
        return timeA.localeCompare(timeB)
      })
    })
    
    return groups
  }, [reminders])

  const sortedDates = Object.keys(groupedReminders).sort()

  if (reminders.length === 0) {
    return (
      <div className="glass-effect rounded-2xl p-12 text-center">
        <Inbox className="w-16 h-16 text-gray-300 dark:text-gray-600 mx-auto mb-4" />
        <h3 className="text-xl font-semibold text-gray-600 dark:text-gray-400 mb-2">
          Nenhum lembrete ainda
        </h3>
        <p className="text-gray-500 dark:text-gray-500">
          Crie seu primeiro lembrete usando o formulário acima! 🚀
        </p>
      </div>
    )
  }

  return (
    <div className="glass-effect rounded-2xl p-8 shadow-2xl">
      <h2 className="text-2xl font-bold text-gray-800 dark:text-gray-100 mb-6 flex items-center gap-2">
        <ListTodo className="w-6 h-6 text-blue-600 dark:text-blue-400" />
        Meus Lembretes
        <span className="ml-auto text-sm font-normal bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300 px-3 py-1 rounded-full">
          {reminders.length} {reminders.length === 1 ? 'lembrete' : 'lembretes'}
        </span>
      </h2>

      <div className="space-y-6">
        {sortedDates.map((date, dateIndex) => {
          const dateObj = parseISO(date)
          const formattedDate = format(dateObj, "d 'de' MMMM 'de' yyyy", { locale: ptBR })
          const dayOfWeek = format(dateObj, 'EEEE', { locale: ptBR })
          
          // Calcular índice inicial para esta data (soma todos os lembretes das datas anteriores)
          let globalIndexStart = 0
          for (let i = 0; i < dateIndex; i++) {
            globalIndexStart += groupedReminders[sortedDates[i]].length
          }
          
          return (
            <div key={date} className="animate-slide-up">
              {/* Cabeçalho da Data */}
              <div className="mb-3 pb-2 border-b-2 border-blue-200 dark:border-blue-800">
                <h3 className="text-lg font-bold text-gray-700 dark:text-gray-300 capitalize">
                  📅 {formattedDate}
                </h3>
                <p className="text-sm text-gray-500 dark:text-gray-400 capitalize">
                  {dayOfWeek}
                </p>
              </div>
              
              {/* Lista de Lembretes da Data */}
              <div className="space-y-2 ml-4">
                {groupedReminders[date].map((reminder, localIndex) => (
                  <ReminderItem
                    key={reminder.id}
                    reminder={reminder}
                    onDelete={onDeleteReminder}
                    onEdit={onEditReminder}
                    onToggleComplete={onToggleComplete}
                    index={globalIndexStart + localIndex}
                  />
                ))}
              </div>
            </div>
          )
        })}
      </div>
    </div>
  )
}

export default ReminderList
