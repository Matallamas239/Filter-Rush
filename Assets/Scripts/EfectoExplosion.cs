using UnityEngine;

public class EfectoExplosion : MonoBehaviour
{
    private static Material materialParticulasPorDefecto;

    /// <summary>
    /// Genera una explosión visual y de partículas en la posición dada.
    /// </summary>
    public static void Explotar(Vector3 posicion, Color? colorBase = null)
    {
        GameObject expObj = new GameObject("FX_ExplosionBasura");
        expObj.transform.position = posicion;

        EfectoExplosion fx = expObj.AddComponent<EfectoExplosion>();
        fx.IniciarExplosion(colorBase);
    }

    private void IniciarExplosion(Color? colorBase)
    {
        Color colorPrincipal = colorBase ?? new Color(0.15f, 0.85f, 1f, 1f); // Celeste / agua viva
        Color colorDestello = new Color(1f, 0.95f, 0.5f, 1f);   // Amarillo / destello brillante

        // 1. Luz de flash dentro del tubo (hace brillar el vidrio)
        GameObject luzObj = new GameObject("FlashLuz");
        luzObj.transform.SetParent(transform);
        luzObj.transform.localPosition = Vector3.zero;
        Light luz = luzObj.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = colorPrincipal;
        luz.range = 7f;
        luz.intensity = 8f;

        // 2. Sistema de Partículas principal: Fragmentos de plástico y chispas
        ParticleSystem ps = gameObject.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystemRenderer psRenderer = GetComponent<ParticleSystemRenderer>();
        ConfigurarMaterial(psRenderer);

        var main = ps.main;
        main.duration = 0.5f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 11f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.38f);
        main.startColor = new ParticleSystem.MinMaxGradient(colorPrincipal, colorDestello);
        main.gravityModifier = 1.6f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.None;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 32) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(colorPrincipal, 0.4f), new GradientColorKey(Color.cyan, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.85f, 0.6f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = grad;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 1f);
        curve.AddKey(0.7f, 0.75f);
        curve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.x = new ParticleSystem.MinMaxCurve(-360f * Mathf.Deg2Rad, 360f * Mathf.Deg2Rad);
        rot.y = new ParticleSystem.MinMaxCurve(-360f * Mathf.Deg2Rad, 360f * Mathf.Deg2Rad);
        rot.z = new ParticleSystem.MinMaxCurve(-360f * Mathf.Deg2Rad, 360f * Mathf.Deg2Rad);

        ps.Play();

        // 3. Salpicadura de agua / burbujas
        CrearBurbujasSplash(colorPrincipal);

        // Auto-destrucción del efecto tras 1.2 segundos
        Destroy(gameObject, 1.2f);
    }

    private void CrearBurbujasSplash(Color colorAgua)
    {
        GameObject splashObj = new GameObject("SplashBurbujas");
        splashObj.transform.SetParent(transform);
        splashObj.transform.localPosition = Vector3.zero;

        ParticleSystem psSplash = splashObj.AddComponent<ParticleSystem>();
        psSplash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystemRenderer psRenderer = splashObj.GetComponent<ParticleSystemRenderer>();
        ConfigurarMaterial(psRenderer);

        var main = psSplash.main;
        main.duration = 0.4f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 7f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
        main.startColor = new Color(0.9f, 1f, 1f, 0.85f);
        main.gravityModifier = 0.6f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = psSplash.emission;
        emission.enabled = true;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 24) });

        var shape = psSplash.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;

        var colorOverLifetime = psSplash.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(colorAgua, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = grad;

        psSplash.Play();
    }

    private static void ConfigurarMaterial(ParticleSystemRenderer psRenderer)
    {
        if (materialParticulasPorDefecto == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");

            if (shader != null)
            {
                materialParticulasPorDefecto = new Material(shader);
                if (materialParticulasPorDefecto.HasProperty("_Surface"))
                {
                    materialParticulasPorDefecto.SetFloat("_Surface", 1);
                }
                if (materialParticulasPorDefecto.HasProperty("_Blend"))
                {
                    materialParticulasPorDefecto.SetFloat("_Blend", 0);
                }
            }
        }

        if (materialParticulasPorDefecto != null)
        {
            psRenderer.material = materialParticulasPorDefecto;
        }
    }

    void Update()
    {
        // Atenuar gradualmente la luz del flash
        Light luz = GetComponentInChildren<Light>();
        if (luz != null && luz.intensity > 0)
        {
            luz.intensity = Mathf.MoveTowards(luz.intensity, 0f, Time.deltaTime * 35f);
        }
    }
}
