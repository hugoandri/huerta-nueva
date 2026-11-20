// Identidad del jugador. Hoy: un id aleatorio guardado en el navegador.
// En la versión online, el id vendrá de la cuenta (por ejemplo, el usuario de Supabase Auth)
// y el resto del juego no cambia: todo usa perfil.id.
const CLAVE = 'huerta-nueva-perfil';

function idAleatorio() {
  if (globalThis.crypto?.randomUUID) return crypto.randomUUID();
  return 'j-' + Math.random().toString(36).slice(2) + Date.now().toString(36);
}

export function obtenerPerfil() {
  try {
    const p = JSON.parse(localStorage.getItem(CLAVE) || 'null');
    if (p?.id) return p;
  } catch (e) { /* sin almacenamiento: perfil temporal */ }
  const p = { id: idAleatorio(), nombre: 'Granjera' };
  try { localStorage.setItem(CLAVE, JSON.stringify(p)); } catch (e) { /* perfil temporal */ }
  return p;
}
