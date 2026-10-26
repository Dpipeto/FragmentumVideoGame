"""
La música del Nivel 1 (la Ira), con los sonidos que trae LMMS. Escribe dos proyectos de LMMS:

  Musica/Nivel1/Nivel1_Ira.mmp         El nivel. Anexo A: «percusión industrial pesada + sintetizador
                                       distorsionado, ~160 BPM, sin melodía reconocible». Mi frigio,
                                       160 BPM, 64 compases (96 s): golpes de metal; entra la caja y un
                                       bajo ácido en semicorcheas; un respiro con el sinte en un zumbido;
                                       todo junto con golpes de sinte; el bajo sube a Fa; y vuelta.
  Musica/Nivel1/Nivel1_Centinela.mmp   La Centinela Ardiente. Lo mismo, más pesado: Mi frigio, 168 BPM,
                                       48 compases (69 s), con doble bombo, un riff de acordes de quinta y
                                       un yunque de forja a contratiempo.

Ninguno lleva melodía: solo ritmo, riffs y golpes. Para pasarlos al juego: exportar_musica.py.

    python Herramientas/componer_nivel1.py            # no pisa un .mmp que ya exista
    python Herramientas/componer_nivel1.py --forzar   # lo vuelve a escribir (se pierde lo editado)
"""
import os
import sys

import musica_lmms as m

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CARPETA = os.path.join(RAIZ, 'Musica', 'Nivel1')

S = 12  # una semicorchea, en ticks (16 por compás)


def en_semicorcheas(pasos, nota=57, dur=10):
    """(semicorchea, volumen[, nota]) -> notas."""
    return [(n[0] * S, dur, n[2] if len(n) > 2 else nota, n[1]) for n in pasos]


def riff(texto, dur=10):
    """Un riff en semicorcheas: «E1 - E2 E1 ...» (16 por compás, «-» es silencio). Un «>» tras la nota
    («E1>») la acentúa."""
    notas = []
    for i, token in enumerate(texto.split()):
        if token == '-':
            continue
        acento = token.endswith('>')
        notas.append((i * S, dur, token.rstrip('>'), 115 if acento else 85))
    return notas


def tambores(p, reverb, nombre='Tambores', bitcrush=64):
    return p.canal(nombre, envios=[(0, 1.0), (reverb, 0.15)],
                   efectos=[m.bitcrush(frecuencia=22050, niveles=bitcrush, entrada_db=6, recorte_db=-2)])


def sinte_distorsionado(p, reverb):
    """Tres sierras desafinadas por un bitcrush que las rompe: el «sintetizador distorsionado»."""
    canal = p.canal('Sinte', volumen=0.45, envios=[(0, 1.0), (reverb, 0.2)],
                    efectos=[m.bitcrush(frecuencia=11025, niveles=24, entrada_db=12, recorte_db=-6),
                             m.filtro_doble(corte1=3500, reso1=0.6, tipo1=0)])
    sierras = m.triple([m.oscilador(m.SIERRA, 40, fino_izq=-8, fino_der=8), m.oscilador(m.SIERRA, 40, fino_izq=7, fino_der=-7),
                        m.oscilador(m.SIERRA, 30, grueso=-12)])
    return canal, sierras


def ruido_que_sube(p, reverb, segundos):
    """Un ruido blanco cuyo filtro se abre a lo largo de «segundos»: lleva al compás siguiente."""
    canal = p.canal('Ruido', volumen=1.0, envios=[(0, 1.0), (reverb, 0.4)])
    env = m.eldata(vol=m.env_volumen(ataque=segundos * 0.8, liberacion=0.1),
                   cut=m.envolvente('elcut', att=m.segundos(segundos), sustain=1.0, rel=0.1, amt=1.0),
                   fcut=150, fres=0.9, ftype=0, filtro=True)
    return canal, m.triple([m.oscilador(m.RUIDO, 60)]), env


# =============================================================================================
# El nivel
# =============================================================================================

BOMBO_A = [(0, 120), (4, 100), (7, 90), (10, 100), (12, 105)]
BOMBO_MEDIO = [(0, 120), (10, 100)]
BOMBO_C = [(0, 125), (3, 90), (4, 110), (8, 115), (11, 95), (12, 110), (14, 90)]
METAL = [(0, 70, 'E5'), (2, 55, 'B4'), (3, 45, 'E5'), (5, 60, 'F5'), (6, 70, 'E5'), (8, 70, 'B4'), (10, 55, 'E5'),
         (11, 45, 'F5'), (13, 60, 'E5'), (14, 70, 'B4')]
RIFF_MI = 'E1> - E2 E1 - E1 F1> - E1 - E2 - G1 F1 E1 -'
RIFF_FA = 'F1> - F2 F1 - F1 Gb1> - F1 - F2 - Ab1 Gb1 F1 -'


def ira():
    p = m.Proyecto(160, 64, notas_del_proyecto=(
        'Nivel 1 (la Ira). Mi frigio, 160 BPM, 64 compases en bucle, sin melodía. 1-8: el bombo y el metal. '
        '9-24: la caja y el bajo ácido. 25-32: un respiro, con el sinte en un zumbido. 33-48: todo, con golpes de '
        'sinte. 49-56: el bajo sube a Fa. 57-64: de vuelta en Mi, hasta el redoble.'))
    reverb = p.canal('Reverb', efectos=[m.reverb(tamano=0.6, color=7000)])
    golpes = tambores(p, reverb)
    sinte, sierras = sinte_distorsionado(p, reverb)
    bajo = p.canal('Bajo', volumen=0.47, envios=[(0, 1.0), (reverb, 0.05)])
    ruido, blanco, env_ruido = ruido_que_sube(p, reverb, 2 * 4 * 60 / 160)

    t_bombo = p.pista('Bombo', **m.golpe('drums/kick_distorted01.ogg'), canal=golpes, vol=20)
    t_caja = p.pista('Caja', **m.golpe('drums/snare_harsh01.ogg', liberacion=0.15), canal=golpes, vol=58)
    t_metal = p.pista('Metal', **m.golpe('misc/metalish_dong01.ogg', 'C4', cents=5), canal=golpes, vol=107, pan=-20)
    t_yunque = p.pista('Yunque', **m.golpe('misc/dong03.ogg', 'D5', liberacion=0.4), canal=golpes, vol=116, pan=25)
    t_golpe = p.pista('Golpe', **m.golpe('misc/hit01.ogg', liberacion=0.5), canal=golpes, vol=80)
    t_bajo = p.pista('Bajo acido', **m.lb302(corte=0.35, resonancia=0.75, envolvente=0.55, caida=0.18, distorsion=0.6),
                     canal=bajo, vol=80)
    t_sinte = p.pista('Sinte', sierras, m.eldata(vol=m.env_volumen(ataque=0.005, decaimiento=0.25, nivel=0.5, liberacion=0.15)),
                      canal=sinte, vol=80)
    t_ruido = p.pista('Ruido', blanco, env_ruido, canal=ruido, vol=110)

    for c in range(64):
        seccion = 'I' if c < 8 else 'II' if c < 24 else 'III' if c < 32 else 'IV' if c < 48 else 'V' if c < 56 else 'VI'
        # Bombo
        bombo = BOMBO_MEDIO if seccion == 'III' else BOMBO_C if seccion in ('IV', 'V', 'VI') else BOMBO_A
        t_bombo.patron(c, en_semicorcheas(bombo))
        # Metal: siempre, menos en el respiro, donde solo suena a ratos
        metal = METAL if seccion != 'III' else METAL[::3]
        t_metal.patron(c, [(n[0] * S, 6, n[2], n[1]) for n in metal])
        # Caja
        if seccion != 'I':
            if c % 8 == 7 and seccion != 'III':
                caja = [(4, 110), (8, 60), (9, 65), (10, 72), (11, 80), (12, 90), (13, 98), (14, 106), (15, 115)]
            elif seccion == 'III':
                caja = [(8, 110)]
            else:
                caja = [(4, 115), (12, 115), (15, 50)]
            t_caja.patron(c, en_semicorcheas(caja, dur=12))
        # Bajo ácido
        if seccion in ('II', 'IV', 'VI'):
            t_bajo.patron(c, riff(RIFF_MI))
        elif seccion == 'V':
            t_bajo.patron(c, riff(RIFF_FA if c % 2 == 0 else RIFF_MI))
        elif seccion == 'III' and c % 2 == 0:
            t_bajo.patron(c, [(0, 2 * 192 - 12, 'E1', 100)])
        # Sinte: un zumbido en el respiro; golpes de quinta en lo fuerte
        if seccion == 'III' and c % 4 == 0:
            t_sinte.patron(c, [(0, 4 * 192 - 24, 'E2', 70), (0, 4 * 192 - 24, 'B2', 60)])
        elif seccion in ('IV', 'VI') and c % 2 == 1:
            t_sinte.patron(c, [(pos * S, 18, k, 100) for pos in (0, 3, 6) for k in ('E2', 'B2', 'E3')])
        elif seccion == 'V' and c % 2 == 0:
            t_sinte.patron(c, [(pos * S, 18, k, 100) for pos in (0, 3, 6) for k in ('F2', 'C3', 'F3')])
        # Yunque, a contratiempo, en lo fuerte
        if seccion in ('IV', 'VI'):
            t_yunque.patron(c, [(6 * S, 24, 'E5', 90), (14 * S, 24, 'E5', 75)])
        # El golpe grande, al empezar cada sección
        if c in (0, 8, 24, 32, 48, 56):
            t_golpe.patron(c, [(0, 192, 57, 110)])
        # El ruido que sube, al cerrar cada sección
        if c in (6, 22, 30, 46, 62):
            t_ruido.patron(c, [(0, 2 * 192, 'E4', 100)])
    return p


# =============================================================================================
# La Centinela Ardiente
# =============================================================================================

DOBLE_BOMBO = [(0, 120), (1, 80), (2, 90), (3, 85), (6, 110), (8, 120), (9, 80), (10, 90), (11, 85), (14, 110)]
# El riff, en acordes de quinta, a lo largo de dos compases: «golpes», no una melodía.
RIFF_CENTINELA = [[(0, 'E'), (3, 'E'), (6, 'E'), (8, 'E'), (10, 'E'), (12, 'F'), (13, 'F')],
                  [(0, 'E'), (3, 'E'), (6, 'E'), (8, 'G'), (10, 'F'), (11, 'F'), (14, 'E')]]
QUINTAS = {'E': ('E2', 'B2', 'E3'), 'F': ('F2', 'C3', 'F3'), 'Gb': ('Gb2', 'Db3', 'Gb3'), 'G': ('G2', 'D3', 'G3'),
           'Ab': ('Ab2', 'Eb3', 'Ab3')}
MEDIO_TONO_ARRIBA = {'E': 'F', 'F': 'Gb', 'G': 'Ab'}


def centinela():
    p = m.Proyecto(168, 48, notas_del_proyecto=(
        'Nivel 1 (la Ira): la Centinela Ardiente. Mi frigio, 168 BPM, 48 compases en bucle. 1-8: el doble bombo y '
        'el yunque. 9-24: el riff de quintas, con el bajo. 25-32: a medio tiempo, con golpes de fuego. 33-48: el '
        'riff medio tono arriba y de vuelta, hasta el redoble.'))
    reverb = p.canal('Reverb', efectos=[m.reverb(tamano=0.7, color=6000)])
    golpes = tambores(p, reverb, bitcrush=48)
    sinte, sierras = sinte_distorsionado(p, reverb)
    bajo = p.canal('Bajo', volumen=0.63, envios=[(0, 1.0), (reverb, 0.05)])
    ruido, blanco, env_ruido = ruido_que_sube(p, reverb, 2 * 4 * 60 / 168)

    t_bombo = p.pista('Bombo', **m.golpe('drums/kick_hard01.ogg'), canal=golpes, vol=35)
    t_caja = p.pista('Caja', **m.golpe('drums/snare_harsh01.ogg', liberacion=0.15), canal=golpes, vol=58)
    t_yunque = p.pista('Yunque', **m.golpe('misc/dong03.ogg', 'D5', liberacion=0.4), canal=golpes, vol=138, pan=20)
    t_metal = p.pista('Metal', **m.golpe('misc/metalish_dong01.ogg', 'C4', cents=5), canal=golpes, vol=102, pan=-20)
    t_golpe = p.pista('Golpe', **m.golpe('misc/hit01.ogg', liberacion=0.5), canal=golpes, vol=85)
    t_fuego = p.pista('Fuego', **m.golpe('effects/explode01.ogg', liberacion=0.5), canal=golpes, vol=53)
    t_bajo = p.pista('Bajo acido', **m.lb302(corte=0.3, resonancia=0.7, envolvente=0.6, caida=0.15, distorsion=0.7),
                     canal=bajo, vol=80)
    t_sinte = p.pista('Riff', sierras, m.eldata(vol=m.env_volumen(ataque=0.005, decaimiento=0.2, nivel=0.55, liberacion=0.1)),
                      canal=sinte, vol=85)
    t_ruido = p.pista('Ruido', blanco, env_ruido, canal=ruido, vol=110)

    for c in range(48):
        seccion = 'A' if c < 8 else 'B' if c < 24 else 'C' if c < 32 else 'D'
        medio = seccion == 'C'
        t_bombo.patron(c, en_semicorcheas([(0, 125), (8, 115)] if medio else DOBLE_BOMBO))
        if seccion != 'A':
            caja = [(8, 120)] if medio else [(4, 115), (12, 115)]
            if c % 8 == 7:
                caja = [(4, 110)] + [(i, 60 + 7 * (i - 8)) for i in range(8, 16)]
            t_caja.patron(c, en_semicorcheas(caja, dur=12))
        t_yunque.patron(c, [(6 * S, 24, 'E5', 95), (14 * S, 24, 'E5', 80)] if not medio else [(0, 48, 'E4', 110)])
        t_metal.patron(c, [(i * S, 6, 'E5' if i % 4 == 0 else 'B4', 70 if i % 4 == 0 else 45) for i in range(0, 16, 2)])
        if seccion in ('B', 'C', 'D'):
            subir = seccion == 'D' and (c - 32) % 8 >= 4  # medio tono arriba, la mitad de la última sección
            golpes_riff = RIFF_CENTINELA[c % 2]
            if medio:
                golpes_riff = [g for g in golpes_riff if g[0] in (0, 8)]
            golpes_riff = [(pos, MEDIO_TONO_ARRIBA[raiz] if subir else raiz) for pos, raiz in golpes_riff]
            t_sinte.patron(c, [(pos * S, 30 if medio else 16, k, 105) for pos, raiz in golpes_riff for k in QUINTAS[raiz]])
            # El bajo, la fundamental una octava abajo
            t_bajo.patron(c, [(pos * S, 10, m.tecla(QUINTAS[raiz][0]) - 12, 110) for pos, raiz in golpes_riff])
        if c in (0, 8, 24, 32, 40):
            t_golpe.patron(c, [(0, 192, 57, 115)])
        if medio and c % 2 == 0:
            t_fuego.patron(c, [(0, 192, 45, 100)])  # una octava abajo: un estallido grave
        if c in (6, 22, 30, 46):
            t_ruido.patron(c, [(0, 2 * 192, 'E4', 100)])
    return p


PROYECTOS = {'Nivel1_Ira.mmp': ira, 'Nivel1_Centinela.mmp': centinela}


if __name__ == '__main__':
    forzar = '--forzar' in sys.argv
    os.makedirs(CARPETA, exist_ok=True)
    for archivo, componer in PROYECTOS.items():
        ruta = os.path.join(CARPETA, archivo)
        if os.path.exists(ruta) and not forzar:
            print(f'{archivo}: ya existe, no se toca (--forzar para volver a escribirlo).')
            continue
        proyecto = componer()
        proyecto.guardar(ruta)
        print(f'{archivo}: {proyecto.compases:g} compases a {proyecto.bpm} BPM ({proyecto.segundos:.1f} s), '
              f'{len(proyecto.pistas)} pistas.')
