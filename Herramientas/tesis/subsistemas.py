"""
Las pruebas de la última ejecución agrupadas por subsistema, para la tabla de la tesis.

    python subsistemas.py <resultados-EditMode.xml> <resultados-PlayMode.xml>

Sin argumentos lee los de la ejecución del 20-10-2026 (salida_faseD*, en la carpeta temporal de aquella sesión).
"""
import collections
import os
import xml.etree.ElementTree as ET

SP = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
GRUPOS = {
    'Núcleo (FSM y combate)': ['MaquinaEstadosTests', 'TablaTransicionesTests', 'ExportadorDiagramasFSMTests', 'HealthTests'],
    'Jugador': ['ArteKaelTests', 'PlayerAbilitiesTests', 'PlayerHealthTests', 'TablaJugadorTests',
                'AnimacionJugadorPlayModeTests', 'JugadorPlayModeTests'],
    'Enemigos': ['DatosEnemigosTests', 'DeteccionTests', 'EcoIraTests', 'TablaEnemigoTests', 'EnemigosTristezaTests',
                 'EnemigosMiedoTests', 'EnemigosCulpaTests', 'EcoIraPlayModeTests', 'EnemigoPlayModeTests',
                 'EnemigosMiedoPlayModeTests', 'EnemigosCulpaPlayModeTests', 'GolemPlayModeTests'],
    'Jefes': ['CentinelaArteTests', 'DatosJefesTests', 'DoncellaLamentoTests', 'EspectroDelPavorTests', 'JuezSilenteTests',
              'SaludJefeTests', 'TablaJefeTests', 'VerificadorCentinelaTests', 'OndaArteTests', 'ErupcionArteTests',
              'CentinelaAnimatorPlayModeTests', 'CentinelaPlayModeTests', 'DoncellaPlayModeTests',
              'EspectroDelPavorPlayModeTests', 'JefePlayModeTests', 'JuezPlayModeTests'],
    'Niveles y mecánicas de cada mundo': ['DisenoNivel1Tests', 'DisenoNivel2Tests', 'DisenoNivel3Tests', 'DisenoNivel4Tests',
                                          'EscenaNivel2Tests', 'EscenaNivel3Tests', 'EscenaNivel4Tests', 'EscenasNivelTests',
                                          'PreparadorNivelTests', 'MecanicasTristezaTests', 'MecanicasMiedoTests',
                                          'MecanicasCulpaTests', 'GolemMecanicasArteTests', 'VerificadorNivel1Tests',
                                          'ArteMundo2Tests', 'ArteMundo3Tests', 'ArteMundo4Tests', 'PuertasTests',
                                          'Nivel2PlayModeTests', 'Nivel3PlayModeTests', 'Nivel4PlayModeTests',
                                          'MecanicasPlayModeTests', 'MecanicasMiedoPlayModeTests',
                                          'MecanicasCulpaPlayModeTests', 'GolemMecanicasAnimatorPlayModeTests',
                                          'InundacionPlayModeTests', 'ArteMundo3PlayModeTests', 'ArteMundo4PlayModeTests',
                                          'PuertasPlayModeTests'],
    'Progresión, guardado, flujo y puntaje': ['DatosProgresionTests', 'LevelDatabaseTests', 'SaveSystemTests',
                                              'CheckpointManagerTests', 'PuntajeTests', 'TablaJuegoTests',
                                              'ProgresionPlayModeTests', 'PuntajePlayModeTests', 'FlujoNivelPlayModeTests'],
    'Narrativa, cinemáticas y registro emocional': ['CamaraCinematicaTests', 'CinematicaBeatsTests', 'CinematicaDoncellaTests',
                                                    'CinematicaInicialTests', 'CinematicaJefe1Tests', 'CinematicaPavorTests',
                                                    'DatosNarrativaTests', 'NarrativaTests', 'RegistroEmocionalTests',
                                                    'CinematicaRejillaPlayModeTests', 'NarrativaPlayModeTests'],
    'Interfaz y escenario': ['GuiaMecanicasTests', 'FondoParallaxTests', 'PintorEscenarioTests',
                             'MenuMecanicasPlayModeTests', 'ParallaxPlayModeTests'],
    'Música, efectos de sonido y opciones': ['MusicaTests', 'SonidosTests', 'OpcionesTests', 'MusicaPlayModeTests',
                                             'SonidosKaelPlayModeTests', 'OpcionesPlayModeTests'],
}

DE_20_DE_OCTUBRE = (os.path.join(SP, 'salida_faseD2/resultados-EditMode.xml'),
                    os.path.join(SP, 'salida_faseD1/resultados-PlayMode.xml'))


def contar(resultados=DE_20_DE_OCTUBRE):
    de = {c: g for g, cs in GRUPOS.items() for c in cs}
    res = collections.OrderedDict((g, [0, 0]) for g in GRUPOS)
    sin = set()
    for i, f in enumerate(resultados):
        for tc in ET.parse(f).getroot().iter('test-case'):
            c = tc.get('classname').split('.')[-1]
            if c not in de:
                sin.add(c)
                continue
            res[de[c]][i] += 1
    return res, sin


if __name__ == '__main__':
    import sys
    res, sin = contar(tuple(sys.argv[1:3]) if len(sys.argv) >= 3 else DE_20_DE_OCTUBRE)
    for g, (e, p) in res.items():
        print(f'{g:45s} {e:4d} {p:4d} {e + p:4d}')
    print('total', sum(v[0] for v in res.values()), sum(v[1] for v in res.values()), 'sin grupo:', sin)
