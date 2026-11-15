using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.Management;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using System.IO;
using System.Collections.Generic;

/// <summary>Perfil de compilación Android para Quest y activación del proveedor XR.</summary>
public static class ConfigurarQuest
{
    const string Ruta="Assets/NivelVR/ConfiguracionXR";
    [MenuItem("Proyecto/Configurar Android para Meta Quest")]
    public static void Configurar()
    {
        CrearCarpetas();
        var contenedor=AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(Ruta+"/XRGeneralSettingsPerBuildTarget.asset");
        if(!contenedor){contenedor=ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();AssetDatabase.CreateAsset(contenedor,Ruta+"/XRGeneralSettingsPerBuildTarget.asset");}
        var ajustes=contenedor.SettingsForBuildTarget(BuildTargetGroup.Android);
        if(!ajustes){ajustes=ScriptableObject.CreateInstance<XRGeneralSettings>();ajustes.name="Android Quest";ajustes.InitManagerOnStart=true;AssetDatabase.AddObjectToAsset(ajustes,contenedor);contenedor.SetSettingsForBuildTarget(BuildTargetGroup.Android,ajustes);}
        var manager=ajustes.Manager;
        if(!manager){manager=ScriptableObject.CreateInstance<XRManagerSettings>();manager.name="Proveedor Quest";ajustes.Manager=manager;AssetDatabase.AddObjectToAsset(manager,contenedor);}
        manager.automaticLoading=true;manager.automaticRunning=true;
        bool tiene=false;foreach(var loader in manager.activeLoaders)if(loader is OpenXRLoader)tiene=true;
        if(!tiene){var openXR=ScriptableObject.CreateInstance<OpenXRLoader>();openXR.name="OpenXR para Meta Quest";AssetDatabase.AddObjectToAsset(openXR,contenedor);manager.TryAddLoader(openXR);}
        EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey,contenedor,true);
        EditorUtility.SetDirty(contenedor);EditorUtility.SetDirty(ajustes);EditorUtility.SetDirty(manager);

        PlayerSettings.productName="Proyecto Medicos";
        PlayerSettings.companyName="ProyectoMedicos";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android,"com.proyectomedicos.quirofano");
        PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel23;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android,ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.useCustomKeystore=false;
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3});
        PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android,ManagedStrippingLevel.Medium);
        PlayerSettings.defaultInterfaceOrientation=UIOrientation.LandscapeLeft;
        PlayerSettings.runInBackground=true;
        var escenasDelJuego=new List<EditorBuildSettingsScene>();
        foreach(var guid in AssetDatabase.FindAssets("t:Scene",new[]{"Assets/NivelVR/Escenas"}))
        {
            string ruta=AssetDatabase.GUIDToAssetPath(guid);
            escenasDelJuego.Add(new EditorBuildSettingsScene(ruta,true));
        }
        escenasDelJuego.Sort((a,b)=>string.Compare(a.path,b.path,System.StringComparison.Ordinal));
        EditorBuildSettings.scenes=escenasDelJuego.ToArray();
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        Debug.Log("Android listo para Meta Quest: ARM64, IL2CPP, XR automático y niveles disponibles en Build Settings.");
    }
    public static void CrearAPK(){Configurar();string salida=Path.GetFullPath("Build/Practica_Quirofano_Quest.apk");Directory.CreateDirectory(Path.GetDirectoryName(salida));var opciones=new BuildPlayerOptions{scenes=System.Array.ConvertAll(EditorBuildSettings.scenes,e=>e.path),locationPathName=salida,target=BuildTarget.Android,options=BuildOptions.None};var resultado=BuildPipeline.BuildPlayer(opciones);if(resultado.summary.result!=BuildResult.Succeeded)throw new System.Exception("No se pudo generar el APK: "+resultado.summary.result);Debug.Log("APK creado: "+salida);}
    static void CrearCarpetas(){if(!AssetDatabase.IsValidFolder("Assets/NivelVR"))AssetDatabase.CreateFolder("Assets","NivelVR");if(!AssetDatabase.IsValidFolder(Ruta))AssetDatabase.CreateFolder("Assets/NivelVR","ConfiguracionXR");}
}

public class CrearNivelAlAbrir
{
    // Exposed for Unity batch mode so the initial level can be generated automatically.
    public static void CrearEscena(){CrearNivelInicial.Crear();ConfigurarQuest.Configurar();}
    public static void CrearCincoNiveles(){CrearNivelInicial.Crear();CrearNivelesDosACinco.CrearTodo();ConfigurarQuest.Configurar();}
}
