import { Download, FileJson, FileSpreadsheet, Upload, X } from 'lucide-react'
import { useState } from 'react'

function ExportImport({ reminders, onImportSuccess }) {
  const [showImport, setShowImport] = useState(false)
  const [importError, setImportError] = useState(null)

  // Exportar para JSON
  const exportToJSON = () => {
    const dataStr = JSON.stringify(reminders, null, 2)
    const dataBlob = new Blob([dataStr], { type: 'application/json' })
    const url = URL.createObjectURL(dataBlob)
    const link = document.createElement('a')
    link.href = url
    link.download = `lembretes-${new Date().toISOString().split('T')[0]}.json`
    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)
    URL.revokeObjectURL(url)
  }

  // Exportar para CSV
  const exportToCSV = () => {
    const headers = ['Nome', 'Descrição', 'Categoria', 'Data', 'Horário', 'Concluído', 'Data de Criação']
    const rows = reminders.map(r => {
      const data = typeof r.data === 'string' ? r.data : new Date(r.data).toISOString()
      const horario = r.horario ? (typeof r.horario === 'string' ? r.horario.substring(0, 5) : r.horario.toString().substring(0, 5)) : ''
      const dataCriacao = typeof r.dataCriacao === 'string' ? r.dataCriacao : new Date(r.dataCriacao).toISOString()
      
      return [
        `"${(r.nome || '').replace(/"/g, '""')}"`,
        `"${(r.descricao || '').replace(/"/g, '""')}"`,
        `"${(r.categoria || '').replace(/"/g, '""')}"`,
        `"${data}"`,
        `"${horario}"`,
        r.concluido ? 'Sim' : 'Não',
        `"${dataCriacao}"`
      ].join(',')
    })

    const csvContent = [headers.join(','), ...rows].join('\n')
    const dataBlob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' })
    const url = URL.createObjectURL(dataBlob)
    const link = document.createElement('a')
    link.href = url
    link.download = `lembretes-${new Date().toISOString().split('T')[0]}.csv`
    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)
    URL.revokeObjectURL(url)
  }

  // Importar de JSON
  const handleFileImport = (event) => {
    const file = event.target.files[0]
    if (!file) return

    setImportError(null)

    if (file.type === 'application/json' || file.name.endsWith('.json')) {
      const reader = new FileReader()
      reader.onload = (e) => {
        try {
          const importedData = JSON.parse(e.target.result)
          
          // Validar formato
          if (!Array.isArray(importedData)) {
            throw new Error('O arquivo JSON deve conter um array de lembretes')
          }

          // Validar estrutura básica
          importedData.forEach((item, index) => {
            if (!item.nome || !item.data) {
              throw new Error(`Lembrete ${index + 1} está incompleto: nome e data são obrigatórios`)
            }
          })

          onImportSuccess(importedData)
          setShowImport(false)
          event.target.value = '' // Limpar input
        } catch (error) {
          setImportError(error.message || 'Erro ao importar arquivo JSON')
          event.target.value = ''
        }
      }
      reader.readAsText(file)
    } else {
      setImportError('Formato não suportado. Use arquivos JSON (.json)')
      event.target.value = ''
    }
  }

  return (
    <div className="glass-effect rounded-2xl p-6 shadow-2xl mb-6">
      <div className="flex items-center justify-between mb-4">
        <h3 className="text-lg font-semibold text-gray-800 dark:text-gray-100 flex items-center gap-2">
          <Download className="w-5 h-5 text-blue-600 dark:text-blue-400" />
          Exportar / Importar
        </h3>
        {showImport && (
          <button
            onClick={() => {
              setShowImport(false)
              setImportError(null)
            }}
            className="text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-200"
          >
            <X className="w-5 h-5" />
          </button>
        )}
      </div>

      <div className="flex flex-col sm:flex-row gap-3">
        {/* Botões de Exportação */}
        <div className="flex gap-3 flex-1">
          <button
            onClick={exportToJSON}
            disabled={reminders.length === 0}
            className="flex items-center gap-2 px-4 py-2 bg-blue-600 dark:bg-blue-500 text-white rounded-lg hover:bg-blue-700 dark:hover:bg-blue-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex-1 justify-center"
            title="Exportar lembretes em formato JSON"
          >
            <FileJson className="w-4 h-4" />
            <span className="hidden sm:inline">Exportar JSON</span>
            <span className="sm:hidden">JSON</span>
          </button>

          <button
            onClick={exportToCSV}
            disabled={reminders.length === 0}
            className="flex items-center gap-2 px-4 py-2 bg-green-600 dark:bg-green-500 text-white rounded-lg hover:bg-green-700 dark:hover:bg-green-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex-1 justify-center"
            title="Exportar lembretes em formato CSV"
          >
            <FileSpreadsheet className="w-4 h-4" />
            <span className="hidden sm:inline">Exportar CSV</span>
            <span className="sm:hidden">CSV</span>
          </button>
        </div>

        {/* Botão de Importação */}
        {!showImport ? (
          <button
            onClick={() => setShowImport(true)}
            className="flex items-center gap-2 px-4 py-2 bg-purple-600 dark:bg-purple-500 text-white rounded-lg hover:bg-purple-700 dark:hover:bg-purple-600 transition-colors justify-center"
            title="Importar lembretes de arquivo JSON"
          >
            <Upload className="w-4 h-4" />
            <span>Importar</span>
          </button>
        ) : (
          <div className="flex flex-col gap-2 flex-1">
            <label className="flex items-center gap-2 px-4 py-2 bg-purple-600 dark:bg-purple-500 text-white rounded-lg hover:bg-purple-700 dark:hover:bg-purple-600 transition-colors cursor-pointer justify-center">
              <Upload className="w-4 h-4" />
              <span>Selecionar Arquivo JSON</span>
              <input
                type="file"
                accept=".json,application/json"
                onChange={handleFileImport}
                className="hidden"
              />
            </label>
            {importError && (
              <div className="text-sm text-red-600 dark:text-red-400 bg-red-50 dark:bg-red-900/30 p-2 rounded">
                {importError}
              </div>
            )}
            <p className="text-xs text-gray-500 dark:text-gray-400">
              ⚠️ Importar substituirá seus lembretes atuais. Certifique-se de fazer backup primeiro.
            </p>
          </div>
        )}
      </div>
    </div>
  )
}

export default ExportImport

