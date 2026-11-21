# Compone el tramo recto de muralla (tal como lo dibuja js/cliente/render.js) para cada tileset de
# mazmorra, y una máscara. Recorte de 96x80: 3 columnas, desde 2 filas sobre la pared hasta media
# fila de suelo. Uso: python3 componer.py  (escribe fondo_<tileset>.png y mascara.png)
import zlib, struct, base64, json, sys, os
R = os.path.join(os.path.dirname(__file__), '..', '..')
def dec(raw):
    pos=8; idat=b''
    while pos < len(raw):
        ln,=struct.unpack('>I',raw[pos:pos+4]); typ=raw[pos+4:pos+8]; data=raw[pos+8:pos+8+ln]
        if typ==b'IHDR': w,h=struct.unpack('>II',data[:8]); ct=data[9]
        if typ==b'IDAT': idat+=data
        pos+=12+ln
    bpp=4 if ct==6 else 3; px=zlib.decompress(idat); st=w*bpp; out=[]; prev=bytearray(st); i=0
    for y in range(h):
        f=px[i]; i+=1; line=bytearray(px[i:i+st]); i+=st
        for x in range(st):
            a=line[x-bpp] if x>=bpp else 0; b=prev[x]; c=prev[x-bpp] if x>=bpp else 0
            if f==1: line[x]=(line[x]+a)&255
            elif f==2: line[x]=(line[x]+b)&255
            elif f==3: line[x]=(line[x]+(a+b)//2)&255
            elif f==4:
                q=a+b-c; pa,pb,pc=abs(q-a),abs(q-b),abs(q-c)
                line[x]=(line[x]+(a if pa<=pb and pa<=pc else b if pb<=pc else c))&255
        prev=line
        out.append(line if bpp==4 else bytearray(b''.join(bytes(line[k*3:k*3+3])+b'\xff' for k in range(w))))
    return w,h,out
def png(w,h,rows):
    raw=b''.join(b'\x00'+bytes(r) for r in rows)
    ch=lambda t,d: struct.pack('>I',len(d))+t+d+struct.pack('>I',zlib.crc32(t+d)&0xffffffff)
    return b'\x89PNG\r\n\x1a\n'+ch(b'IHDR',struct.pack('>IIBBBBB',w,h,8,6,0,0,0))+ch(b'IDAT',zlib.compress(raw))+ch(b'IEND',b'')
T=32; CW,CH=96,80; X0,Y0=32,32           # celdas 0..4; muro en filas 0..2, suelo en 3..4; pared en (2,2)
muro=lambda x,y: 1 if y<=2 else 0
for nombre in sys.argv[1:] or ['cueva','cripta']:
    tiles=[dec(base64.b64decode(u.split(',')[1])) for u in json.load(open(os.path.join(R,f'assets/tilesets/{nombre}.json')))['tiles']]
    can=[bytearray([0,0,0,0]*CW) for _ in range(CH)]
    def pegar(img,wx,wy):
        w,h,rows=img
        for yy in range(h):
            cy=wy+yy-Y0
            if not 0<=cy<CH: continue
            for xx in range(w):
                cx=wx+xx-X0
                if not 0<=cx<CW: continue
                r,g,b,a=rows[yy][xx*4:xx*4+4]
                if a==0: continue
                dr,dg,db,_=can[cy][cx*4:cx*4+4]; al=a/255
                can[cy][cx*4:cx*4+4]=bytes([round(r*al+dr*(1-al)),round(g*al+dg*(1-al)),round(b*al+db*(1-al)),255])
    for y in range(0,5):
        for x in range(0,5): pegar(tiles[0],x*T,y*T)
    for j in range(-1,5):
        for i in range(-1,5):
            v=lambda x,y: 1-muro(x,y)
            n=v(i,j)*8+v(i+1,j)*4+v(i,j+1)*2+v(i+1,j+1)
            if n: pegar(tiles[n],i*T+T//2,j*T+T//2)
    open(f'fondo_{nombre}.png','wb').write(png(CW,CH,can))
mask=[bytearray([0,0,0,255]*CW) for _ in range(CH)]
for y in range(4,78):
    for x in range(14,82): mask[y][x*4:x*4+4]=b'\xff\xff\xff\xff'
open('mascara.png','wb').write(png(CW,CH,mask))
print('ok')
