using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Fragmentum.Core;
using Fragmentum.Narrative;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UI;

namespace Fragmentum.UI
{
    /// <summary>
    /// Reproductor de cinemáticas (<c>CutscenePlayer</c> del plan): muestra un
    /// <see cref="CutsceneData"/> a pantalla completa mientras la FSM global está en CUTSCENE. La
    /// FSM es la que bloquea el input del jugador y detiene el juego mientras dura.
    ///
    /// - Qué se ve en cada instante lo decide <see cref="LineaTiempoCinematica"/>; esta pantalla
    ///   solo lo aplica: fundidos cruzados entre imágenes o beats que suben desde negro y bajan a
    ///   negro con sus propios tiempos (<see cref="CutsceneData.montaje"/>).
    /// - <b>Pixel art</b> (<see cref="CutsceneData.escalaEntera"/>): las imágenes, el encuadre,
    ///   las brasas, los temblores y el flash se dibujan con la cámara de cinemáticas
    ///   (<see cref="CamaraCinematica"/>), en la rejilla de 240 × 135 y ampliados por un entero sin
    ///   interpolar. Siempre opaca. El texto va en la misma rejilla si la cinemática tiene fuente
    ///   bitmap; si no, en la interfaz, como en la cinemática inicial, pero a la escala de la
    ///   imagen y anclado en su píxel del plano.
    /// - <b>Imágenes pintadas</b> (el resto): en la interfaz, con su proporción sobre fondo negro, y
    ///   los textos por tercios de la <b>imagen</b>, no de la pantalla.
    /// - <b>Sonido:</b> los cues de cada diapositiva suenan en su instante y se cortan en seco
    ///   donde diga el guion. Si falta el clip de uno, avisa por consola con su nombre y sigue sin él.
    /// - <b>El flash</b> del final de un beat dura los fotogramas que diga, contados uno a uno: a
    ///   30 fps no se alarga al doble.
    /// - Pasados los primeros segundos (<see cref="CutsceneData.bloqueoSalto"/>), cualquier tecla,
    ///   botón o clic la salta. Si termina con flash, va directo a él. Un aviso discreto lo dice.
    /// - <b>Funciona sin arte:</b> una diapositiva sin imagen se ve en negro con su texto, y sin
    ///   texto dice qué falta (criterio de aceptación de la Fase 8). En un montaje por beats, un
    ///   beat sin imagen es negro a propósito.
    /// - <b>Sobre el juego</b> (<see cref="CutsceneData.sobreElJuego"/>): en vez de negro, un velo
    ///   y dos franjas de cine sobre la escena congelada.
    /// - Cuenta el tiempo sin escala, porque la cinemática para el juego.
    /// - Al terminar se lo dice a la FSM global, que decide a dónde se va.
    /// </summary>
    public class PantallaCinematica : MonoBehaviour
    {
        /// <summary>
        /// El volumen de la música de una cinemática: el mismo que la del nivel (<c>LevelManager</c>), porque
        /// las dos salen igual de fuertes de <c>Herramientas/exportar_musica.py</c>. Encima, el de la música
        /// de las opciones (<see cref="AjustesAudio"/>).
        /// </summary>
        public const float VolumenMusica = 0.6f;

        private const int TamanoTexto = 40;
        private const int TamanoTitulo = 76;

        // Alto de cada franja de cine, en fracción de la pantalla, cuando va sobre el juego.
        private const float AltoFranja = 0.11f;
        private static readonly Color ColorTexto = new Color(0.94f, 0.93f, 0.85f, 1f);

        private static readonly HashSet<string> AvisadasSinArte = new HashSet<string>();
        private static readonly HashSet<string> AvisadasSinCamara = new HashSet<string>();
        private static readonly List<LineaTiempoCinematica.TextoVisible> SinTextos = new List<LineaTiempoCinematica.TextoVisible>();

        private sealed class Ranura
        {
            public Text Texto;
            public CanvasGroup Grupo;
        }

        private sealed class CueSonando
        {
            public AudioSource Fuente;
            public float Fin;
        }

        private AspectRatioFitter _marco;
        private Canvas _lienzo;
        private Image _fondo;
        private Image _franjaArriba;
        private Image _franjaAbajo;
        private Image _abajo;
        private Image _encima;
        private CanvasGroup _grupoAbajo;
        private CanvasGroup _grupoEncima;
        private readonly Dictionary<POSICIONES_CARTEL, Ranura> _ranuras = new Dictionary<POSICIONES_CARTEL, Ranura>();
        private readonly List<(Text texto, CanvasGroup grupo, Shadow sombra)> _textosSobreRejilla = new List<(Text, CanvasGroup, Shadow)>();
        private Text _aviso;
        private CanvasGroup _grupoAviso;
        private AudioSource _audio;
        private AudioSource _musica;
        private readonly List<AudioSource> _fuentesCue = new List<AudioSource>();
        private readonly List<CueSonando> _cuesSonando = new List<CueSonando>();
        private bool _construida;
        private float _inicio;
        private float _tAnterior;
        private int _indiceAudio = -1;
        private int _frameSaltoPedido = -1;
        private int _flashObjetivo;
        private int _flashVistos = -1;
        private bool _terminarTrasFlash;
        private IDisposable _escuchaBotones;

        public CutsceneData Actual { get; private set; }

        public LineaTiempoCinematica Linea { get; private set; }

        public bool Reproduciendo { get; private set; }

        /// <summary>Por donde suena la música de la cinemática (<c>CutsceneData.musica</c>).</summary>
        public AudioSource Musica => _musica;

        /// <summary>La cámara de las cinemáticas en pixel art. La pone el UIManager.</summary>
        public CamaraCinematica Camara { get; set; }

        /// <summary>¿La cinemática en curso se dibuja con la cámara de cinemáticas, en su rejilla?</summary>
        public bool EnRejilla { get; private set; }

        /// <summary>La rejilla de la cinemática en curso: el tamaño de sus planos.</summary>
        public Vector2Int Rejilla => Actual != null ? Actual.Rejilla : CamaraCinematica.RejillaPorDefecto;

        /// <summary>Segundos desde que empezó la cinemática en curso.</summary>
        public float Tiempo => Reproduciendo ? Time.unscaledTime - _inicio : 0f;

        /// <summary>Diapositiva en curso.</summary>
        public int Indice => Linea != null ? Mathf.Max(0, Linea.IndiceEn(Tiempo)) : 0;

        /// <summary>¿Está en el flash final, ya sin avanzar la línea de tiempo?</summary>
        public bool EnFlashFinal => Reproduciendo && _terminarTrasFlash;

        /// <summary>Los textos que se ven ahora, uno por línea.</summary>
        public string TextoActual
        {
            get
            {
                if (EnRejilla && Camara != null && Actual != null && Actual.fuente != null)
                {
                    return Camara.TextoVisible;
                }

                var texto = new StringBuilder();
                foreach (var ranura in _ranuras.Values)
                {
                    if (ranura.Grupo.alpha > 0f && !string.IsNullOrEmpty(ranura.Texto.text))
                    {
                        texto.AppendLine(ranura.Texto.text);
                    }
                }

                foreach (var (textoUi, grupo, _) in _textosSobreRejilla)
                {
                    if (grupo.alpha > 0f && !string.IsNullOrEmpty(textoUi.text))
                    {
                        texto.AppendLine(textoUi.text);
                    }
                }

                return texto.ToString().TrimEnd();
            }
        }

        /// <summary>Los textos de la interfaz sobre la rejilla (pixel art sin fuente bitmap), para las pruebas.</summary>
        public IReadOnlyList<Text> TextosSobreLaRejilla => _textosSobreRejilla.ConvertAll(t => t.texto);

        /// <summary>Opacidad actual del aviso «pulsa cualquier tecla».</summary>
        public float AlfaAviso => _grupoAviso != null ? _grupoAviso.alpha : 0f;

        public bool Visible => _construida && gameObject.activeSelf;

        /// <summary>
        /// En pixel art, por cuánto se multiplica cada píxel del plano en pantalla; 0 si la
        /// cinemática en curso escala libre.
        /// </summary>
        public int EscalaEntera => EnRejilla && Camara != null ? Camara.Escala : 0;

        /// <summary>El rectángulo de la imagen, en píxeles de pantalla.</summary>
        public Rect RectImagenEnPantalla
        {
            get
            {
                if (EnRejilla)
                {
                    var rejilla = CamaraCinematica.RectEnPantalla(Screen.width, Screen.height, Rejilla);
                    return new Rect(rejilla.x, rejilla.y, rejilla.width, rejilla.height);
                }

                var esquinas = new Vector3[4];
                ((RectTransform)_marco.transform).GetWorldCorners(esquinas);
                var lienzo = Lienzo;
                var camara = lienzo != null && lienzo.renderMode != RenderMode.ScreenSpaceOverlay ? lienzo.worldCamera : null;
                var abajo = RectTransformUtility.WorldToScreenPoint(camara, esquinas[0]);
                var arriba = RectTransformUtility.WorldToScreenPoint(camara, esquinas[2]);
                return Rect.MinMaxRect(abajo.x, abajo.y, arriba.x, arriba.y);
            }
        }

        private Canvas Lienzo
        {
            get
            {
                if (_lienzo == null)
                {
                    var padre = GetComponentInParent<Canvas>(true);
                    _lienzo = padre != null ? padre.rootCanvas : null;
                }

                return _lienzo;
            }
        }

        /// <summary>¿Se ve la escena detrás? (Cinemática sobre el juego.)</summary>
        public bool SobreElJuego => Visible && !EnRejilla && _fondo != null && _fondo.color.a < 1f;

        public void Construir()
        {
            if (_construida)
            {
                return;
            }

            _construida = true;
            gameObject.SetActive(false);
            var raiz = (RectTransform)transform;
            FabricaUI.Estirar(raiz);

            _fondo = FabricaUI.Panel(raiz, "Fondo", Color.black);
            _franjaArriba = Franja(raiz, "FranjaArriba", 1f);
            _franjaAbajo = Franja(raiz, "FranjaAbajo", 0f);

            // El marco tiene la proporción de la imagen y cabe en la pantalla: las franjas negras
            // quedan fuera de él, y los textos se colocan dentro.
            var marco = FabricaUI.Objeto("Marco", raiz);
            FabricaUI.Estirar(marco);
            _marco = marco.gameObject.AddComponent<AspectRatioFitter>();
            _marco.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            _marco.aspectRatio = 16f / 9f;

            (_abajo, _grupoAbajo) = CapaDeImagen(marco, "ImagenAbajo");
            (_encima, _grupoEncima) = CapaDeImagen(marco, "ImagenEncima");

            _ranuras[POSICIONES_CARTEL.ARRIBA] = CrearRanura(marco, "TextoArriba", 5f / 6f);
            _ranuras[POSICIONES_CARTEL.CENTRO] = CrearRanura(marco, "TextoCentro", 0.5f);
            _ranuras[POSICIONES_CARTEL.ABAJO] = CrearRanura(marco, "TextoAbajo", 1f / 6f);

            _aviso = FabricaUI.Texto(raiz, "Aviso", string.Empty, 22, Color.white, TextAnchor.LowerRight);
            FabricaUI.Colocar(_aviso.rectTransform, new Vector2(1f, 0f), new Vector2(-48f, 36f), new Vector2(900f, 40f), new Vector2(1f, 0f));
            _aviso.raycastTarget = false;
            _grupoAviso = Grupo(_aviso.gameObject);

            _audio = NuevaFuente();
            _musica = NuevaFuente();
            _musica.volume = VolumenMusica;
        }

        /// <summary>Empieza a reproducir. Sin datos o sin diapositivas, termina en el acto.</summary>
        public void Reproducir(CutsceneData cinematica)
        {
            Construir();
            Actual = cinematica;
            if (cinematica == null || cinematica.diapositivas.Count == 0)
            {
                Debug.LogWarning(cinematica == null
                    ? "[Cinemática] La FSM pidió una cinemática sin datos: se continúa sin ella."
                    : $"[Cinemática] '{cinematica.Id}' no tiene diapositivas: se continúa sin ella.");
                Terminar();
                return;
            }

            if (cinematica.FaltaArte && AvisadasSinArte.Add(cinematica.Id))
            {
                Debug.LogWarning(cinematica.sobreElJuego
                    ? $"[Cinemática] '{cinematica.Id}' tiene diapositivas sin imagen (PENDIENTES B1): se ven sobre la escena con su texto."
                    : $"[Cinemática] '{cinematica.Id}' tiene diapositivas sin imagen (PENDIENTES B1): se ven en negro con su texto.");
            }

            EnRejilla = cinematica.escalaEntera && Camara != null;
            if (cinematica.escalaEntera && Camara == null && AvisadasSinCamara.Add(cinematica.Id))
            {
                Debug.LogWarning($"[Cinemática] '{cinematica.Id}' es pixel art, pero no hay cámara de cinemáticas: se ve estirada en la interfaz.");
            }

            // En la rejilla la imagen la pone la cámara, que es opaca: la interfaz solo deja el aviso.
            _fondo.color = EnRejilla ? Color.clear : new Color(0f, 0f, 0f, cinematica.sobreElJuego ? cinematica.opacidadVelo : 1f);
            _franjaArriba.enabled = cinematica.sobreElJuego && !EnRejilla;
            _franjaAbajo.enabled = cinematica.sobreElJuego && !EnRejilla;
            if (EnRejilla)
            {
                Camara.FijarRejilla(cinematica.Rejilla);
                Camara.Empezar();
            }
            else if (Camara != null)
            {
                Camara.Ocultar();
            }

            Linea = new LineaTiempoCinematica(cinematica);
            _inicio = Time.unscaledTime;
            _tAnterior = -1f;
            _indiceAudio = -1;
            _frameSaltoPedido = -1;
            _flashVistos = -1;
            _terminarTrasFlash = false;
            CortarCues();
            Reproduciendo = true;
            gameObject.SetActive(true);
            _aviso.text = cinematica.avisoSaltar;

            _musica.Stop();
            if (cinematica.musica != null)
            {
                _musica.clip = cinematica.musica;
                _musica.volume = VolumenMusica * AjustesAudio.Musica;
                _musica.Play();
            }

            // Cualquier tecla, botón del mando o clic.
            _escuchaBotones?.Dispose();
            _escuchaBotones = InputSystem.onAnyButtonPress.Call(_ => PedirSalto());

            Aplicar(0f);
        }

        /// <summary>
        /// Cualquier tecla: salta la cinemática si ya pasó el bloqueo inicial. Se aplica en el
        /// frame siguiente, para que la misma pulsación no llegue también al juego (Esc pausaría
        /// nada más volver). Devuelve si se aceptó.
        /// </summary>
        public bool PedirSalto()
        {
            if (!Reproduciendo || _terminarTrasFlash || !Linea.PuedeSaltar(Tiempo))
            {
                return false;
            }

            if (_frameSaltoPedido < 0)
            {
                _frameSaltoPedido = Time.frameCount;
            }

            return true;
        }

        /// <summary>Esc o Start (desde el UIManager): salta en el acto si ya pasó el bloqueo inicial.</summary>
        public void Saltar()
        {
            if (Reproduciendo && !_terminarTrasFlash && Linea.PuedeSaltar(Tiempo))
            {
                Cortar();
            }
        }

        /// <summary>La oculta sin avisar a la FSM (cuando otro sistema cambió el estado).</summary>
        public void Ocultar()
        {
            Reproduciendo = false;
            EnRejilla = false;
            _flashVistos = -1;
            _terminarTrasFlash = false;
            DejarDeEscuchar();
            CortarCues();

            if (_audio != null)
            {
                _audio.Stop();
                _musica.Stop();
            }

            if (Camara != null)
            {
                Camara.Ocultar();
            }

            if (_construida)
            {
                gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (!Reproduciendo)
            {
                return;
            }

            // El flash: se cuentan los fotogramas que ya se han visto con él.
            if (_flashVistos >= 0 && ++_flashVistos >= _flashObjetivo)
            {
                _flashVistos = -1;
                Camara.Flash(false);
                if (_terminarTrasFlash)
                {
                    Terminar();
                    return;
                }
            }

            if (_terminarTrasFlash)
            {
                return; // en el flash final: la línea de tiempo ya terminó
            }

            if (_frameSaltoPedido >= 0 && Time.frameCount > _frameSaltoPedido)
            {
                Cortar();
                return;
            }

            var t = Tiempo;
            if (Linea.Terminada(t))
            {
                Aplicar(t); // lo que ocurre justo al final, como el flash del último beat
                if (_flashVistos >= 0)
                {
                    _terminarTrasFlash = true;
                    return;
                }

                Terminar();
                return;
            }

            Aplicar(t);
        }

        private void OnDisable() => DejarDeEscuchar();

        // Saltar: directo al flash final si lo hay; si no, se termina.
        private void Cortar()
        {
            var flash = EnRejilla ? Linea.FlashFinal : 0;
            if (flash <= 0)
            {
                Terminar();
                return;
            }

            CortarCues();
            _audio.Stop();
            Camara.Escribir(SinTextos, Actual);
            EscribirSobreLaRejilla(SinTextos, Vector2Int.zero);
            EmpezarFlash(flash);
            _terminarTrasFlash = true;
        }

        private void Aplicar(float t)
        {
            var (abajo, encima) = Linea.ImagenesEn(t);
            var textos = Linea.TextosEn(t);
            if (EnRejilla)
            {
                Camara.Pintar(Imagen(abajo), abajo.Alfa, Encuadre(abajo), Imagen(encima), encima.Alfa, Encuadre(encima));
                var beat = abajo.Indice >= 0 ? Actual.diapositivas[abajo.Indice] : null;
                Camara.Brasas(beat != null && beat.brasas, abajo.Alfa);

                // Con fuente bitmap, el texto en la rejilla; sin ella, en la interfaz. En los dos casos
                // se mueve con el encuadre de su plano.
                var enLaRejilla = Actual.fuente != null;
                var encuadreTexto = EncuadreDe(Linea.IndiceEn(t));
                Camara.Escribir(enLaRejilla ? textos : SinTextos, Actual, encuadreTexto);
                EscribirSobreLaRejilla(enLaRejilla ? SinTextos : textos, encuadreTexto);
                if (Linea.TembloresEntre(_tAnterior, t) > 0)
                {
                    Camara.Temblar(Actual.temblorPixeles, Actual.temblorFotogramas);
                }

                var flash = Linea.FlashEntre(_tAnterior, t);
                if (flash > 0)
                {
                    EmpezarFlash(flash);
                }

                _grupoAbajo.alpha = 0f;
                _grupoEncima.alpha = 0f;
                foreach (var ranura in _ranuras.Values)
                {
                    ranura.Grupo.alpha = 0f;
                }
            }
            else
            {
                Pintar(_abajo, _grupoAbajo, abajo);
                Pintar(_encima, _grupoEncima, encima);
                EscribirEnLaInterfaz(textos);
                EscribirSobreLaRejilla(SinTextos, Vector2Int.zero);
            }

            foreach (var cue in Linea.CuesEntre(_tAnterior, t))
            {
                Sonar(cue);
            }

            CortarCuesHasta(t);
            _grupoAviso.alpha = Linea.AlfaAviso(t);
            SonarAudioDe(Linea.IndiceEn(t));
            _tAnterior = t;
        }

        private Sprite Imagen(LineaTiempoCinematica.Capa capa) => capa.Indice >= 0 ? Actual.diapositivas[capa.Indice]?.imagen : null;

        private Vector2Int Encuadre(LineaTiempoCinematica.Capa capa) => EncuadreDe(capa.Indice);

        private Vector2Int EncuadreDe(int indice) =>
            indice >= 0 && indice < Actual.diapositivas.Count && Actual.diapositivas[indice] != null ? Actual.diapositivas[indice].desplazamiento : Vector2Int.zero;

        private void EmpezarFlash(int fotogramas)
        {
            _flashObjetivo = fotogramas;
            _flashVistos = 0;
            Camara.Flash(true);
        }

        private void EscribirEnLaInterfaz(List<LineaTiempoCinematica.TextoVisible> textos)
        {
            foreach (var ranura in _ranuras.Values)
            {
                ranura.Grupo.alpha = 0f;
            }

            // Si dos textos comparten tercio a la vez, se ve el más opaco.
            foreach (var visible in textos)
            {
                var ranura = _ranuras[visible.Cartel.posicion];
                if (visible.Alfa <= ranura.Grupo.alpha)
                {
                    continue;
                }

                ranura.Texto.text = visible.Cartel.texto;
                ranura.Texto.fontSize = visible.Cartel.titulo ? TamanoTitulo : TamanoTexto;
                ranura.Grupo.alpha = visible.Alfa;
            }
        }

        /// <summary>
        /// Pixel art sin fuente bitmap: el texto va en la interfaz, como en la cinemática inicial,
        /// pero anclado en su píxel del plano y a la escala de la imagen: la letra mide
        /// <see cref="CutsceneData.tamanoFuente"/> píxeles del plano (el doble, un título).
        /// </summary>
        private void EscribirSobreLaRejilla(List<LineaTiempoCinematica.TextoVisible> textos, Vector2Int encuadre)
        {
            var rejilla = CamaraCinematica.RectEnPantalla(Screen.width, Screen.height, Rejilla);
            var lienzo = Lienzo;
            var factor = lienzo != null && lienzo.scaleFactor > 0f ? lienzo.scaleFactor : 1f;
            for (var i = 0; i < textos.Count; i++)
            {
                var (texto, grupo, sombra) = TextoSobreRejilla(i);
                ColocarSobreRejilla(texto, sombra, textos[i].Cartel, Actual, rejilla, factor, encuadre);
                grupo.alpha = textos[i].Alfa;
            }

            for (var i = textos.Count; i < _textosSobreRejilla.Count; i++)
            {
                _textosSobreRejilla[i].grupo.alpha = 0f;
            }
        }

        /// <summary>
        /// Coloca un texto de la interfaz sobre la rejilla: anclado en el píxel de su cartel (movido
        /// con el <paramref name="encuadre"/> de su plano), con la letra de
        /// <see cref="CutsceneData.tamanoFuente"/> píxeles del plano y la sombra de la cinemática
        /// inicial (3 px a 1080p), a la misma escala. <paramref name="rejilla"/> es el rectángulo del
        /// plano en píxeles de pantalla y <paramref name="factor"/>, la escala del lienzo. Lo usa
        /// también la captura del editor.
        /// </summary>
        public static void ColocarSobreRejilla(Text texto, Shadow sombra, Cartel cartel, CutsceneData cinematica, RectInt rejilla, float factor,
            Vector2Int encuadre = default)
        {
            var plano = cinematica.Rejilla;
            var k = rejilla.width / plano.x;
            var (punto, alineacion) = CamaraCinematica.Anclaje(cartel, encuadre, plano);
            var (fx, fy) = TextoEnRejilla.Factores(alineacion);

            texto.text = cartel.texto;
            texto.alignment = alineacion;
            texto.color = cinematica.colorTexto;
            texto.fontSize = Mathf.Max(1, Mathf.RoundToInt(cinematica.tamanoFuente * (cartel.titulo ? 2 : 1) * k / factor));
            var desfase = Mathf.Max(1, Mathf.RoundToInt(k * 3f / 8f));
            sombra.effectDistance = new Vector2(desfase, -desfase) / factor;

            var rect = texto.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(fx, 1f - fy);
            rect.sizeDelta = new Vector2(rejilla.width, rejilla.height) / factor;
            rect.anchoredPosition = new Vector2(rejilla.x + punto.x * k, rejilla.y + (plano.y - punto.y) * k) / factor;
        }

        private (Text, CanvasGroup, Shadow) TextoSobreRejilla(int indice)
        {
            while (_textosSobreRejilla.Count <= indice)
            {
                var texto = FabricaUI.Texto(transform, $"TextoSobreRejilla{_textosSobreRejilla.Count + 1}", string.Empty, TamanoTexto, ColorTexto);
                texto.raycastTarget = false;
                texto.horizontalOverflow = HorizontalWrapMode.Overflow;
                texto.verticalOverflow = VerticalWrapMode.Overflow;
                var rect = texto.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.zero;
                var sombra = texto.gameObject.AddComponent<Shadow>();
                sombra.effectColor = new Color(0f, 0f, 0f, 0.85f);
                _textosSobreRejilla.Add((texto, Grupo(texto.gameObject), sombra));
            }

            return _textosSobreRejilla[indice];
        }

        private void Pintar(Image imagen, CanvasGroup grupo, LineaTiempoCinematica.Capa capa)
        {
            var sprite = Imagen(capa);
            if (imagen.sprite != sprite)
            {
                imagen.sprite = sprite;
                if (sprite != null)
                {
                    _marco.aspectRatio = sprite.rect.width / sprite.rect.height;
                }
            }

            imagen.enabled = sprite != null;
            grupo.alpha = sprite != null ? capa.Alfa : 0f;
        }

        // --- Sonido ---

        private void Sonar(LineaTiempoCinematica.CueEnCurso enCurso)
        {
            var cue = enCurso.Cue;
            if (cue.clip == null)
            {
                Debug.LogWarning($"[Cinemática] '{Actual.Id}': falta el audio «{cue.nombre}» (diapositiva {enCurso.Diapositiva + 1}, " +
                                 $"a los {enCurso.Inicio.ToString("0.##", CultureInfo.InvariantCulture)} s). Sigue sin él.");
                return;
            }

            var fuente = FuenteLibre();
            fuente.clip = cue.clip;
            fuente.loop = cue.bucle;
            fuente.Play();
            _cuesSonando.Add(new CueSonando { Fuente = fuente, Fin = enCurso.Fin });
        }

        // Corte seco de los cues cuyo final ya llegó; los que no son en bucle se sueltan al acabar.
        private void CortarCuesHasta(float t)
        {
            for (var i = _cuesSonando.Count - 1; i >= 0; i--)
            {
                var sonando = _cuesSonando[i];
                if (t >= sonando.Fin || (!sonando.Fuente.loop && !sonando.Fuente.isPlaying))
                {
                    sonando.Fuente.Stop();
                    _cuesSonando.RemoveAt(i);
                }
            }
        }

        private void CortarCues()
        {
            foreach (var sonando in _cuesSonando)
            {
                sonando.Fuente.Stop();
            }

            _cuesSonando.Clear();
        }

        private AudioSource FuenteLibre()
        {
            foreach (var fuente in _fuentesCue)
            {
                if (!_cuesSonando.Exists(c => c.Fuente == fuente))
                {
                    return fuente;
                }
            }

            var nueva = NuevaFuente();
            _fuentesCue.Add(nueva);
            return nueva;
        }

        private AudioSource NuevaFuente()
        {
            var fuente = gameObject.AddComponent<AudioSource>();
            fuente.playOnAwake = false;
            fuente.ignoreListenerPause = true;
            return fuente;
        }

        private void SonarAudioDe(int indice)
        {
            if (indice == _indiceAudio)
            {
                return;
            }

            _indiceAudio = indice;
            _audio.Stop();
            var diapositiva = indice >= 0 ? Actual.diapositivas[indice] : null;
            if (diapositiva != null && diapositiva.audio != null)
            {
                _audio.clip = diapositiva.audio;
                _audio.Play();
            }
        }

        private void DejarDeEscuchar()
        {
            _escuchaBotones?.Dispose();
            _escuchaBotones = null;
        }

        private void Terminar()
        {
            Ocultar();
            var juego = GameStateMachine.Instancia;
            if (juego != null)
            {
                juego.TerminarCinematica();
            }
        }

        // --- Construcción ---

        // Franja negra de cine pegada al borde de arriba (borde = 1) o de abajo (borde = 0).
        private static Image Franja(RectTransform raiz, string nombre, float borde)
        {
            var franja = FabricaUI.Panel(raiz, nombre, Color.black, false);
            var rect = franja.rectTransform;
            rect.anchorMin = new Vector2(0f, borde > 0.5f ? 1f - AltoFranja : 0f);
            rect.anchorMax = new Vector2(1f, borde > 0.5f ? 1f : AltoFranja);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            franja.enabled = false;
            return franja;
        }

        private static (Image, CanvasGroup) CapaDeImagen(Transform marco, string nombre)
        {
            var imagen = FabricaUI.Objeto(nombre, marco).gameObject.AddComponent<Image>();
            imagen.preserveAspect = true;
            imagen.raycastTarget = false;
            imagen.enabled = false;
            FabricaUI.Estirar(imagen.rectTransform);
            return (imagen, Grupo(imagen.gameObject));
        }

        private static Ranura CrearRanura(Transform marco, string nombre, float alturaRelativa)
        {
            var texto = FabricaUI.Texto(marco, nombre, string.Empty, TamanoTexto, ColorTexto);
            texto.raycastTarget = false;
            var rect = texto.rectTransform;
            rect.anchorMin = new Vector2(0.08f, alturaRelativa);
            rect.anchorMax = new Vector2(0.92f, alturaRelativa);
            rect.sizeDelta = new Vector2(0f, 260f);
            rect.anchoredPosition = Vector2.zero;

            // Sombra para que se lea sobre cualquier imagen.
            var sombra = texto.gameObject.AddComponent<Shadow>();
            sombra.effectColor = new Color(0f, 0f, 0f, 0.85f);
            sombra.effectDistance = new Vector2(3f, -3f);

            return new Ranura { Texto = texto, Grupo = Grupo(texto.gameObject) };
        }

        private static CanvasGroup Grupo(GameObject objeto)
        {
            var grupo = objeto.AddComponent<CanvasGroup>();
            grupo.alpha = 0f;
            grupo.blocksRaycasts = false;
            grupo.interactable = false;
            return grupo;
        }
    }
}
