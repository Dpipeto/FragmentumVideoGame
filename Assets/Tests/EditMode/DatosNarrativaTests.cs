using System.Linq;
using Fragmentum.Core;
using Fragmentum.Data;
using Fragmentum.EditorTools;
using Fragmentum.Narrative;
using Fragmentum.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Fragmentum.Tests
{
    /// <summary>
    /// La narrativa generada está enlazada a los niveles: cinemática inicial, cierres de cada
    /// mundo, diálogos que nombran la emoción y el registro emocional conectado a la interfaz.
    /// </summary>
    public class DatosNarrativaTests
    {
        private static LevelDatabase BaseDeDatos()
        {
            var bd = AssetDatabase.LoadAssetAtPath<LevelDatabase>(GeneradorProgresion.RutaBaseDeDatos);
            Assert.IsNotNull(bd, "Falta la LevelDatabase: ejecuta Fragmentum → Regenerar todo.");
            return bd;
        }

        [Test]
        public void LaCinematicaInicial_EsLaEntradaDelNivel1_YSoloSeVeUnaVez()
        {
            var inicial = BaseDeDatos().niveles[0].cinematicaEntrada;

            Assert.IsNotNull(inicial, "Plan §3.6: la cinemática inicial es la entrada del Nivel 1.");
            Assert.AreEqual("Inicial", inicial.Id);
            Assert.IsTrue(inicial.soloPrimeraVez);
            Assert.IsTrue(inicial.saltable);
            Assert.IsNotEmpty(inicial.diapositivas);
        }

        [Test]
        public void CadaMundo_TieneCinematicaDeCierre_YDialogosQueNombranSuEmocion()
        {
            foreach (var nivel in BaseDeDatos().niveles)
            {
                Assert.IsNotNull(nivel.cinematicaCierre, $"{nivel.name} sin cinemática de cierre.");
                Assert.IsNotNull(nivel.dialogos, $"{nivel.name} sin diálogos.");

                var emocion = nivel.emocion.nombreCanonico;
                foreach (var momento in new[] { MOMENTOS_DIALOGO.APERTURA, MOMENTOS_DIALOGO.CIERRE })
                {
                    var lineas = nivel.dialogos.Del(momento);
                    Assert.IsNotEmpty(lineas, $"{nivel.name}: falta la línea de {momento}.");
                    Assert.IsTrue(lineas.Any(l => l.texto.Contains(emocion)), $"{nivel.name}: la línea de {momento} debe nombrar «{emocion}» (tarea G).");
                }

                Assert.AreEqual(nivel.jefe != null, nivel.dialogos.Del(MOMENTOS_DIALOGO.PRE_JEFE).Count > 0,
                    $"{nivel.name}: hay línea antes del jefe si y solo si el mundo tiene jefe.");
            }
        }

        [Test]
        public void CadaJefe_SePresentaConUnaCinematicaCortaSobreElJuego()
        {
            foreach (var nivel in BaseDeDatos().niveles)
            {
                var presentacion = nivel.cinematicaJefe;
                if (nivel.jefe == null || nivel.emocion.id == EMOCIONES.INTEGRACION)
                {
                    Assert.IsNull(presentacion, $"{nivel.name}: sin jefe, o el final, que ya se presenta en su entrada.");
                    continue;
                }

                Assert.IsNotNull(presentacion, $"{nivel.name}: falta la presentación del jefe.");
                Assert.IsTrue(presentacion.saltable);
                Assert.LessOrEqual(presentacion.DuracionTotal, 10f, "Es corta.");
                Assert.IsTrue(presentacion.diapositivas.SelectMany(d => d.carteles).Any(c => c.titulo && c.texto == nivel.jefe.nombreCanonico.ToUpperInvariant()),
                    "Presenta al jefe por su nombre.");

                if (presentacion.diapositivas.Any(d => d.imagen != null))
                {
                    // Con sus planos y su guion (la Centinela): opaca, en pixel art y una vez por
                    // partida. Lo demás del guion lo comprueba CinematicaJefe1Tests.
                    Assert.IsFalse(presentacion.sobreElJuego, $"{nivel.name}: con planos, la presentación es opaca.");
                    Assert.IsTrue(presentacion.escalaEntera, $"{nivel.name}: los planos se ven a escala entera.");
                    Assert.IsTrue(presentacion.soloPrimeraVez, $"{nivel.name}: se ve una vez por partida.");
                    continue;
                }

                // Sin planos ni guion todavía: el texto provisional sobre la arena.
                Assert.IsTrue(presentacion.sobreElJuego, $"{nivel.name}: sin planos, se ve la arena detrás.");
                Assert.IsFalse(presentacion.soloPrimeraVez, "Se ve cada vez que se carga el nivel (no al reintentar).");
                Assert.IsTrue(presentacion.diapositivas.Any(d => d.texto != null && d.texto.StartsWith(nivel.jefe.nombreCanonico + ":")),
                    "Y lleva su diálogo antes del combate.");
            }
        }

        // --- El guion (Docs/Diseno/Guion.md) ---

        [Test]
        public void ElGuion_EstaCargado_SinNingunTextoPendiente()
        {
            var textos = AssetDatabase.FindAssets("t:DialogueData", new[] { GeneradorNarrativa.Carpeta })
                .Select(g => AssetDatabase.LoadAssetAtPath<DialogueData>(AssetDatabase.GUIDToAssetPath(g)))
                .SelectMany(d => d.lineas.Select(l => (d.name, l.texto)))
                .Concat(AssetDatabase.FindAssets("t:CutsceneData", new[] { GeneradorNarrativa.Carpeta })
                    .Select(g => AssetDatabase.LoadAssetAtPath<CutsceneData>(AssetDatabase.GUIDToAssetPath(g)))
                    .SelectMany(c => c.diapositivas.SelectMany(d => (d.carteles ?? new System.Collections.Generic.List<Cartel>()).Select(k => k.texto).Append(d.texto)).Select(t => (c.name, t))))
                .ToList();

            Assert.IsNotEmpty(textos);
            foreach (var (asset, texto) in textos)
            {
                Assert.IsFalse(GeneradorNarrativa.EsProvisional(texto), $"{asset}: queda un placeholder («{texto}»).");
            }
        }

        [Test]
        public void CadaMundo_DiceLasLineasDelGuion()
        {
            foreach (var nivel in BaseDeDatos().niveles)
            {
                var guion = GuionMundos.De(nivel.emocion.id);
                Assert.IsNotNull(guion, $"{nivel.name}: el guion no tiene su mundo.");
                Assert.AreEqual(guion.Apertura, nivel.dialogos.Del(MOMENTOS_DIALOGO.APERTURA).Single().texto, nivel.name);
                Assert.AreEqual(guion.Cierre, nivel.dialogos.Del(MOMENTOS_DIALOGO.CIERRE).Single().texto, nivel.name);
                if (nivel.jefe != null)
                {
                    var antes = nivel.dialogos.Del(MOMENTOS_DIALOGO.PRE_JEFE).Single();
                    Assert.AreEqual(guion.AntesDelJefe, antes.texto, nivel.name);
                    Assert.AreEqual(nivel.jefe.nombreCanonico, antes.hablante, "Habla el Arquitecto, no Kael.");
                }

                foreach (var linea in nivel.dialogos.lineas)
                {
                    Assert.LessOrEqual(linea.texto.Length, 120, $"{nivel.name}: «{linea.texto}» no se lee en {linea.duracion} s.");
                }
            }
        }

        [Test]
        public void EnLaTristeza_KaelTieneLaPistaDelTapon_YNombraElAtaqueCargado()
        {
            foreach (var nivel in BaseDeDatos().niveles)
            {
                var guion = GuionMundos.De(nivel.emocion.id);
                var pistas = nivel.dialogos.Del(MOMENTOS_DIALOGO.PISTA);
                if (string.IsNullOrEmpty(guion.Pista))
                {
                    Assert.IsEmpty(pistas, $"{nivel.name}: su mundo no tiene pista.");
                    continue;
                }

                Assert.AreEqual(guion.Pista, pistas.Single().texto, nivel.name);
                Assert.AreEqual("Kael", pistas.Single().hablante, "Se lo dice Kael.");
            }

            // La del mundo 2 nombra la habilidad que da el mundo 1: es lo que rompe el tapón de hielo.
            var cargado = BaseDeDatos().niveles[0].habilidadOtorgada.nombre;
            StringAssert.Contains(cargado, GuionMundos.De(EMOCIONES.TRISTEZA).Pista);
            Assert.IsTrue(string.IsNullOrEmpty(GuionMundos.De(EMOCIONES.IRA).Pista), "En la Ira todavía no la tiene.");
        }

        [Test]
        public void LasCinematicasDeCierre_SonLasDelGuion_YNombranLaHabilidadOElFragmento()
        {
            foreach (var nivel in BaseDeDatos().niveles.Where(n => n.emocion.id != EMOCIONES.INTEGRACION))
            {
                var guion = GuionMundos.De(nivel.emocion.id);
                CollectionAssert.AreEqual(guion.CinematicaCierre, nivel.cinematicaCierre.diapositivas.Select(d => d.texto).ToArray(), nivel.name);
            }

            var final = BaseDeDatos().niveles[5];
            CollectionAssert.AreEqual(GuionMundos.EntradaFinal, final.cinematicaEntrada.diapositivas.Select(d => d.texto).ToArray());
            CollectionAssert.AreEqual(GuionMundos.Final, final.cinematicaCierre.diapositivas.Select(d => d.texto).ToArray());
            Assert.Greater(final.cinematicaCierre.diapositivas.Last().duracion, final.cinematicaCierre.diapositivas[0].duracion,
                "La pregunta final se queda más tiempo.");
        }

        [Test]
        public void LasPresentacionesSinPlanos_LlevanLaLineaDelJefeYSuMundo()
        {
            foreach (var nivel in BaseDeDatos().niveles.Where(n => n.cinematicaJefe != null && n.cinematicaJefe.diapositivas.All(d => d.imagen == null)))
            {
                var guion = GuionMundos.De(nivel.emocion.id);
                var textos = nivel.cinematicaJefe.diapositivas.Select(d => d.texto).ToList();
                CollectionAssert.Contains(textos, $"{nivel.jefe.nombreCanonico}: {guion.AntesDelJefe}", nivel.name);
                Assert.IsTrue(nivel.cinematicaJefe.diapositivas.SelectMany(d => d.carteles).Any(c => c.texto == nivel.nombreMundo),
                    $"{nivel.name}: bajo el nombre del jefe, su mundo.");
                Assert.IsTrue(nivel.cinematicaJefe.llevaDialogoDelJefe, $"{nivel.name}: ya dice la línea del jefe; el HUD no la repite.");
            }
        }

        [Test]
        public void LaPresentacionDeLaIra_NoDiceLaLineaDelJefe_AsiQueLaDiceElHud()
        {
            // Su texto es de Kael («No recuerdo esta sala…»): la línea de la Centinela va en el HUD
            // al empezar el combate, también en partida nueva, cuando la presentación se ve.
            var ira = BaseDeDatos().niveles.Single(n => n.emocion != null && n.emocion.id == EMOCIONES.IRA);
            Assert.IsNotNull(ira.cinematicaJefe);
            Assert.IsFalse(ira.cinematicaJefe.llevaDialogoDelJefe);
            Assert.IsTrue(ira.dialogos.Del(MOMENTOS_DIALOGO.PRE_JEFE).Any(), "La Centinela tiene su línea antes del combate.");
        }

        [Test]
        public void ElRegistroEmocional_NoPideCodigoDeParticipante()
        {
            Assert.IsFalse(GeneradorNarrativa.CargarConfiguracionRegistro().pedirParticipante, "Decisión del diseño (PENDIENTES N12).");
        }

        [Test]
        public void InteractuarEsPulsar_NoMantener()
        {
            var acciones = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>("Assets/Settings/InputSystem_Actions.inputactions");
            Assert.IsNotNull(acciones);
            var interactuar = acciones.FindAction("Player/Interact", true);
            Assert.IsEmpty(interactuar.interactions, "Decisión del diseño (PENDIENTES N4): E se pulsa, no se mantiene.");
        }

        [Test]
        public void ElFinal_TerminaConLaPreguntaDelAnexo()
        {
            var final = BaseDeDatos().niveles[5];

            Assert.IsNotNull(final.cinematicaEntrada, "Anexo A: el final empieza con una cinemática de confrontación.");
            Assert.IsFalse(final.cinematicaEntrada.soloPrimeraVez);
            Assert.AreEqual("¿Eres un error… o una nueva identidad?", final.cinematicaCierre.diapositivas.Last().texto);
        }

        [Test]
        public void LosSistemas_TienenDialogosYElRegistroConfigurado()
        {
            var sistemas = Resources.Load<GameObject>(Bootstrap.RutaRecurso);

            Assert.IsNotNull(sistemas.GetComponent<DialogueManager>());
            var interfaz = sistemas.GetComponent<UIManager>();
            var configuracion = new SerializedObject(interfaz).FindProperty("registro").objectReferenceValue as ConfiguracionRegistro;
            Assert.IsNotNull(configuracion, "El fin de nivel usa la configuración del registro emocional.");
            Assert.IsTrue(configuracion.activo);
            StringAssert.Contains("{emocion}", configuracion.pregunta);
        }

        [Test]
        public void ElMenuPrincipal_TieneElBotonYoReconstruido()
        {
            var escena = EditorSceneManager.OpenScene(GeneradorProgresion.RutaEscenaMenu, OpenSceneMode.Additive);
            try
            {
                var menu = escena.GetRootGameObjects().Select(r => r.GetComponentInChildren<MainMenu>(true)).FirstOrDefault(m => m != null);
                Assert.IsNotNull(menu);
                Assert.IsNotNull(new SerializedObject(menu).FindProperty("botonYoReconstruido").objectReferenceValue);
            }
            finally
            {
                EditorSceneManager.CloseScene(escena, true);
            }
        }
    }
}
