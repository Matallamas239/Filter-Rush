using UnityEngine;

/// <summary>
/// Script que se asigna a cada objeto contaminante.
/// Se mueve en línea recta desde el punto de generación hasta el final de la tubería.
/// Notifica al GameManager si pasa el filtro sin ser detectado.
/// </summary>
public class Contaminant : MonoBehaviour
{
    // ── Configuración pública ──────────────────────────────────────────────
    [Header("Tipo y Movimiento")]
    [Tooltip("Tipo de contaminante (determina qué filtro lo elimina)")]
    public ContaminantType type;

    [Tooltip("Velocidad de desplazamiento")]
    public float speed = 3f;

    [Tooltip("Dirección de movimiento (se normaliza automáticamente)")]
    public Vector3 moveDirection = Vector3.forward;

    [Header("Distancia máxima antes de considerarse 'escapado'")]
    [Tooltip("Si supera esta distancia del origen, se destruye y penaliza")]
    public float maxTravelDistance = 30f;

    // ── Estado interno ─────────────────────────────────────────────────────
    private Vector3 _spawnPosition;
    private bool _filtered = false;   // true = ya fue eliminado correctamente
    private bool _escaped = false;    // true = ya pasó todos los filtros sin eliminarse

    // Referencia al GameManager (se busca en Start)
    private GameManager _gameManager;

    // ── Color visual por tipo ──────────────────────────────────────────────
    private static readonly Color[] TypeColors =
    {
        new Color(1f, 0.3f, 0.3f),   // TypeA → rojo
        new Color(0.3f, 1f, 0.3f),   // TypeB → verde
        new Color(0.3f, 0.5f, 1f)    // TypeC → azul
    };

    // ══════════════════════════════════════════════════════════════════════
    // Unity lifecycle
    // ══════════════════════════════════════════════════════════════════════

    private void Start()
    {
        _spawnPosition = transform.position;
        _gameManager = GameManager.Instance;
        moveDirection = moveDirection.normalized;

        // ── Garantizar Rigidbody kinematic ────────────────────────────────
        // OBLIGATORIO para que OnTriggerEnter funcione en los cubos-filtro:
        // Unity requiere que AL MENOS UNO de los dos objetos tenga Rigidbody.
        // Usamos isKinematic = true para que el motor de física no mueva
        // el objeto y dejamos el movimiento a transform.Translate.
        var rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>(); // crear si no existe
        rb.useGravity  = false;
        rb.isKinematic = true;

        // Colorear el objeto según su tipo para feedback visual inmediato
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = new Material(renderer.material); // instancia propia
            renderer.material.color = TypeColors[(int)type];
        }
    }

    private void Update()
    {
        if (_filtered || _escaped) return;

        // Mover el contaminante
        transform.Translate(moveDirection * speed * Time.deltaTime, Space.World);

        // Comprobar si escapó (superó la distancia máxima)
        float distanceTraveled = Vector3.Distance(transform.position, _spawnPosition);
        if (distanceTraveled >= maxTravelDistance)
        {
            OnEscape();
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // Métodos públicos llamados desde FilterZone / GameManager
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Llamado por FilterZone cuando el jugador presiona la tecla correcta
    /// mientras este contaminante está dentro del área del filtro.
    /// </summary>
    public void Filter()
    {
        if (_filtered || _escaped) return;

        _filtered = true;
        _gameManager?.OnContaminantFiltered(this);
        Destroy(gameObject);
    }

    /// <summary>
    /// Llamado por FilterZone cuando el jugador presiona la tecla EQUIVOCADA
    /// mientras este contaminante está en la zona.
    /// </summary>
    public void WrongFilter()
    {
        if (_filtered || _escaped) return;

        _gameManager?.OnWrongFilter(this);
        // El contaminante NO se destruye; sigue moviéndose
    }

    // ══════════════════════════════════════════════════════════════════════
    // Privados
    // ══════════════════════════════════════════════════════════════════════

    private void OnEscape()
    {
        _escaped = true;
        _gameManager?.OnContaminantEscaped(this);
        Destroy(gameObject);
    }

    // ══════════════════════════════════════════════════════════════════════
    // Getters
    // ══════════════════════════════════════════════════════════════════════

    public bool IsFiltered => _filtered;
    public bool HasEscaped => _escaped;
}
