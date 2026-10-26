using System.Linq;
using Fragmentum.Core.Input;
using Fragmentum.EditorTools;
using Fragmentum.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using TIPOS = Fragmentum.EditorTools.DisenoNivel3.TIPOS_ENEMIGO;

namespace Fragmentum.Tests
{
    /// <summary>
    /// El recorrido de diseño del Nivel 3 (<see cref="DisenoNivel3"/>) se puede cruzar
    /// <b>caminando</b>, sin hacer ruido; la hondonada es de verdad un callejón; cumple los recuentos
    /// del Anexo A; y bajo cada rama por la que se pasa cabe Kael. Lo comprueba sobre los datos;
    /// <see cref="EscenaNivel3Tests"/> comprueba que la escena los sigue.
    /// </summary>
    public class DisenoNivel3Tests
    {
        // Los mismos márgenes que en los otros dos niveles: nunca se pide el límite exacto.
        private const float MargenAlcance = 0.85f;
        private const float MargenAltura = 0.9f;

        private static readonly ParametrosJugador Kael = new ParametrosJugador();

        private static float Gravedad => Mathf.Abs(Physics2D.gravity.y) * Kael.escalaGravedad;
        private static float AlturaSalto => Kael.velocidadSalto * Kael.velocidadSalto / (2f * Gravedad);
        private static float AlturaDobleSalto => AlturaSalto + Kael.velocidadDobleSalto * Kael.velocidadDobleSalto / (2f * Gravedad);
        private static float VelocidadCaminando => Kael.velocidadMaxima * DisenoNivel3.FactorCaminar;

        /// <summary>
        /// Distancia que avanza Kael caminando con el doble salto hasta caer en algo
        /// <paramref name="subida"/> más alto: el salto más largo que se puede dar sin ruido.
        /// </summary>
        private static float AlcanceCaminando(float subida)
        {
            var caida = Gravedad * Kael.multiplicadorGravedadCaida;
            var tiempo = Kael.velocidadSalto / Gravedad + Kael.velocidadDobleSalto / Gravedad +
                         Mathf.Sqrt(2f * Mathf.Max(0f, AlturaDobleSalto - subida) / caida);
            return VelocidadCaminando * tiempo;
        }

        [Test]
        public void CaminandoNoSeHaceRuido_YLasMedidasSonLasDelDocumento()
        {
            var objeto = new GameObject("Medidas");
            try
            {
                var entrada = objeto.AddComponent<FuenteInputJugador>();
                var modo = objeto.AddComponent<PlayerModoMovimiento>();
                Assert.AreEqual(DisenoNivel3.FactorCaminar, new SerializedObject(entrada).FindProperty("factorCaminar").floatValue, 1e-4f,
                    "El diseño camina a lo que camina Kael con Alt.");
                Assert.Less(DisenoNivel3.FactorCaminar, new SerializedObject(modo).FindProperty("umbralSigilo").floatValue,
                    "Caminando, Kael va por debajo del umbral del sigilo: no hace ruido.");
            }
            finally
            {
                Object.DestroyImmediate(objeto);
            }

            Assert.AreEqual(2.1f, VelocidadCaminando, 0.01f, "Caminando, 2,1 u/s.");
            Assert.AreEqual(2.5f, AlcanceCaminando(0f), 0.1f, "Con el doble salto, caminando se avanzan unos 2,5 u.");
        }

        [Test]
        public void CadaPaso_SeDaCaminando()
        {
            foreach (var paso in DisenoNivel3.Pasos)
            {
                var desde = DisenoNivel3.PisadaDe(paso.Desde);
                var hasta = DisenoNivel3.PisadaDe(paso.Hasta);
                var subida = hasta.Y - desde.Y;
                var hueco = Mathf.Max(hasta.Desde - desde.Hasta, desde.Desde - hasta.Hasta, 0f);
                Assert.LessOrEqual(subida, AlturaDobleSalto * MargenAltura, $"{paso}: demasiado alto.");
                Assert.LessOrEqual(hueco, AlcanceCaminando(Mathf.Max(0f, subida)) * MargenAlcance, $"{paso}: no se llega caminando.");
            }
        }

        [Test]
        public void LosPasos_LlevanDelInicioALaArena_SinBajarALaHondonada()
        {
            // Lo que se alcanza saltando desde el inicio, más lo que se alcanza dejándose caer
            // (de una pisada a otra más baja que queda debajo o justo al lado).
            var alcanzadas = new System.Collections.Generic.HashSet<string> { "SueloInicio" };
            bool Anadir(string nombre) => alcanzadas.Add(nombre);
            var cambia = true;
            while (cambia)
            {
                cambia = false;
                foreach (var paso in DisenoNivel3.Pasos.Where(p => alcanzadas.Contains(p.Desde) && p.Hasta != DisenoNivel3.LaTrampa))
                {
                    cambia |= Anadir(paso.Hasta);
                }

                foreach (var desde in alcanzadas.ToList())
                {
                    var arriba = DisenoNivel3.PisadaDe(desde);
                    foreach (var abajo in DisenoNivel3.TodoLoQueSePisa.Where(b => b.Nombre != DisenoNivel3.LaTrampa && b.Arriba < arriba.Y))
                    {
                        var juntos = abajo.Desde <= arriba.Hasta + 0.01f && abajo.Hasta >= arriba.Desde - 0.01f;
                        if (juntos)
                        {
                            cambia |= Anadir(abajo.Nombre);
                        }
                    }
                }
            }

            Assert.IsTrue(alcanzadas.Contains("SueloArena"), "Por las copas se llega a la arena.");
            Assert.IsTrue(alcanzadas.Contains("RamaArenaIzquierda") && alcanzadas.Contains("RamaArenaDerecha"));
        }

        [Test]
        public void LaHondonada_EsUnCallejon_SoloSeSalePorElEscalon()
        {
            var fondo = DisenoNivel3.PisadaDe(DisenoNivel3.LaTrampa);
            var raices = DisenoNivel3.Paredes.Single(p => p.Nombre == "RaicesDelCallejon");
            Assert.AreEqual(fondo.Hasta, raices.Desde, 1e-4f, "Las raíces la cierran por la derecha.");
            Assert.IsTrue(DisenoNivel3.NoAgarra(raices.Nombre), "Y no agarran: el salto de pared no la abre.");
            Assert.Greater(raices.Arriba - fondo.Y, AlturaDobleSalto, "Ni se saltan.");

            // Desde el fondo, ninguna pisada de encima (las copas y el puesto del vigía) se alcanza.
            foreach (var arriba in DisenoNivel3.TodoLoQueSePisa.Where(b => b.Desde >= 61f && b.Hasta <= raices.Hasta && b.Arriba > fondo.Y))
            {
                Assert.Greater(arriba.Arriba - fondo.Y, AlturaDobleSalto, $"{arriba.Nombre} no se alcanza desde el fondo.");
            }

            // Se vuelve a la bifurcación por el escalón.
            CollectionAssert.Contains(DisenoNivel3.Pasos.Select(p => p.ToString()).ToList(), new DisenoNivel3.Paso(DisenoNivel3.LaTrampa, "EscalonDeLaHondonada").ToString());
            CollectionAssert.Contains(DisenoNivel3.Pasos.Select(p => p.ToString()).ToList(), new DisenoNivel3.Paso("EscalonDeLaHondonada", "SueloDelBosque").ToString());
            Assert.IsFalse(DisenoNivel3.NoAgarra("ParedIzquierda"), "Solo las raíces no agarran.");
        }

        [Test]
        public void BajoCadaRama_CabeKael()
        {
            // Bajo una rama se pasa andando: tiene que quedar sitio entre ella y lo que se pisa debajo.
            foreach (var rama in DisenoNivel3.Ramas.Concat(DisenoNivel3.RamasPodridas).Concat(DisenoNivel3.PlataformasArena))
            {
                foreach (var x in new[] { rama.Desde + 0.05f, (rama.Desde + rama.Hasta) * 0.5f, rama.Hasta - 0.05f })
                {
                    var debajo = DisenoNivel3.SueloBajo(x, rama.Abajo - 0.01f);
                    Assert.GreaterOrEqual(rama.Abajo - debajo, DisenoNivel3.HuecoBajoUnaRama, $"{rama.Nombre} en x {x:0.##}: Kael no pasa por debajo.");
                }

                Assert.AreEqual(DisenoNivel3.GrosorRama, rama.Arriba - rama.Abajo, 1e-4f, rama.Nombre);
            }
        }

        [Test]
        public void LasRamasPodridas_CaenSobreLaTrampa_YLaUltimaSoloAsusta()
        {
            var barranco = DisenoNivel3.RamasPodridas.Single(r => r.Nombre == "RamaPodridaDelBarranco");
            Assert.AreEqual(DisenoNivel3.PisadaDe("FondoDelBarranco").Y, DisenoNivel3.SueloBajo((barranco.Desde + barranco.Hasta) * 0.5f, barranco.Abajo), 1e-4f,
                "La del barranco cae al fondo del barranco.");
            var copas = DisenoNivel3.RamasPodridas.Single(r => r.Nombre == "RamaPodridaDeLasCopas");
            Assert.AreEqual(DisenoNivel3.PisadaDe(DisenoNivel3.LaTrampa).Y, DisenoNivel3.SueloBajo((copas.Desde + copas.Hasta) * 0.5f, copas.Abajo), 1e-4f,
                "La de las copas cae a la hondonada: caer es acabar en la trampa.");
            var bajada = DisenoNivel3.RamasPodridas.Single(r => r.Nombre == "RamaPodridaDeLaBajada");
            Assert.AreEqual(0f, DisenoNivel3.SueloBajo((bajada.Desde + bajada.Hasta) * 0.5f, bajada.Abajo), 1e-4f, "La de la bajada, sobre suelo firme.");

            // La del barranco tiene el cepo debajo.
            Assert.IsTrue(DisenoNivel3.Cepos.Any(c => c.x > barranco.Desde && c.x < barranco.Hasta && Mathf.Approximately(c.y, DisenoNivel3.PisadaDe("FondoDelBarranco").Y)));
        }

        [Test]
        public void DelBarrancoYDeLaTrinchera_SeSaleConUnSaltoQueNoDaContraUnaRama()
        {
            var barranco = DisenoNivel3.PisadaDe("FondoDelBarranco");
            Assert.LessOrEqual(DisenoNivel3.PisadaDe("SueloInicio").Y - barranco.Y, AlturaDobleSalto * MargenAltura);
            Assert.Greater(DisenoNivel3.PisadaDe("SueloInicio").Y - barranco.Y, AlturaSalto * MargenAltura, "Del barranco se sale con el doble salto: caer cuesta.");

            // Pegado a cada pared del barranco, Kael (0,8 u de ancho) no tiene una rama encima.
            var ramas = DisenoNivel3.Ramas.Concat(DisenoNivel3.RamasPodridas).ToList();
            Assert.IsFalse(ramas.Any(r => r.Desde < barranco.Desde + 0.8f && r.Hasta > barranco.Desde && r.Arriba <= 0f), "Junto a la pared de la izquierda, sin rama encima.");
            Assert.IsFalse(ramas.Any(r => r.Hasta > barranco.Hasta - 0.8f && r.Desde < barranco.Hasta && r.Arriba <= 0f), "Ni junto a la de la derecha.");

            var trinchera = DisenoNivel3.PisadaDe("FondoDeLaTrinchera");
            Assert.LessOrEqual(DisenoNivel3.PisadaDe("SueloDelBosque").Y - trinchera.Y, AlturaSalto * MargenAltura, "De la trinchera se sale con un salto.");
        }

        [Test]
        public void LaArena_Mide25Por15_YSusRamasSeAlcanzanConElDobleSalto()
        {
            var limites = DisenoNivel3.LimitesArena;
            Assert.AreEqual(25f, limites.y - limites.x, 0.01f, "Como las arenas de los otros dos mundos.");
            Assert.AreEqual(15f, DisenoNivel3.TechoArena - DisenoNivel3.SueloArena, 0.01f);
            Assert.That(DisenoNivel3.Jefe, Is.InRange(limites.x, limites.y));
            Assert.That(DisenoNivel3.Salida, Is.InRange(limites.x, limites.y));
            Assert.That(DisenoNivel3.Entrada, Is.InRange(limites.x, limites.y));
            Assert.AreEqual((limites.x + limites.y) * 0.5f, DisenoNivel3.Jefe, 0.01f, "El Espectro espera en el centro de la sala.");

            foreach (var rama in DisenoNivel3.PlataformasArena)
            {
                var subida = rama.Arriba - DisenoNivel3.SueloArena;
                Assert.Greater(subida, AlturaSalto * MargenAltura, $"{rama.Nombre}: con un salto no.");
                Assert.LessOrEqual(subida, AlturaDobleSalto * MargenAltura, $"{rama.Nombre}: con el doble salto, sí.");
                Assert.That(rama.Desde, Is.InRange(limites.x, limites.y));
                Assert.That(rama.Hasta, Is.InRange(limites.x, limites.y));
            }

            // La puerta de salida mide 3 u: ninguna rama se monta sobre ella. Y las dos, a la misma distancia del jefe.
            Assert.LessOrEqual(DisenoNivel3.PlataformasArena.Max(r => r.Hasta), DisenoNivel3.Salida - 1.5f, "Ninguna rama tapa la puerta de salida.");
            var centros = DisenoNivel3.PlataformasArena.Select(r => (r.Desde + r.Hasta) * 0.5f).OrderBy(c => c).ToList();
            Assert.AreEqual(DisenoNivel3.Jefe - centros[0], centros[1] - DisenoNivel3.Jefe, 1e-4f, "Simétricas respecto al jefe.");
        }

        [Test]
        public void LasSecciones_SonCuatro_YLlevanLaCamaraDeLaParedIzquierdaAlFondoDeLaArena()
        {
            var secciones = DisenoNivel3.Secciones;
            Assert.AreEqual(4, secciones.Length, "Las dos del Anexo A, el claro y la arena.");
            Assert.AreEqual(DisenoNivel3.TextosDeLasSecciones.Length, secciones.Length);
            for (var i = 1; i < secciones.Length; i++)
            {
                Assert.AreEqual(secciones[i - 1].y, secciones[i].x, 1e-4f, "Cada una empieza donde acaba la anterior.");
            }

            var izquierda = DisenoNivel3.Paredes.Single(p => p.Nombre == "ParedIzquierda");
            var fondo = DisenoNivel3.Paredes.Single(p => p.Nombre == "ParedArena");
            Assert.AreEqual(izquierda.Hasta, secciones[0].x - DisenoNivel3.MargenCamara, 1e-4f, "La cámara empieza en la pared de la izquierda.");
            Assert.AreEqual(fondo.Hasta, secciones[3].y + DisenoNivel3.MargenCamara, 1e-4f, "Y llega a la del fondo de la arena.");
            Assert.AreEqual(DisenoNivel3.PuertaDesde, secciones[3].x, 1e-4f, "La última es la arena.");
            Assert.LessOrEqual(DisenoNivel3.AlturaCamara.x, DisenoNivel3.PisadaDe(DisenoNivel3.LaTrampa).Y - 2f, "La cámara baja hasta la hondonada.");
            Assert.GreaterOrEqual(DisenoNivel3.AlturaCamara.y, DisenoNivel3.TechoArena, "Y sube hasta el techo de la arena.");
        }

        [Test]
        public void LosBloques_EmpiezanYAcabanEnUnidadesEnteras()
        {
            foreach (var bloque in DisenoNivel3.Suelos.Concat(DisenoNivel3.Paredes).Concat(DisenoNivel3.Ramas)
                         .Concat(DisenoNivel3.RamasPodridas).Concat(DisenoNivel3.PlataformasArena))
            {
                Assert.AreEqual(Mathf.Round(bloque.Desde), bloque.Desde, 1e-4f, bloque.Nombre);
                Assert.AreEqual(Mathf.Round(bloque.Hasta), bloque.Hasta, 1e-4f, bloque.Nombre);
            }
        }

        [Test]
        public void LosEnemigos_SonLosDelAnexoA_YPatrullanSobreUnaSolaPisada()
        {
            var enemigos = DisenoNivel3.Enemigos;
            Assert.AreEqual(2, enemigos.Count(e => e.Tipo == TIPOS.ESPECTRO && e.X < DisenoNivel3.Secciones[0].y), "Anexo A: 2 Espectros en la sección 1.");
            Assert.AreEqual(4, enemigos.Count(e => e.Tipo == TIPOS.ESPECTRO && e.X >= DisenoNivel3.Secciones[1].x && e.X < DisenoNivel3.Secciones[1].y), "4 en la sección 2.");
            Assert.AreEqual(3, enemigos.Count(e => e.Tipo == TIPOS.CENTINELA_CIEGO && e.X >= DisenoNivel3.Secciones[1].x && e.X < DisenoNivel3.Secciones[1].y), "Y 3 Centinelas Ciegos.");
            Assert.IsFalse(enemigos.Any(e => e.X >= DisenoNivel3.PuertaDesde), "En la arena, solo el jefe.");
            Assert.AreEqual(enemigos.Length, enemigos.Select(e => e.Nombre).Distinct().Count(), "Cada uno con su nombre: el marcador los distingue por él.");

            foreach (var enemigo in enemigos)
            {
                Assert.AreEqual(enemigo.Y, DisenoNivel3.SueloBajo(enemigo.X, enemigo.Y), 1e-4f, $"{enemigo.Nombre} de pie en lo que pisa.");
                var pisada = DisenoNivel3.TodoLoQueSePisa.Single(b => enemigo.X >= b.Desde && enemigo.X < b.Hasta && Mathf.Approximately(b.Arriba, enemigo.Y));
                Assert.That(enemigo.Desde, Is.InRange(pisada.Desde, pisada.Hasta), enemigo.Nombre);
                Assert.That(enemigo.Hasta, Is.InRange(pisada.Desde, pisada.Hasta), enemigo.Nombre);
                Assert.That(enemigo.X, Is.InRange(enemigo.Desde, enemigo.Hasta), enemigo.Nombre);
            }

            // Cada ruta tiene sus Centinelas: la salida de la trinchera, el puesto del vigía y la hondonada.
            Assert.IsTrue(enemigos.Any(e => e.Tipo == TIPOS.CENTINELA_CIEGO && e.Y == 0f && e.X < 58f), "Uno a la salida de la trinchera.");
            Assert.IsTrue(enemigos.Any(e => e.Tipo == TIPOS.CENTINELA_CIEGO && e.Y == DisenoNivel3.PisadaDe("PuestoDelVigia").Y), "Uno en el puesto del vigía.");
            Assert.IsTrue(enemigos.Any(e => e.Tipo == TIPOS.CENTINELA_CIEGO && e.Y == DisenoNivel3.PisadaDe(DisenoNivel3.LaTrampa).Y), "Uno en la hondonada.");
        }

        [Test]
        public void ElClaro_TieneTresArboles_YElAcechadorMerodeaEntreEllos()
        {
            var claro = DisenoNivel3.PisadaDe("Claro");
            var acechador = DisenoNivel3.Enemigos.Single(e => e.Tipo == TIPOS.ACECHADOR);
            Assert.That(acechador.X, Is.InRange(DisenoNivel3.Secciones[2].x, DisenoNivel3.Secciones[2].y), "El mini-jefe, en la sección 3.");
            Assert.AreEqual(claro.Y, acechador.Y, 1e-4f, "En el claro.");
            Assert.AreEqual(new Vector2(claro.Desde, claro.Hasta), DisenoNivel3.LimitesAcechador, "Su territorio es el claro: embistiendo, no sale de él.");

            Assert.AreEqual(3, DisenoNivel3.Arboles.Length, "Tres árboles gruesos.");
            var mitad = DisenoNivel3.TamanoArbol.x * 0.5f;
            var largo = GeneradorEnemigos.TamanoAcechador.x;
            foreach (var x in DisenoNivel3.Arboles)
            {
                Assert.Greater(x - mitad, claro.Desde, $"El árbol de x {x}, dentro del claro.");
                Assert.Less(x + mitad, claro.Hasta, $"El árbol de x {x}, dentro del claro.");
                Assert.AreEqual(claro.Y, DisenoNivel3.SueloBajo(x, claro.Y), 1e-4f, $"El árbol de x {x}, de pie en su suelo.");
                Assert.That(x, Is.InRange(acechador.Desde, acechador.Hasta), "Merodea entre ellos.");
            }

            for (var i = 1; i < DisenoNivel3.Arboles.Length; i++)
            {
                var hueco = DisenoNivel3.Arboles[i] - DisenoNivel3.Arboles[i - 1] - 2f * mitad;
                Assert.Greater(hueco, largo + 1f, "Entre dos árboles caben el Acechador y su carrera.");
            }

            Assert.IsFalse(DisenoNivel3.Arboles.Any(x => Mathf.Abs(x - acechador.X) < mitad + largo * 0.5f), "Empieza entre dos árboles, sin tocarlos.");
            Assert.Less(Mathf.Abs(DisenoNivel3.Antorchas[3].x - DisenoNivel3.Arboles[0]), DisenoNivel3.RadioAntorcha,
                "La antorcha del claro alumbra el primer árbol: deja ver al Acechador antes de entrar.");
        }

        [Test]
        public void LosCheckpoints_EstanEnLaTrinchera_TrasLasCopas_YEnLaAntesala()
        {
            var puntos = DisenoNivel3.Checkpoints;
            Assert.AreEqual(3, puntos.Length);
            Assert.AreEqual(DisenoNivel3.PisadaDe("FondoDeLaTrinchera").Y, puntos[0].y, 1e-4f, "El primero, en el fondo de la trinchera.");
            Assert.That(puntos[0].x, Is.InRange(DisenoNivel3.PisadaDe("FondoDeLaTrinchera").Desde, DisenoNivel3.PisadaDe("FondoDeLaTrinchera").Hasta));
            Assert.That(puntos[1].x, Is.InRange(DisenoNivel3.PisadaDe("PuestoDelVigia").Hasta, DisenoNivel3.Secciones[1].y), "El segundo, tras las copas.");
            Assert.That(puntos[2].x, Is.InRange(DisenoNivel3.PisadaDe("Antesala").Desde, DisenoNivel3.PisadaDe("Antesala").Hasta), "El tercero, en la antesala.");

            foreach (var punto in puntos)
            {
                Assert.AreEqual(punto.y, DisenoNivel3.SueloBajo(punto.x, punto.y), 1e-4f, $"La bandera de x {punto.x} está de pie en su suelo.");
                Assert.AreEqual(punto.y, DisenoNivel3.SueloBajo(punto.x + 0.5f, punto.y), 1e-4f, $"La bandera de x {punto.x} no queda en un borde.");
                Assert.AreEqual(punto.y, DisenoNivel3.SueloBajo(punto.x - 0.5f, punto.y), 1e-4f, $"La bandera de x {punto.x} no queda en un borde.");
                Assert.IsFalse(DisenoNivel3.Enemigos.Any(e => e.Desde - 2f < punto.x && e.Hasta + 2f > punto.x && Mathf.Approximately(e.Y, punto.y)),
                    $"Sin enemigos rondando el checkpoint de x {punto.x}.");
            }
        }

        [Test]
        public void LasAntorchas_YLosCepos_EstanEnElSuelo_YLaPrimeraAntorchaEsElTutorial()
        {
            Assert.AreEqual(4, DisenoNivel3.Antorchas.Length);
            foreach (var antorcha in DisenoNivel3.Antorchas)
            {
                Assert.AreEqual(antorcha.y, DisenoNivel3.SueloBajo(antorcha.x, antorcha.y), 1e-4f, $"La antorcha de x {antorcha.x}, en el suelo.");
            }

            Assert.AreEqual(3, DisenoNivel3.Cepos.Count(c => c.x < DisenoNivel3.Secciones[0].y), "Anexo A: 3 trampas ocultas en la sección 1.");
            Assert.AreEqual(2, DisenoNivel3.Cepos.Count(c => Mathf.Approximately(c.y, DisenoNivel3.PisadaDe(DisenoNivel3.LaTrampa).Y)), "Y 2 en la hondonada.");
            foreach (var cepo in DisenoNivel3.Cepos)
            {
                Assert.AreEqual(cepo.y, DisenoNivel3.SueloBajo(cepo.x, cepo.y), 1e-4f, $"El cepo de x {cepo.x}, en el suelo.");
            }

            // La primera antorcha enseña lo que hace la luz: alumbra el primer cepo y despierta al primer Espectro.
            var primera = DisenoNivel3.Antorchas[0];
            Assert.Less(Vector2.Distance(primera, DisenoNivel3.Cepos[0]), DisenoNivel3.RadioAntorcha);
            var espectro = DisenoNivel3.Enemigos.First(e => e.Tipo == TIPOS.ESPECTRO);
            Assert.Less(Mathf.Abs(espectro.X - primera.x), DisenoNivel3.RadioAntorcha);
            Assert.Greater(Mathf.Abs(espectro.X - DisenoNivel3.Inicio), DisenoNivel3.RadioLuzKael + 2f, "Y Kael no lo ve con su luz al aparecer.");
        }

        [Test]
        public void LaLuzDeCadaTramo_CubreElNivelEntero_YLaArenaEsLoMasOscuro()
        {
            var luces = DisenoNivel3.Luces;
            Assert.AreEqual(DisenoNivel3.Paredes.Single(p => p.Nombre == "ParedIzquierda").Desde, luces.First().Desde, 1e-4f, "Desde la pared de la izquierda…");
            Assert.AreEqual(DisenoNivel3.Paredes.Single(p => p.Nombre == "ParedArena").Hasta, luces.Last().Hasta, 1e-4f, "…hasta el fondo de la arena.");
            for (var i = 1; i < luces.Length; i++)
            {
                Assert.AreEqual(luces[i - 1].Hasta, luces[i].Desde, 1e-4f, "Sin huecos ni solapes entre tramos.");
            }

            var linde = luces[0];
            var bosque = luces[1];
            var claro = luces[2];
            var arena = luces[3];
            Assert.AreEqual(DisenoNivel3.Secciones[1].x, bosque.Desde, 1e-4f, "La oscuridad empieza con el bosque cerrado, antes de la trinchera.");
            Assert.Less(bosque.Desde, DisenoNivel3.PisadaDe("FondoDeLaTrinchera").Desde);
            Assert.Less(bosque.Intensidad, linde.Intensidad * 0.25f, "El bosque cerrado es mucho más oscuro que la linde (Anexo A: «oscuridad completa»).");
            Assert.Less(bosque.Intensidad, claro.Intensidad * 0.25f, "Y que el claro, donde entra la luna.");
            Assert.Less(arena.Intensidad, bosque.Intensidad, "La arena es lo más oscuro (Anexo A: «solo la luz del jugador»).");
            Assert.AreEqual(DisenoNivel3.PuertaDesde, arena.Desde, 1e-4f, "Desde la puerta de la arena.");

            // La antorcha del claro queda a oscuras: por eso sirve para ver al Acechador antes de entrar.
            var delClaro = DisenoNivel3.Antorchas[3];
            Assert.Less(delClaro.x, claro.Desde);
            Assert.Greater(delClaro.x, DisenoNivel3.Secciones[2].x);
        }

        [Test]
        public void LasCintas_MarcanLasCopas_YLaAntorchaDeLaBifurcacionAlumbraLaPrimera()
        {
            var copas = new[] { "RamaDeLaBifurcacion", "RamaDeLasCopas", "PuestoDelVigia", "RamaDeLaBajada" };
            Assert.AreEqual(copas.Length, DisenoNivel3.Cintas.Length, "Una cinta en cada rama sana de la ruta buena.");
            for (var i = 0; i < copas.Length; i++)
            {
                var rama = DisenoNivel3.Ramas.Single(r => r.Nombre == copas[i]);
                var cinta = DisenoNivel3.Cintas[i];
                Assert.That(cinta.x, Is.InRange(rama.Desde, rama.Hasta), $"La cinta {i + 1} está atada a {rama.Nombre}.");
                Assert.AreEqual(rama.Arriba, cinta.y, 1e-4f, $"Y cuelga de lo alto de {rama.Nombre}.");
            }

            Assert.IsFalse(DisenoNivel3.Cintas.Any(c => DisenoNivel3.RamasPodridas.Any(r => c.x >= r.Desde && c.x <= r.Hasta)), "Ninguna en una rama podrida.");

            // La antorcha de la bifurcación enseña la ruta buena: su luz (desde la llama) llega a la primera cinta.
            var bifurcacion = DisenoNivel3.Antorchas[2];
            var llama = bifurcacion + new Vector2(0f, GeneradorMecanicas.AlturaLlama);
            Assert.Less(Vector2.Distance(llama, DisenoNivel3.Cintas[0]), DisenoNivel3.RadioAntorcha);
        }

        [Test]
        public void LasRamasPodridas_MidenLoQueLaDeLasMecanicas_YCaminandoNoDaTiempoACruzarlas()
        {
            foreach (var rama in DisenoNivel3.RamasPodridas)
            {
                Assert.AreEqual(GeneradorMecanicas.TamanoRama.x, rama.Hasta - rama.Desde, 1e-4f, $"{rama.Nombre}: el largo del prefab de la rama podrida.");
                Assert.AreEqual(GeneradorMecanicas.TamanoRama.y, rama.Arriba - rama.Abajo, 1e-4f, rama.Nombre);
            }

            // Caminando se tarda en cruzarlas mucho más de lo que crujen: hay que saltar de ellas en
            // cuanto se pisan (las pruebas de los pasos ya comprueban que la siguiente rama se alcanza).
            var cruzarCaminando = GeneradorMecanicas.TamanoRama.x / VelocidadCaminando;
            Assert.Greater(cruzarCaminando, DisenoNivel3.CrujidoRama * 2f);
            Assert.Greater(DisenoNivel3.VueltaRama, DisenoNivel3.CrujidoRama, "Rota, tarda en volver: caer cuesta.");
        }
    }
}
