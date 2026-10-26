"""
Los efectos de sonido de Kael, con los sonidos que trae LMMS. Escribe un proyecto de LMMS por efecto, en
Sonidos/Kael:

  Salto.mmp          Al saltar (y al saltar desde una pared): el pie que empuja y el aire, que sube.
  DobleSalto.mmp     El segundo salto, en el aire: el aire más alto, una capa que aletea y un brillo que sube.
  Dash.mmp           Un arranque grave y una ráfaga de aire que crece y se va.
  Ataque.mmp         El tajo de la espada: un silbido de aire, rápido, y el filo, que baja.
  AtaqueCargado.mmp  El tajo cargado: más lento y más grave, con un golpe sordo y el metal de la hoja.
  Danio.mmp          El golpe que recibe: un impacto, un crujido y un quejido que cae.

Cada uno mide un compás a 125 BPM (1,92 s; un tick son 10 ms) y suena desde el principio. Para pasarlos al
juego: exportar_sonidos.py, que los corta donde se apagan.

    python Herramientas/componer_sonidos_kael.py            # no pisa un .mmp que ya exista
    python Herramientas/componer_sonidos_kael.py --forzar   # los vuelve a escribir (se pierde lo editado)
"""
import os
import sys

import musica_lmms as m

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CARPETA = os.path.join(RAIZ, 'Sonidos', 'Kael')
BPM = 125  # un tick, 10 ms
PASO_BANDA = 2  # el filtro de LMMS que deja pasar una banda (BandPass csg)


def ms(milisegundos):
    """Los ticks de esos milisegundos (a 125 BPM, un tick son 10 ms)."""
    return milisegundos / 10.0


def efecto(notas):
    return m.Proyecto(BPM, 1, notas_del_proyecto=notas, bucle=False)


def aire(desde, hasta, ataque, caida, mantener=0.0, reso=3.0, ataque_corte=None, caida_corte=None, tipo=PASO_BANDA):
    """
    Ruido por un filtro que se mueve: el aire. El filtro va de «desde» a «hasta» (Hz) mientras crece el sonido
    (ataque_corte) y vuelve mientras se apaga (caida_corte). reso: lo estrecho que es (3, un silbido de
    viento; 1, un soplo ancho). LMMS sube el filtro (barrido² × 6000) Hz: el barrido sale de ahí (medido).
    """
    ataque_corte = ataque if ataque_corte is None else ataque_corte
    caida_corte = caida if caida_corte is None else caida_corte
    corte, barrido = desde, ((hasta - desde) / 6000.0) ** 0.5
    return dict(instrumento=m.triple([m.oscilador(m.RUIDO, 100)]),
                envolventes=m.eldata(vol=m.env_volumen(ataque=ataque, mantener=mantener, decaimiento=caida, nivel=0.0,
                                                       liberacion=0.02),
                                     cut=m.envolvente('elcut', att=m.segundos(ataque_corte), hold=m.segundos(mantener),
                                                      dec=m.segundos(caida_corte), sustain=0.0, rel=m.segundos(0.02),
                                                      amt=barrido),
                                     fcut=corte, fres=reso, ftype=tipo, filtro=True))


def golpe_sordo(volumen=None, **valores):
    """Kicker: un seno que cae de startfreq a endfreq en decay ms, con su chasquido (click) y su ruido. volumen:
    si se da, se apaga en esos segundos (la distorsión de Kicker lo sostiene hasta el final y lo corta en seco)."""
    vol = m.env_volumen(decaimiento=volumen, nivel=0.0, liberacion=0.02) if volumen else None
    return dict(instrumento=m.kicker(**valores), envolventes=m.eldata(vol=vol))


def retro(onda, **valores):
    return dict(instrumento=m.sfxr(onda, **valores), envolventes=m.eldata())


# =============================================================================================

def salto():
    p = efecto('El salto de Kael (y el salto desde la pared): el pie que empuja (Kicker) y el aire, que sube '
               '(ruido por un filtro que se abre).')
    p.pista('Impulso', **golpe_sordo(startfreq=170, endfreq=60, decay=110, dist=0.2, distend=0.1, env=0.3,
                                     noise=0.15, click=0.2, slope=0.2), vol=45).en(0, [(0, ms(150), 57, 100)])
    p.pista('Aire', **aire(desde=250, hasta=1400, ataque=0.03, mantener=0.03, caida=0.17, ataque_corte=0.09,
                           caida_corte=0.12), vol=100).en(0, [(0, ms(300), 57, 100)])
    return p


def doble_salto():
    p = efecto('El doble salto de Kael, en el aire: el aire más alto, otra capa que aletea detrás y un brillo que '
               'sube (sfxr, un seno).')
    p.pista('Aire', **aire(desde=400, hasta=2200, ataque=0.02, mantener=0.02, caida=0.16, ataque_corte=0.07,
                           caida_corte=0.12), vol=100).en(0, [(0, ms(260), 57, 100)])
    p.pista('Aleteo', **aire(desde=300, hasta=1200, ataque=0.02, caida=0.14, ataque_corte=0.05, reso=2.0), vol=90) \
        .en(ms(50), [(0, ms(200), 57, 100)])
    p.pista('Brillo', **retro(m.SFXR_SENO, startFreq=m.sfxr_hz(500), slide=0.25, att=m.sfxr_segundos(0.01),
                              hold=m.sfxr_segundos(0.06), dec=m.sfxr_segundos(0.12), lpFilCut=0.75), vol=22) \
        .en(0, [(0, ms(220), 57, 100)])
    return p


def dash():
    p = efecto('El dash de Kael: un arranque grave (Kicker) y una ráfaga de aire que crece y se va, con su cuerpo grave.')
    p.pista('Arranque', **golpe_sordo(startfreq=240, endfreq=50, decay=150, dist=0.4, distend=0.2, env=0.25,
                                      noise=0.2, click=0.3, slope=0.25), vol=55).en(0, [(0, ms(200), 57, 100)])
    p.pista('Rafaga', **aire(desde=200, hasta=1600, ataque=0.015, mantener=0.08, caida=0.25, ataque_corte=0.08,
                             caida_corte=0.22, reso=2.5), vol=110).en(0, [(0, ms(380), 57, 100)])
    p.pista('Cuerpo', **aire(desde=150, hasta=700, ataque=0.01, mantener=0.05, caida=0.25, ataque_corte=0.05,
                             caida_corte=0.2, reso=0.7, tipo=0), vol=140).en(0, [(0, ms(330), 57, 100)])
    return p


def ataque():
    p = efecto('El tajo de la espada de Kael: un silbido de aire, rápido, y el filo, que baja (sfxr, un seno).')
    p.pista('Tajo', **aire(desde=600, hasta=3200, ataque=0.035, caida=0.13, ataque_corte=0.05, caida_corte=0.1,
                           reso=4.0), vol=110).en(0, [(0, ms(200), 57, 100)])
    p.pista('Filo', **retro(m.SFXR_SENO, startFreq=m.sfxr_hz(2200), slide=-0.3, att=m.sfxr_segundos(0.02),
                            hold=m.sfxr_segundos(0.02), dec=m.sfxr_segundos(0.1), hpFilCut=0.2), vol=35) \
        .en(ms(20), [(0, ms(160), 57, 100)])
    return p


def ataque_cargado():
    p = efecto('El tajo cargado de Kael: más lento y más grave que el normal, con un golpe sordo (Kicker) donde '
               'corta, el filo y el metal de la hoja.')
    p.pista('Tajo', **aire(desde=300, hasta=2000, ataque=0.05, mantener=0.03, caida=0.25, ataque_corte=0.08,
                           caida_corte=0.2, reso=3.0), vol=120).en(0, [(0, ms(350), 57, 100)])
    p.pista('Peso', **golpe_sordo(startfreq=140, endfreq=38, decay=320, dist=0.3, distend=0.1, env=0.2, noise=0.25,
                                  click=0.4, slope=0.3, volumen=0.25), vol=90).en(ms(50), [(0, ms(380), 57, 100)])
    p.pista('Filo', **retro(m.SFXR_SENO, startFreq=m.sfxr_hz(1400), slide=-0.25, att=m.sfxr_segundos(0.02),
                            hold=m.sfxr_segundos(0.04), dec=m.sfxr_segundos(0.15), hpFilCut=0.2), vol=35) \
        .en(ms(40), [(0, ms(240), 57, 100)])
    p.pista('Metal', **m.golpe('misc/metalish_dong01.ogg', 'C4', decaimiento=0.25, nivel=0.0, liberacion=0.05),
            vol=40).en(ms(50), [(0, ms(300), 'G4', 100)])
    return p


def danio():
    p = efecto('Kael recibe un golpe: un impacto (Kicker), un crujido (sfxr, ruido) y un quejido que cae (sfxr, '
               'una cuadrada apagada).')
    p.pista('Impacto', **golpe_sordo(startfreq=320, endfreq=55, decay=140, dist=1.2, distend=0.6, env=0.18,
                                     noise=0.35, click=0.6, slope=0.3), vol=100).en(0, [(0, ms(200), 57, 100)])
    p.pista('Crujido', **retro(m.SFXR_RUIDO, startFreq=m.sfxr_hz(180), slide=-0.15, att=0.0,
                               hold=m.sfxr_segundos(0.02), dec=m.sfxr_segundos(0.12), lpFilCut=0.8), vol=70) \
        .en(0, [(0, ms(180), 57, 100)])
    p.pista('Quejido', **retro(m.SFXR_CUADRADA, startFreq=m.sfxr_hz(330), slide=-0.2, sqrDuty=0.4, att=0.0,
                               hold=m.sfxr_segundos(0.06), dec=m.sfxr_segundos(0.16), lpFilCut=0.65, lpFilReso=0.3),
            vol=70).en(ms(10), [(0, ms(260), 57, 100)])
    return p


PROYECTOS = {'Salto.mmp': salto, 'DobleSalto.mmp': doble_salto, 'Dash.mmp': dash, 'Ataque.mmp': ataque,
             'AtaqueCargado.mmp': ataque_cargado, 'Danio.mmp': danio}


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
        print(f'{archivo}: {len(proyecto.pistas)} pistas.')
