import { Calendar, Clock, FileText, Plus, Tag } from 'lucide-react'
import { useEffect, useState } from 'react'

const CATEGORIAS_PREDEFINIDAS = [
  { value: '', label: 'Sem categoria' },
  { value: 'Trabalho', label: '💼 Trabalho' },
  { value: 'Pessoal', label: '👤 Pessoal' },
  { value: 'Saúde', label: '🏥 Saúde' },
  { value: 'Estudos', label: '📚 Estudos' },
  { value: 'Lazer', label: '🎮 Lazer' },
  { value: 'Compras', label: '🛒 Compras' },
  { value: 'Família', label: '👨‍👩‍👧‍👦 Família' },
  { value: 'Outros', label: '📌 Outros' },
]

function ReminderForm({ onAddReminder, editingReminder = null, onCancelEdit = null }) {
  const [nome, setNome] = useState(editingReminder?.nome || '')
  const [data, setData] = useState(editingReminder?.data ? editingReminder.data.split('T')[0] : '')
  const [horario, setHorario] = useState(editingReminder?.horario ? editingReminder.horario.substring(0, 5) : '')
  const [descricao, setDescricao] = useState(editingReminder?.descricao || '')
  const [categoria, setCategoria] = useState(editingReminder?.categoria || '')
  const [isSubmitting, setIsSubmitting] = useState(false)

  // Atualizar campos quando editingReminder mudar
  useEffect(() => {
    if (editingReminder) {
      setNome(editingReminder.nome || '')
      // Tratar data - pode vir como string ISO ou apenas date
      let dateValue = ''
      if (editingReminder.data) {
        const dateStr = typeof editingReminder.data === 'string' ? editingReminder.data : editingReminder.data.toString()
        dateValue = dateStr.includes('T') ? dateStr.split('T')[0] : dateStr.substring(0, 10)
      }
      setData(dateValue)
      
      // Tratar horário - pode vir como string HH:mm:ss ou TimeSpan
      let horarioValue = ''
      if (editingReminder.horario) {
        const horarioStr = typeof editingReminder.horario === 'string' 
          ? editingReminder.horario 
          : editingReminder.horario.toString()
        horarioValue = horarioStr.substring(0, 5)
      }
      setHorario(horarioValue)
      setDescricao(editingReminder.descricao || '')
      setCategoria(editingReminder.categoria || '')
    } else {
      setNome('')
      setData('')
      setHorario('')
      setDescricao('')
      setCategoria('')
    }
  }, [editingReminder])

  const handleSubmit = async (e) => {
    e.preventDefault()
    
    // Validação: data e horário não podem ser no passado (apenas ao criar novo, não ao editar)
    if (!editingReminder) {
      // Criar data no horário local para evitar problemas de timezone
      const [year, month, day] = data.split('-').map(Number)
      const selectedDate = new Date(year, month - 1, day) // month é 0-indexed
      
      // Se houver horário, combina data + horário
      if (horario) {
        const [hours, minutes] = horario.split(':').map(Number)
        selectedDate.setHours(hours, minutes, 0, 0)
      } else {
        // Se não houver horário, considera o final do dia
        selectedDate.setHours(23, 59, 59, 999)
      }
      
      const now = new Date()
      
      // Comparação considerando apenas data e hora, não timezone
      if (selectedDate < now) {
        alert('⚠️ A data e horário do lembrete devem ser no futuro!')
        return
      }
    }

    setIsSubmitting(true)
    const success = await onAddReminder(
      nome, 
      data, 
      horario || null, 
      descricao || null,
      categoria || null,
      editingReminder?.id
    )
    
    if (success) {
      // Limpar formulário
      setNome('')
      setData('')
      setHorario('')
      setDescricao('')
      setCategoria('')
      
      // Feedback visual de sucesso
      const button = document.querySelector('button[type="submit"]')
      button?.classList.add('animate-bounce-subtle')
      setTimeout(() => {
        button?.classList.remove('animate-bounce-subtle')
      }, 600)
    }
    
    setIsSubmitting(false)
  }

  // Data mínima é hoje (apenas ao criar novo, não ao editar)
  const getMinDate = () => {
    if (editingReminder) {
      return undefined // Permite qualquer data ao editar
    }
    const today = new Date()
    return today.toISOString().split('T')[0]
  }

  return (
    <div className="glass-effect rounded-2xl p-8 shadow-2xl">
      <h2 className="text-2xl font-bold text-gray-800 dark:text-gray-100 mb-6 flex items-center gap-2">
        <Plus className="w-6 h-6 text-blue-600 dark:text-blue-400" />
        {editingReminder ? 'Editar Lembrete' : 'Criar Novo Lembrete'}
      </h2>
      
      {editingReminder && (
        <button
          onClick={onCancelEdit}
          className="mb-4 text-sm text-gray-600 dark:text-gray-400 hover:text-gray-800 dark:hover:text-gray-200"
        >
          ← Cancelar edição
        </button>
      )}
      
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
          {editingReminder && (
            <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">
              ℹ️ Você pode editar a data mesmo se estiver no passado
            </p>
          )}
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

        {/* Campo Descrição (Opcional) */}
        <div className="space-y-2">
          <label 
            htmlFor="descricao" 
            className="flex items-center gap-2 text-sm font-semibold text-gray-700 dark:text-gray-300"
          >
            <FileText className="w-4 h-4" />
            Descrição <span className="text-xs font-normal text-gray-500 dark:text-gray-400">(opcional)</span>
          </label>
          <textarea
            id="descricao"
            value={descricao}
            onChange={(e) => setDescricao(e.target.value)}
            placeholder="Adicione detalhes sobre este lembrete..."
            rows="3"
            className="input-field resize-none"
          />
        </div>

        {/* Campo Categoria (Opcional) */}
        <div className="space-y-2">
          <label 
            htmlFor="categoria" 
            className="flex items-center gap-2 text-sm font-semibold text-gray-700 dark:text-gray-300"
          >
            <Tag className="w-4 h-4" />
            Categoria <span className="text-xs font-normal text-gray-500 dark:text-gray-400">(opcional)</span>
          </label>
          <select
            id="categoria"
            value={categoria}
            onChange={(e) => setCategoria(e.target.value)}
            className="input-field"
          >
            {CATEGORIAS_PREDEFINIDAS.map((cat) => (
              <option key={cat.value} value={cat.value}>
                {cat.label}
              </option>
            ))}
          </select>
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
              {editingReminder ? 'Salvar Alterações' : 'Criar Lembrete'}
            </>
          )}
        </button>
      </form>
    </div>
  )
}

export default ReminderForm

