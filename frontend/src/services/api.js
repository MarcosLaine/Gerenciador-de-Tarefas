import { authService } from './authService';

const API_BASE_URL = '/api/lembretes';

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
  async createReminder(nome, data) {
    try {
      const response = await fetch(API_BASE_URL, {
        method: 'POST',
        headers: getHeaders(),
        body: JSON.stringify({ nome, data }),
      });
      
      if (response.status === 401) {
        authService.logout();
        window.location.href = '/login';
        throw new Error('Sessão expirada');
      }
      
      if (!response.ok) {
        throw new Error('Erro ao criar lembrete');
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

