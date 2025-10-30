import { Calendar, Clock, FileText, Plus } from 'lucide-react'
import { useState } from 'react'

function ReminderForm({ onAddReminder }) {
  const [nome, setNome] = useState('')
  const [data, setData] = useState('')
  const [horario, setHorario] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  const handleSubmit = async (e) => {
    e.preventDefault()
    
    // Validação: data não pode ser no passado
    const selectedDate = new Date(data)
    const today = new Date()
    today.setHours(0, 0, 0, 0)
    
    if (selectedDate < today) {
      alert('⚠️ A data do lembrete deve ser hoje ou no futuro!')
      return
    }

    setIsSubmitting(true)
    const success = await onAddReminder(nome, data, horario || null)
    
    if (success) {
      // Limpar formulário
      setNome('')
      setData('')
      setHorario('')
      
      // Feedback visual de sucesso
      const button = document.querySelector('button[type="submit"]')
      button?.classList.add('animate-bounce-subtle')
      setTimeout(() => {
        button?.classList.remove('animate-bounce-subtle')
      }, 600)
    }
    
    setIsSubmitting(false)
  }

  // Data mínima é hoje
  const getMinDate = () => {
    const today = new Date()
    return today.toISOString().split('T')[0]
  }

  return (
    <div className="glass-effect rounded-2xl p-8 shadow-2xl">
      <h2 className="text-2xl font-bold text-gray-800 dark:text-gray-100 mb-6 flex items-center gap-2">
        <Plus className="w-6 h-6 text-blue-600 dark:text-blue-400" />
        Criar Novo Lembrete
      </h2>
      
      <form onSubmit={handleSubmit} className="space-y-5">
        {/* Campo Nome */}
        <div className="space-y-2">
          <label 
            htmlFor="nome" 
            className="flex items-center gap-2 text-sm font-semibold text-gray-700 dark:text-gray-300"
          >
            <FileText className="w-4 h-4" />
            Nome do Lembrete
          </label>
          <input
            type="text"
            id="nome"
            value={nome}
            onChange={(e) => setNome(e.target.value)}
            placeholder="Ex: Reunião importante, Aniversário..."
            required
            className="input-field"
          />
        </div>

        {/* Campo Data */}
        <div className="space-y-2">
          <label 
            htmlFor="data" 
            className="flex items-center gap-2 text-sm font-semibold text-gray-700 dark:text-gray-300"
          >
            <Calendar className="w-4 h-4" />
            Data
          </label>
          <input
            type="date"
            id="data"
            value={data}
            onChange={(e) => setData(e.target.value)}
            min={getMinDate()}
            required
            className="input-field"
          />
        </div>

        {/* Campo Horário (Opcional) */}
        <div className="space-y-2">
          <label 
            htmlFor="horario" 
            className="flex items-center gap-2 text-sm font-semibold text-gray-700 dark:text-gray-300"
          >
            <Clock className="w-4 h-4" />
            Horário <span className="text-xs font-normal text-gray-500 dark:text-gray-400">(opcional)</span>
          </label>
          <input
            type="time"
            id="horario"
            value={horario}
            onChange={(e) => setHorario(e.target.value)}
            className="input-field"
          />
        </div>

        {/* Botão Submit */}
        <button
          type="submit"
          disabled={isSubmitting}
          className="btn-primary w-full flex items-center justify-center gap-2 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {isSubmitting ? (
            <>
              <div className="animate-spin rounded-full h-5 w-5 border-2 border-white border-t-transparent"></div>
              Criando...
            </>
          ) : (
            <>
              <Plus className="w-5 h-5" />
              Criar Lembrete
            </>
          )}
        </button>
      </form>
    </div>
  )
}

export default ReminderForm

