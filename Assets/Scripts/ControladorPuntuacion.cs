using System;
using UnityEngine;

public class ControladorPuntuacion : MonoBehaviour
{
    public static ControladorPuntuacion Instancia { get; private set; }

    [Header("Configuración de Puntos")]
    [Tooltip("Puntos que se otorgan al destruir correctamente una basura.")]
    public int puntosPorBasura = 100;

    [Tooltip("Puntos que se descuentan al presionar una tecla sin haber basura en el área.")]
    public int penalizacionFallo = 25;

    [Header("Vidas y Reglas de la Partida")]
    [Tooltip("Cantidad máxima de vidas (basuras que se pueden escapar antes de perder).")]
    public int vidasMaximas = 3;

    [Tooltip("Cantidad de basuras que el jugador debe recolectar para ganar la partida.")]
    public int metaBasurasParaGanar = 20;

    [Header("Dificultad y Velocidad Dinámica")]
    [Tooltip("Multiplicador de velocidad base al inicio de la partida.")]
    public float velocidadBase = 1.0f;
    [Tooltip("Multiplicador de velocidad máximo alcanzable.")]
    public float velocidadMaxima = 1.75f;
    [Tooltip("Tiempo en segundos de juego continuo necesario para alcanzar la velocidad máxima.")]
    public float tiempoParaVelocidadMaxima = 90f;

    [Header("Estado Actual")]
    [SerializeField] private int puntuacionActual = 0;
    [SerializeField] private int mejorPuntuacion = 0;
    [SerializeField] private int comboActual = 0;
    [SerializeField] private int comboMaximo = 0;
    [SerializeField] private int multiplicador = 1;
    [SerializeField] private int basurasRecicladas = 0;
    [SerializeField] private int vidasActuales = 3;
    [SerializeField] private bool juegoTerminado = false;
    [SerializeField] private bool juegoGanado = false;
    [SerializeField] private float tiempoTranscurrido = 0f;
    [SerializeField] private float multiplicadorVelocidadActual = 1.0f;

    public int PuntuacionActual => puntuacionActual;
    public int MejorPuntuacion => mejorPuntuacion;
    public int ComboActual => comboActual;
    public int ComboMaximo => comboMaximo;
    public int Multiplicador => multiplicador;
    public int BasurasRecicladas => basurasRecicladas;
    public int VidasActuales => vidasActuales;
    public int VidasMaximas => vidasMaximas;
    public int MetaBasurasParaGanar => metaBasurasParaGanar;
    public bool JuegoTerminado => juegoTerminado;
    public bool JuegoGanado => juegoGanado;
    public float TiempoTranscurrido => tiempoTranscurrido;
    public float MultiplicadorVelocidad => multiplicadorVelocidadActual;

    // Eventos para la interfaz de usuario
    public event Action<int, int, int, int> EnPuntuacionActualizada;
    public event Action<int, int, int, int, int, int, int, int> EnEstadoJuegoActualizado;
    public event Action EnVictoria;
    public event Action EnGameOver;

    private const string PREF_KEY_HIGH_SCORE = "FilterRush_HighScore";

    void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        Instancia = this;

        vidasActuales = vidasMaximas;
        multiplicadorVelocidadActual = velocidadBase;
        mejorPuntuacion = PlayerPrefs.GetInt(PREF_KEY_HIGH_SCORE, 0);
    }

    void Start()
    {
        NotificarCambios();
    }

    void Update()
    {
        if (juegoTerminado || Time.timeScale <= 0f) return;

        tiempoTranscurrido += Time.deltaTime;

        // Progresión continua en base al tiempo y a las basuras recicladas con éxito
        float progresoTiempo = Mathf.Clamp01(tiempoTranscurrido / Mathf.Max(15f, tiempoParaVelocidadMaxima));
        float progresoReciclaje = Mathf.Clamp01((float)basurasRecicladas / Mathf.Max(1, metaBasurasParaGanar));
        
        // El factor combina principalmente el tiempo transcurrido (75%) más el progreso de reciclaje (25%)
        float factorCombinado = Mathf.Clamp01((progresoTiempo * 0.75f) + (progresoReciclaje * 0.25f));
        multiplicadorVelocidadActual = Mathf.Lerp(velocidadBase, velocidadMaxima, factorCombinado);
    }

    public void SumarAcierto(int puntosPersonalizados = -1, int bonoCombo = 0)
    {
        if (juegoTerminado) return;

        comboActual += (1 + bonoCombo);
        if (comboActual > comboMaximo)
        {
            comboMaximo = comboActual;
        }

        basurasRecicladas++;

        // Multiplicador aumenta cada 5 aciertos continuos (1x -> 2x -> 3x -> 4x max)
        multiplicador = Mathf.Clamp(1 + (comboActual / 5), 1, 4);

        int ptsOtorgados = puntosPersonalizados > 0 ? puntosPersonalizados : puntosPorBasura;
        puntuacionActual += ptsOtorgados * multiplicador;

        if (puntuacionActual > mejorPuntuacion)
        {
            mejorPuntuacion = puntuacionActual;
            PlayerPrefs.SetInt(PREF_KEY_HIGH_SCORE, mejorPuntuacion);
            PlayerPrefs.Save();
        }

        NotificarCambios();

        // Verificar condición de victoria
        if (basurasRecicladas >= metaBasurasParaGanar)
        {
            juegoTerminado = true;
            juegoGanado = true;
            EnVictoria?.Invoke();
        }
    }

    public void RegistrarFallo()
    {
        if (juegoTerminado) return;

        comboActual = 0;
        multiplicador = 1;
        puntuacionActual = Mathf.Max(0, puntuacionActual - penalizacionFallo);

        NotificarCambios();
    }

    public void RegistrarBasuraEscapada()
    {
        if (juegoTerminado) return;

        comboActual = 0;
        multiplicador = 1;
        vidasActuales = Mathf.Max(0, vidasActuales - 1);

        NotificarCambios();

        // Verificar condición de derrota
        if (vidasActuales <= 0)
        {
            juegoTerminado = true;
            juegoGanado = false;
            EnGameOver?.Invoke();
        }
    }

    public void RomperCombo()
    {
        comboActual = 0;
        multiplicador = 1;

        NotificarCambios();
    }

    public void ReiniciarPuntuacion()
    {
        puntuacionActual = 0;
        comboActual = 0;
        comboMaximo = 0;
        multiplicador = 1;
        basurasRecicladas = 0;
        vidasActuales = vidasMaximas;
        juegoTerminado = false;
        juegoGanado = false;
        tiempoTranscurrido = 0f;
        multiplicadorVelocidadActual = velocidadBase;

        SpawnBasura.ReiniciarCadenciaGlobal();

        NotificarCambios();
    }

    public void BorrarRecord()
    {
        mejorPuntuacion = 0;
        PlayerPrefs.DeleteKey(PREF_KEY_HIGH_SCORE);
        PlayerPrefs.Save();

        NotificarCambios();
    }

    private void NotificarCambios()
    {
        EnPuntuacionActualizada?.Invoke(puntuacionActual, mejorPuntuacion, comboActual, multiplicador);
        EnEstadoJuegoActualizado?.Invoke(
            puntuacionActual,
            mejorPuntuacion,
            comboActual,
            multiplicador,
            vidasActuales,
            vidasMaximas,
            basurasRecicladas,
            metaBasurasParaGanar
        );
    }
}
