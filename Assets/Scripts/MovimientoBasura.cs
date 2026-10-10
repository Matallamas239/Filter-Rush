using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public enum TipoBasura
{
    Normal,
    Turbo,
    Dorada
}

public class MovimientoBasura : MonoBehaviour
{
    [Header("Tipo y Recompensa")]
    public TipoBasura tipo = TipoBasura.Normal;
    public int puntosExtra = 0;
    public int bonoCombo = 0;
    public Color colorParticulas = new Color(0.15f, 0.85f, 1f, 1f);

    [Header("Cinemática y Flotabilidad")]
    [HideInInspector] public Vector3 direccion = new Vector3(0, 0, 1);
    [HideInInspector] public float velocidad = 7f;

    [Tooltip("Frecuencia del bamboleo de agua.")]
    public float frecuenciaBamboleo = 5.0f;
    [Tooltip("Amplitud de la oscilación en el agua.")]
    public float amplitudBamboleo = 0.07f;
    [Tooltip("Velocidad de rotación continua sobre su propio eje.")]
    public float velocidadGiroEje = 90f;

    private Rigidbody rb;
    private Renderer rendVisual;
    private Vector3 posicionVisualOriginal;
    private KillArea killAreaActual;
    private bool enAreaReciclaje = false;
    private float desfaseTiempo;
    private Vector3 escalaOriginal;
    private float tiempoDeVida = 0f;
    private bool interactuado = false;

    public bool EnAreaReciclaje => enAreaReciclaje;
    public KillArea AreaActual => killAreaActual;

    void Awake()
    {
        Physics.autoSyncTransforms = true;
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        desfaseTiempo = Random.Range(0f, 10f);
        escalaOriginal = transform.localScale;

        // Buscar renderer visual en este objeto o en sus hijos
        rendVisual = GetComponentInChildren<Renderer>();
        if (rendVisual != null)
        {
            posicionVisualOriginal = rendVisual.transform.localPosition;
        }
    }

    void Start()
    {
        ConfigurarTipoBasura();
        StartCoroutine(AnimacionSpawnPop());
    }

    void Update()
    {
        if (Time.timeScale <= 0f) return;

        tiempoDeVida += Time.deltaTime;

        // Feedback visual interactivo cuando está dentro del área de reciclaje
        if (enAreaReciclaje)
        {
            // Pulso vibrante rápido para avisar al jugador que está lista para ser destruida
            float escalaPulso = 1f + Mathf.Sin(Time.time * 18f) * 0.12f;
            transform.localScale = escalaOriginal * escalaPulso;
        }

        // Detección de clic directo con el mouse sobre esta basurita
        DetectarClicJugador();
    }

    void FixedUpdate()
    {
        if (interactuado) return;

        // 1. Movimiento rectilíneo principal veloz en dirección del tubo hacia el jugador
        Vector3 dirNorm = direccion.sqrMagnitude > 0.001f ? direccion.normalized : Vector3.forward;
        Vector3 avance = dirNorm * (velocidad * Time.fixedDeltaTime);

        if (rb != null)
        {
            rb.MovePosition(rb.position + avance);
        }
        else
        {
            transform.position += avance;
        }

        // 2. Ondulación senoidal estética en el hijo visual (sin frenar ni desviar la física)
        if (rendVisual != null)
        {
            float ondaVertical = Mathf.Sin((Time.time * frecuenciaBamboleo) + desfaseTiempo) * amplitudBamboleo;
            float ondaLateral = Mathf.Cos((Time.time * (frecuenciaBamboleo * 0.7f)) + desfaseTiempo) * (amplitudBamboleo * 0.6f);
            rendVisual.transform.localPosition = posicionVisualOriginal + new Vector3(ondaLateral, ondaVertical, 0f);
        }

        // 3. Rotación tridimensional continua sobre su eje
        float anguloGiro = velocidadGiroEje * Time.fixedDeltaTime;
        Quaternion rotacionGiro = Quaternion.AngleAxis(anguloGiro, dirNorm);

        if (rb != null)
        {
            rb.MoveRotation(rotacionGiro * rb.rotation);
        }
        else
        {
            transform.rotation = rotacionGiro * transform.rotation;
        }
    }

    /// <summary>
    /// Configura aleatoriamente variantes interactivas de basuritas (Normal, Turbo, Dorada).
    /// </summary>
    private void ConfigurarTipoBasura()
    {
        float suerte = Random.value;

        if (suerte < 0.12f)
        {
            // 12% Basura Dorada (Ultra bonus)
            tipo = TipoBasura.Dorada;
            puntosExtra = 150;
            bonoCombo = 1;
            colorParticulas = new Color(1f, 0.88f, 0.2f, 1f); // Oro reluciente
            velocidad *= 1.15f;
            AplicarTinteVisual(new Color(1f, 0.85f, 0.15f, 1f), true);
        }
        else if (suerte < 0.35f)
        {
            // 23% Basura Turbo (Veloz / Agua Viva)
            tipo = TipoBasura.Turbo;
            puntosExtra = 50;
            bonoCombo = 0;
            colorParticulas = new Color(0.1f, 0.95f, 1f, 1f); // Cian eléctrico
            velocidad *= 1.30f; // Va notablemente más rápida
            AplicarTinteVisual(new Color(0.15f, 0.9f, 1f, 1f), false);
        }
        else
        {
            // 65% Basura Normal (Plástico clásico)
            tipo = TipoBasura.Normal;
            puntosExtra = 0;
            bonoCombo = 0;
            colorParticulas = new Color(0.2f, 0.75f, 1f, 1f);
        }
    }

    private void AplicarTinteVisual(Color colorTinte, bool agregarLuz)
    {
        if (rendVisual != null)
        {
            Material mat = rendVisual.material;
            if (mat != null)
            {
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", colorTinte);
                else if (mat.HasProperty("_Color")) mat.SetColor("_Color", colorTinte);

                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", colorTinte * 0.7f);
                }
            }
        }

        if (agregarLuz)
        {
            GameObject luzObj = new GameObject("Luz_BasuraEspecial");
            luzObj.transform.SetParent(transform, false);
            luzObj.transform.localPosition = Vector3.zero;

            Light luz = luzObj.AddComponent<Light>();
            luz.type = LightType.Point;
            luz.color = colorTinte;
            luz.range = 3.5f;
            luz.intensity = 2.5f;
        }
    }

    /// <summary>
    /// Animación elástica procedural al salir expulsada por la presión del tubo.
    /// </summary>
    private IEnumerator AnimacionSpawnPop()
    {
        float duracion = 0.28f;
        float elapsed = 0f;

        while (elapsed < duracion)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duracion;
            // Efecto Squash & Stretch elástico
            float escala = Mathf.LerpUnclamped(0.4f, 1.0f, Mathf.Sin(t * Mathf.PI * 0.7f) * 1.15f);
            if (!enAreaReciclaje)
            {
                transform.localScale = escalaOriginal * Mathf.Clamp(escala, 0.3f, 1.25f);
            }
            yield return null;
        }

        if (!enAreaReciclaje)
        {
            transform.localScale = escalaOriginal;
        }
    }

    /// <summary>
    /// Permite al jugador interactuar haciendo clic directamente con el ratón sobre la basurita.
    /// </summary>
    private void DetectarClicJugador()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
        if (Camera.main == null) return;

        Ray rayo = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(rayo, out RaycastHit hit, 150f))
        {
            // Comprobar si el clic impactó este objeto o a alguno de sus colliders
            if (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform) || (hit.rigidbody != null && hit.rigidbody.gameObject == gameObject))
            {
                OnClicDirecto();
            }
        }
    }

    private void OnClicDirecto()
    {
        if (interactuado) return;

        if (enAreaReciclaje && killAreaActual != null)
        {
            interactuado = true;
            killAreaActual.ProcesarDestruccionExterna(gameObject);
        }
        else
        {
            // Hizo clic antes de tiempo: sacudida interactiva de feedback
            StartCoroutine(SacudidaFalloVisual());
        }
    }

    private IEnumerator SacudidaFalloVisual()
    {
        Vector3 escalaInicial = transform.localScale;
        float duracion = 0.15f;
        float elapsed = 0f;

        while (elapsed < duracion)
        {
            elapsed += Time.deltaTime;
            float factor = Mathf.Sin(elapsed * 45f) * 0.15f;
            transform.localScale = escalaInicial + new Vector3(factor, -factor, factor);
            yield return null;
        }

        transform.localScale = escalaInicial;
    }

    public void NotificarEntradaArea(KillArea area)
    {
        enAreaReciclaje = true;
        killAreaActual = area;
    }

    public void NotificarSalidaArea(KillArea area)
    {
        if (killAreaActual == area)
        {
            enAreaReciclaje = false;
            killAreaActual = null;
            transform.localScale = escalaOriginal;
        }
    }
}
