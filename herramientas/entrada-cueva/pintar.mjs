import fs from 'node:fs';
const API = 'https://api.pixellab.ai/v2', H = { Authorization: 'Bearer proxy-injected', 'Content-Type': 'application/json' };
const b64 = f => ({ type: 'base64', base64: fs.readFileSync(f).toString('base64'), format: 'png' });
const desc = 'mine tunnel entrance dug into the grey stone cliff wall: a rocky outcrop of the same grey stones bulging from the cliff, with a dark arched tunnel opening at ground level framed by thick wooden beams, a small hanging lantern, dirt at the entrance, seen from a top-down RPG camera from the south, front facing';
const v3 = await fetch(API + '/inpaint-v3', { method: 'POST', headers: H, body: JSON.stringify({ description: desc, inpainting_image: { image: b64('fondo.png'), size: { width: 128, height: 128 } }, mask_image: { image: b64('mascara.png'), size: { width: 128, height: 128 } } }) }).then(async r => ({ s: r.status, j: await r.json() }));
console.log('v3', v3.s, JSON.stringify(v3.j).slice(0, 150));
for (let k = 0; v3.j.background_job_id && k < 80; k++) {
  const j = await fetch(`${API}/background-jobs/${v3.j.background_job_id}`, { headers: H }).then(r => r.json());
  if (j.status === 'completed') { const lr = j.last_response || {}; const ims = lr.images || (lr.image ? [lr.image] : []); ims.forEach((im, i) => fs.writeFileSync(`v3_${i}.png`, Buffer.from(im.base64, 'base64'))); console.log('v3 listo', ims.length, Object.keys(lr)); break; }
  if (j.status === 'failed') { console.log('v3 falló', JSON.stringify(j).slice(0, 300)); break; }
  await new Promise(s => setTimeout(s, 5000));
}
