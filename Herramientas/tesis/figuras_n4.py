"""Figuras del mundo de la Culpa para TesisIA (24-10-2026), con el estilo de las del Miedo (figuras_t3)."""
import os
import sys

from figuras_t3 import CIN, DIS, SAL, S, hoja, panel


def personajes_culpa():
    hoja([
        ('Penitente (el Eco de la Culpa)', [(S('Enemigos', 'Penitente', 'I1'), 'Quieto'), (S('Enemigos', 'Penitente', 'r2'), 'Andar'),
                                           (S('Enemigos', 'Penitente', 'a2'), 'Embiste'), (S('Enemigos', 'Penitente', 'a3'), 'Zarpazo'),
                                           (S('Enemigos', 'Penitente', 'd3'), 'Muere')]),
        ('Artillero', [(S('Enemigos', 'Artillero', 'K1'), 'Reposo'), (S('Enemigos', 'Artillero', 'A1'), 'Apunta'),
                       (S('Enemigos', 'Artillero', 'F1'), 'Dispara'), (S('Enemigos', 'Artillero', 'G2'), 'Culatazo')]),
        ('El Verdugo (mini-jefe)', [(S('Enemigos', 'Verdugo', 'W1'), 'Anda'), (S('Enemigos', 'Verdugo', 'H1'), 'Alza el hacha'),
                                    (S('Enemigos', 'Verdugo', 'H2'), 'Hachazo'), (S('Enemigos', 'Verdugo', 'C2'), 'Cadena'),
                                    (S('Enemigos', 'Verdugo', 'K2'), 'De rodillas')]),
        ('El Juez Silente (jefe)', [(S('Jefes', 'Juez', 'I1'), 'Espera'), (S('Jefes', 'Juez', 'O2'), 'La orden'),
                                    (S('Jefes', 'Juez', 'P2'), 'El compañero'), (S('Jefes', 'Juez', 'K1'), 'De rodillas'),
                                    (S('Jefes', 'Juez', 'M4'), 'Cae')]),
    ], os.path.join(SAL, 'personajes_culpa.png'))


def capturas_n4():
    panel([os.path.join(DIS, f'Nivel4_{n}.png') for n in ('Puertas_CheckpointEncendido', 'Memorias_Tejados',
                                                          'Memorias_Carta', 'Puertas_Onda')],
          os.path.join(SAL, 'nivel4_capturas.jpg'))


def cinematicas_4():
    panel([os.path.join(CIN, n) for n in ('Juez_Presentacion_3s.png', 'Juez_Presentacion_6.5s.png', 'Culpa_Memoria_LaOrden.png',
                                          'Juez_Derrota_2s.png', 'Juez_Derrota_7s.png', 'Culpa_Salida_10s.png')],
          os.path.join(SAL, 'cinematicas_4.jpg'), columnas=3, ancho=640, alto=360)


if __name__ == '__main__':
    for q in sys.argv[1:] or ['personajes_culpa', 'capturas_n4', 'cinematicas_4']:
        globals()[q]()
        print('hecho', q)
