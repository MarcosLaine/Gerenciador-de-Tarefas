const API_BASE_URL = '/api/auth';

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

      if (!response.ok) {
        const error = await response.json();
        throw new Error(error.message || 'Erro ao registrar');
      }

      const data = await response.json();
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

      if (!response.ok) {
        const error = await response.json();
        throw new Error(error.message || 'Email ou senha inválidos');
      }

      const data = await response.json();
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

