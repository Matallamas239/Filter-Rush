using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class MenuUIController : MonoBehaviour
{
    [Header("Referencias de HUD")]
    public Text textoScore;
    public Text textoHighScore;
    public Text textoCombo;
    public Button botonPausa;

    [Header("HUD de Vidas y Meta")]
    [Tooltip("Texto para mostrar las vidas restantes (corazones).")]
    public Text textoVidas;
    [Tooltip("Texto para mostrar el progreso hacia la meta de reciclaje.")]
    public Text textoMeta;
    [Tooltip("Texto para mostrar la velocidad del flujo y el tiempo transcurrido.")]
    public Text textoVelocidad;

    [Header("Paneles Principales")]
    public GameObject panelMenuPausa;
    public GameObject panelOpciones;
    public GameObject panelControles;

    [Header("Paneles de Fin de Partida")]
    public GameObject panelVictoria;
    public Text textoResumenVictoria;
    public Button botonJugarDeNuevoVictoria;

    public GameObject panelGameOver;
    public Text textoResumenGameOver;
    public Button botonReintentarGameOver;

    [Header("Controles de Opciones")]
    public Slider sliderVolumenMusica;
    public Slider sliderVolumenGeneral;
    public Button botonBorrarRecord;
    public Button botonVolverOpciones;

    [Header("Controles de Menú")]
    public Button botonReanudar;
    public Button botonReiniciar;
    public Button botonAbrirOpciones;
    public Button botonAbrirControles;
    public Button botonVolverControles;

    private bool estaPausado = false;
    private const string PREF_VOL_MUSICA = "VolumenMusica";
    private const string PREF_VOL_GENERAL = "VolumenGeneral";

    void Start()
    {
        // 1. Crear elementos de HUD y Paneles si no fueron asignados en el Inspector
        CrearElementosHUDSiFaltan();
        CrearPanelesFinDePartidaSiFaltan();

        // 2. Suscribirse a los eventos del controlador de puntuación
        if (ControladorPuntuacion.Instancia != null)
        {
            ControladorPuntuacion.Instancia.EnPuntuacionActualizada += ActualizarTextosPuntuacion;
            ControladorPuntuacion.Instancia.EnEstadoJuegoActualizado += ActualizarHUDCompleto;
            ControladorPuntuacion.Instancia.EnVictoria += MostrarPantallaVictoria;
            ControladorPuntuacion.Instancia.EnGameOver += MostrarPantallaGameOver;

            // Inicializar datos en pantalla
            var cp = ControladorPuntuacion.Instancia;
            ActualizarHUDCompleto(
                cp.PuntuacionActual,
                cp.MejorPuntuacion,
                cp.ComboActual,
                cp.Multiplicador,
                cp.VidasActuales,
                cp.VidasMaximas,
                cp.BasurasRecicladas,
                cp.MetaBasurasParaGanar
            );
        }

        // 3. Configurar listeners de botones del menú
        if (botonPausa != null) botonPausa.onClick.AddListener(AlternarPausa);
        if (botonReanudar != null) botonReanudar.onClick.AddListener(ReanudarJuego);
        if (botonReiniciar != null) botonReiniciar.onClick.AddListener(ReiniciarPartida);
        if (botonAbrirOpciones != null) botonAbrirOpciones.onClick.AddListener(MostrarOpciones);
        if (botonAbrirControles != null) botonAbrirControles.onClick.AddListener(MostrarControles);
        if (botonVolverOpciones != null) botonVolverOpciones.onClick.AddListener(MostrarMenuPrincipal);
        if (botonVolverControles != null) botonVolverControles.onClick.AddListener(MostrarMenuPrincipal);
        if (botonBorrarRecord != null) botonBorrarRecord.onClick.AddListener(BorrarRecord);

        // 4. Inicializar sliders de audio
        float volMusicaGuardado = PlayerPrefs.GetFloat(PREF_VOL_MUSICA, 0.6f);
        float volGeneralGuardado = PlayerPrefs.GetFloat(PREF_VOL_GENERAL, 1.0f);

        if (sliderVolumenMusica != null)
        {
            sliderVolumenMusica.value = volMusicaGuardado;
            sliderVolumenMusica.onValueChanged.AddListener(OnVolumenMusicaCambiado);
            OnVolumenMusicaCambiado(volMusicaGuardado);
        }

        if (sliderVolumenGeneral != null)
        {
            sliderVolumenGeneral.value = volGeneralGuardado;
            sliderVolumenGeneral.onValueChanged.AddListener(OnVolumenGeneralCambiado);
            OnVolumenGeneralCambiado(volGeneralGuardado);
        }

        // Asegurar que los paneles inicien cerrados
        if (panelMenuPausa != null) panelMenuPausa.SetActive(false);
        if (panelOpciones != null) panelOpciones.SetActive(false);
        if (panelControles != null) panelControles.SetActive(false);
        if (panelVictoria != null) panelVictoria.SetActive(false);
        if (panelGameOver != null) panelGameOver.SetActive(false);
    }

    void Update()
    {
        // Abrir / Cerrar con tecla Escape
        if (Keyboard.current != null && Keyboard.current[Key.Escape].wasPressedThisFrame)
        {
            AlternarPausa();
        }

        ActualizarHUDVelocidadYTiempo();
    }

    void OnDestroy()
    {
        if (ControladorPuntuacion.Instancia != null)
        {
            ControladorPuntuacion.Instancia.EnPuntuacionActualizada -= ActualizarTextosPuntuacion;
            ControladorPuntuacion.Instancia.EnEstadoJuegoActualizado -= ActualizarHUDCompleto;
            ControladorPuntuacion.Instancia.EnVictoria -= MostrarPantallaVictoria;
            ControladorPuntuacion.Instancia.EnGameOver -= MostrarPantallaGameOver;
        }
    }

    public void ActualizarTextosPuntuacion(int score, int highScore, int combo, int multiplicador)
    {
        if (textoScore != null)
        {
            textoScore.text = $"SCORE: {score:N0}";
        }

        if (textoHighScore != null)
        {
            textoHighScore.text = $"HIGH SCORE: {highScore:N0}";
        }

        if (textoCombo != null)
        {
            if (combo > 1)
            {
                textoCombo.gameObject.SetActive(true);
                textoCombo.text = $"COMBO x{multiplicador} ({combo})";
            }
            else
            {
                textoCombo.gameObject.SetActive(false);
            }
        }
    }

    public void ActualizarHUDCompleto(
        int score,
        int highScore,
        int combo,
        int multiplicador,
        int vidas,
        int maxVidas,
        int recicladas,
        int meta
    )
    {
        ActualizarTextosPuntuacion(score, highScore, combo, multiplicador);

        if (textoVidas != null)
        {
            int vidasPositivas = Mathf.Clamp(vidas, 0, maxVidas);
            int vidasPerdidas = Mathf.Max(0, maxVidas - vidasPositivas);
            string corazonesVivos = string.Join(" ", new string('♥', vidasPositivas).ToCharArray());
            string corazonesMuertos = string.Join(" ", new string('♥', vidasPerdidas).ToCharArray());
            string separador = (vidasPositivas > 0 && vidasPerdidas > 0) ? " " : "";
            textoVidas.text = $"VIDAS: <color=#FF3B69>{corazonesVivos}</color>{separador}<color=#556677>{corazonesMuertos}</color>";
        }

        if (textoMeta != null)
        {
            textoMeta.text = $"💧 META: {recicladas} / {meta}";
        }
    }

    public void AlternarPausa()
    {
        // Si la partida ya finalizó (ganó o perdió), no alternar el menú de pausa
        if (ControladorPuntuacion.Instancia != null && ControladorPuntuacion.Instancia.JuegoTerminado)
        {
            return;
        }

        if (estaPausado)
        {
            ReanudarJuego();
        }
        else
        {
            PausarJuego();
        }
    }

    public void PausarJuego()
    {
        estaPausado = true;
        Time.timeScale = 0f;
        MostrarMenuPrincipal();
        if (panelMenuPausa != null) panelMenuPausa.SetActive(true);
    }

    public void ReanudarJuego()
    {
        estaPausado = false;
        Time.timeScale = 1f;
        if (panelMenuPausa != null) panelMenuPausa.SetActive(false);
        if (panelOpciones != null) panelOpciones.SetActive(false);
        if (panelControles != null) panelControles.SetActive(false);
        if (panelVictoria != null) panelVictoria.SetActive(false);
        if (panelGameOver != null) panelGameOver.SetActive(false);
    }

    public void MostrarMenuPrincipal()
    {
        if (panelMenuPausa != null) panelMenuPausa.SetActive(true);
        if (panelOpciones != null) panelOpciones.SetActive(false);
        if (panelControles != null) panelControles.SetActive(false);
        if (panelVictoria != null) panelVictoria.SetActive(false);
        if (panelGameOver != null) panelGameOver.SetActive(false);
    }

    public void MostrarOpciones()
    {
        if (panelMenuPausa != null) panelMenuPausa.SetActive(false);
        if (panelOpciones != null) panelOpciones.SetActive(true);
        if (panelControles != null) panelControles.SetActive(false);
    }

    public void MostrarControles()
    {
        if (panelMenuPausa != null) panelMenuPausa.SetActive(false);
        if (panelOpciones != null) panelOpciones.SetActive(false);
        if (panelControles != null) panelControles.SetActive(true);
    }

    public void MostrarPantallaVictoria()
    {
        estaPausado = true;
        Time.timeScale = 0f;

        if (panelMenuPausa != null) panelMenuPausa.SetActive(false);
        if (panelOpciones != null) panelOpciones.SetActive(false);
        if (panelControles != null) panelControles.SetActive(false);
        if (panelGameOver != null) panelGameOver.SetActive(false);

        if (panelVictoria != null)
        {
            panelVictoria.SetActive(true);
            if (textoResumenVictoria != null && ControladorPuntuacion.Instancia != null)
            {
                var cp = ControladorPuntuacion.Instancia;
                textoResumenVictoria.text =
                    $"<color=#00E5FF>SCORE FINAL:</color> {cp.PuntuacionActual:N0}\n" +
                    $"<color=#FFEAA7>RÉCORD:</color> {cp.MejorPuntuacion:N0}\n" +
                    $"<color=#55EFC4>BASURAS RECICLADAS:</color> {cp.BasurasRecicladas} / {cp.MetaBasurasParaGanar}\n" +
                    $"<color=#FD79A8>COMBO MÁXIMO:</color> x{cp.ComboMaximo}";
            }
        }
    }

    public void MostrarPantallaGameOver()
    {
        estaPausado = true;
        Time.timeScale = 0f;

        if (panelMenuPausa != null) panelMenuPausa.SetActive(false);
        if (panelOpciones != null) panelOpciones.SetActive(false);
        if (panelControles != null) panelControles.SetActive(false);
        if (panelVictoria != null) panelVictoria.SetActive(false);

        if (panelGameOver != null)
        {
            panelGameOver.SetActive(true);
            if (textoResumenGameOver != null && ControladorPuntuacion.Instancia != null)
            {
                var cp = ControladorPuntuacion.Instancia;
                textoResumenGameOver.text =
                    $"<color=#FF7675>PUNTOS OBTENIDOS:</color> {cp.PuntuacionActual:N0}\n" +
                    $"<color=#FFEAA7>RÉCORD:</color> {cp.MejorPuntuacion:N0}\n" +
                    $"<color=#74B9FF>BASURAS FILTRADAS:</color> {cp.BasurasRecicladas} / {cp.MetaBasurasParaGanar}\n\n" +
                    $"<i>¡El filtro se saturó de contaminación! No dejes escapar las botellas.</i>";
            }
        }
    }

    public void ReiniciarPartida()
    {
        // Ocultar paneles de fin de partida
        if (panelVictoria != null) panelVictoria.SetActive(false);
        if (panelGameOver != null) panelGameOver.SetActive(false);

        // Destruir toda la basura que esté actualmente en pantalla
        MovimientoBasura[] basuras = FindObjectsByType<MovimientoBasura>();
        foreach (var b in basuras)
        {
            if (b != null) Destroy(b.gameObject);
        }

        // Reiniciar puntuación, vidas y meta
        if (ControladorPuntuacion.Instancia != null)
        {
            ControladorPuntuacion.Instancia.ReiniciarPuntuacion();
        }

        ReanudarJuego();
    }

    public void BorrarRecord()
    {
        if (ControladorPuntuacion.Instancia != null)
        {
            ControladorPuntuacion.Instancia.BorrarRecord();
        }
    }

    public void OnVolumenMusicaCambiado(float nuevoVol)
    {
        PlayerPrefs.SetFloat(PREF_VOL_MUSICA, nuevoVol);
        ControladorMusica mus = FindAnyObjectByType<ControladorMusica>();
        if (mus != null)
        {
            mus.AjustarVolumen(nuevoVol);
        }
    }

    public void OnVolumenGeneralCambiado(float nuevoVol)
    {
        PlayerPrefs.SetFloat(PREF_VOL_GENERAL, nuevoVol);
        AudioListener.volume = nuevoVol;
    }

    private void CrearElementosHUDSiFaltan()
    {
        Transform contenedorHUD = textoScore != null ? textoScore.transform.parent : transform;
        Font fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (fuente == null) fuente = Resources.GetBuiltinResource<Font>("Arial.ttf");

        // Limpiar cualquier objeto huérfano suelto de pruebas previas
        Transform viejoWidget = contenedorHUD.Find("Widget_EstadoPartida");
        if (viejoWidget != null) Destroy(viejoWidget.gameObject);

        Transform viejoVidas = contenedorHUD.Find("Text_Vidas");
        if (viejoVidas != null) Destroy(viejoVidas.gameObject);

        Transform viejoMeta = contenedorHUD.Find("Text_Meta");
        if (viejoMeta != null) Destroy(viejoMeta.gameObject);

        // Crear tarjeta contenedora para Vidas y Meta perfectamente alineada abajo de la guía de teclas
        GameObject widget = new GameObject("Widget_EstadoPartida");
        widget.transform.SetParent(contenedorHUD, false);

        RectTransform rtWidget = widget.AddComponent<RectTransform>();
        rtWidget.anchorMin = new Vector2(0f, 1f);
        rtWidget.anchorMax = new Vector2(0f, 1f);
        rtWidget.pivot = new Vector2(0f, 1f);
        rtWidget.anchoredPosition = new Vector2(30f, -115f);
        rtWidget.sizeDelta = new Vector2(285f, 122f);

        Image imgFondo = widget.AddComponent<Image>();
        imgFondo.color = new Color(0.02f, 0.08f, 0.16f, 0.88f);

        // 1. Texto de Vidas (dentro de la tarjeta, fila superior)
        GameObject objVidas = new GameObject("Text_Vidas");
        objVidas.transform.SetParent(widget.transform, false);

        RectTransform rtVidas = objVidas.AddComponent<RectTransform>();
        rtVidas.anchorMin = new Vector2(0f, 1f);
        rtVidas.anchorMax = new Vector2(0f, 1f);
        rtVidas.pivot = new Vector2(0f, 1f);
        rtVidas.anchoredPosition = new Vector2(16f, -12f);
        rtVidas.sizeDelta = new Vector2(250f, 28f);

        textoVidas = objVidas.AddComponent<Text>();
        textoVidas.font = fuente;
        textoVidas.fontSize = 18;
        textoVidas.fontStyle = FontStyle.Bold;
        textoVidas.color = Color.white;
        textoVidas.supportRichText = true;
        textoVidas.alignment = TextAnchor.MiddleLeft;
        textoVidas.text = "VIDAS: <color=#FF3B69>♥ ♥ ♥</color>";

        // 2. Texto de Meta (dentro de la tarjeta, fila intermedia)
        GameObject objMeta = new GameObject("Text_Meta");
        objMeta.transform.SetParent(widget.transform, false);

        RectTransform rtMeta = objMeta.AddComponent<RectTransform>();
        rtMeta.anchorMin = new Vector2(0f, 1f);
        rtMeta.anchorMax = new Vector2(0f, 1f);
        rtMeta.pivot = new Vector2(0f, 1f);
        rtMeta.anchoredPosition = new Vector2(16f, -48f);
        rtMeta.sizeDelta = new Vector2(250f, 28f);

        textoMeta = objMeta.AddComponent<Text>();
        textoMeta.font = fuente;
        textoMeta.fontSize = 16;
        textoMeta.fontStyle = FontStyle.Bold;
        textoMeta.color = new Color(0.2f, 0.9f, 1f, 1f);
        textoMeta.supportRichText = true;
        textoMeta.alignment = TextAnchor.MiddleLeft;
        textoMeta.text = "💧 META: 0 / 20";

        // 3. Texto de Velocidad del Flujo y Tiempo (dentro de la tarjeta, fila inferior)
        GameObject objVel = new GameObject("Text_Velocidad");
        objVel.transform.SetParent(widget.transform, false);

        RectTransform rtVel = objVel.AddComponent<RectTransform>();
        rtVel.anchorMin = new Vector2(0f, 1f);
        rtVel.anchorMax = new Vector2(0f, 1f);
        rtVel.pivot = new Vector2(0f, 1f);
        rtVel.anchoredPosition = new Vector2(16f, -84f);
        rtVel.sizeDelta = new Vector2(255f, 28f);

        textoVelocidad = objVel.AddComponent<Text>();
        textoVelocidad.font = fuente;
        textoVelocidad.fontSize = 15;
        textoVelocidad.fontStyle = FontStyle.Bold;
        textoVelocidad.color = new Color(0.9f, 0.95f, 1f, 1f);
        textoVelocidad.supportRichText = true;
        textoVelocidad.alignment = TextAnchor.MiddleLeft;
        textoVelocidad.text = "⚡ FLUJO: <color=#00E5FF>1.0x</color>  <color=#8899AA>⏱ 00:00</color>";
    }

    private void ActualizarHUDVelocidadYTiempo()
    {
        if (textoVelocidad == null || ControladorPuntuacion.Instancia == null) return;

        var cp = ControladorPuntuacion.Instancia;
        float factor = cp.MultiplicadorVelocidad;
        float t = cp.TiempoTranscurrido;

        int minutos = Mathf.FloorToInt(t / 60f);
        int segundos = Mathf.FloorToInt(t % 60f);

        string colorHex = factor < 1.35f ? "#00E5FF" : (factor < 1.85f ? "#FFEAA7" : "#FF3B69");
        string tag = factor >= 2.0f ? " <color=#FF3B69><b>¡RUSH!</b></color>" : "";

        textoVelocidad.text = $"⚡ FLUJO: <color={colorHex}>{factor:F1}x</color>{tag}  <color=#8899AA>⏱ {minutos:00}:{segundos:00}</color>";
    }

    private void CrearPanelesFinDePartidaSiFaltan()
    {
        if (panelVictoria == null)
        {
            panelVictoria = CrearPanelFinJuego(
                "Panel_Victoria",
                "¡AGUA PURIFICADA! 💧🎉",
                new Color(0f, 0.95f, 0.85f, 1f),
                "¡Misión Cumplida! Todos los tubos han sido saneados exitosamente.",
                "▶ JUGAR DE NUEVO",
                out textoResumenVictoria,
                ReiniciarPartida
            );
        }

        if (panelGameOver == null)
        {
            panelGameOver = CrearPanelFinJuego(
                "Panel_GameOver",
                "¡FILTRO SATURADO! 💀",
                new Color(1f, 0.28f, 0.35f, 1f),
                "Demasiados residuos se escaparon y contaminaron el flujo del agua.",
                "🔄 REINTENTAR",
                out textoResumenGameOver,
                ReiniciarPartida
            );
        }
    }

    private GameObject CrearPanelFinJuego(
        string nombre,
        string titulo,
        Color colorTitulo,
        string mensaje,
        string textoBoton,
        out Text outTextoResumen,
        UnityEngine.Events.UnityAction accionBoton
    )
    {
        Font fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (fuente == null) fuente = Resources.GetBuiltinResource<Font>("Arial.ttf");

        // 1. Contenedor a pantalla completa
        GameObject panelObj = new GameObject(nombre);
        panelObj.transform.SetParent(transform, false);
        RectTransform rtPanel = panelObj.AddComponent<RectTransform>();
        rtPanel.anchorMin = Vector2.zero;
        rtPanel.anchorMax = Vector2.one;
        rtPanel.sizeDelta = Vector2.zero;
        rtPanel.anchoredPosition = Vector2.zero;

        // Fondo oscuro semitransparente
        Image imgFondo = panelObj.AddComponent<Image>();
        imgFondo.color = new Color(0.01f, 0.04f, 0.08f, 0.93f);

        // 2. Tarjeta / Ventana central
        GameObject tarjeta = new GameObject("Tarjeta");
        tarjeta.transform.SetParent(panelObj.transform, false);
        RectTransform rtTarjeta = tarjeta.AddComponent<RectTransform>();
        rtTarjeta.anchorMin = new Vector2(0.5f, 0.5f);
        rtTarjeta.anchorMax = new Vector2(0.5f, 0.5f);
        rtTarjeta.sizeDelta = new Vector2(620f, 480f);
        rtTarjeta.anchoredPosition = Vector2.zero;

        Image imgTarjeta = tarjeta.AddComponent<Image>();
        imgTarjeta.color = new Color(0.04f, 0.12f, 0.22f, 0.98f);

        // 3. Título
        GameObject objTitulo = new GameObject("Texto_Titulo");
        objTitulo.transform.SetParent(tarjeta.transform, false);
        RectTransform rtTitulo = objTitulo.AddComponent<RectTransform>();
        rtTitulo.anchorMin = new Vector2(0.5f, 1f);
        rtTitulo.anchorMax = new Vector2(0.5f, 1f);
        rtTitulo.pivot = new Vector2(0.5f, 1f);
        rtTitulo.anchoredPosition = new Vector2(0, -35f);
        rtTitulo.sizeDelta = new Vector2(580f, 60f);

        Text txtTitulo = objTitulo.AddComponent<Text>();
        txtTitulo.font = fuente;
        txtTitulo.fontSize = 38;
        txtTitulo.fontStyle = FontStyle.Bold;
        txtTitulo.alignment = TextAnchor.MiddleCenter;
        txtTitulo.color = colorTitulo;
        txtTitulo.text = titulo;

        // 4. Subtítulo / Mensaje
        GameObject objSub = new GameObject("Texto_Mensaje");
        objSub.transform.SetParent(tarjeta.transform, false);
        RectTransform rtSub = objSub.AddComponent<RectTransform>();
        rtSub.anchorMin = new Vector2(0.5f, 1f);
        rtSub.anchorMax = new Vector2(0.5f, 1f);
        rtSub.pivot = new Vector2(0.5f, 1f);
        rtSub.anchoredPosition = new Vector2(0, -100f);
        rtSub.sizeDelta = new Vector2(540f, 60f);

        Text txtSub = objSub.AddComponent<Text>();
        txtSub.font = fuente;
        txtSub.fontSize = 18;
        txtSub.alignment = TextAnchor.MiddleCenter;
        txtSub.color = new Color(0.75f, 0.85f, 0.95f, 0.9f);
        txtSub.text = mensaje;

        // 5. Resumen de estadísticas
        GameObject objResumen = new GameObject("Texto_Resumen");
        objResumen.transform.SetParent(tarjeta.transform, false);
        RectTransform rtRes = objResumen.AddComponent<RectTransform>();
        rtRes.anchorMin = new Vector2(0.5f, 0.5f);
        rtRes.anchorMax = new Vector2(0.5f, 0.5f);
        rtRes.pivot = new Vector2(0.5f, 0.5f);
        rtRes.anchoredPosition = new Vector2(0, -20f);
        rtRes.sizeDelta = new Vector2(520f, 150f);

        outTextoResumen = objResumen.AddComponent<Text>();
        outTextoResumen.font = fuente;
        outTextoResumen.fontSize = 22;
        outTextoResumen.fontStyle = FontStyle.Bold;
        outTextoResumen.alignment = TextAnchor.MiddleCenter;
        outTextoResumen.lineSpacing = 1.3f;
        outTextoResumen.color = Color.white;
        outTextoResumen.supportRichText = true;
        outTextoResumen.text = "Cargando resultados...";

        // 6. Botón de Acción Principal
        GameObject btnObj = new GameObject("Boton_Accion");
        btnObj.transform.SetParent(tarjeta.transform, false);
        RectTransform rtBtn = btnObj.AddComponent<RectTransform>();
        rtBtn.anchorMin = new Vector2(0.5f, 0f);
        rtBtn.anchorMax = new Vector2(0.5f, 0f);
        rtBtn.pivot = new Vector2(0.5f, 0f);
        rtBtn.anchoredPosition = new Vector2(0, 35f);
        rtBtn.sizeDelta = new Vector2(300f, 55f);

        Image imgBtn = btnObj.AddComponent<Image>();
        imgBtn.color = colorTitulo;

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = colorTitulo;
        cb.highlightedColor = Color.white;
        cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        btn.colors = cb;
        btn.onClick.AddListener(accionBoton);

        GameObject txtBtnObj = new GameObject("Texto_Boton");
        txtBtnObj.transform.SetParent(btnObj.transform, false);
        RectTransform rtTxtBtn = txtBtnObj.AddComponent<RectTransform>();
        rtTxtBtn.anchorMin = Vector2.zero;
        rtTxtBtn.anchorMax = Vector2.one;
        rtTxtBtn.sizeDelta = Vector2.zero;

        Text txtBtn = txtBtnObj.AddComponent<Text>();
        txtBtn.font = fuente;
        txtBtn.fontSize = 20;
        txtBtn.fontStyle = FontStyle.Bold;
        txtBtn.alignment = TextAnchor.MiddleCenter;
        txtBtn.color = new Color(0.02f, 0.05f, 0.1f, 1f);
        txtBtn.text = textoBoton;

        panelObj.SetActive(false);
        return panelObj;
    }
}
