import { useMemo } from 'react'
import { format, parseISO } from 'date-fns'
import { ptBR } from 'date-fns/locale/pt-BR'
import { ListTodo, Inbox } from 'lucide-react'
import ReminderItem from './ReminderItem'

function ReminderList({ reminders, onDeleteReminder }) {
  // Agrupar lembretes por data
  const groupedReminders = useMemo(() => {
    const groups = {}
    
    reminders.forEach(reminder => {
      const date = format(parseISO(reminder.data), 'yyyy-MM-dd')
      if (!groups[date]) {
        groups[date] = []
      }
      groups[date].push(reminder)
    })
    
    return groups
  }, [reminders])

  const sortedDates = Object.keys(groupedReminders).sort()

  if (reminders.length === 0) {
    return (
      <div className="glass-effect rounded-2xl p-12 text-center">
        <Inbox className="w-16 h-16 text-gray-300 mx-auto mb-4" />
        <h3 className="text-xl font-semibold text-gray-600 mb-2">
          Nenhum lembrete ainda
        </h3>
        <p className="text-gray-500">
          Crie seu primeiro lembrete usando o formulário acima! 🚀
        </p>
      </div>
    )
  }

  return (
    <div className="glass-effect rounded-2xl p-8 shadow-2xl">
      <h2 className="text-2xl font-bold text-gray-800 mb-6 flex items-center gap-2">
        <ListTodo className="w-6 h-6 text-blue-600" />
        Meus Lembretes
        <span className="ml-auto text-sm font-normal bg-blue-100 text-blue-700 px-3 py-1 rounded-full">
          {reminders.length} {reminders.length === 1 ? 'lembrete' : 'lembretes'}
        </span>
      </h2>

      <div className="space-y-6">
        {sortedDates.map((date) => {
          const dateObj = parseISO(date)
          const formattedDate = format(dateObj, "d 'de' MMMM 'de' yyyy", { locale: ptBR })
          const dayOfWeek = format(dateObj, 'EEEE', { locale: ptBR })
          
          return (
            <div key={date} className="animate-slide-up">
              {/* Cabeçalho da Data */}
              <div className="mb-3 pb-2 border-b-2 border-blue-200">
                <h3 className="text-lg font-bold text-gray-700 capitalize">
                  📅 {formattedDate}
                </h3>
                <p className="text-sm text-gray-500 capitalize">
                  {dayOfWeek}
                </p>
              </div>
              
              {/* Lista de Lembretes da Data */}
              <div className="space-y-2 ml-4">
                {groupedReminders[date].map((reminder, index) => (
                  <ReminderItem
                    key={reminder.id}
                    reminder={reminder}
                    onDelete={onDeleteReminder}
                    index={index}
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
