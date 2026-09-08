using UnityEngine;
using UnityEngine.InputSystem;  // ← New Input System

/// <summary>
/// Script que se asigna a cada cubo-filtro.
/// Detecta qué contaminantes están dentro de su zona (Trigger Collider)
/// y responde a la tecla asignada (Q, W o E) usando el New Input System.
///
/// REQUISITOS en el Inspector:
///   - El cubo debe tener un Collider con "Is Trigger" = true
///   - Asignar FilterKey y FilterType
/// </summary>
public class FilterZone : MonoBehaviour
{
    // ── Configuración pública ──────────────────────────────────────────────
    [Header("Identificación del filtro")]
    [Tooltip("Tecla que activa este filtro (New Input System)")]
    public Key filterKey = Key.Q;

    [Tooltip("Tipo de contaminante que este filtro elimina")]
    public ContaminantType filterType = ContaminantType.TypeA;

    [Header("Feedback visual – Colores")]
    [Tooltip("Color normal del filtro en reposo")]
    public Color idleColor    = new Color(0.8f, 0.8f, 0.8f, 0.4f);

    [Tooltip("Color cuando hay un contaminante correcto dentro")]
    public Color alertColor   = new Color(1f,   0.9f, 0.1f, 0.7f);

    [Tooltip("Color al activar correctamente")]
    public Color successColor = new Color(0.2f, 1f,   0.4f, 0.9f);

    [Tooltip("Color al activar incorrectamente / sin contaminante")]
    public Color failColor    = new Color(1f,   0.2f, 0.2f, 0.9f);

    [Tooltip("Duración del flash de éxito/fallo en segundos")]
    public float flashDuration = 0.25f;

    [Header("Feedback visual – Luces")]
    [Tooltip("Primera luz que representa el estado del filtro (asignar desde el Inspector)")]
    public Light filterLight1;

    [Tooltip("Segunda luz que representa el estado del filtro (asignar desde el Inspector)")]
    public Light filterLight2;

    [Tooltip("Intensidad de la luz en estado idle (0 = apagada)")]
    public float idleLightIntensity    = 0f;

    [Tooltip("Intensidad de la luz cuando hay un contaminante correcto dentro")]
    public float alertLightIntensity   = 2.5f;

    [Tooltip("Intensidad de la luz en el flash de éxito/fallo")]
    public float flashLightIntensity   = 5f;

    // ── Estado interno ─────────────────────────────────────────────────────
    private Contaminant _contaminantInZone = null;

    private Renderer _renderer;
    private Material _material;
    private float    _flashTimer = 0f;
    private bool     _isFlashing = false;

    // Luces
    private Light[] _lights;

    // ══════════════════════════════════════════════════════════════════════
    // Unity lifecycle
    // ══════════════════════════════════════════════════════════════════════

    private void Start()
    {
        _renderer = GetComponent<Renderer>();
        if (_renderer != null)
        {
            _material          = new Material(_renderer.material);
            _renderer.material = _material;
            SetColor(idleColor);
        }

        // Construir el array de luces con las que estén asignadas
        var lightList = new System.Collections.Generic.List<Light>();
        if (filterLight1 != null) lightList.Add(filterLight1);
        if (filterLight2 != null) lightList.Add(filterLight2);
        _lights = lightList.ToArray();

        // Estado inicial de las luces
        SetLights(idleColor, idleLightIntensity);

        var col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning($"[FilterZone] '{name}': el Collider no tiene 'Is Trigger' activado. " +
                             "Actívalo para que funcione la detección de contaminantes.", this);
        }
    }

    private void Update()
    {
        // ── Temporizador del flash visual ──────────────────────────────────
        if (_isFlashing)
        {
            _flashTimer -= Time.deltaTime;
            if (_flashTimer <= 0f)
            {
                _isFlashing = false;
                UpdateIdleColor();
            }
        }

        // ── Detectar pulsación con New Input System ────────────────────────
        // Keyboard.current puede ser null si no hay teclado conectado
        if (Keyboard.current == null) return;

        if (Keyboard.current[filterKey].wasPressedThisFrame)
        {
            ActivateFilter();
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // Detección de contaminantes mediante Trigger
    // ══════════════════════════════════════════════════════════════════════

    private void OnTriggerEnter(Collider other)
    {
        var contaminant = other.GetComponent<Contaminant>();
        if (contaminant == null || contaminant.IsFiltered || contaminant.HasEscaped) return;

        _contaminantInZone = contaminant;
        UpdateIdleColor();
    }

    private void OnTriggerExit(Collider other)
    {
        var contaminant = other.GetComponent<Contaminant>();
        if (contaminant == null) return;

        if (_contaminantInZone == contaminant)
        {
            _contaminantInZone = null;
            if (!_isFlashing)
                UpdateIdleColor();  // resetea color Y luces
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // Lógica del filtro
    // ══════════════════════════════════════════════════════════════════════

    private void ActivateFilter()
    {
        if (_contaminantInZone == null ||
            _contaminantInZone.IsFiltered ||
            _contaminantInZone.HasEscaped)
        {
            // Presionó la tecla sin contaminante válido en la zona
            Flash(failColor);
            GameManager.Instance?.OnEmptyFilterPress(this);
            return;
        }

        if (_contaminantInZone.type == filterType)
        {
            // ✅ Filtro correcto
            Flash(successColor);
            _contaminantInZone.Filter();
            _contaminantInZone = null;
        }
        else
        {
            // ❌ Tipo incorrecto
            Flash(failColor);
            _contaminantInZone.WrongFilter();
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // Feedback visual
    // ══════════════════════════════════════════════════════════════════════

    private void UpdateIdleColor()
    {
        if (_isFlashing) return;

        bool correctTypeInZone = _contaminantInZone != null &&
                                 _contaminantInZone.type == filterType;

        if (correctTypeInZone)
        {
            SetColor(alertColor);
            SetLights(alertColor, alertLightIntensity);
        }
        else
        {
            SetColor(idleColor);
            SetLights(idleColor, idleLightIntensity);
        }
    }

    private void Flash(Color color)
    {
        SetColor(color);
        SetLights(color, flashLightIntensity);
        _flashTimer = flashDuration;
        _isFlashing = true;
    }

    private void SetColor(Color color)
    {
        if (_material != null)
            _material.color = color;
    }

    /// <summary>
    /// Aplica color e intensidad a todas las luces asignadas a este filtro.
    /// </summary>
    private void SetLights(Color color, float intensity)
    {
        if (_lights == null) return;
        foreach (var light in _lights)
        {
            if (light == null) continue;
            light.color     = color;
            light.intensity = intensity;
            light.enabled   = intensity > 0f;
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // Gizmos de edición
    // ══════════════════════════════════════════════════════════════════════

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, transform.localScale);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f,
            $"[{filterKey}] → {filterType}");
#endif
    }
}
