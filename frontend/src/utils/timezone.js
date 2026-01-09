// Utilitário para detectar e gerenciar timezone do usuário

/**
 * Detecta o timezone do usuário
 * Usa o timezone do navegador (mais confiável e não requer permissão)
 * Fallback: America/Sao_Paulo
 */
export async function detectTimezone() {
  try {
    // Usar timezone do navegador (mais confiável e não requer permissão)
    const browserTimezone = Intl.DateTimeFormat().resolvedOptions().timeZone;
    
    if (browserTimezone) {
      console.log('[Timezone] Timezone detectado do navegador:', browserTimezone);
      return browserTimezone;
    }
  } catch (error) {
    console.warn('[Timezone] Erro ao detectar timezone do navegador:', error);
  }

  // Fallback: São Paulo
  console.log('[Timezone] Usando timezone padrão: America/Sao_Paulo');
  return 'America/Sao_Paulo';
}

/**
 * Obtém timezone a partir de coordenadas usando API pública
 */
async function getTimezoneFromCoordinates(lat, lon) {
  try {
    // Usar API pública para obter timezone (timezoneapi.io ou similar)
    // Alternativa: usar uma biblioteca como timezone-js
    // Por enquanto, vamos usar uma abordagem mais simples
    
    // Mapeamento básico de coordenadas para timezones principais
    // Para produção, considere usar uma API como timezoneapi.io
    const timezone = estimateTimezoneFromCoordinates(lat, lon);
    return timezone;
  } catch (error) {
    console.warn('[Timezone] Erro ao obter timezone de coordenadas:', error);
    return null;
  }
}

/**
 * Estima timezone a partir de coordenadas (aproximação)
 */
function estimateTimezoneFromCoordinates(lat, lon) {
  // Aproximação básica baseada em longitude
  // Para produção, use uma API ou biblioteca mais precisa
  
  // América do Sul (Brasil)
  if (lat >= -35 && lat <= 5 && lon >= -75 && lon <= -30) {
    return 'America/Sao_Paulo';
  }
  
  // América do Norte - Leste
  if (lat >= 25 && lat <= 50 && lon >= -85 && lon <= -65) {
    return 'America/New_York';
  }
  
  // América do Norte - Central
  if (lat >= 25 && lat <= 50 && lon >= -105 && lon <= -85) {
    return 'America/Chicago';
  }
  
  // América do Norte - Oeste
  if (lat >= 25 && lat <= 50 && lon >= -125 && lon <= -105) {
    return 'America/Los_Angeles';
  }
  
  // Europa
  if (lat >= 35 && lat <= 70 && lon >= -10 && lon <= 40) {
    return 'Europe/London';
  }
  
  // Ásia - Japão
  if (lat >= 30 && lat <= 45 && lon >= 125 && lon <= 145) {
    return 'Asia/Tokyo';
  }
  
  // Fallback: usar offset UTC baseado na longitude
  const offset = Math.round(lon / 15);
  // Converter para timezone IANA aproximado
  if (offset === -3) return 'America/Sao_Paulo';
  if (offset === -5) return 'America/New_York';
  if (offset === -6) return 'America/Chicago';
  if (offset === -8) return 'America/Los_Angeles';
  if (offset === 0) return 'Europe/London';
  if (offset === 9) return 'Asia/Tokyo';
  
  // Fallback final
  return 'America/Sao_Paulo';
}

/**
 * Obtém o timezone atual do navegador (sem geolocation)
 */
export function getBrowserTimezone() {
  try {
    return Intl.DateTimeFormat().resolvedOptions().timeZone || 'America/Sao_Paulo';
  } catch {
    return 'America/Sao_Paulo';
  }
}
