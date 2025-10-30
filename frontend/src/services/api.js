import { authService } from './authService';

// Usa variável de ambiente em produção, proxy em desenvolvimento
const API_BASE_URL = import.meta.env.VITE_API_URL 
  ? `${import.meta.env.VITE_API_URL}/api/lembretes`
  : window.location.hostname === 'localhost'
    ? '/api/lembretes'  // Desenvolvimento local
    : 'https://lembretes-api.onrender.com/api/lembretes';  // Produção (fallback)

const getHeaders = () => {
  return {
    'Content-Type': 'application/json',
    ...authService.getAuthHeader(),
  };
};

export const api = {
  // Buscar todos os lembretes
  async getAllReminders() {
    try {
      const response = await fetch(API_BASE_URL, {
        headers: getHeaders(),
      });
      
      if (response.status === 401) {
        authService.logout();
        window.location.href = '/login';
        throw new Error('Sessão expirada');
      }
      
      if (!response.ok) {
        throw new Error('Erro ao buscar lembretes');
      }
      
      return await response.json();
    } catch (error) {
      console.error('Erro ao buscar lembretes:', error);
      throw error;
    }
  },

  // Criar novo lembrete
  async createReminder(nome, data, horario = null) {
    try {
      const body = { nome, data };
      if (horario) {
        body.horario = horario; // Envia como string no formato "HH:mm"
      }
      
      const response = await fetch(API_BASE_URL, {
        method: 'POST',
        headers: getHeaders(),
        body: JSON.stringify(body),
      });
      
      if (response.status === 401) {
        authService.logout();
        window.location.href = '/login';
        throw new Error('Sessão expirada');
      }
      
      if (!response.ok) {
        // Tentar obter mensagem de erro detalhada
        let errorMessage = 'Erro ao criar lembrete';
        try {
          const errorData = await response.json();
          if (errorData.message) {
            errorMessage = errorData.message;
          } else if (errorData.errors) {
            // Se houver erros de validação, construir mensagem
            const validationErrors = Object.values(errorData.errors).flat();
            errorMessage = validationErrors.join(', ');
          }
        } catch (e) {
          // Se não conseguir parsear o erro, usar mensagem padrão
        }
        throw new Error(errorMessage);
      }
      
      return await response.json();
    } catch (error) {
      console.error('Erro ao criar lembrete:', error);
      throw error;
    }
  },

  // Deletar lembrete
  async deleteReminder(id) {
    try {
      const response = await fetch(`${API_BASE_URL}/${id}`, {
        method: 'DELETE',
        headers: getHeaders(),
      });
      
      if (response.status === 401) {
        authService.logout();
        window.location.href = '/login';
        throw new Error('Sessão expirada');
      }
      
      if (!response.ok) {
        throw new Error('Erro ao deletar lembrete');
      }
      
      return true;
    } catch (error) {
      console.error('Erro ao deletar lembrete:', error);
      throw error;
    }
  },
};

