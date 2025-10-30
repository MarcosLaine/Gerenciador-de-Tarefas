import { Upload, X } from 'lucide-react'
import { useState } from 'react'

function ExportImport({ onImportSuccess }) {
  const [showImport, setShowImport] = useState(false)
  const [importError, setImportError] = useState(null)

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
          <Upload className="w-5 h-5 text-purple-600 dark:text-purple-400" />
          Importar Lembretes
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

      <div>
        {!showImport ? (
          <button
            onClick={() => setShowImport(true)}
            className="w-full flex items-center justify-center gap-2 px-4 py-2 bg-purple-600 dark:bg-purple-500 text-white rounded-lg hover:bg-purple-700 dark:hover:bg-purple-600 transition-colors"
            title="Importar lembretes de arquivo JSON"
          >
            <Upload className="w-4 h-4" />
            <span>Importar do JSON</span>
          </button>
        ) : (
          <div className="flex flex-col gap-2">
            <label className="flex items-center justify-center gap-2 px-4 py-2 bg-purple-600 dark:bg-purple-500 text-white rounded-lg hover:bg-purple-700 dark:hover:bg-purple-600 transition-colors cursor-pointer">
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

