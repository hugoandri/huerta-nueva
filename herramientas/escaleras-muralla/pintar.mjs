// Pinta las escaleras dentro del tramo de muralla de cada mazmorra con inpaint-v3 y restaura el borde.
// Uso: PIXELLAB_API_KEY=... node pintar.mjs   (necesita fondo_<tileset>.png y mascara.png de componer.py)
import fs from 'node:fs';
const API = 'https://api.pixellab.ai/v2', H = { Authorization: `Bearer ${process.env.PIXELLAB_API_KEY}`, 'Content-Type': 'application/json' };
const img = f => ({ image: { type: 'base64', base64: fs.readFileSync(f).toString('base64'), format: 'png' }, size: { width: 96, height: 80 } });
const VISTA = 'seen from a top-down RPG camera from the south, front facing, built into the wall, no people';
const PIEZAS = {
  escalera_subir: 'stone archway doorway carved into the dungeon wall with a single flight of worn stone steps going up toward bright daylight, ' + VISTA,
  escalera_bajar: 'a single wooden ladder leaning against the dungeon wall, its top reaching a glowing square wooden trapdoor hatch opening in the ceiling above, warm light shining down from the hatch onto the ladder, 3D shading, ' + VISTA,
};
const tilesets = process.argv.slice(2).length ? process.argv.slice(2) : ['cueva', 'cripta'];
const esperar = ms => new Promise(r => setTimeout(r, ms));
const solo = process.env.SOLO;
await Promise.all(tilesets.flatMap(ts => Object.entries(PIEZAS).filter(([id]) => !solo || solo === id).map(async ([id, desc]) => {
  const r = await fetch(API + '/inpaint-v3', { method: 'POST', headers: H, body: JSON.stringify({ description: desc, inpainting_image: img(`fondo_${ts}.png`), mask_image: img('mascara.png') }) });
  const j = await r.json(); if (!r.ok) return console.log(id, ts, r.status, JSON.stringify(j).slice(0, 200));
  for (let k = 0; k < 90; k++) {
    const b = await fetch(`${API}/background-jobs/${j.background_job_id}`, { headers: H }).then(x => x.json());
    if (b.status === 'completed') { fs.writeFileSync(`${id}_${ts}.png`, Buffer.from(b.last_response.image.base64, 'base64')); return console.log('✓', id, ts); }
    if (b.status === 'failed') return console.log('✗', id, ts);
    await esperar(5000);
  }
})));
