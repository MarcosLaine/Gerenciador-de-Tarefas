# 📝 Gerenciador de Tarefas

Sistema moderno e intuitivo para gerenciamento de lembretes e tarefas, desenvolvido com React, Vite, Tailwind CSS e ASP.NET Core.

## ✨ Funcionalidades

- ➕ **Adicionar lembretes** com nome e data
- 📋 **Visualizar lista de lembretes** agrupados por data
- 🗑️ **Remover lembretes** com animações suaves
- 🎨 **Interface moderna** com glassmorphism e animações
- 📱 **Design responsivo** para todos os dispositivos
- 🌈 **Gradientes animados** e efeitos visuais sofisticados
- 🎯 **Validação de datas** - não permite criar lembretes para datas passadas
- 📅 **Formatação de datas** em português brasileiro

## 🚀 Tecnologias Utilizadas

### Frontend
- **React 18** - Biblioteca JavaScript para construção de interfaces
- **Vite** - Build tool extremamente rápida
- **Tailwind CSS** - Framework CSS utilitário
- **Lucide React** - Ícones modernos e elegantes
- **date-fns** - Manipulação e formatação de datas

### Backend
- **ASP.NET Core 8 Web API** - Framework para APIs RESTful
- **C#** - Linguagem de programação

## 📋 Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js](https://nodejs.org/) (versão 18 ou superior)
- [npm](https://www.npmjs.com/) ou [yarn](https://yarnpkg.com/)

## 🛠️ Instalação e Execução

### Opção 1: Setup Automatizado (Recomendado)

**Windows:**
```bash
setup.bat
```

**Linux/Mac:**
```bash
chmod +x setup.sh
./setup.sh
```

### Opção 2: Setup Manual

#### 1. Clone o repositório

```bash
git clone <https://github.com/marcoslaine/gerenciador-de-tarefas.git>
cd Gerenciador-de-Tarefas
```

#### 2. Configure e inicie o Backend (API)

```bash
cd LembretesApi
dotnet restore
dotnet build
dotnet run
```

A API estará rodando em `http://localhost:5285`

#### 3. Configure e inicie o Frontend React

Em outro terminal:

```bash
cd frontend
npm install
npm run dev
```

O frontend estará disponível em `http://localhost:3000`

#### 4. Acesse a aplicação

Abra seu navegador e acesse: **http://localhost:3000**

## 📁 Estrutura do Projeto

```
Gerenciador-de-Tarefas/
├── frontend/                    # Aplicação React
│   ├── src/
│   │   ├── components/         # Componentes React
│   │   │   ├── ReminderForm.jsx
│   │   │   ├── ReminderList.jsx
│   │   │   └── ReminderItem.jsx
│   │   ├── services/           # Serviços de API
│   │   │   └── api.js
│   │   ├── App.jsx             # Componente principal
│   │   ├── main.jsx            # Entry point
│   │   └── index.css           # Estilos Tailwind
│   ├── index.html
│   ├── vite.config.js
│   ├── tailwind.config.js
│   ├── postcss.config.js
│   └── package.json
│
├── LembretesApi/               # Backend ASP.NET Core
│   ├── Controllers/
│   │   └── LembretesController.cs
│   ├── Models/
│   │   └── Lembrete.cs
│   ├── Program.cs
│   ├── appsettings.json
│   └── LembretesApi.csproj
│
├── setup.bat                   # Script de setup Windows
├── setup.sh                    # Script de setup Linux/Mac
├── .gitignore
└── README.md
```

## 🎨 Design Features

### Glassmorphism
Interface com efeito de vidro fosco, criando uma aparência moderna e elegante com:
- Backdrop blur
- Transparências sutis
- Bordas suaves
- Sombras em camadas

### Animações Suaves
- **Fade in/out** - Entrada e saída suave de elementos
- **Slide animations** - Deslizamento elegante
- **Hover effects** - Feedback visual ao passar o mouse
- **Loading states** - Spinners e transições durante carregamento

### Gradientes Dinâmicos
Fundo com gradientes que mudam suavemente criando uma experiência visual rica e moderna.

### Responsividade Total
Interface 100% responsiva que se adapta perfeitamente a:
- 📱 **Mobile** (smartphones)
- 💻 **Tablet** (iPads e tablets)
- 🖥️ **Desktop** (monitores grandes)

## 🔌 API Endpoints

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| **GET** | `/api/lembretes` | Retorna todos os lembretes |
| **POST** | `/api/lembretes` | Cria um novo lembrete |
| **DELETE** | `/api/lembretes/{id}` | Remove um lembrete específico |

### Exemplo de Request (POST):
```json
{
  "nome": "Reunião importante",
  "data": "2024-12-25"
}
```

### Exemplo de Response:
```json
{
  "id": 1,
  "nome": "Reunião importante",
  "data": "2024-12-25T00:00:00"
}
```

## 📝 Modelo de Dados

```csharp
public class Lembrete
{
    public int Id { get; set; }
    public string? Nome { get; set; }
    public DateTime Data { get; set; }
}
```

## 🧪 Scripts Disponíveis

### Frontend
```bash
npm run dev      # Inicia o servidor de desenvolvimento
npm run build    # Build para produção
npm run preview  # Preview do build de produção
npm run lint     # Executa o linter
```

### Backend
```bash
dotnet run       # Inicia o servidor
dotnet build     # Compila o projeto
dotnet watch run # Inicia com hot reload
dotnet test      # Executa os testes
```

## 💡 Decisões Técnicas

### Por que React?
- Componentes reutilizáveis e manuteníveis
- Hooks modernos para gerenciamento de estado
- Ecossistema rico e ativo
- Performance otimizada com Virtual DOM

### Por que Vite?
- Build extremamente rápido
- Hot Module Replacement instantâneo
- Configuração mínima
- Otimizações automáticas

### Por que Tailwind CSS?
- Desenvolvimento rápido com utility classes
- Design system consistente
- Fácil customização
- Bundle otimizado (apenas CSS usado)

### Por que ASP.NET Core?
- Performance excepcional
- Type-safe com C#
- CORS configurado
- Fácil deploy

## 🐛 Troubleshooting

### Erro de CORS
Se encontrar erros de CORS no console do navegador:
1. Verifique se o backend está rodando em `http://localhost:5285`
2. Confirme que está acessando o frontend via `http://localhost:3000`
3. Reinicie ambos os servidores

### Porta já em uso
**Frontend (porta 3000):**
- Altere em `frontend/vite.config.js` → `server.port`

**Backend (porta 5285):**
- Altere em `LembretesApi/Properties/launchSettings.json`

### npm install falha
```bash
# Limpar cache e reinstalar
npm cache clean --force
rm -rf node_modules package-lock.json
npm install
```

### .NET não encontrado
Instale o .NET 8 SDK:
- **Windows**: https://dotnet.microsoft.com/download/dotnet/8.0
- **Mac**: `brew install --cask dotnet-sdk`
- **Linux**: https://learn.microsoft.com/dotnet/core/install/linux

## 🚀 Próximas Features (Roadmap)

- [ ] 🌙 Dark mode
- [ ] 🏷️ Categorias e tags
- [ ] 🔔 Notificações push
- [ ] ✏️ Edição inline
- [ ] 🔍 Busca e filtros
- [ ] 📤 Export/Import (JSON/CSV)
- [ ] 🔄 Lembretes recorrentes
- [ ] 👥 Multi-usuários
- [ ] 🗄️ Persistência em banco de dados
- [ ] 📊 Dashboard com estatísticas

## 📄 Licença

Este projeto está sob a licença MIT. Sinta-se livre para usar, modificar e distribuir.

## 🤝 Contribuindo

Contribuições são bem-vindas! Sinta-se livre para:
1. Fazer um fork do projeto
2. Criar uma branch para sua feature (`git checkout -b feature/AmazingFeature`)
3. Commit suas mudanças (`git commit -m 'Add some AmazingFeature'`)
4. Push para a branch (`git push origin feature/AmazingFeature`)
5. Abrir um Pull Request

## 👨‍💻 Desenvolvido com

- ❤️ Paixão por código limpo
- ☕ Muito café
- 🎨 Atenção aos detalhes
- 🚀 Foco em performance

---

**Aproveite o gerenciador de tarefas!** ✨
