using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.SpatialTracking;
using UnityEngine.XR;
using System.Collections.Generic;
using System.IO;

public static class CrearNivelesDosACinco
{
    const string Base="Assets/NivelVR";
    static Material pared,piso,blanco,petroleo,acento,acero,pantalla,tela,piel,rojo,goma;
    [MenuItem("Proyecto/Crear niveles 2 al 5")]
    public static void CrearTodo()
    {
        PrepararMateriales();
        CrearEscena(2,"Nivel_2_Urgencias","URGENCIAS",new Color(.92f,.35f,.12f),new[]{"monitor","oxigeno","desfibrilador","camilla"},"Nivel_3_Preparacion",false);
        CrearEscena(3,"Nivel_3_Preparacion","PREPARACIÓN DE INSTRUMENTAL",new Color(.05f,.65f,.66f),new[]{"bisturi","pinza","tijeras","bandeja"},"Nivel_4_Recuperacion",false);
        CrearEscena(4,"Nivel_4_Recuperacion","RECUPERACIÓN",new Color(.17f,.67f,.48f),new[]{"monitor","suero","oxigeno","paciente"},"Nivel_5_Estabilizacion",false);
        CrearEscena(5,"Nivel_5_Estabilizacion","ESTABILIZACIÓN",new Color(.8f,.12f,.17f),new[]{"alarma","monitor","oxigeno","carro"},"",true);
        ConfigurarQuest.Configurar();
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        Debug.Log("Niveles 2–5 creados y añadidos a Build Settings.");
    }
    static void CrearEscena(int nivel,string archivo,string titulo,Color color,string[] pasos,string siguiente,bool final)
    {
        var escena=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var raiz=new GameObject("Escenario").transform;
        piso=Material("Piso_Nivel_"+nivel,new Color(.27f,.38f,.4f));pared=Material("Pared_Nivel_"+nivel,new Color(.78f,.84f,.84f));
        acento=Material("Acento_Nivel_"+nivel,color);blanco=Material("Blanco_Nivel_"+nivel,new Color(.91f,.95f,.94f));
        petroleo=Material("Petroleo_Nivel_"+nivel,new Color(.02f,.13f,.16f));tela=Material("Tela_Nivel_"+nivel,new Color(.02f,.28f,.26f));
        Caja("Piso",raiz,new Vector3(0,-.08f,0),new Vector3(8,.16f,8),piso);
        Caja("Techo",raiz,new Vector3(0,3.3f,0),new Vector3(8,.16f,8),blanco);
        Caja("Pared_del_fondo",raiz,new Vector3(0,1.6f,4),new Vector3(8,3.2f,.16f),pared);
        Caja("Pared_izquierda",raiz,new Vector3(-4,1.6f,0),new Vector3(.16f,3.2f,8),pared);
        Caja("Pared_derecha",raiz,new Vector3(4,1.6f,0),new Vector3(.16f,3.2f,8),pared);
        Caja("Pared_de_entrada",raiz,new Vector3(0,1.6f,-4),new Vector3(2.3f,3.2f,.16f),pared);
        Caja("Puerta_de_salida",raiz,new Vector3(0,1.2f,3.88f),new Vector3(1.65f,2.4f,.08f),petroleo);
        Caja("Marco_de_salida",raiz,new Vector3(0,2.48f,3.81f),new Vector3(1.8f,.12f,.08f),acento);
        for(int i=-1;i<=1;i++)Caja("Franja_de_piso",raiz,new Vector3(i*2,.012f,-.1f),new Vector3(.035f,.015f,7.5f),acento);
        CrearJugador(raiz,out Transform jugador,out Transform camara,out Transform manoI,out Transform manoD);
        var pecho=Paciente(raiz,nivel);
        var ecg=CrearMonitor(raiz,nivel,out GameObject monitor);
        var luzAlerta=CrearAlerta(raiz,color);
        Transform lampara=CrearLampara(raiz);
        CrearEquipos(raiz,nivel,monitor);
        if(nivel==2)AjustarCamilla(raiz);
        CrearPanel(raiz,titulo,pasos[0],color);
        CrearObjetivos(raiz,nivel,pasos);
        var salida=Caja("Panel_para_continuar",raiz,new Vector3(0,2.72f,3.75f),new Vector3(1.25f,.32f,.07f),acento);
        var accionSalida=salida.AddComponent<AccionQuirurgica>();accionSalida.tipo="continuar";
        Texto("Texto_de_salida",final?"FIN DE LA PRÁCTICA":"CONTINUAR",new Vector3(0,2.71f,3.69f),.11f,blanco);
        LuzGeneral(raiz);
        var control=new GameObject("Control_del_nivel").AddComponent<ControlNivelVR>();
        control.jugador=jugador;control.camara=camara;control.manoIzquierda=manoI;control.manoDerecha=manoD;
        control.pechoPaciente=pecho;control.trazoMonitor=ecg.transform;control.lampara=lampara;control.luzAlerta=luzAlerta.transform;
        control.secuencia=pasos;control.escenaSiguiente=siguiente;control.nivelFinal=final;
        control.instrucciones=GameObject.Find("Instrucciones").GetComponent<TextMesh>();control.estado=GameObject.Find("Estado_del_nivel").GetComponent<TextMesh>();control.rayo=GameObject.Find("Rayo_de_interaccion").GetComponent<LineRenderer>();
        EditorSceneManager.SaveScene(escena,Base+"/Escenas/"+archivo+".unity");
    }
    static void CrearEquipos(Transform r,int nivel,GameObject monitor)
    {
        switch(nivel)
        {
            case 2:
                Modelo("Camilla_Emergencia",new Vector3(0,0,.7f),Vector3.one);
                ObjetivoEnModelo(monitor,"monitor");ObjetivoModelo("Tanque_Oxigeno","oxigeno",new Vector3(-2.2f,0,.5f),new Vector3(.8f,.8f,.8f));
                ObjetivoModelo("Desfibrilador","desfibrilador",new Vector3(2.3f,.35f,1.3f),new Vector3(.8f,.8f,.8f));
                Marca("Punto_de_camilla",new Vector3(.92f,1.12f,1.25f),"camilla");
                break;
            case 3:
                Modelo("Mesa_Instrumental",new Vector3(0,0,.8f),Vector3.one);
                ObjetivoModelo("Bisturi","bisturi",new Vector3(-1.4f,1.02f,.4f),new Vector3(.75f,.75f,.75f));
                ObjetivoModelo("Pinza_Hemostatica","pinza",new Vector3(-.55f,1.02f,.4f),new Vector3(.75f,.75f,.75f));
                ObjetivoModelo("Tijeras_Quirurgicas","tijeras",new Vector3(.35f,1.02f,.4f),new Vector3(.75f,.75f,.75f));
                Marca("Bandeja_de_acero",new Vector3(1.7f,1.12f,.8f),"bandeja");
                ObjetivoEnModelo(monitor,"monitor");
                break;
            case 4:
                Modelo("Mesa_Quirurgica",new Vector3(0,0,.7f),new Vector3(.85f,.85f,.85f));
                Modelo("Tanque_Oxigeno",new Vector3(-2.5f,0,1.1f),new Vector3(.72f,.72f,.72f));
                ObjetivoEnModelo(monitor,"monitor");ObjetivoModelo("Tanque_Oxigeno","oxigeno",new Vector3(-2.5f,0,1.1f),new Vector3(.72f,.72f,.72f));
                PosteSuero(new Vector3(2.35f,0,.7f));Marca("Bomba_de_suero",new Vector3(3.05f,1.15f,.7f),"suero");
                Marca("Control_de_paciente",new Vector3(.82f,1.35f,.35f),"paciente");
                break;
            case 5:
                Modelo("Camilla_Emergencia",new Vector3(0,0,.6f),Vector3.one);
                Modelo("Desfibrilador",new Vector3(2.5f,.4f,1.5f),new Vector3(.85f,.85f,.85f));
                Modelo("Tanque_Oxigeno",new Vector3(-2.45f,0,1.4f),new Vector3(.78f,.78f,.78f));
                ObjetivoEnModelo(monitor,"monitor");ObjetivoModelo("Tanque_Oxigeno","oxigeno",new Vector3(-2.45f,0,1.4f),new Vector3(.78f,.78f,.78f));
                ObjetivoModelo("Desfibrilador","carro",new Vector3(2.5f,.4f,1.5f),new Vector3(.85f,.85f,.85f));
                Marca("Alarma_de_emergencia",new Vector3(0,2.65f,3.65f),"alarma");
                break;
        }
    }
    static void CrearObjetivos(Transform raiz,int nivel,string[] pasos)
    {
        // Visible instruction markers for each action; each is a simple, clickable game target.
        for(int i=0;i<pasos.Length;i++)
        {
            Vector3 p=new Vector3(-2.55f+i*1.7f,1.95f,2.9f);
            var panel=Caja("Indicador_"+(i+1),raiz,p,new Vector3(1.35f,.36f,.07f),i==0?acento:petroleo);panel.GetComponent<Collider>().enabled=false;
            Texto("Etiqueta_objetivo_"+(i+1),pasos[i].Replace('_',' ').ToUpperInvariant(),p+new Vector3(0,0,-.055f),.095f,blanco);
        }
    }
    static void AjustarCamilla(Transform raiz)
    {
        var camilla=raiz.Find("Equipos/Camilla_Emergencia");if(!camilla)return;
        var salida=GameObject.Find("Punto_de_camilla");if(!salida)return;
        var marca=salida.GetComponent<Renderer>();if(marca)marca.enabled=false;var collider=salida.GetComponent<Collider>();if(collider)collider.enabled=false;
        salida.transform.position=camilla.position+new Vector3(0,.92f,.08f);
        var texto=new GameObject("Etiqueta_de_camilla");texto.transform.SetParent(salida.transform);texto.transform.localPosition=new Vector3(0,.12f,-.09f);
        var m=texto.AddComponent<TextMesh>();m.text="ACERCA LA CAMILLA";m.fontSize=40;m.characterSize=.012f;m.anchor=TextAnchor.MiddleCenter;m.alignment=TextAlignment.Center;m.color=Color.white;
    }
    static void CrearPanel(Transform raiz,string titulo,string primero,Color color)
    {
        Caja("Panel_de_titulo",raiz,new Vector3(0,2.85f,3.79f),new Vector3(3.55f,.88f,.09f),petroleo);
        Texto("Titulo_del_nivel",titulo,new Vector3(0,3.03f,3.72f),.14f,blanco);
        var txt=Texto("Instrucciones","JOYSTICK IZQ. MOVER · JOYSTICK DER. GIRAR\nAPUNTA Y APRIETA EL GATILLO",new Vector3(0,2.79f,3.72f),.083f,blanco);
        Texto("Estado_del_nivel","Objetivo 1: "+primero,new Vector3(0,2.51f,3.72f),.095f,Material("Texto_acento_"+titulo,color));
    }
    static Transform Paciente(Transform r,int nivel)
    {
        Caja("Cuerpo_del_paciente",r,new Vector3(0,1.02f,.72f),new Vector3(.66f,.27f,1.45f),piel);
        Esfera("Cabeza_del_paciente",r,new Vector3(0,1.2f,-.25f),new Vector3(.37f,.32f,.38f),piel);
        Caja("Cobija",r,new Vector3(0,1.18f,.85f),new Vector3(.78f,.045f,.78f),tela);
        return GameObject.Find("Cuerpo_del_paciente").transform;
    }
    static GameObject CrearMonitor(Transform r,int nivel,out GameObject raizMonitor)
    {
        raizMonitor=Modelo("Monitor_Signos_Vitales",new Vector3(2.25f,.85f,.65f),new Vector3(.82f,.82f,.82f));
        if(!raizMonitor)raizMonitor=Caja("Monitor_de_signos_vitales",r,new Vector3(2.25f,1.25f,.65f),new Vector3(.82f,.75f,.28f),petroleo);
        ObjetivoEnModelo(raizMonitor,"monitor");
        var pantallaObj=Caja("Pantalla_activa",r,new Vector3(2.25f,1.4f,.43f),new Vector3(.58f,.32f,.025f),pantalla);ObjetivoEnModelo(pantallaObj,"monitor");
        var trazo=Caja("Trazo_ECG",r,new Vector3(2.25f,1.4f,.405f),new Vector3(.36f,.018f,.012f),acento);ObjetivoEnModelo(trazo,"monitor");return trazo;
    }
    static Transform CrearLampara(Transform r)
    {
        var cuerpo=Caja("Lampara_de_techo",r,new Vector3(0,2.55f,.5f),new Vector3(1.18f,.18f,.8f),blanco);
        Caja("Luz_difusa",r,new Vector3(0,2.43f,.5f),new Vector3(.92f,.04f,.58f),acento);
        var luz=new GameObject("Luz_de_lampara");luz.transform.SetParent(cuerpo.transform);luz.transform.localPosition=new Vector3(0,-.25f,0);var l=luz.AddComponent<Light>();l.type=LightType.Spot;l.range=5;l.intensity=2.4f;l.spotAngle=72;l.color=new Color(.75f,.92f,1);
        return cuerpo.transform;
    }
    static GameObject CrearAlerta(Transform r,Color color)
    {
        var g=Esfera("Luz_de_alerta",r,new Vector3(3.45f,2.65f,3.72f),new Vector3(.2f,.2f,.2f),Material("Luz_de_alerta_"+ColorUtility.ToHtmlStringRGB(color), color));
        var luz=g.AddComponent<Light>();luz.type=LightType.Point;luz.range=4;luz.intensity=1.3f;luz.color=color;return g;
    }
    static void CrearJugador(Transform raiz,out Transform jugador,out Transform camara,out Transform manoI,out Transform manoD)
    {
        var g=new GameObject("Jugador_VR");g.transform.SetParent(raiz);g.transform.position=new Vector3(0,0,-2.4f);jugador=g.transform;
        var c=new GameObject("Camara_VR");c.tag="MainCamera";c.transform.SetParent(g.transform);c.transform.localPosition=new Vector3(0,1.62f,0);var cam=c.AddComponent<Camera>();cam.stereoTargetEye=StereoTargetEyeMask.Both;cam.nearClipPlane=.03f;cam.farClipPlane=70;c.AddComponent<AudioListener>();c.AddComponent<TrackedPoseDriver>().SetPoseSource(TrackedPoseDriver.DeviceType.GenericXRDevice,TrackedPoseDriver.TrackedPose.Head);camara=c.transform;
        manoI=Mano(g.transform,"Mano_izquierda",new Vector3(-.25f,1.25f,.4f));manoD=Mano(g.transform,"Mano_derecha",new Vector3(.25f,1.25f,.4f));
        var linea=new GameObject("Rayo_de_interaccion").AddComponent<LineRenderer>();linea.positionCount=2;linea.startWidth=.012f;linea.endWidth=.004f;linea.material=acento;linea.useWorldSpace=true;
    }
    static Transform Mano(Transform p,string nombre,Vector3 pos){var g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name=nombre;g.transform.SetParent(p);g.transform.localPosition=pos;g.transform.localScale=new Vector3(.09f,.09f,.14f);g.GetComponent<Renderer>().sharedMaterial=acento;Object.DestroyImmediate(g.GetComponent<Collider>());return g.transform;}
    static void PosteSuero(Vector3 p){var r=new GameObject("Soporte_de_suero").transform;r.position=p;Cilindro("Poste_de_suero",r,new Vector3(0,1.05f,0),.035f,2.1f,acero);Caja("Bolsa_de_suero",r,new Vector3(0,1.72f,0),new Vector3(.34f,.44f,.12f),blanco);Caja("Base_de_suero",r,new Vector3(0,.05f,0),new Vector3(.5f,.1f,.5f),acero);}
    static void ObjetivoModelo(string nombre,string accion,Vector3 p,Vector3 escala){var g=Modelo(nombre,p,escala);if(!g)g=Caja(nombre,new GameObject("Equipos").transform,p,Vector3.one*.25f,acero);ObjetivoEnModelo(g,accion);}
    static void ObjetivoEnModelo(GameObject g,string tipo){if(!g)return;var a=g.GetComponent<AccionQuirurgica>();if(!a)a=g.AddComponent<AccionQuirurgica>();a.tipo=tipo;}
    static GameObject Modelo(string nombre,Vector3 p,Vector3 escala)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Modelos_MEDIVR/FBX_Para_Unity/"+nombre+".fbx");if(!prefab)return null;
        var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab);g.name=nombre;g.transform.position=p;g.transform.localScale=escala;
        foreach(var m in g.GetComponentsInChildren<MeshFilter>())if(m.sharedMesh&&!m.GetComponent<Collider>())m.gameObject.AddComponent<MeshCollider>();return g;
    }
    static void Marca(string n,Vector3 p,string tipo){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.position=p;g.transform.localScale=new Vector3(.55f,.16f,.12f);g.GetComponent<Renderer>().sharedMaterial=acento;ObjetivoEnModelo(g,tipo);}
    static GameObject Caja(string n,Transform p,Vector3 pos,Vector3 dim,Material m){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(p);g.transform.localPosition=pos;g.transform.localScale=dim;g.GetComponent<Renderer>().sharedMaterial=m;return g;}
    static GameObject Esfera(string n,Transform p,Vector3 pos,Vector3 dim,Material m){var g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name=n;g.transform.SetParent(p);g.transform.localPosition=pos;g.transform.localScale=dim;g.GetComponent<Renderer>().sharedMaterial=m;return g;}
    static void Cilindro(string n,Transform p,Vector3 pos,float radio,float alto,Material m){var g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name=n;g.transform.SetParent(p);g.transform.localPosition=pos;g.transform.localScale=new Vector3(radio,alto/2,radio);g.GetComponent<Renderer>().sharedMaterial=m;}
    static TextMesh Texto(string n,string texto,Vector3 p,float tam,Material m){var g=new GameObject(n);g.transform.position=p;g.transform.rotation=Quaternion.Euler(0,180,0);var t=g.AddComponent<TextMesh>();t.text=texto;t.fontSize=48;t.characterSize=tam/48;t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.color=m.color;return t;}
    static void LuzGeneral(Transform p){var l=new GameObject("Luz_general").AddComponent<Light>();l.transform.SetParent(p);l.type=LightType.Directional;l.intensity=1.1f;l.transform.rotation=Quaternion.Euler(45,-25,0);RenderSettings.ambientLight=new Color(.42f,.48f,.51f);}
    static Material Material(string n,Color c){var m=AssetDatabase.LoadAssetAtPath<Material>(Base+"/Materiales/"+n+".mat");if(m)return m;var s=Shader.Find("Universal Render Pipeline/Lit");if(!s)s=Shader.Find("Standard");m=new Material(s){name=n,color=c};if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",c);AssetDatabase.CreateAsset(m,Base+"/Materiales/"+n+".mat");return m;}
    static void PrepararMateriales(){acero=AssetDatabase.LoadAssetAtPath<Material>(Base+"/Materiales/Acero.mat");if(!acero)acero=Material("Acero",new Color(.58f,.66f,.68f));pantalla=Material("Pantalla_oscura",new Color(.01f,.07f,.08f));piel=Material("Piel_simulada",new Color(.68f,.43f,.34f));goma=Material("Goma_oscura",new Color(.05f,.07f,.08f));}
}






