using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;  // ← New Input System
using TMPro; // Requiere TextMeshPro (viene incluido en Unity por defecto)

/// <summary>
/// Controlador central del juego.
/// Lleva la puntuación, vidas y estado global.
/// Se comunica con Contaminant, FilterZone y ContaminantSpawner.
/// 
/// Coloca este script en un GameObject vacío llamado "GameManager".
/// </summary>
public class GameManager : MonoBehaviour
{
    // ── Singleton ──────────────────────────────────────────────────────────
    public static GameManager Instance { get; private set; }

    // ── Configuración pública ──────────────────────────────────────────────
    [Header("Puntuación")]
    [Tooltip("Puntos por filtrar correctamente un contaminante")]
    public int pointsPerFilter = 100;

    [Tooltip("Puntos PERDIDOS por dejar escapar un contaminante")]
    public int penaltyEscape = 50;

    [Tooltip("Puntos PERDIDOS por presionar el filtro equivocado")]
    public int penaltyWrongFilter = 25;

    [Header("Vidas")]
    [Tooltip("Número de vidas iniciales")]
    public int maxLives = 3;

    [Header("Referencias UI (opcional — puede dejarse vacío en prototipo)")]
    public TMP_Text scoreText;
    public TMP_Text livesText;
    public TMP_Text messageText;
    public GameObject gameOverPanel;

    [Header("Referencia al Spawner")]
    public ContaminantSpawner spawner;

    // ── Estado del juego ───────────────────────────────────────────────────
    private int _score = 0;
    private int _lives;
    private bool _gameOver = false;

    // ── Temporizador de mensaje flotante ───────────────────────────────────
    private float _messageTimer = 0f;
    private const float MessageDuration = 1.2f;

    // ══════════════════════════════════════════════════════════════════════
    // Unity lifecycle
    // ══════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        // Singleton: destruir duplicados
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        _lives = maxLives;
        _gameOver = false;

        UpdateScoreUI();
        UpdateLivesUI();
        HideMessage();

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    private void Update()
    {
        // Ocultar el mensaje flotante cuando expira
        if (_messageTimer > 0f)
        {
            _messageTimer -= Time.deltaTime;
            if (_messageTimer <= 0f)
                HideMessage();
        }

        // Reiniciar con R cuando hay Game Over (New Input System)
        if (_gameOver && Keyboard.current != null && Keyboard.current[Key.R].wasPressedThisFrame)
            RestartGame();
    }

    // ══════════════════════════════════════════════════════════════════════
    // Callbacks llamados por Contaminant / FilterZone
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>Contaminante filtrado correctamente.</summary>
    public void OnContaminantFiltered(Contaminant c)
    {
        if (_gameOver) return;

        _score += pointsPerFilter;
        UpdateScoreUI();
        ShowMessage($"+{pointsPerFilter}  ✔", Color.green);
        Debug.Log($"[GameManager] Filtrado correctamente: {c.type} | Puntuación: {_score}");
    }

    /// <summary>Contaminante pasó todos los filtros sin ser detenido.</summary>
    public void OnContaminantEscaped(Contaminant c)
    {
        if (_gameOver) return;

        _score = Mathf.Max(0, _score - penaltyEscape);
        LoseLife();
        UpdateScoreUI();
        ShowMessage($"-{penaltyEscape}  ✖ Escapó", Color.red);
        Debug.Log($"[GameManager] Escapó: {c.type} | Puntuación: {_score} | Vidas: {_lives}");
    }

    /// <summary>Jugador presionó el filtro equivocado mientras había un contaminante.</summary>
    public void OnWrongFilter(Contaminant c)
    {
        if (_gameOver) return;

        _score = Mathf.Max(0, _score - penaltyWrongFilter);
        UpdateScoreUI();
        ShowMessage($"-{penaltyWrongFilter}  ✖ Filtro incorrecto", Color.yellow);
        Debug.Log($"[GameManager] Filtro incorrecto para: {c.type} | Puntuación: {_score}");
    }

    /// <summary>Jugador presionó una tecla de filtro sin que haya contaminante en la zona.</summary>
    public void OnEmptyFilterPress(FilterZone zone)
    {
        if (_gameOver) return;
        // Opcional: puedes añadir penalización aquí
        Debug.Log($"[GameManager] Tecla {zone.filterKey} presionada en zona vacía.");
    }

    // ══════════════════════════════════════════════════════════════════════
    // Vidas y Game Over
    // ══════════════════════════════════════════════════════════════════════

    private void LoseLife()
    {
        _lives--;
        UpdateLivesUI();

        if (_lives <= 0)
            TriggerGameOver();
    }

    private void TriggerGameOver()
    {
        _gameOver = true;
        spawner?.StopSpawning();

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        ShowMessage("GAME OVER  —  Pulsa R para reiniciar", Color.red);
        Debug.Log($"[GameManager] GAME OVER | Puntuación final: {_score}");
    }

    private void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    // ══════════════════════════════════════════════════════════════════════
    // UI helpers
    // ══════════════════════════════════════════════════════════════════════

    private void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = $"Puntos: {_score}";
    }

    private void UpdateLivesUI()
    {
        if (livesText != null)
            livesText.text = $"Vidas: {_lives}";
    }

    private void ShowMessage(string msg, Color color)
    {
        if (messageText != null)
        {
            messageText.text = msg;
            messageText.color = color;
            messageText.gameObject.SetActive(true);
        }
        _messageTimer = MessageDuration;
    }

    private void HideMessage()
    {
        if (messageText != null)
            messageText.gameObject.SetActive(false);
    }

    // ══════════════════════════════════════════════════════════════════════
    // Propiedades de lectura
    // ══════════════════════════════════════════════════════════════════════

    public int Score => _score;
    public int Lives => _lives;
    public bool IsGameOver => _gameOver;
}
