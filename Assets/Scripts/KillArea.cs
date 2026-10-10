using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class KillArea : MonoBehaviour
{
    [Header("Configuración de Control")]
    [Tooltip("La tecla que el jugador debe presionar para destruir la basura en esta área (Q, W, E, R).")]
    public Key teclaParaDestruir = Key.Q;

    [Header("Efectos Visuales")]
    [Tooltip("Prefab de explosión personalizado opcional. Si está vacío, se genera una explosión procedural.")]
    public GameObject prefabExplosion;

    [Header("Efectos de Audio")]
    public AudioClip clipAcierto;
    public AudioClip clipFallo;

    private AudioSource audioSource;
    private BoxCollider boxCollider;
    private List<GameObject> basurasEnArea = new List<GameObject>();

    void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D sound
        }
    }

    void Update()
    {
        // No procesar inputs si el juego está en pausa o ya terminó la partida
        if (Time.timeScale <= 0f || (ControladorPuntuacion.Instancia != null && ControladorPuntuacion.Instancia.JuegoTerminado)) return;

        // 1. Detección por teclado (Q, W, E, R)
        if (Keyboard.current != null && Keyboard.current[teclaParaDestruir].wasPressedThisFrame)
        {
            IntentarDestruirPorInput();
        }

        // 2. Detección interactiva por clic de ratón sobre el área del tubo
        DetectarClicEnArea();
    }

    private void IntentarDestruirPorInput()
    {
        // Limpiamos referencias nulas
        basurasEnArea.RemoveAll(item => item == null);

        // Respaldo de detección: si no se detectó por trigger pero hay basura en los límites de este collider
        if (basurasEnArea.Count == 0 && boxCollider != null)
        {
            Collider[] hits = Physics.OverlapBox(boxCollider.bounds.center, boxCollider.bounds.extents * 1.15f, transform.rotation);
            foreach (var hit in hits)
            {
                MovimientoBasura mov = hit.GetComponentInParent<MovimientoBasura>();
                if (mov == null && hit.attachedRigidbody != null)
                {
                    mov = hit.attachedRigidbody.GetComponent<MovimientoBasura>();
                }

                if (mov != null && !basurasEnArea.Contains(mov.gameObject))
                {
                    basurasEnArea.Add(mov.gameObject);
                    mov.NotificarEntradaArea(this);
                }
            }
        }

        if (basurasEnArea.Count > 0)
        {
            GameObject basuraADestruir = basurasEnArea[0];
            basurasEnArea.RemoveAt(0);
            EjecutarDestruccion(basuraADestruir);
        }
        else
        {
            PenalizarFallo();
        }
    }

    private void DetectarClicEnArea()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
        if (Camera.main == null || boxCollider == null) return;

        Ray rayo = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (boxCollider.Raycast(rayo, out RaycastHit hit, 150f))
        {
            basurasEnArea.RemoveAll(item => item == null);
            if (basurasEnArea.Count > 0)
            {
                GameObject basuraADestruir = basurasEnArea[0];
                basurasEnArea.RemoveAt(0);
                EjecutarDestruccion(basuraADestruir);
            }
        }
    }

    /// <summary>
    /// Llamado cuando el jugador interactúa directamente haciendo clic sobre la basurita.
    /// </summary>
    public void ProcesarDestruccionExterna(GameObject basuraADestruir)
    {
        if (Time.timeScale <= 0f || (ControladorPuntuacion.Instancia != null && ControladorPuntuacion.Instancia.JuegoTerminado)) return;

        basurasEnArea.RemoveAll(item => item == null);
        if (basurasEnArea.Contains(basuraADestruir))
        {
            basurasEnArea.Remove(basuraADestruir);
            EjecutarDestruccion(basuraADestruir);
        }
    }

    private void EjecutarDestruccion(GameObject basuraADestruir)
    {
        if (basuraADestruir == null) return;

        MovimientoBasura mov = basuraADestruir.GetComponent<MovimientoBasura>();

        // Obtener la posición visual exacta de la basura
        Vector3 posExplosion = basuraADestruir.transform.position;
        Renderer rend = basuraADestruir.GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            posExplosion = rend.bounds.center;
        }

        // Generar explosión de partículas adaptada al color del tipo de basura
        Color colorExplosion = mov != null ? mov.colorParticulas : new Color(0.15f, 0.85f, 1f, 1f);
        if (prefabExplosion != null)
        {
            Instantiate(prefabExplosion, posExplosion, Quaternion.identity);
        }
        else
        {
            EfectoExplosion.Explotar(posExplosion, colorExplosion);
        }

        // Destruir el objeto de basura
        Destroy(basuraADestruir);

        // Sumar puntos con bonos según la variante interactiva de la basura
        if (ControladorPuntuacion.Instancia != null)
        {
            int ptsBase = (mov != null && mov.puntosExtra > 0) 
                ? (ControladorPuntuacion.Instancia.puntosPorBasura + mov.puntosExtra) 
                : -1;
            int bonoCombo = mov != null ? mov.bonoCombo : 0;
            ControladorPuntuacion.Instancia.SumarAcierto(ptsBase, bonoCombo);
        }

        // Sonido de acierto dinámico
        if (clipAcierto != null && audioSource != null)
        {
            float pitchAcierto = (mov != null && mov.tipo == TipoBasura.Dorada) ? 1.35f : Random.Range(0.95f, 1.15f);
            audioSource.pitch = pitchAcierto;
            audioSource.PlayOneShot(clipAcierto, 0.85f);
        }
    }

    private void PenalizarFallo()
    {
        if (ControladorPuntuacion.Instancia != null)
        {
            ControladorPuntuacion.Instancia.RegistrarFallo();
        }

        if (clipFallo != null && audioSource != null)
        {
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(clipFallo, 0.5f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        MovimientoBasura mov = other.GetComponentInParent<MovimientoBasura>();
        if (mov == null && other.attachedRigidbody != null)
        {
            mov = other.attachedRigidbody.GetComponent<MovimientoBasura>();
        }

        if (mov != null)
        {
            if (!basurasEnArea.Contains(mov.gameObject))
            {
                basurasEnArea.Add(mov.gameObject);
                mov.NotificarEntradaArea(this);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        MovimientoBasura mov = other.GetComponentInParent<MovimientoBasura>();
        if (mov == null && other.attachedRigidbody != null)
        {
            mov = other.attachedRigidbody.GetComponent<MovimientoBasura>();
        }

        if (mov != null)
        {
            mov.NotificarSalidaArea(this);
        }

        GameObject obj = mov != null ? mov.gameObject : other.gameObject;
        if (basurasEnArea.Contains(obj))
        {
            basurasEnArea.Remove(obj);

            // Una basura se escapó sin ser destruida: Romper combo y restar vida
            if (ControladorPuntuacion.Instancia != null)
            {
                ControladorPuntuacion.Instancia.RegistrarBasuraEscapada();
            }
        }
    }
}
