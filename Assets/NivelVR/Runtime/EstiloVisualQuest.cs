using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Mejora visual ligera para Quest sin postprocesado costoso.</summary>
[DisallowMultipleComponent]
public sealed class EstiloVisualQuest : MonoBehaviour
{
    MaterialPropertyBlock bloque;

    void Awake()
    {
        bloque = new MaterialPropertyBlock();
        ConfigurarCalidad();
        ConfigurarAmbiente();
        MejorarMateriales();
        ConfigurarLuces();
    }

    void ConfigurarCalidad()
    {
        QualitySettings.antiAliasing = 4;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
        QualitySettings.shadowDistance = 18f;
        QualitySettings.shadowResolution = ShadowResolution.Medium;
        QualitySettings.shadowCascades = 2;
        QualitySettings.pixelLightCount = 4;
    }

    void ConfigurarAmbiente()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.48f, .55f, .62f);
        RenderSettings.ambientEquatorColor = new Color(.24f, .29f, .34f);
        RenderSettings.ambientGroundColor = new Color(.09f, .11f, .13f);
        RenderSettings.ambientIntensity = .72f;
        RenderSettings.fog = false;
    }

    void MejorarMateriales()
    {
        foreach (var render in FindObjectsOfType<Renderer>(true))
        {
            if (!render || render is LineRenderer || render.sharedMaterial == null) continue;
            string nombre = Ruta(render.transform).ToLowerInvariant();
            Color color = ColorBase(render.sharedMaterial);
            float metal = 0f, suavidad = .28f;
            Color emision = Color.black;

            if (Contiene(nombre,"acero","metal","bisturi","pinza","hemostato","separador","instrumento"))
            { color = Color.Lerp(color,new Color(.56f,.63f,.66f),.72f);metal=.82f;suavidad=.68f; }
            else if (Contiene(nombre,"higado")) { color=new Color(.34f,.075f,.055f);suavidad=.46f; }
            else if (Contiene(nombre,"estomago")) { color=new Color(.66f,.29f,.31f);suavidad=.48f; }
            else if (Contiene(nombre,"intestino","asa_intestinal")) { color=new Color(.68f,.34f,.27f);suavidad=.5f; }
            else if (Contiene(nombre,"rinon")) { color=new Color(.38f,.075f,.085f);suavidad=.43f; }
            else if (Contiene(nombre,"coagulo","hemorragia","sangrado")) { color=new Color(.26f,.008f,.015f);suavidad=.42f; }
            else if (Contiene(nombre,"piel","cubierta_abdominal","paciente","pecho"))
            { color=Color.Lerp(color,new Color(.66f,.43f,.34f),.55f);suavidad=.3f; }
            else if (Contiene(nombre,"gasa","sabana","almohada")) { color=Color.Lerp(color,new Color(.88f,.93f,.94f),.7f);suavidad=.18f; }
            else if (Contiene(nombre,"monitor","trazo"))
            { color=Color.Lerp(color,new Color(.025f,.25f,.22f),.45f);emision=new Color(.02f,.55f,.38f)*1.25f;suavidad=.62f; }
            else if (Contiene(nombre,"pared","techo")) { color=Color.Lerp(color,new Color(.68f,.75f,.76f),.35f);suavidad=.16f; }
            else if (Contiene(nombre,"piso","suelo")) { color=Color.Lerp(color,new Color(.23f,.3f,.32f),.28f);suavidad=.34f; }

            render.GetPropertyBlock(bloque);
            bloque.SetColor("_BaseColor",color);bloque.SetColor("_Color",color);
            bloque.SetFloat("_Metallic",metal);bloque.SetFloat("_Smoothness",suavidad);bloque.SetFloat("_Glossiness",suavidad);
            if(emision.maxColorComponent>0){bloque.SetColor("_EmissionColor",emision);render.sharedMaterial.EnableKeyword("_EMISSION");}
            render.SetPropertyBlock(bloque);
        }
    }

    void ConfigurarLuces()
    {
        foreach(var luz in FindObjectsOfType<Light>(true))
        {
            if(luz.type==LightType.Directional)
            {
                luz.color=new Color(.86f,.92f,1f);luz.intensity=.75f;luz.shadows=LightShadows.Soft;
                luz.shadowStrength=.62f;luz.shadowBias=.075f;luz.shadowNormalBias=.35f;
            }
        }
        CrearLuzRelleno("Luz_Clinica_Suave",new Vector3(0f,2.75f,.35f),new Color(.78f,.9f,1f),1.15f,5.5f);
        CrearLuzRelleno("Luz_Ambiente_Quirurgica",new Vector3(-1.65f,2.15f,-.9f),new Color(.55f,.72f,1f),.55f,4.2f);
    }

    void CrearLuzRelleno(string nombre,Vector3 posicion,Color color,float intensidad,float alcance)
    {
        if(GameObject.Find(nombre))return;
        var objeto=new GameObject(nombre);objeto.transform.position=posicion;
        var luz=objeto.AddComponent<Light>();luz.type=LightType.Point;luz.color=color;luz.intensity=intensidad;luz.range=alcance;
        luz.shadows=LightShadows.None;luz.renderMode=LightRenderMode.ForcePixel;
    }

    static bool Contiene(string texto,params string[] palabras){foreach(var palabra in palabras)if(texto.Contains(palabra))return true;return false;}
    static string Ruta(Transform t){string ruta=t.name;while(t.parent){t=t.parent;ruta=t.name+"/"+ruta;}return ruta;}
    static Color ColorBase(Material material)
    {
        if(material.HasProperty("_BaseColor"))return material.GetColor("_BaseColor");
        if(material.HasProperty("_Color"))return material.GetColor("_Color");
        return Color.white;
    }
}
