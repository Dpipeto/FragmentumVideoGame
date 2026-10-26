"""
Actualiza TesisIA.docx tras el tercer concepto de evaluación (16-10-2026) con lo construido en Unity hasta el
20-10-2026: tres mundos jugables y el cuarto en desarrollo. Parte de la versión del 13-10-2026 (la que el usuario
tenía en Descargas, con sus cambios) y escribe una nueva.

    python actualizar_tesisia.py <entrada.docx> <salida.docx>
"""
import sys

import t3_anexos
import t3_parte1
import t3_parte2
from docx_util import Documento


def main(entrada, salida):
    d = Documento(entrada)
    t3_parte1.portada(d)
    t3_parte1.preliminares(d)
    t3_parte1.capitulo1(d)
    t3_parte1.capitulo3_metodologia(d)
    t3_parte1.diseno(d)
    t3_parte2.implementacion(d)
    t3_parte2.pruebas_y_resultados(d)
    t3_parte2.capitulo9(d)
    t3_parte2.conclusiones(d)
    t3_parte2.referencias(d)
    t3_anexos.anexos(d)
    cuenta, faltan = d.renumerar()
    print('tablas y figuras:', cuenta, '; referencias sin destino:', faltan or 'ninguna')
    d.guardar(salida)
    print('guardado', salida)


if __name__ == '__main__':
    main(sys.argv[1], sys.argv[2])
