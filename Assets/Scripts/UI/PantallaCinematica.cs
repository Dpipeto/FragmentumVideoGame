using System;
using System.Collections.Generic;
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
    /// <see cref="CutsceneData"/> a pantalla completa mientras la FSM global está en CUTSCENE.
    ///
    /// - Qué se ve en cada instante lo decide <see cref="LineaTiempoCinematica"/>; esta pantalla
    ///   solo lo aplica. Las imágenes y los textos entran y salen con <c>CanvasGroup.alpha</c>:
    ///   fundido cruzado entre imágenes y fundido en cada texto, nunca un corte duro.
    /// - Las imágenes conservan su proporción sobre fondo negro, y los textos se colocan por
    ///   tercios de la <b>imagen</b>, no de la pantalla: en 16:10 un título «arriba» sigue cayendo
    ///   sobre la imagen y no sobre la franja negra.
    /// - Pasados los primeros segundos (<see cref="CutsceneData.bloqueoSalto"/>), cualquier tecla,
    ///   botón o clic la salta entera. Un aviso discreto lo dice.
    /// - <b>Funciona sin arte:</b> una diapositiva sin imagen se ve en negro con su texto, y sin
    ///   texto dice qué falta (criterio de aceptación de la Fase 8).
    /// - <b>Sobre el juego</b> (<see cref="CutsceneData.sobreElJuego"/>): en vez de negro, un velo
    ///   y dos franjas de cine sobre la escena congelada. Es la presentación del jefe: se ve la
    ///   arena con el jefe mientras habla.
    /// - <b>Escala entera</b> (<see cref="CutsceneData.escalaEntera"/>), para el pixel art: la imagen
    ///   mide en pantalla su tamaño por el mayor número entero que quepa, centrada y alineada a
    ///   píxeles enteros. Así no se interpola nada, aunque el Canvas de la interfaz escale libre.
    /// - Cuenta el tiempo sin escala, porque la cinemática para el juego.
    /// - Al terminar se lo dice a la FSM global, que decide a dónde se va.
    /// </summary>
    public class PantallaCinematica : MonoBehaviour
    {
        private const int TamanoTexto = 40;
        private const int TamanoTitulo = 76;

        // Alto de cada franja de cine, en fracción de la pantalla, cuando va sobre el juego.
        private const float AltoFranja = 0.11f;
        private static readonly Color ColorTexto = new Color(0.94f, 0.93f, 0.85f, 1f);

        private static readonly HashSet<string> AvisadasSinArte = new HashSet<string>();

        private sealed class Ranura
        {
            public Text Texto;
            public CanvasGroup Grupo;
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
        private Text _aviso;
        private CanvasGroup _grupoAviso;
        private AudioSource _audio;
        private AudioSource _musica;
        private bool _construida;
        private float _inicio;
        private int _indiceAudio = -1;
        private int _frameSaltoPedido = -1;
        private IDisposable _escuchaBotones;

        public CutsceneData Actual { get; private set; }

        public LineaTiempoCinematica Linea { get; private set; }

        public bool Reproduciendo { get; private set; }

        /// <summary>Segundos desde que empezó la cinemática en curso.</summary>
        public float Tiempo => Reproduciendo ? Time.unscaledTime - _inicio : 0f;

        /// <summary>Diapositiva en curso.</summary>
        public int Indice => Linea != null ? Mathf.Max(0, Linea.IndiceEn(Tiempo)) : 0;

        /// <summary>Los textos que se ven ahora, uno por línea.</summary>
        public string TextoActual
        {
            get
            {
                var texto = new StringBuilder();
                foreach (var ranura in _ranuras.Values)
                {
                    if (ranura.Grupo.alpha > 0f && !string.IsNullOrEmpty(ranura.Texto.text))
                    {
                        texto.AppendLine(ranura.Texto.text);
                    }
                }

                return texto.ToString().TrimEnd();
            }
        }

        /// <summary>Opacidad actual del aviso «pulsa cualquier tecla».</summary>
        public float AlfaAviso => _grupoAviso != null ? _grupoAviso.alpha : 0f;

        public bool Visible => _construida && gameObject.activeSelf;

        /// <summary>
        /// Con escala entera, por cuánto se multiplica la imagen (en píxeles de pantalla); 0 si la
        /// cinemática en curso escala libre.
        /// </summary>
        public int EscalaEntera { get; private set; }

        /// <summary>El rectángulo de la imagen, en píxeles de pantalla.</summary>
        public Rect RectImagenEnPantalla
        {
            get
            {
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
        public bool SobreElJuego => Visible && _fondo != null && _fondo.color.a < 1f;

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

            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.ignoreListenerPause = true;

            _musica = gameObject.AddComponent<AudioSource>();
            _musica.playOnAwake = false;
            _musica.ignoreListenerPause = true;
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

            _fondo.color = new Color(0f, 0f, 0f, cinematica.sobreElJuego ? cinematica.opacidadVelo : 1f);
            EscalarLibre(!cinematica.escalaEntera);
            _franjaArriba.enabled = cinematica.sobreElJuego;
            _franjaAbajo.enabled = cinematica.sobreElJuego;

            Linea = new LineaTiempoCinematica(cinematica);
            _inicio = Time.unscaledTime;
            _indiceAudio = -1;
            _frameSaltoPedido = -1;
            Reproduciendo = true;
            gameObject.SetActive(true);
            _aviso.text = cinematica.avisoSaltar;

            _musica.Stop();
            if (cinematica.musica != null)
            {
                _musica.clip = cinematica.musica;
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
            if (!Reproduciendo || !Linea.PuedeSaltar(Tiempo))
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
            if (Reproduciendo && Linea.PuedeSaltar(Tiempo))
            {
                Terminar();
            }
        }

        /// <summary>La oculta sin avisar a la FSM (cuando otro sistema cambió el estado).</summary>
        public void Ocultar()
        {
            Reproduciendo = false;
            DejarDeEscuchar();

            if (_audio != null)
            {
                _audio.Stop();
                _musica.Stop();
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

            var t = Tiempo;
            if (Linea.Terminada(t) || (_frameSaltoPedido >= 0 && Time.frameCount > _frameSaltoPedido))
            {
                Terminar();
                return;
            }

            Aplicar(t);
        }

        private void OnDisable() => DejarDeEscuchar();

        private void Aplicar(float t)
        {
            var (abajo, encima) = Linea.ImagenesEn(t);
            Pintar(_abajo, _grupoAbajo, abajo);
            Pintar(_encima, _grupoEncima, encima);
            if (Actual.escalaEntera)
            {
                AjustarEscalaEntera(_encima.sprite != null ? _encima.sprite : _abajo.sprite);
            }

            foreach (var ranura in _ranuras.Values)
            {
                ranura.Grupo.alpha = 0f;
            }

            // Si dos textos comparten tercio a la vez, se ve el más opaco.
            foreach (var visible in Linea.TextosEn(t))
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

            _grupoAviso.alpha = Linea.AlfaAviso(t);
            SonarAudioDe(Linea.IndiceEn(t));
        }

        private void Pintar(Image imagen, CanvasGroup grupo, LineaTiempoCinematica.Capa capa)
        {
            var sprite = capa.Indice >= 0 ? Actual.diapositivas[capa.Indice]?.imagen : null;
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

        // Escala libre: el marco cabe en la pantalla con la proporción de la imagen.
        private void EscalarLibre(bool libre)
        {
            _marco.enabled = libre;
            EscalaEntera = 0;
            if (libre)
            {
                FabricaUI.Estirar((RectTransform)_marco.transform);
            }
        }

        // Escala entera: el marco mide k veces la imagen en píxeles de pantalla, con k el mayor
        // entero que cabe, y sus bordes caen en píxeles enteros. El tamaño se pasa a unidades del
        // Canvas dividiendo por su escala: así da igual cómo escale el resto de la interfaz.
        private void AjustarEscalaEntera(Sprite imagen)
        {
            if (imagen == null)
            {
                return;
            }

            var lienzo = Lienzo;
            var factor = lienzo != null && lienzo.scaleFactor > 0f ? lienzo.scaleFactor : 1f;
            var pantalla = lienzo != null ? lienzo.pixelRect.size : new Vector2(Screen.width, Screen.height);
            var tamano = imagen.rect.size;
            var k = Mathf.Max(1, Mathf.FloorToInt(Mathf.Min(pantalla.x / tamano.x, pantalla.y / tamano.y)));
            var enPixeles = tamano * k;

            // Si el borde negro que sobra es impar, medio píxel a la izquierda (o abajo) deja la
            // imagen en píxeles enteros.
            var medioPixel = new Vector2(Mathf.Repeat(pantalla.x - enPixeles.x, 2f), Mathf.Repeat(pantalla.y - enPixeles.y, 2f)) * 0.5f;

            var rect = (RectTransform)_marco.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = enPixeles / factor;
            rect.anchoredPosition = -medioPixel / factor;
            EscalaEntera = k;
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
