import { addHours, isToday, parseISO } from 'date-fns'
import { AlertCircle, CheckCircle2, ChevronDown, ChevronUp, Clock, Edit, Tag, X } from 'lucide-react'
import { useState } from 'react'

// Função para obter cor da categoria
const getCategoryColor = (categoria) => {
  const colors = {
    'Trabalho': 'bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300 border-blue-300 dark:border-blue-700',
    'Pessoal': 'bg-purple-100 dark:bg-purple-900/30 text-purple-700 dark:text-purple-300 border-purple-300 dark:border-purple-700',
    'Saúde': 'bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-300 border-red-300 dark:border-red-700',
    'Estudos': 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-300 border-green-300 dark:border-green-700',
    'Lazer': 'bg-yellow-100 dark:bg-yellow-900/30 text-yellow-700 dark:text-yellow-300 border-yellow-300 dark:border-yellow-700',
    'Compras': 'bg-orange-100 dark:bg-orange-900/30 text-orange-700 dark:text-orange-300 border-orange-300 dark:border-orange-700',
    'Família': 'bg-pink-100 dark:bg-pink-900/30 text-pink-700 dark:text-pink-300 border-pink-300 dark:border-pink-700',
    'Outros': 'bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 border-gray-300 dark:border-gray-600',
  }
  
  // Se a categoria não está nas predefinidas, usa uma cor padrão para personalizadas
  return colors[categoria] || 'bg-indigo-100 dark:bg-indigo-900/30 text-indigo-700 dark:text-indigo-300 border-indigo-300 dark:border-indigo-700'
}

// Função para verificar se lembrete está próximo (hoje ou próximas 24h)
const isReminderSoon = (reminder) => {
  if (reminder.concluido) return false
  
  try {
    const reminderDate = typeof reminder.data === 'string' 
      ? parseISO(reminder.data) 
      : new Date(reminder.data)
    
    // Se tiver horário, combinar
    let dataCompleta = reminderDate
    if (reminder.horario) {
      const year = reminderDate.getUTCFullYear()
      const month = reminderDate.getUTCMonth()
      const day = reminderDate.getUTCDate()
      const horarioStr = typeof reminder.horario === 'string' 
        ? reminder.horario 
        : reminder.horario.toString()
      const [hours, minutes] = horarioStr.split(':').map(Number)
      dataCompleta = new Date(year, month, day, hours || 0, minutes || 0, 0)
    }
    
    const now = new Date()
    const next24Hours = addHours(now, 24)
    
    // Verifica se é hoje ou nas próximas 24 horas
    return (isToday(dataCompleta) || (dataCompleta <= next24Hours && dataCompleta >= now))
  } catch {
    return false
  }
}

function ReminderItem({ reminder, onDelete, onEdit, onToggleComplete, index }) {
  const [isDeleting, setIsDeleting] = useState(false)
  const [showDescricao, setShowDescricao] = useState(false)

  const handleDelete = async () => {
    if (window.confirm(`Tem certeza que deseja deletar o lembrete "${reminder.nome}"?`)) {
      setIsDeleting(true)
      try {
        await onDelete(reminder.id)
      } catch (err) {
        console.error('Erro ao deletar:', err)
      } finally {
        setIsDeleting(false)
      }
    }
  }

  const handleToggleCheck = async () => {
    try {
      await onToggleComplete(reminder.id, !reminder.concluido)
    } catch (err) {
      console.error('Erro ao alterar status:', err)
    }
  }

  // Formatar horário se existir
  const formatHorario = (horario) => {
    if (!horario) return null;
    // horario pode vir como string "HH:mm:ss" ou objeto TimeSpan
    const timeStr = typeof horario === 'string' ? horario : horario.toString();
    return timeStr.substring(0, 5); // Pega apenas HH:mm
  }

  const isCompleted = reminder.concluido || false
  const isSoon = isReminderSoon(reminder)

  return (
    <div
      className={`
        group relative flex flex-col p-4 rounded-lg 
        bg-gradient-to-r from-white to-blue-50 dark:from-gray-800 dark:to-gray-700 
        border-2 ${isCompleted ? 'border-green-300 dark:border-green-700 opacity-75' : isSoon ? 'border-orange-400 dark:border-orange-600 ring-2 ring-orange-200 dark:ring-orange-900/50' : 'border-blue-100 dark:border-gray-600'}
        hover:border-blue-300 dark:hover:border-blue-500 hover:shadow-md transition-all duration-200
        ${isDeleting ? 'opacity-50 scale-95' : 'opacity-100 scale-100'}
      `}
      style={{
        animationDelay: `${index * 50}ms`,
      }}
    >
      {/* Conteúdo principal */}
      <div className="flex items-center justify-between gap-3">
        {/* Checkbox e conteúdo */}
        <div className="flex items-center gap-3 flex-1">
          <button
            onClick={handleToggleCheck}
            className="flex-shrink-0 focus:outline-none"
            title={isCompleted ? 'Desmarcar como concluído' : 'Marcar como concluído'}
          >
            <CheckCircle2 
              className={`w-5 h-5 transition-colors ${
                isCompleted 
                  ? 'text-green-500 dark:text-green-400' 
                  : 'text-gray-300 dark:text-gray-600 hover:text-green-400'
              }`} 
            />
          </button>
          
          <div className="flex flex-col flex-1 min-w-0">
            <div className="flex items-center gap-2 flex-wrap">
              <span 
                className={`text-gray-800 dark:text-gray-200 font-medium break-words ${
                  isCompleted ? 'line-through text-gray-500 dark:text-gray-500' : ''
                }`}
              >
                {reminder.nome}
              </span>
              {isSoon && !isCompleted && (
                <span className="flex items-center gap-1 text-xs text-orange-600 dark:text-orange-400 font-semibold">
                  <AlertCircle className="w-3 h-3" />
                  Próximo
                </span>
              )}
            </div>
            <div className="flex items-center gap-3 mt-1 flex-wrap">
              {reminder.horario && (
                <span className="text-sm text-gray-600 dark:text-gray-400 flex items-center gap-1">
                  <Clock className="w-3 h-3" />
                  {formatHorario(reminder.horario)}
                </span>
              )}
              {reminder.categoria && (
                <span className={`text-xs px-2 py-0.5 rounded-full border ${getCategoryColor(reminder.categoria)} flex items-center gap-1`}>
                  <Tag className="w-3 h-3" />
                  {reminder.categoria}
                </span>
              )}
            </div>
            {reminder.descricao && (
              <button
                onClick={() => setShowDescricao(!showDescricao)}
                className="text-xs text-blue-600 dark:text-blue-400 hover:underline mt-1 text-left flex items-center gap-1"
              >
                {showDescricao ? (
                  <>
                    <ChevronUp className="w-3 h-3" />
                    Ocultar descrição
                  </>
                ) : (
                  <>
                    <ChevronDown className="w-3 h-3" />
                    Mostrar descrição
                  </>
                )}
              </button>
            )}
          </div>
        </div>

        {/* Botões de ação */}
        <div className="flex items-center gap-2 opacity-0 group-hover:opacity-100 transition-opacity duration-200">
          <button
            onClick={() => onEdit(reminder)}
            className="flex items-center gap-1 px-3 py-1.5 text-blue-600 dark:text-blue-400 hover:bg-blue-50 dark:hover:bg-blue-900/30 rounded-lg transition-colors"
            title="Editar lembrete"
          >
            <Edit className="w-4 h-4" />
            <span className="hidden sm:inline">Editar</span>
          </button>
          
          <button
            onClick={handleDelete}
            disabled={isDeleting}
            className="btn-danger flex items-center gap-1"
            title="Remover lembrete"
          >
            <X className="w-4 h-4" />
            <span className="hidden sm:inline">{isDeleting ? 'Removendo...' : 'Remover'}</span>
          </button>
        </div>
      </div>

      {/* Descrição expandida */}
      {showDescricao && reminder.descricao && (
        <div className="mt-3 pt-3 border-t border-gray-200 dark:border-gray-600">
          <p className="text-sm text-gray-600 dark:text-gray-400 whitespace-pre-wrap">
            {reminder.descricao}
          </p>
        </div>
      )}

      {/* Badge de Numeração Sequencial */}
      <span className="absolute -top-2 -left-2 bg-blue-500 dark:bg-blue-600 text-white text-xs font-bold px-2 py-0.5 rounded-full opacity-0 group-hover:opacity-100 transition-opacity duration-200">
        #{index + 1}
      </span>
      
      {/* Badge de Concluído */}
      {isCompleted && (
        <span className="absolute -top-2 -right-2 bg-green-500 dark:bg-green-600 text-white text-xs font-bold px-2 py-0.5 rounded-full">
          ✓ Concluído
        </span>
      )}
    </div>
  )
}

export default ReminderItem

