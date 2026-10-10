using UnityEngine;

public class FlujoAgua : MonoBehaviour
{
    [Header("Configuración del Flujo")]
    [Tooltip("Velocidad a la que se mueve el agua en los ejes X y Y (usualmente solo necesitas mover uno).")]
    public Vector2 velocidadFlujo = new Vector2(0f, 0.5f);
    
    private Material materialAgua;
    private Vector2 offsetActual;

    void Start()
    {
        // Obtenemos el material del objeto
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            // Usamos material (en vez de sharedMaterial) para que solo afecte a este objeto
            materialAgua = rend.material; 
        }
        else
        {
            Debug.LogWarning("El script FlujoAgua necesita estar en un objeto con un MeshRenderer.");
        }
    }

    void Update()
    {
        if (materialAgua != null)
        {
            float factorVelocidad = ControladorPuntuacion.Instancia != null 
                ? ControladorPuntuacion.Instancia.MultiplicadorVelocidad 
                : 1f;

            // Calculamos el nuevo desplazamiento (offset) basado en el tiempo y la velocidad dinámica
            offsetActual += (velocidadFlujo * factorVelocidad) * Time.deltaTime;

            // En Universal Render Pipeline (URP), la textura principal se llama "_BaseMap"
            if (materialAgua.HasProperty("_BaseMap"))
            {
                materialAgua.SetTextureOffset("_BaseMap", offsetActual);
            }
            // Mantenemos "_MainTex" por si acaso cambias a un material estándar de Unity
            else if (materialAgua.HasProperty("_MainTex"))
            {
                materialAgua.SetTextureOffset("_MainTex", offsetActual);
            }
            
            // Si el shader usa Normal Map (Bump Map), también lo movemos
            if (materialAgua.HasProperty("_BumpMap"))
            {
                materialAgua.SetTextureOffset("_BumpMap", offsetActual);
            }
        }
    }
}
