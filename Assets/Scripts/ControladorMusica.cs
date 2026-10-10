using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ControladorMusica : MonoBehaviour
{
    [Header("Configuración de Audio")]
    [Tooltip("Clip de música de fondo que se reproducirá en bucle.")]
    public AudioClip musicaFondo;

    [Tooltip("Volumen de la música (0 = silencio, 1 = máximo).")]
    [Range(0f, 1f)]
    public float volumen = 0.5f;

    [Tooltip("Si es true, la música continuará sonando si se recarga la escena.")]
    public bool noDestruirAlCargar = false;

    private AudioSource audioSource;
    private static ControladorMusica instancia;

    void Awake()
    {
        if (noDestruirAlCargar)
        {
            if (instancia != null && instancia != this)
            {
                Destroy(gameObject);
                return;
            }
            instancia = this;
            DontDestroyOnLoad(gameObject);
        }

        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = volumen;

        if (musicaFondo != null)
        {
            audioSource.clip = musicaFondo;
        }
    }

    void Start()
    {
        if (audioSource.clip != null && !audioSource.isPlaying)
        {
            audioSource.volume = volumen;
            audioSource.Play();
        }
    }

    void Update()
    {
        // Actualiza el volumen si se modifica desde el inspector
        if (audioSource != null && audioSource.volume != volumen)
        {
            audioSource.volume = volumen;
        }
    }

    public void AjustarVolumen(float nuevoVolumen)
    {
        volumen = Mathf.Clamp01(nuevoVolumen);
        if (audioSource != null)
        {
            audioSource.volume = volumen;
        }
    }

    public void Pausar()
    {
        if (audioSource != null) audioSource.Pause();
    }

    public void Reanudar()
    {
        if (audioSource != null) audioSource.UnPause();
    }
}
