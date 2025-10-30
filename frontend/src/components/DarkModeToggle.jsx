import { Moon, Sun } from 'lucide-react'

function DarkModeToggle({ isDarkMode, onToggle }) {
  return (
    <button
      onClick={onToggle}
      className="relative p-2 rounded-lg bg-gray-200 dark:bg-gray-700 hover:bg-gray-300 dark:hover:bg-gray-600 transition-all duration-300 group"
      aria-label={isDarkMode ? 'Ativar modo claro' : 'Ativar modo escuro'}
      title={isDarkMode ? 'Modo claro' : 'Modo escuro'}
    >
      <div className="relative w-6 h-6">
        {/* Ícone de Sol */}
        <Sun 
          className={`absolute inset-0 w-6 h-6 text-yellow-500 transition-all duration-300 transform ${
            isDarkMode 
              ? 'rotate-90 scale-0 opacity-0' 
              : 'rotate-0 scale-100 opacity-100'
          }`}
        />
        {/* Ícone de Lua */}
        <Moon 
          className={`absolute inset-0 w-6 h-6 text-blue-400 transition-all duration-300 transform ${
            isDarkMode 
              ? 'rotate-0 scale-100 opacity-100' 
              : '-rotate-90 scale-0 opacity-0'
          }`}
        />
      </div>
    </button>
  )
}

export default DarkModeToggle

