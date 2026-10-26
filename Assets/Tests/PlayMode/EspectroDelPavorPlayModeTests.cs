using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fragmentum.Bosses;
using Fragmentum.Bosses.Patrones;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using Fragmentum.Data;
using Fragmentum.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Fragmentum.Tests.PlayMode
{
    /// <summary>
    /// El Espectro del Pavor (fase D del Mundo 3) con su prefab, el jugador real y física real, en
    /// una arena de 25 u como la suya:
    ///
    /// - Antes del combate se ve; al empezar se desvanece, y los golpes lo atraviesan.
    /// - El tajo aparece a la espalda de Kael (delante, si Kael está contra una pared), avisa 0,6 s,
    ///   quita 2 y deja una ventana de 1,5 s en la que los golpes valen el doble.
    /// - La embestida cruza la arena desde la pared más lejana a Kael, a su altura (por su rama, si
    ///   está en una; a ras de suelo, si está debajo), y al otro lado se ve 1 s.
    /// - Las hojas brillan en cuatro o cinco franjas, la primera bajo Kael, y después quitan 1.
    /// - Las sombras: tres figuras, solo la verdadera con luciérnaga dorada; golpear a una sombra la
    ///   deshace y encoge la luz de Kael 3 s; golpear a la verdadera las deshace a todas; lanzan su
    ///   tajo una tras otra y jadean juntas.
    /// - El grito de la fase 2 hace parpadear la luz de Kael, y al reiniciarse vuelve a esperar visible.
    /// </summary>
    public class EspectroDelPavorPlayModeTests : PruebaPlayModeBase
    {
        private const string RutaPrefab = "Assets/Prefabs/Jefes/EspectroDelPavor.prefab";
        private const string CarpetaPatrones = "Assets/Data/Jefes/Patrones";
        private static readonly Vector2 LimitesArena = new Vector2(-12.5f, 12.5f);

        // Una rama como las de su arena: a 3,5 u, de media unidad de grueso.
        private const float Rama = 3.5f;
        private const float GrosorRama = 0.5f;

        [UnityTearDown]
        public IEnumerator BorrarZonas()
        {
            foreach (var zona in Object.FindObjectsByType<ZonaDeDanio>(FindObjectsInactive.Include))
            {
                Object.Destroy(zona.gameObject);
            }

            foreach (var sombra in Object.FindObjectsByType<SombraDelPavor>(FindObjectsInactive.Include))
            {
                Object.Destroy(sombra.gameObject);
            }

            yield return null;
        }

        private EspectroDelPavor CrearPavor(float x = 0f)
        {
            Suelo(-20f, 20f);
            var objeto = Object.Instantiate(CargarPrefab(RutaPrefab), new Vector3(x, 0.05f, 0f), Quaternion.identity);
            Creados.Add(objeto);
            return objeto.GetComponent<EspectroDelPavor>();
        }

        // Una copia de sus datos con una sola fase de esos patrones, para aislar uno.
        private void Solo(EspectroDelPavor pavor, params string[] patrones)
        {
            var datos = Object.Instantiate(pavor.Datos);
            Creados.Add(datos);
            datos.fases = new List<FaseJefe>
            {
                new FaseJefe
                {
                    nombre = "Prueba",
                    umbralVida = 1f,
                    pausaEntrePatrones = 0.5f,
                    patrones = patrones.Select(p => CargarAsset<PatronAtaque>($"{CarpetaPatrones}/{p}.asset")).ToList(),
                },
            };
            pavor.FijarDatos(datos);
        }

        private static List<ZonaDeDanio> Zonas(string nombre) =>
            Object.FindObjectsByType<ZonaDeDanio>(FindObjectsInactive.Exclude).Where(z => z.name.EndsWith(nombre)).ToList();

        // --- Invisible ---

        [UnityTest]
        public IEnumerator AntesDelCombateSeVe_AlEmpezarSeDesvanece_YLosGolpesLoAtraviesan()
        {
            var pavor = CrearPavor();
            yield return CrearJugadorEnSuelo(new Vector2(-8f, 0f));
            yield return null;
            Assert.IsTrue(pavor.Visible, "Antes del combate espera visible: su presentación.");
            Assert.AreEqual(1f, pavor.Opacidad, 0.01f);

            pavor.Activar(Jugador.transform, LimitesArena);
            yield return new WaitForSeconds(0.5f);
            Assert.IsFalse(pavor.Visible);
            Assert.AreEqual(0f, pavor.Opacidad, 0.01f, "Al empezar el combate se desvanece.");

            var vida = pavor.Vida.Vida;
            Assert.IsFalse(pavor.Vida.RecibirGolpe(new Golpe(1, PiesJugador)), "Invisible, el golpe lo atraviesa.");
            Assert.AreEqual(vida, pavor.Vida.Vida);
        }

        // --- Tajo ---

        [UnityTest]
        public IEnumerator ElTajo_ApareceALaEspaldaDeKael_AvisaSeisDecimas_QuitaDos_YJadeaUnoYMedio()
        {
            var pavor = CrearPavor(8f);
            Solo(pavor, "Tajo");
            yield return CrearJugadorEnSuelo(new Vector2(0f, 0f));
            Assert.AreEqual(1, Jugador.Mirando, "Kael mira a la derecha: su espalda, a la izquierda.");
            var vidaKael = Jugador.Vida.Vida;
            pavor.Activar(Jugador.transform, LimitesArena);

            yield return EsperarHasta(() => pavor.Destellando, 2.5f, "que aparezca y destelle");
            Assert.AreEqual(PiesJugador.x - 2.5f, pavor.Centro.x, 0.15f, "A 2,5 u, a la espalda de Kael.");
            Assert.IsTrue(pavor.Tangible, "Mientras avisa, se le ve y se le puede golpear.");
            var aviso = Time.time;
            yield return EsperarHasta(() => Jugador.Vida.Vida < vidaKael, 1.5f, "que el tajo alcance a Kael, que no se movió");
            Assert.AreEqual(0.6f, Time.time - aviso, 0.15f, "Tras 0,6 s de aviso.");
            Assert.AreEqual(vidaKael - 2, Jugador.Vida.Vida, "El tajo quita 2.");

            yield return EsperarHasta(() => pavor.EsVulnerable, 0.6f, "que se quede jadeando");
            var vida = pavor.Vida.Vida;
            Assert.IsTrue(pavor.Vida.RecibirGolpe(new Golpe(1, PiesJugador)));
            Assert.AreEqual(vida - 2, pavor.Vida.Vida, "Jadeando, los golpes valen el doble.");
            var jadeo = Time.time;
            yield return EsperarHasta(() => !pavor.EsVulnerable, 2f, "que acabe de jadear");
            Assert.AreEqual(1.5f, Time.time - jadeo, 0.2f, "Jadea 1,5 s.");
            Assert.IsFalse(pavor.Visible, "Y vuelve a no verse.");
        }

        [UnityTest]
        public IEnumerator ContraUnaPared_ElTajoApareceDelanteDeKael()
        {
            var pavor = CrearPavor(8f);
            Solo(pavor, "Tajo");
            yield return CrearJugadorEnSuelo(new Vector2(-11.8f, 0f));
            pavor.Activar(Jugador.transform, LimitesArena);

            yield return EsperarHasta(() => pavor.Destellando, 2.5f, "que aparezca y destelle");
            Assert.AreEqual(PiesJugador.x + 2.5f, pavor.Centro.x, 0.15f, "A su espalda no cabe: delante.");
        }

        // --- Embestida ---

        [UnityTest]
        public IEnumerator LaEmbestida_CruzaDesdeLaParedMasLejana_QuitaUno_YAlOtroLadoSeVeUnSegundo()
        {
            var pavor = CrearPavor();
            Solo(pavor, "EmbestidaSombra");
            yield return CrearJugadorEnSuelo(new Vector2(4f, 0f));
            var vidaKael = Jugador.Vida.Vida;
            pavor.Activar(Jugador.transform, LimitesArena);

            yield return EsperarHasta(() => pavor.Destellando, 2.5f, "que aparezca junto a una pared");
            Assert.Less(pavor.Centro.x, LimitesArena.x + 2f, "La pared más lejana a Kael: la de la izquierda.");
            yield return EsperarHasta(() => Jugador.Vida.Vida < vidaKael, 3f, "que lo arrolle");
            Assert.AreEqual(vidaKael - 1, Jugador.Vida.Vida, "Quita 1.");

            yield return EsperarHasta(() => !pavor.Cruzando, 3f, "que llegue al otro lado");
            Assert.Greater(pavor.Centro.x, LimitesArena.y - 2f, "Hasta la pared de enfrente.");
            Assert.IsTrue(pavor.Visible, "Al otro lado se queda a la vista…");
            yield return new WaitForSeconds(1.2f);
            Assert.IsFalse(pavor.Visible, "…1 s.");
        }

        [UnityTest]
        public IEnumerator ConKaelEnUnaRama_LaEmbestidaCruzaPorLaRama_YLeAlcanza()
        {
            var pavor = CrearPavor();
            Solo(pavor, "EmbestidaSombra");
            Bloque("Rama", 3f, 7f, Rama - GrosorRama, Rama, Capas.Suelo);
            yield return CrearJugadorEnSuelo(new Vector2(5f, Rama));
            var vidaKael = Jugador.Vida.Vida;
            pavor.Activar(Jugador.transform, LimitesArena);

            yield return EsperarHasta(() => pavor.Destellando, 2.5f, "que aparezca junto a una pared");
            Assert.Less(pavor.Centro.x, LimitesArena.x + 2f, "La pared más lejana a Kael: la de la izquierda.");
            Assert.AreEqual(Rama, pavor.Pies.y, 0.05f, "A la altura de Kael: con los pies a la de su rama, en el aire.");

            yield return EsperarHasta(() => Zonas("Embestida").Count > 0, 1f, "que embista");
            Assert.AreEqual(Rama + 0.8f, Zonas("Embestida")[0].transform.position.y, 0.05f, "La embestida va por la rama.");
            yield return EsperarHasta(() => Jugador.Vida.Vida < vidaKael, 2f, "que lo arrolle en su rama");
            Assert.AreEqual(vidaKael - 1, Jugador.Vida.Vida, "Quita 1.");
            Assert.AreEqual(Rama, pavor.Pies.y, 0.05f, "Cruza sin bajar.");

            pavor.Vida.Matar();
            yield return null;
            Assert.IsTrue(pavor.Derrotado);
            Assert.AreEqual(pavor.Suelo, pavor.Pies.y, 0.05f, "Si cae en el aire, cae al suelo de su arena: allí deja su fragmento.");
        }

        [UnityTest]
        public IEnumerator ConKaelBajoUnaRama_LaEmbestidaVaARasDeSuelo()
        {
            var pavor = CrearPavor();
            Solo(pavor, "EmbestidaSombra");
            Bloque("Rama", 3f, 7f, Rama - GrosorRama, Rama, Capas.Suelo);
            yield return CrearJugadorEnSuelo(new Vector2(5f, 0f));
            pavor.Activar(Jugador.transform, LimitesArena);

            yield return EsperarHasta(() => pavor.Destellando, 2.5f, "que aparezca junto a una pared");
            Assert.AreEqual(pavor.Suelo, pavor.Pies.y, 0.05f, "Kael pisa el suelo, aunque tenga una rama encima: a ras de suelo.");
        }

        // --- Hojas ---

        [UnityTest]
        public IEnumerator LasHojas_BrillanEnCuatroOCincoFranjas_LaPrimeraBajoKael_YDespuesQuitanUno()
        {
            var pavor = CrearPavor();
            Solo(pavor, "Hojas");
            yield return CrearJugadorEnSuelo(new Vector2(-3f, 0f));
            var vidaKael = Jugador.Vida.Vida;
            pavor.Activar(Jugador.transform, LimitesArena);

            yield return EsperarHasta(() => Zonas("Hojas").Count > 0, 2.5f, "que el suelo brille");
            var franjas = Zonas("Hojas");
            Assert.That(franjas.Count, Is.InRange(4, 5), "Cuatro o cinco franjas.");
            Assert.IsTrue(franjas.Any(z => Mathf.Abs(z.transform.position.x - PiesJugador.x) < 0.2f), "Una, bajo Kael.");
            Assert.IsTrue(franjas.All(z => !z.Activa), "Mientras brillan, todavía no hacen daño.");
            Assert.AreEqual(vidaKael, Jugador.Vida.Vida);

            yield return EsperarHasta(() => Jugador.Vida.Vida < vidaKael, 1.5f, "que caigan las hojas");
            Assert.AreEqual(vidaKael - 1, Jugador.Vida.Vida, "Quitan 1.");
        }

        // --- Sombras ---

        [UnityTest]
        public IEnumerator LasSombras_SoloLaVerdaderaLlevaLaLuciernagaDorada_YGolpearUnaSombraEncogeLaLuzDeKael()
        {
            var pavor = CrearPavor();
            Solo(pavor, "Sombras");
            yield return CrearJugadorEnSuelo(new Vector2(0f, 0f));
            var luz = Jugador.GetComponent<LuzJugador>();
            luz.Encender(3f);
            pavor.Activar(Jugador.transform, LimitesArena);

            yield return EsperarHasta(() => pavor.Sombras.Count == 2, 2.5f, "que aparezcan las tres figuras");
            Assert.IsTrue(pavor.ConLuciernaga, "La verdadera lleva su luciérnaga dorada.");
            Assert.IsTrue(pavor.Visible);
            var lugares = pavor.Sombras.Select(s => s.Centro.x).Append(pavor.Centro.x).OrderBy(x => x).ToList();
            Assert.AreEqual(LimitesArena.x + 3f, lugares[0], 0.2f, "A la izquierda…");
            Assert.AreEqual(0f, lugares[1], 0.2f, "…en el centro…");
            Assert.AreEqual(LimitesArena.y - 3f, lugares[2], 0.2f, "…y a la derecha.");

            var sombra = pavor.Sombras[0];
            var golpeadas = 0;
            pavor.OnSombraGolpeada += _ => golpeadas++;
            Assert.IsTrue(sombra.RecibirGolpe(new Golpe(1, PiesJugador)));
            Assert.IsTrue(sombra.Deshecha, "Golpear a una sombra la deshace.");
            Assert.AreEqual(1, golpeadas);
            Assert.AreEqual(1.5f, luz.Radio, 0.01f, "Y encoge la luz de Kael a la mitad.");
            Assert.AreEqual(1, pavor.Sombras.Count);

            var vida = pavor.Vida.Vida;
            Assert.IsTrue(pavor.Vida.RecibirGolpe(new Golpe(1, PiesJugador)), "La verdadera se puede golpear.");
            Assert.Less(pavor.Vida.Vida, vida, "Golpearla cuenta…");
            Assert.AreEqual(0, pavor.Sombras.Count, "…y deshace a las sombras.");

            yield return new WaitForSeconds(3.1f);
            Assert.AreEqual(3f, luz.Radio, 0.01f, "A los 3 s, la luz de Kael vuelve a su radio.");
        }

        [UnityTest]
        public IEnumerator LasSombras_LanzanSuTajoUnaTrasOtra_YDespuesJadeanLasTres()
        {
            var pavor = CrearPavor();
            Solo(pavor, "Sombras");
            yield return CrearJugadorEnSuelo(new Vector2(-20f, 0f));
            var tajos = new HashSet<ZonaDeDanio>();
            pavor.Activar(Jugador.transform, LimitesArena);

            yield return EsperarHasta(() => pavor.Sombras.Count == 2, 2.5f, "que aparezcan las tres figuras");
            var fin = Time.time + 6f;
            while (Time.time < fin && !pavor.EsVulnerable)
            {
                foreach (var zona in Zonas("Tajo"))
                {
                    tajos.Add(zona);
                }

                yield return null;
            }

            Assert.AreEqual(3, tajos.Count, "Tres tajos, uno por figura.");
            Assert.IsTrue(pavor.EsVulnerable, "Después jadean…");
            Assert.AreEqual(2, pavor.Sombras.Count, "…las tres.");
            yield return EsperarHasta(() => !pavor.EsVulnerable, 2f, "que acaben de jadear");
            yield return null;
            Assert.AreEqual(0, pavor.Sombras.Count, "Al acabar, las sombras se deshacen.");
            Assert.IsFalse(pavor.ConLuciernaga);
        }

        // --- Fase 2 y reinicio ---

        [UnityTest]
        public IEnumerator AlEmpezarLaFase2_GritaYLaLuzDeKaelParpadea()
        {
            var pavor = CrearPavor();
            yield return CrearJugadorEnSuelo(new Vector2(-8f, 0f));
            var luz = Jugador.GetComponent<LuzJugador>();
            luz.Encender(3f);
            pavor.Activar(Jugador.transform, LimitesArena);
            yield return null;

            pavor.Vida.PerderVida(pavor.Vida.VidaMaxima / 2);
            yield return null;
            yield return null;
            Assert.AreEqual(2, pavor.Fase);
            Assert.IsTrue(luz.Parpadeando, "Su grito: la luz de Kael parpadea…");
            yield return new WaitForSeconds(pavor.Grito + 0.1f);
            Assert.IsFalse(luz.Parpadeando, "…0,9 s.");
            Assert.AreEqual(luz.Intensidad, luz.Luz.intensity, 1e-4f, "Y vuelve a su intensidad.");
        }

        [UnityTest]
        public IEnumerator AlReiniciarse_VuelveAEsperarVisible_SinSombras_YConLaLuzDeKaelEntera()
        {
            var pavor = CrearPavor();
            Solo(pavor, "Sombras");
            yield return CrearJugadorEnSuelo(new Vector2(0f, 0f));
            var luz = Jugador.GetComponent<LuzJugador>();
            luz.Encender(3f);
            pavor.Activar(Jugador.transform, LimitesArena);
            yield return EsperarHasta(() => pavor.Sombras.Count == 2, 2.5f, "que aparezcan las tres figuras");
            pavor.Sombras[0].RecibirGolpe(new Golpe(1, PiesJugador));
            Assert.AreEqual(1.5f, luz.Radio, 0.01f);

            pavor.Reiniciar();
            yield return null;
            Assert.IsFalse(pavor.EnCombate);
            Assert.AreEqual(0, pavor.Sombras.Count, "Sin sombras.");
            Assert.IsTrue(pavor.Visible, "Vuelve a esperar visible.");
            Assert.AreEqual(3f, luz.Radio, 0.01f, "Y la luz de Kael, entera.");
            Assert.AreEqual(0f, pavor.transform.position.x, 0.01f, "En su sitio.");
        }
    }
}
