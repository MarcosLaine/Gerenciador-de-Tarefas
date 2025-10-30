import { addDays, isPast, isToday, parseISO, startOfToday } from 'date-fns'
import { AlertCircle, Calendar, CheckCircle2, Clock, Tag, TrendingUp } from 'lucide-react'
import { useMemo } from 'react'

function Dashboard({ reminders }) {
  const stats = useMemo(() => {
    const total = reminders.length
    const concluidos = reminders.filter(r => r.concluido).length
    const pendentes = total - concluidos
    
    // Lembretes hoje
    const hoje = reminders.filter(r => {
      try {
        const reminderDate = typeof r.data === 'string' ? parseISO(r.data) : new Date(r.data)
        return isToday(reminderDate) && !r.concluido
      } catch {
        return false
      }
    }).length
    
    // Lembretes atrasados
    const atrasados = reminders.filter(r => {
      try {
        const reminderDate = typeof r.data === 'string' ? parseISO(r.data) : new Date(r.data)
        // Combinar com horário se existir
        let dataCompleta = reminderDate
        if (r.horario) {
          const year = reminderDate.getUTCFullYear()
          const month = reminderDate.getUTCMonth()
          const day = reminderDate.getUTCDate()
          const horarioStr = typeof r.horario === 'string' ? r.horario : r.horario.toString()
          const [hours, minutes] = horarioStr.split(':').map(Number)
          dataCompleta = new Date(year, month, day, hours || 0, minutes || 0, 0)
        }
        return isPast(dataCompleta) && !r.concluido
      } catch {
        return false
      }
    }).length
    
    // Próximos 7 dias
    const proximos7Dias = reminders.filter(r => {
      try {
        const reminderDate = typeof r.data === 'string' ? parseISO(r.data) : new Date(r.data)
        const today = startOfToday()
        const next7Days = addDays(today, 7)
        const reminderDateOnly = new Date(reminderDate.getUTCFullYear(), reminderDate.getUTCMonth(), reminderDate.getUTCDate())
        const todayDate = new Date(today.getFullYear(), today.getMonth(), today.getDate())
        const next7DaysDate = new Date(next7Days.getFullYear(), next7Days.getMonth(), next7Days.getDate())
        return reminderDateOnly >= todayDate && reminderDateOnly <= next7DaysDate && !r.concluido
      } catch {
        return false
      }
    }).length
    
    // Estatísticas por categoria
    const porCategoria = {}
    reminders.forEach(r => {
      const cat = r.categoria || 'Sem categoria'
      if (!porCategoria[cat]) {
        porCategoria[cat] = { total: 0, concluidos: 0 }
      }
      porCategoria[cat].total++
      if (r.concluido) {
        porCategoria[cat].concluidos++
      }
    })
    
    return {
      total,
      concluidos,
      pendentes,
      hoje,
      atrasados,
      proximos7Dias,
      porCategoria
    }
  }, [reminders])

  const statCards = [
    {
      title: 'Total de Lembretes',
      value: stats.total,
      icon: Calendar,
      color: 'bg-blue-500 dark:bg-blue-600',
      bgColor: 'bg-blue-100 dark:bg-blue-900/30'
    },
    {
      title: 'Concluídos',
      value: stats.concluidos,
      icon: CheckCircle2,
      color: 'bg-green-500 dark:bg-green-600',
      bgColor: 'bg-green-100 dark:bg-green-900/30'
    },
    {
      title: 'Pendentes',
      value: stats.pendentes,
      icon: Clock,
      color: 'bg-yellow-500 dark:bg-yellow-600',
      bgColor: 'bg-yellow-100 dark:bg-yellow-900/30'
    },
    {
      title: 'Hoje',
      value: stats.hoje,
      icon: AlertCircle,
      color: 'bg-orange-500 dark:bg-orange-600',
      bgColor: 'bg-orange-100 dark:bg-orange-900/30'
    },
    {
      title: 'Atrasados',
      value: stats.atrasados,
      icon: TrendingUp,
      color: 'bg-red-500 dark:bg-red-600',
      bgColor: 'bg-red-100 dark:bg-red-900/30'
    },
    {
      title: 'Próximos 7 Dias',
      value: stats.proximos7Dias,
      icon: Calendar,
      color: 'bg-purple-500 dark:bg-purple-600',
      bgColor: 'bg-purple-100 dark:bg-purple-900/30'
    }
  ]

  return (
    <div className="glass-effect rounded-2xl p-6 shadow-2xl">
      <h2 className="text-lg font-bold text-gray-800 dark:text-gray-100 mb-4 flex items-center gap-2">
        <TrendingUp className="w-5 h-5 text-blue-600 dark:text-blue-400" />
        Dashboard
      </h2>

      {/* Cards de Estatísticas */}
      <div className="grid grid-cols-2 gap-2 mb-4">
        {statCards.map((stat, index) => {
          const Icon = stat.icon
          return (
            <div
              key={index}
              className={`${stat.bgColor} rounded-lg p-3 border-2 border-transparent hover:border-gray-300 dark:hover:border-gray-600 transition-all duration-200`}
            >
              <div className="flex items-center justify-between">
                <div className="flex-1 min-w-0">
                  <p className="text-xs text-gray-600 dark:text-gray-400 mb-1 truncate">{stat.title}</p>
                  <p className="text-xl font-bold text-gray-800 dark:text-gray-200">{stat.value}</p>
                </div>
                <div className={`${stat.color} p-2 rounded-full text-white flex-shrink-0 ml-2`}>
                  <Icon className="w-4 h-4" />
                </div>
              </div>
            </div>
          )
        })}
      </div>

      {/* Estatísticas por Categoria */}
      {Object.keys(stats.porCategoria).length > 0 && (
        <div>
          <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2 flex items-center gap-2">
            <Tag className="w-4 h-4" />
            Por Categoria
          </h3>
          <div className="space-y-2 max-h-40 overflow-y-auto pr-2">
            {Object.entries(stats.porCategoria).map(([categoria, dados]) => {
              const porcentagem = dados.total > 0 ? Math.round((dados.concluidos / dados.total) * 100) : 0
              return (
                <div
                  key={categoria}
                  className="bg-white dark:bg-gray-800 rounded-lg p-2 border border-gray-200 dark:border-gray-700"
                >
                  <div className="flex items-center justify-between mb-1">
                    <span className="text-xs font-medium text-gray-700 dark:text-gray-300">
                      {categoria || 'Sem categoria'}
                    </span>
                    <span className="text-xs text-gray-500 dark:text-gray-400">
                      {dados.concluidos}/{dados.total}
                    </span>
                  </div>
                  <div className="w-full bg-gray-200 dark:bg-gray-700 rounded-full h-1.5">
                    <div
                      className="bg-blue-600 dark:bg-blue-500 h-1.5 rounded-full transition-all duration-300"
                      style={{ width: `${porcentagem}%` }}
                    />
                  </div>
                </div>
              )
            })}
          </div>
        </div>
      )}
    </div>
  )
}

export default Dashboard

