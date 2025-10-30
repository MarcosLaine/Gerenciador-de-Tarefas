import { CheckCircle2, X } from 'lucide-react'
import { useState } from 'react'

function ReminderItem({ reminder, onDelete, index }) {
  const [isDeleting, setIsDeleting] = useState(false)

  const handleDelete = async () => {
    if (window.confirm(`Tem certeza que deseja deletar o lembrete "${reminder.nome}"?`)) {
      setIsDeleting(true)
      await onDelete(reminder.id)
    }
  }

  return (
    <div
      className={`
        group relative flex items-center justify-between p-4 rounded-lg 
        bg-gradient-to-r from-white to-blue-50 dark:from-gray-800 dark:to-gray-700 
        border-2 border-blue-100 dark:border-gray-600
        hover:border-blue-300 dark:hover:border-blue-500 hover:shadow-md transition-all duration-200
        ${isDeleting ? 'opacity-50 scale-95' : 'opacity-100 scale-100'}
      `}
      style={{
        animationDelay: `${index * 50}ms`,
      }}
    >
      {/* Ícone de Check */}
      <div className="flex items-center gap-3 flex-1">
        <CheckCircle2 className="w-5 h-5 text-green-500 dark:text-green-400 flex-shrink-0" />
        <span className="text-gray-800 dark:text-gray-200 font-medium break-words">
          {reminder.nome}
        </span>
      </div>

      {/* Botão de Deletar */}
      <button
        onClick={handleDelete}
        disabled={isDeleting}
        className="btn-danger flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity duration-200 ml-4"
        title="Remover lembrete"
      >
        <X className="w-4 h-4" />
        {isDeleting ? 'Removendo...' : 'Remover'}
      </button>

      {/* Badge de ID (para debug) */}
      <span className="absolute -top-2 -left-2 bg-blue-500 dark:bg-blue-600 text-white text-xs font-bold px-2 py-0.5 rounded-full opacity-0 group-hover:opacity-100 transition-opacity duration-200">
        #{reminder.id}
      </span>
    </div>
  )
}

export default ReminderItem

