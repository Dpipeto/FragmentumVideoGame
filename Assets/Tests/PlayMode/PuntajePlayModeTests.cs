using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fragmentum.Core;
using Fragmentum.Core.Juego;
using Fragmentum.Core.Puntuacion;
using Fragmentum.Core.Save;
using Fragmentum.Enemies;
using Fragmentum.Levels;
using Fragmentum.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Fragmentum.Tests.PlayMode
{
    /// <summary>
    /// El puntaje jugando el Nivel 1 de verdad: los enemigos suman lo que valen (el Golem, que es
    /// el mini-jefe, varias veces lo que un Eco), el reloj corre solo mientras se juega, cruzar la
    /// salida anota el puntaje con su bono por tiempo y lo enseña el fin de nivel, y continuar
    /// desde un checkpoint no pone el marcador a cero ni deja cobrar dos veces al mismo enemigo.
    /// También el menú principal: el fondo, los nombres de los autores y el puntaje total.
    /// </summary>
    public class PuntajePlayModeTests : PruebaFlujoBase
    {
        private static LevelManager Nivel => Object.FindAnyObjectByType<LevelManager>();

        private static MarcadorNivel Marcador => MarcadorNivel.Actual;

        // Los enemigos del nivel de izquierda a derecha, tal como están al cargar la escena.
        private static List<EnemyController> Enemigos() =>
            Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude)
                .OrderBy(e => e.transform.position.x).ToList();

        private static List<EnemyController> Ecos() => Enemigos().Where(e => !e.Datos.hiperarmadura).ToList();

        private static EnemyController Golem() => Enemigos().First(e => e.Datos.hiperarmadura);

        [UnityTest]
        public IEnumerator AlEmpezar_ElMarcadorEstaACero_YElHudLoEnseña()
        {
            yield return EntrarEnNivel1();

            Assert.IsNotNull(Marcador, "El LevelManager crea el marcador del nivel.");
            Assert.AreSame(Marcador, Nivel.Marcador);
            Assert.AreEqual(0, Marcador.Puntos);
            Assert.Less(Marcador.Segundos, 2f);
            Assert.AreEqual("PUNTAJE  0", Interfaz.Hud.Puntaje.TextoPuntos);
            StringAssert.StartsWith("TIEMPO  00:0", Interfaz.Hud.Puntaje.TextoTiempo);
            Assert.IsEmpty(Interfaz.Hud.Puntaje.TextoGanancia);
        }

        [UnityTest]
        public IEnumerator DerrotarEnemigos_SumaLoQueValeCadaUno_YElMiniJefeValeMas()
        {
            yield return EntrarEnNivel1();
            yield return null;
            var eco = Ecos()[0];
            var golem = Golem();
            var avisos = new List<(int total, int ganados)>();
            EventosJuego.PuntajeCambiado += (total, ganados) => avisos.Add((total, ganados));

            eco.Vida.Matar();
            yield return null;

            Assert.AreEqual(eco.Datos.puntos, Marcador.Puntos);
            Assert.AreEqual(100, Marcador.PuntosEnemigos);
            Assert.AreEqual("PUNTAJE  100", Interfaz.Hud.Puntaje.TextoPuntos);
            Assert.AreEqual("+100", Interfaz.Hud.Puntaje.TextoGanancia, "Un momento, lo que se acaba de ganar.");
            CollectionAssert.AreEqual(new[] { (100, 100) }, avisos);

            golem.Vida.Matar();
            yield return null;

            Assert.Greater(golem.Datos.puntos, eco.Datos.puntos * 3, "El Golem de Ceniza es el mini-jefe del mundo.");
            Assert.AreEqual(eco.Datos.puntos + golem.Datos.puntos, Marcador.Puntos);
            Assert.AreEqual($"PUNTAJE  {ReglasPuntaje.Puntos(Marcador.Puntos)}", Interfaz.Hud.Puntaje.TextoPuntos);
            Assert.AreEqual(2, Marcador.Derrotados);
            Assert.AreEqual(0, Marcador.PuntosJefe);
        }

        [UnityTest]
        public IEnumerator ElReloj_CorreJugando_YSeParaEnPausa()
        {
            yield return EntrarEnNivel1();
            yield return EsperarReal(0.4f);
            var jugando = Marcador.Segundos;
            Assert.Greater(jugando, 0.2f, "Jugando, el reloj corre.");

            Assert.IsTrue(Juego.Pausar());
            yield return null;
            var enPausa = Marcador.Segundos;
            yield return EsperarReal(0.5f);
            Assert.AreEqual(enPausa, Marcador.Segundos, 0.0001f, "En pausa el reloj no corre.");

            Assert.IsTrue(Juego.Reanudar());
            yield return EsperarReal(0.4f);
            Assert.Greater(Marcador.Segundos, enPausa + 0.2f, "Al reanudar vuelve a correr.");
        }

        [UnityTest]
        public IEnumerator CruzarLaSalida_AnotaElPuntajeConSuBonoPorTiempo_YLoEnseñaElFinDeNivel()
        {
            yield return EntrarEnNivel1();
            yield return null;
            Ecos()[0].Vida.Matar();
            yield return null;
            var marcador = Marcador;

            Assert.IsTrue(Nivel.Completar());
            yield return SaltarCinematicaSiHay();
            yield return EsperarEstado(ESTADOS_JUEGO.LEVEL_COMPLETE, 5f);

            Assert.IsTrue(marcador.Cerrado);
            var resultado = marcador.Resultado;
            var bono = ReglasPuntaje.BonoDeTiempo(resultado.Segundos, Nivel1.tiempoConBono, Nivel1.puntosPorSegundo);
            Assert.Greater(bono, 0, "Cruzarlo en segundos deja casi todo el bono.");
            Assert.AreEqual(bono, resultado.BonoTiempo);
            Assert.AreEqual(100, resultado.Enemigos);
            Assert.AreEqual(100 + bono, resultado.Total);
            Assert.IsFalse(resultado.HabiaAnterior);

            // Queda en la partida, y en el archivo.
            Assert.AreEqual(resultado.Total, Gestor.Datos.PuntajeDe(Nivel1.id).puntos);
            Assert.AreEqual(resultado.Total, Gestor.Datos.PuntajeTotal);
            var guardado = new SaveSystem(CarpetaGuardado).Cargar().PuntajeDe(Nivel1.id);
            Assert.IsNotNull(guardado, "El puntaje se guarda con el nivel superado.");
            Assert.AreEqual(resultado.Total, guardado.puntos);

            // Y lo enseña el fin de nivel, tras el registro emocional.
            var pantalla = Interfaz.PantallaFinNivel;
            if (pantalla.MostrandoRegistro)
            {
                pantalla.OmitirRegistro();
            }

            Assert.AreEqual($"PUNTAJE  {ReglasPuntaje.Puntos(resultado.Total)}", pantalla.TextoPuntaje);
            StringAssert.Contains("Enemigos 100", pantalla.DetallePuntaje);
            StringAssert.Contains($"+{ReglasPuntaje.Puntos(bono)}", pantalla.DetallePuntaje);
            StringAssert.DoesNotContain("récord", pantalla.DetallePuntaje, "La primera vez no hay récord que batir.");

            // El marcador cerrado ya no suma ni corre.
            var segundos = marcador.Segundos;
            yield return EsperarReal(0.3f);
            Assert.AreEqual(segundos, marcador.Segundos, 0.0001f);
        }

        [UnityTest]
        public IEnumerator VolverASuperarElNivelConMenosPuntos_NoBorraElMejor()
        {
            Gestor.Datos.AnotarPuntaje(Nivel1.id, 99999, 30f);
            yield return EntrarEnNivel1();
            yield return null;

            Assert.IsTrue(Nivel.Completar());
            yield return SaltarCinematicaSiHay();
            yield return EsperarEstado(ESTADOS_JUEGO.LEVEL_COMPLETE, 5f);

            Assert.AreEqual(99999, Gestor.Datos.PuntajeDe(Nivel1.id).puntos);
            var pantalla = Interfaz.PantallaFinNivel;
            if (pantalla.MostrandoRegistro)
            {
                pantalla.OmitirRegistro();
            }

            StringAssert.Contains("Tu mejor puntaje: 99 999", pantalla.DetallePuntaje);
        }

        [UnityTest]
        public IEnumerator ContinuarDesdeUnCheckpoint_SigueConElPuntajeYElRelojQueTenia()
        {
            yield return EntrarEnNivel1();
            yield return null;
            var ecos = Ecos();
            var primero = ecos[0].name;
            var segundo = ecos[1].name;
            ecos[0].Vida.Matar();
            yield return EsperarReal(0.3f);

            // El checkpoint guarda la foto del intento: 100 puntos, el reloj y a quién se derrotó.
            var checkpoint = Nivel.Checkpoints.Buscar("L1_CP1");
            yield return LlevarJugadorA(EnElSuelo(checkpoint.transform.position));
            Assert.AreSame(checkpoint, Nivel.Checkpoints.Activo);
            var foto = new SaveSystem(CarpetaGuardado).Cargar().intento;
            Assert.AreEqual(Nivel1.id, foto.nivelId);
            Assert.AreEqual(100, foto.puntos);
            Assert.Greater(foto.segundos, 0.2f);
            Assert.AreEqual(1, foto.derrotados.Count);

            // Lo que pasa después del checkpoint no está en la foto.
            Ecos().First(e => e.name == segundo).Vida.Matar();
            yield return null;
            Assert.AreEqual(200, Marcador.Puntos);

            // Salir al menú y continuar: la escena se carga de nuevo, con todos los enemigos vivos.
            Assert.IsTrue(Juego.Pausar());
            Assert.IsTrue(Juego.IrAlMenu());
            yield return EsperarEstado(ESTADOS_JUEGO.MAIN_MENU, 5f);
            Assert.IsTrue(Juego.ContinuarPartida());
            yield return EsperarEstado(ESTADOS_JUEGO.PLAYING, 5f);
            yield return null;

            Assert.AreEqual(100, Marcador.Puntos, "Se sigue con lo que había en el checkpoint, no con cero ni con lo de después.");
            Assert.GreaterOrEqual(Marcador.Segundos, foto.segundos, "Salir no pone el reloj a cero.");
            Assert.AreEqual("PUNTAJE  100", Interfaz.Hud.Puntaje.TextoPuntos);

            // El Eco que ya se cobró vuelve a estar vivo, pero no se cobra dos veces; el otro, sí.
            Ecos().First(e => e.name == primero).Vida.Matar();
            yield return null;
            Assert.AreEqual(100, Marcador.Puntos, "El mismo enemigo no da puntos dos veces en el mismo intento.");
            Ecos().First(e => e.name == segundo).Vida.Matar();
            yield return null;
            Assert.AreEqual(200, Marcador.Puntos);
        }

        [UnityTest]
        public IEnumerator ReiniciarElNivel_EmpiezaOtroIntentoDesdeCero()
        {
            yield return EntrarEnNivel1();
            yield return null;
            Ecos()[0].Vida.Matar();
            yield return LlevarJugadorA(EnElSuelo(Nivel.Checkpoints.Buscar("L1_CP1").transform.position));
            Assert.AreEqual(100, Gestor.Datos.intento.puntos);

            Assert.IsTrue(Juego.Pausar());
            Assert.IsTrue(Juego.ReiniciarNivel());
            yield return EsperarEstado(ESTADOS_JUEGO.LOADING, 5f);
            yield return EsperarEstado(ESTADOS_JUEGO.PLAYING, 5f);
            yield return null;

            Assert.AreEqual(0, Marcador.Puntos, "Reiniciar es empezar otro intento.");
            Assert.Less(Marcador.Segundos, 2f);
            Assert.AreEqual(0, Gestor.Datos.intento.nivelId, "La foto del intento anterior se descarta.");
        }

        // --- El menú principal ---

        [UnityTest]
        public IEnumerator ElMenuPrincipal_TieneFondo_NombraASusAutores_YEnseñaElPuntajeTotal()
        {
            Gestor.Datos.AnotarPuntaje(1, 4200, 300f);
            Gestor.Datos.AnotarPuntaje(2, 5300, 500f);
            var menu = Object.FindAnyObjectByType<MainMenu>();
            Assert.IsNotNull(menu);
            menu.Refrescar();
            yield return null;

            var textos = menu.GetComponentsInChildren<Text>(true);
            var creditos = textos.FirstOrDefault(t => t.name == "Creditos");
            Assert.IsNotNull(creditos, "El menú nombra a quienes hicieron el juego.");
            StringAssert.Contains("David Felipe Garzón Barbosa", creditos.text);
            StringAssert.Contains("Juan Diego Romero Jiménez", creditos.text);

            var titulo = textos.First(t => t.name == "Titulo");
            Assert.AreEqual("FRAGMENTUM", titulo.text);
            Assert.AreSame(FabricaUI.FuenteTitulos, titulo.font, "El título, con la letra nueva.");
            Assert.AreSame(FabricaUI.FuenteTitulos, creditos.font);

            StringAssert.Contains("Puntaje: 9 500", textos.First(t => t.name == "Progreso").text);

            var fondo = Object.FindAnyObjectByType<FondoMenu>();
            Assert.IsNotNull(fondo, "El menú ya no tiene un fondo plano.");
            Assert.IsTrue(fondo.Construido);
            var imagen = fondo.transform.Find("Imagen");
            Assert.IsNotNull(imagen);
            Assert.IsNotNull(imagen.GetComponent<Image>().sprite, "El fondo es una imagen del juego.");
            Assert.AreEqual(0, fondo.transform.GetSiblingIndex(), "Va detrás de todo lo demás.");
        }
    }
}
