using System.Collections;
using UnityEngine;

/// <summary>
/// Script que se asigna a la ESFERA (punto de generación).
/// Genera contaminantes de uno en uno con velocidades y tipos aleatorios,
/// con un intervalo aleatorio entre cada spawn.
///
/// MODO PREFAB  : Arrastra tus prefabs a prefabTypeA/B/C en el Inspector.
/// MODO FALLBACK: Deja los campos vacíos y se crearán esferas de colores automáticamente.
/// </summary>
public class ContaminantSpawner : MonoBehaviour
{
    // ── Configuración pública ──────────────────────────────────────────────
    [Header("Prefabs de contaminantes (dejar vacío para modo prototipo)")]
    [Tooltip("Prefab para TypeA (filtro Q).")]
    public GameObject prefabTypeA;

    [Tooltip("Prefab para TypeB (filtro W).")]
    public GameObject prefabTypeB;

    [Tooltip("Prefab para TypeC (filtro E).")]
    public GameObject prefabTypeC;

    [Header("Velocidad de los contaminantes")]
    [Tooltip("Velocidad mínima aleatoria")]
    [Range(0.5f, 20f)] public float minSpeed = 1.5f;

    [Tooltip("Velocidad máxima aleatoria")]
    [Range(0.5f, 20f)] public float maxSpeed = 5f;

    [Header("Intervalo entre spawns")]
    [Tooltip("Tiempo mínimo entre contaminantes (segundos)")]
    [Range(0.3f, 10f)] public float minInterval = 1.5f;

    [Tooltip("Tiempo máximo entre contaminantes (segundos)")]
    [Range(0.3f, 10f)] public float maxInterval = 4f;

    [Header("Movimiento")]
    [Tooltip("Dirección hacia la que avanzan los contaminantes (ajusta según tu escena)")]
    public Vector3 moveDirection = Vector3.forward;

    [Tooltip("Distancia máxima antes de que un contaminante se destruya automáticamente")]
    public float maxTravelDistance = 30f;

    [Header("Control del juego")]
    [Tooltip("Si está en false, el spawner no genera contaminantes")]
    public bool isSpawning = true;

    // ── Privados ───────────────────────────────────────────────────────────
    private Coroutine _spawnCoroutine;

    // Colores por tipo (se aplican en modo fallback)
    private static readonly Color[] TypeColors =
    {
        new Color(1f,   0.3f, 0.3f),  // TypeA → rojo
        new Color(0.3f, 1f,   0.3f),  // TypeB → verde
        new Color(0.3f, 0.5f, 1f)     // TypeC → azul
    };

    // ══════════════════════════════════════════════════════════════════════
    // Unity lifecycle
    // ══════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (minSpeed    > maxSpeed)    maxSpeed    = minSpeed    + 0.5f;
        if (minInterval > maxInterval) maxInterval = minInterval + 0.5f;
    }

    private void Start()
    {
        if (isSpawning)
            StartSpawning();
    }

    // ══════════════════════════════════════════════════════════════════════
    // API pública
    // ══════════════════════════════════════════════════════════════════════

    public void StartSpawning()
    {
        isSpawning = true;
        if (_spawnCoroutine != null) StopCoroutine(_spawnCoroutine);
        _spawnCoroutine = StartCoroutine(SpawnLoop());
    }

    public void StopSpawning()
    {
        isSpawning = false;
        if (_spawnCoroutine != null)
        {
            StopCoroutine(_spawnCoroutine);
            _spawnCoroutine = null;
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // Coroutine principal
    // ══════════════════════════════════════════════════════════════════════

    private IEnumerator SpawnLoop()
    {
        while (isSpawning)
        {
            float waitTime = Random.Range(minInterval, maxInterval);
            yield return new WaitForSeconds(waitTime);

            if (!isSpawning) yield break;

            SpawnOne();
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // Generación de un contaminante
    // ══════════════════════════════════════════════════════════════════════

    private void SpawnOne()
    {
        int typeIndex        = Random.Range(0, 3);
        ContaminantType type = (ContaminantType)typeIndex;

        GameObject[] prefabs = { prefabTypeA, prefabTypeB, prefabTypeC };
        GameObject   prefab  = prefabs[typeIndex];

        GameObject instance;

        if (prefab != null)
        {
            // ── Modo prefab ───────────────────────────────────────────────
            instance = Instantiate(prefab, transform.position, Quaternion.identity, null);
        }
        else
        {
            // ── Modo fallback ─────────────────────────────────────────────
            // Cada spawn crea su propia primitiva. NO hay objetos-plantilla
            // persistentes, así que no se acumulan al detener Play.
            instance               = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            instance.name          = $"Contaminant_{type}";
            instance.transform.position   = transform.position;
            instance.transform.localScale = Vector3.one * 0.4f;

            // Color: se copia el material existente de la esfera y se cambia
            // solo el color. Esto funciona en Built-in, URP y HDRP sin usar
            // Shader.Find("Standard"), que produce magenta en URP/HDRP.
            var rend = instance.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material       = new Material(rend.sharedMaterial);
                rend.material.color = TypeColors[typeIndex];
            }

            // Añadir la lógica del contaminante
            instance.AddComponent<Contaminant>();
        }

        // Configurar el componente Contaminant
        var contaminant = instance.GetComponent<Contaminant>();
        if (contaminant == null)
        {
            Debug.LogError("[ContaminantSpawner] El prefab no tiene el componente 'Contaminant'.", this);
            Destroy(instance);
            return;
        }

        contaminant.type              = type;
        contaminant.speed             = Random.Range(minSpeed, maxSpeed);
        contaminant.moveDirection     = moveDirection.normalized;
        contaminant.maxTravelDistance = maxTravelDistance;

        Debug.Log($"[ContaminantSpawner] Spawneado: {type} | Velocidad: {contaminant.speed:F2}");
    }

    // ══════════════════════════════════════════════════════════════════════
    // Gizmos
    // ══════════════════════════════════════════════════════════════════════

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, 0.6f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, moveDirection.normalized * 3f);
    }
}
