using System.Collections.Generic;
using UnityEngine;

public class ScanerLuces : MonoBehaviour
{
    [Header("Luces del Escáner")]
    [Tooltip("Arrastra aquí la primera luz Point Light")]
    public Light luz1;
    [Tooltip("Arrastra aquí la segunda luz Point Light")]
    public Light luz2;

    [Header("Configuración de Iluminación")]
    [Tooltip("La intensidad que tendrán las luces cuando se enciendan.")]
    public float intensidadLuz = 2f;
    [Tooltip("El rango (tamaño) que alcanzarán las luces.")]
    public float tamanoLuz = 10f;

    // Guardamos una lista para saber cuánta basura hay adentro en un momento dado
    private List<GameObject> basurasEnArea = new List<GameObject>();

    void Start()
    {
        // Configuramos la intensidad y tamaño, y apagamos las luces al iniciar el juego
        if (luz1 != null) 
        {
            luz1.intensity = intensidadLuz;
            luz1.range = tamanoLuz;
            luz1.enabled = false;
        }
        
        if (luz2 != null) 
        {
            luz2.intensity = intensidadLuz;
            luz2.range = tamanoLuz;
            luz2.enabled = false;
        }
    }

    void Update()
    {
        // Es MUY importante limpiar la lista de objetos nulos.
        // Si el "KillArea" destruye la basura mientras estaba dentro del escáner, 
        // el escáner se quedaría pensando que sigue adentro.
        basurasEnArea.RemoveAll(item => item == null);

        // Si hay al menos un objeto en el área, prendemos las luces, si no, las apagamos.
        bool encender = basurasEnArea.Count > 0;
        
        if (luz1 != null) luz1.enabled = encender;
        if (luz2 != null) luz2.enabled = encender;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Comprobamos si el objeto que entró es basura
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
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Cuando sale normalmente, lo quitamos de la lista
        MovimientoBasura mov = other.GetComponentInParent<MovimientoBasura>();
        if (mov == null && other.attachedRigidbody != null)
        {
            mov = other.attachedRigidbody.GetComponent<MovimientoBasura>();
        }

        GameObject obj = mov != null ? mov.gameObject : other.gameObject;
        if (basurasEnArea.Contains(obj))
        {
            basurasEnArea.Remove(obj);
        }
    }
}
