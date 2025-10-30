// Usa variável de ambiente em produção, ou proxy em desenvolvimento
const API_BASE_URL = import.meta.env.VITE_API_URL 
  ? `${import.meta.env.VITE_API_URL}/api/auth`
  : window.location.hostname === 'localhost' 
    ? '/api/auth'  // Desenvolvimento local
    : 'https://lembretes-api.onrender.com/api/auth';  // Produção (fallback)

export const authService = {
  // Registrar novo usuário
  async register(nome, email, senha) {
    try {
      const response = await fetch(`${API_BASE_URL}/register`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ nome, email, senha }),
      });

      // Verificar se a resposta tem conteúdo antes de fazer parse
      const text = await response.text();
      
      if (!response.ok) {
        try {
          const error = JSON.parse(text);
          // ModelState errors vem com estrutura diferente
          const errorMessage = error.message || 
                              error.title || 
                              (error.errors && Object.values(error.errors).flat().join(', ')) ||
                              'Erro ao registrar';
          throw new Error(errorMessage);
        } catch (parseError) {
          // Se não conseguir fazer parse, usar o texto da resposta ou status
          throw new Error(text || `Erro ${response.status}: ${response.statusText}`);
        }
      }

      if (!text) {
        throw new Error('Resposta vazia do servidor');
      }

      const data = JSON.parse(text);
      this.saveToken(data.token);
      this.saveUser({ nome: data.nome, email: data.email });
      return data;
    } catch (error) {
      console.error('Erro no registro:', error);
      throw error;
    }
  },

  // Login do usuário
  async login(email, senha) {
    try {
      const response = await fetch(`${API_BASE_URL}/login`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ email, senha }),
      });

      // Verificar se a resposta tem conteúdo antes de fazer parse
      const text = await response.text();
      
      if (!response.ok) {
        try {
          const error = JSON.parse(text);
          throw new Error(error.message || error.title || 'Email ou senha inválidos');
        } catch (parseError) {
          // Se não conseguir fazer parse, usar o texto da resposta ou status
          throw new Error(text || `Erro ${response.status}: ${response.statusText}`);
        }
      }

      if (!text) {
        throw new Error('Resposta vazia do servidor');
      }

      const data = JSON.parse(text);
      this.saveToken(data.token);
      this.saveUser({ nome: data.nome, email: data.email });
      return data;
    } catch (error) {
      console.error('Erro no login:', error);
      throw error;
    }
  },

  // Logout
  logout() {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
  },

  // Salvar token no localStorage
  saveToken(token) {
    localStorage.setItem('token', token);
  },

  // Obter token do localStorage
  getToken() {
    return localStorage.getItem('token');
  },

  // Salvar dados do usuário no localStorage
  saveUser(user) {
    localStorage.setItem('user', JSON.stringify(user));
  },

  // Obter dados do usuário do localStorage
  getUser() {
    const user = localStorage.getItem('user');
    return user ? JSON.parse(user) : null;
  },

  // Verificar se o usuário está autenticado
  isAuthenticated() {
    return !!this.getToken();
  },

  // Obter header de autorização
  getAuthHeader() {
    const token = this.getToken();
    return token ? { 'Authorization': `Bearer ${token}` } : {};
  },
};

