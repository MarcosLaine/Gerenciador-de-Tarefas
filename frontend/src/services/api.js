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
  async createReminder(nome, data, horario = null, descricao = null, categoria = null) {
    try {
      // Garantir que a data seja enviada com horário no meio do dia para evitar problemas de timezone
      // Se não tiver horário, usa 12:00 para garantir que a data não mude ao converter timezones
      let dataParaEnviar = data;
      if (!horario) {
        // Adiciona horário 12:00 para evitar problema de timezone
        dataParaEnviar = `${data}T12:00:00`;
      } else {
        // Se tiver horário, combina data e horário
        dataParaEnviar = `${data}T${horario}:00`;
      }
      
      const body = { nome, data: dataParaEnviar };
      if (horario) {
        body.horario = horario; // Envia como string no formato "HH:mm"
      }
      if (descricao) {
        body.descricao = descricao;
      }
      if (categoria) {
        body.categoria = categoria;
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

  // Atualizar lembrete
  async updateReminder(id, nome, data, horario = null, descricao = null, categoria = null) {
    try {
      // Garantir que a data seja enviada com horário no meio do dia para evitar problemas de timezone
      let dataParaEnviar = data;
      if (!horario) {
        // Adiciona horário 12:00 para evitar problema de timezone
        dataParaEnviar = `${data}T12:00:00`;
      } else {
        // Se tiver horário, combina data e horário
        dataParaEnviar = `${data}T${horario}:00`;
      }
      
      const body = { nome, data: dataParaEnviar };
      if (horario) {
        body.horario = horario;
      } else {
        body.horario = null;
      }
      if (descricao) {
        body.descricao = descricao;
      }
      if (categoria) {
        body.categoria = categoria;
      }
      
      const response = await fetch(`${API_BASE_URL}/${id}`, {
        method: 'PUT',
        headers: getHeaders(),
        body: JSON.stringify(body),
      });
      
      if (response.status === 401) {
        authService.logout();
        window.location.href = '/login';
        throw new Error('Sessão expirada');
      }
      
      if (!response.ok) {
        let errorMessage = 'Erro ao atualizar lembrete';
        try {
          const errorData = await response.json();
          if (errorData.message) {
            errorMessage = errorData.message;
          } else if (errorData.errors) {
            const validationErrors = Object.values(errorData.errors).flat();
            errorMessage = validationErrors.join(', ');
          }
        } catch (e) {
          // Ignora erro ao parsear
        }
        throw new Error(errorMessage);
      }
      
      return await response.json();
    } catch (error) {
      console.error('Erro ao atualizar lembrete:', error);
      throw error;
    }
  },

  // Marcar lembrete como concluído
  async markAsCompleted(id) {
    try {
      const response = await fetch(`${API_BASE_URL}/${id}/concluir`, {
        method: 'PATCH',
        headers: getHeaders(),
      });
      
      if (response.status === 401) {
        authService.logout();
        window.location.href = '/login';
        throw new Error('Sessão expirada');
      }
      
      if (!response.ok) {
        throw new Error('Erro ao marcar lembrete como concluído');
      }
      
      return await response.json();
    } catch (error) {
      console.error('Erro ao marcar lembrete como concluído:', error);
      throw error;
    }
  },

  // Desmarcar lembrete como concluído
  async markAsIncomplete(id) {
    try {
      const response = await fetch(`${API_BASE_URL}/${id}/desmarcar`, {
        method: 'PATCH',
        headers: getHeaders(),
      });
      
      if (response.status === 401) {
        authService.logout();
        window.location.href = '/login';
        throw new Error('Sessão expirada');
      }
      
      if (!response.ok) {
        throw new Error('Erro ao desmarcar lembrete');
      }
      
      return await response.json();
    } catch (error) {
      console.error('Erro ao desmarcar lembrete:', error);
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

