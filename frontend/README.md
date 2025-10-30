# 📝 Gerenciador de Tarefas - Frontend

Frontend moderno e elegante desenvolvido com React, Vite e Tailwind CSS.

## ✨ Features

- 🎨 **Design moderno** com glassmorphism e gradientes
- ⚡ **Performance otimizada** com Vite e React 18
- 📱 **Totalmente responsivo** para todos os dispositivos
- 🎭 **Animações suaves** em todas as interações
- 🌈 **UI intuitiva** com feedback visual
- 🔍 **Validações inteligentes** de formulários

## 🚀 Tecnologias

- **React 18** - Biblioteca JavaScript para interfaces
- **Vite** - Build tool ultra-rápida
- **Tailwind CSS** - Framework CSS utilitário
- **Lucide React** - Ícones modernos em SVG
- **date-fns** - Manipulação de datas

## 📦 Instalação

```bash
npm install
```

## 🏃‍♂️ Executar

### Desenvolvimento
```bash
npm run dev
```
Acesse: `http://localhost:3000`

### Build para Produção
```bash
npm run build
```

### Preview da Build
```bash
npm run preview
```

### Linter
```bash
npm run lint
```

## 📁 Estrutura

```
frontend/
├── src/
│   ├── components/          # Componentes React
│   │   ├── ReminderForm.jsx    # Formulário de criação
│   │   ├── ReminderList.jsx    # Lista de lembretes
│   │   └── ReminderItem.jsx    # Item individual
│   ├── services/           # Serviços
│   │   └── api.js             # Client HTTP
│   ├── App.jsx             # Componente raiz
│   ├── main.jsx           # Entry point
│   └── index.css          # Estilos globais
├── index.html             # HTML template
├── vite.config.js        # Config Vite
├── tailwind.config.js    # Config Tailwind
└── package.json          # Dependências
```

## 🎨 Customização

### Cores (tailwind.config.js)
```javascript
theme: {
  extend: {
    colors: {
      primary: { /* suas cores */ }
    }
  }
}
```

### Animações (tailwind.config.js)
```javascript
animation: {
  'fade-in': 'fadeIn 0.5s ease-in-out',
  // adicione suas animações
}
```

## 🔧 Configuração da API

O frontend está configurado para se comunicar com o backend via proxy:

```javascript
// vite.config.js
server: {
  proxy: {
    '/api': 'http://localhost:5285'
  }
}
```

## 📝 Componentes

### ReminderForm
Formulário para criar novos lembretes com:
- Validação de campos
- Validação de data (não permite datas passadas)
- Feedback visual de sucesso/erro
- Loading states

### ReminderList
Lista de lembretes com:
- Agrupamento por data
- Formatação em português
- Estado vazio elegante
- Animações de entrada

### ReminderItem
Card individual com:
- Hover effects
- Botão de delete com confirmação
- Animação de saída

## 🎯 Boas Práticas Implementadas

- ✅ Componentes funcionais com Hooks
- ✅ Separação de responsabilidades
- ✅ Serviço de API isolado
- ✅ CSS-in-JS com Tailwind
- ✅ Validações no frontend
- ✅ Error handling
- ✅ Loading states
- ✅ Responsive design

## 🐛 Troubleshooting

### Build falha
```bash
rm -rf node_modules package-lock.json
npm install
npm run build
```

### Erro de CORS
Certifique-se de que o backend está rodando em `http://localhost:5285`

### Porta em uso
Altere a porta em `vite.config.js`:
```javascript
server: { port: 3001 }
```

## 📚 Recursos

- [React Docs](https://react.dev)
- [Vite Docs](https://vitejs.dev)
- [Tailwind CSS](https://tailwindcss.com)
- [Lucide Icons](https://lucide.dev)

---

Desenvolvido com ❤️ e React
