using UnityEngine;

public class SpawnBasura : MonoBehaviour
{
    [Header("Configuración de Spawn")]
    [Tooltip("El prefab que se va a generar.")]
    public GameObject prefabBasura;
    
    [Tooltip("Probabilidad de que aparezca basura en cada intento (0 a 1). 0.8 = 80%")]
    [Range(0f, 1f)]
    public float probabilidadDeSpawn = 0.8f;

    [Tooltip("Intervalo mínimo de tiempo en segundos entre cada intento de este spawner.")]
    public float intervaloMinimo = 1.4f;
    [Tooltip("Intervalo máximo de tiempo en segundos entre cada intento de este spawner.")]
    public float intervaloMaximo = 2.4f;

    [Header("Control de Ritmo y Dificultad")]
    [Tooltip("Tiempo mínimo global en segundos entre CUALQUIER spawn (evita que varios tubos disparen a la vez).")]
    public float tiempoMinimoEntreCualquierSpawn = 0.6f;

    [Tooltip("Cantidad máxima de basuras vivas al mismo tiempo en el juego (0 = sin límite).")]
    public int maxBasurasEnPantalla = 6;

    [Tooltip("Segundos de gracia al iniciar antes del primer spawn para dar tiempo al jugador.")]
    public float tiempoEsperaInicial = 0.8f;

    [Header("Movimiento")]
    [Tooltip("Dirección hacia donde saldrán disparados los objetos.")]
    public Vector3 direccionDisparo = new Vector3(0, 0, 1);
    
    [Tooltip("Velocidad mínima a la que saldrá el objeto.")]
    public float velocidadMinima = 12f;
    [Tooltip("Velocidad máxima a la que saldrá el objeto.")]
    public float velocidadMaxima = 16f;

    // Control global estático compartido por todos los spawners
    private static float tiempoUltimoSpawnGlobal = -999f;

    private float timer;
    private float siguienteIntervalo;

    public static void ReiniciarCadenciaGlobal()
    {
        tiempoUltimoSpawnGlobal = -999f;
    }

    void Start()
    {
        // Dar tiempo inicial mínimo de gracia
        timer = -tiempoEsperaInicial;
        CalcularSiguienteIntervalo();
    }

    void Update()
    {
        // No generar basura si el juego está en pausa o ya finalizó
        if (Time.timeScale <= 0f || (ControladorPuntuacion.Instancia != null && ControladorPuntuacion.Instancia.JuegoTerminado)) return;

        timer += Time.deltaTime;

        // Si ya pasó el tiempo del intervalo
        if (timer >= siguienteIntervalo)
        {
            IntentarSpawn();
            
            // Reiniciamos el timer y calculamos el tiempo para el siguiente intento
            timer = 0;
            CalcularSiguienteIntervalo();
        }
    }

    private float ObtenerFactorVelocidad()
    {
        return ControladorPuntuacion.Instancia != null ? ControladorPuntuacion.Instancia.MultiplicadorVelocidad : 1.0f;
    }

    void CalcularSiguienteIntervalo()
    {
        float factor = ObtenerFactorVelocidad();

        // Conforme sube el tiempo/velocidad, los intervalos entre spawns se hacen aún más frenéticos
        float minInt = Mathf.Max(0.45f, intervaloMinimo / factor);
        float maxInt = Mathf.Max(0.85f, intervaloMaximo / factor);

        siguienteIntervalo = Random.Range(minInt, maxInt);
    }

    void IntentarSpawn()
    {
        float factor = ObtenerFactorVelocidad();

        // 1. Verificación de cadencia global rápida
        float cadenciaGlobalActual = Mathf.Max(0.2f, tiempoMinimoEntreCualquierSpawn / factor);
        if (Time.time - tiempoUltimoSpawnGlobal < cadenciaGlobalActual)
        {
            timer = siguienteIntervalo * 0.25f;
            return;
        }

        // 2. Verificación de límite de basuras vivas en pantalla
        if (maxBasurasEnPantalla > 0)
        {
            int maxActual = maxBasurasEnPantalla + Mathf.FloorToInt((factor - 1f) * 3f);
            int basurasActivas = FindObjectsByType<MovimientoBasura>().Length;
            if (basurasActivas >= maxActual)
            {
                timer = siguienteIntervalo * 0.3f;
                return;
            }
        }

        // 3. Verificación de probabilidad (aumenta sutilmente con la velocidad)
        float probActual = Mathf.Clamp01(probabilidadDeSpawn + ((factor - 1f) * 0.15f));
        if (Random.value <= probActual)
        {
            Spawnear();
        }
    }

    void Spawnear()
    {
        if (prefabBasura == null)
        {
            Debug.LogWarning("No has asignado el prefab de la basura en el componente SpawnBasura de " + gameObject.name);
            return;
        }

        // Registrar tiempo global para evitar que otro tubo dispare inmediatamente
        tiempoUltimoSpawnGlobal = Time.time;

        // Instanciar el objeto en la posición de este spawner
        GameObject nuevaBasura = Instantiate(prefabBasura, transform.position, transform.rotation);

        // Añadir el script de movimiento (si el prefab no lo tiene ya)
        MovimientoBasura mov = nuevaBasura.GetComponent<MovimientoBasura>();
        if (mov == null)
        {
            mov = nuevaBasura.AddComponent<MovimientoBasura>();
        }

        // Asignar los valores de dirección y velocidad escalados por la dificultad actual
        float factor = ObtenerFactorVelocidad();
        float velBase = Random.Range(velocidadMinima, velocidadMaxima);
        mov.direccion = direccionDisparo;
        mov.velocidad = velBase * factor;

        // Destruir el objeto después de un tiempo por si escapa
        Destroy(nuevaBasura, 15f);
    }
}
