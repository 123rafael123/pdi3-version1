using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using UnityEngine.SpatialTracking;
using System.IO;

public static class CrearNivelInicial
{
    const string Base="Assets/NivelVR";
    static Material pared,piso,blanco,petroleo,verde,acero,goma,pantalla,tela,piel,rojo,higadoMat,estomagoMat,intestinoMat,renalMat,tejidoMat,coaguloMat;
    [MenuItem("Proyecto/Crear nivel VR de cirugía")]
    public static void Crear()
    {
        AsegurarCarpeta(Base+"/Materiales");AsegurarCarpeta(Base+"/Escenas");CrearMateriales();
        var sc=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var raiz=new GameObject("Quirofano").transform;
        C("Piso",raiz,new Vector3(0,-.08f,0),new Vector3(8,.16f,8),piso);
        C("Techo",raiz,new Vector3(0,3.35f,0),new Vector3(8,.16f,8),blanco);
        C("Pared_del_fondo",raiz,new Vector3(0,1.6f,4),new Vector3(8,3.2f,.16f),pared);
        C("Pared_izquierda",raiz,new Vector3(-4,1.6f,0),new Vector3(.16f,3.2f,8),pared);
        C("Pared_derecha",raiz,new Vector3(4,1.6f,0),new Vector3(.16f,3.2f,8),pared);
        C("Pared_entrada",raiz,new Vector3(0,1.6f,-4),new Vector3(2.5f,3.2f,.16f),pared);
        C("Puerta",raiz,new Vector3(0,1.15f,-3.9f),new Vector3(1.45f,2.3f,.08f),petroleo);
        C("Ventana_de_puerta",raiz,new Vector3(0,1.65f,-3.84f),new Vector3(1.1f,.55f,.035f),verde);
        C("Zocalo_de_color",raiz,new Vector3(0,.12f,3.89f),new Vector3(6,.18f,.04f),verde);
        CrearJugador(raiz,out Transform jugador,out Transform camara,out Transform manoI,out Transform manoD);

        // Mesa y paciente
        C("Base_de_mesa_quirurgica",raiz,new Vector3(0,.66f,.65f),new Vector3(1.2f,.2f,2.2f),petroleo);
        C("Colchon_de_mesa",raiz,new Vector3(0,.79f,.65f),new Vector3(1.12f,.12f,2.1f),blanco);
        C("Columna_de_mesa",raiz,new Vector3(0,.35f,.65f),new Vector3(.38f,.65f,.45f),acero);
        var cuerpo=E("Torso_del_paciente",raiz,new Vector3(0,1.02f,.72f),new Vector3(.82f,.48f,1.86f),piel);
        tejidoMat=CrearMaterial("Tejido_interno",new Color(.36f,.075f,.07f));coaguloMat=CrearMaterial("Coagulo_simulado",new Color(.22f,.018f,.025f));
        E("Cabeza_del_paciente",raiz,new Vector3(0,1.12f,-.48f),new Vector3(.39f,.34f,.4f),piel);
        E("Hombro_izquierdo",raiz,new Vector3(-.32f,1.02f,.05f),new Vector3(.3f,.3f,.4f),piel);
        E("Hombro_derecho",raiz,new Vector3(.32f,1.02f,.05f),new Vector3(.3f,.3f,.4f),piel);
        E("Brazo_izquierdo",raiz,new Vector3(-.48f,.93f,.55f),new Vector3(.2f,.2f,.82f),piel);
        E("Brazo_derecho",raiz,new Vector3(.48f,.93f,.55f),new Vector3(.2f,.2f,.82f),piel);
        C("Campo_quirurgico",raiz,new Vector3(0,1.12f,1.55f),new Vector3(.96f,.045f,.55f),tela);
        var cubierta=E("Cubierta_Abdominal",raiz,new Vector3(0,1.268f,.73f),new Vector3(.63f,.035f,.72f),piel);
        var cavidad=CrearCavidadAbdominal(raiz);
        var bordeIzquierdo=E("Borde_de_tejido_izquierdo",raiz,new Vector3(-.20f,1.302f,.73f),new Vector3(.14f,.022f,.5f),tejidoMat);Fisica(bordeIzquierdo,.35f,true,true);
        var bordeDerecho=E("Borde_de_tejido_derecho",raiz,new Vector3(.20f,1.302f,.73f),new Vector3(.14f,.022f,.5f),tejidoMat);Fisica(bordeDerecho,.35f,true,true);
        var zonaIncision=C("Zona_de_Incision",raiz,new Vector3(0,1.296f,.73f),new Vector3(.43f,.012f,.035f),rojo);Accion(zonaIncision,"zona_incision");zonaIncision.GetComponent<Collider>().isTrigger=true;zonaIncision.SetActive(false);
        var zonaRetraccion=C("Zona_de_Separacion",raiz,new Vector3(.24f,1.31f,.73f),new Vector3(.09f,.025f,.16f),verde);Accion(zonaRetraccion,"zona_retraccion");zonaRetraccion.GetComponent<Collider>().isTrigger=true;zonaRetraccion.SetActive(false);
        var zonaHerida=C("Zona_de_Herida",raiz,new Vector3(0,1.335f,.73f),new Vector3(.25f,.025f,.19f),rojo);Accion(zonaHerida,"zona_herida");zonaHerida.GetComponent<Collider>().isTrigger=true;zonaHerida.SetActive(false);
        var higado=CrearHigado(raiz,new Vector3(-.20f,1.38f,.58f));
        var estomago=CrearEstomago(raiz,new Vector3(.10f,1.385f,.79f));
        var intestino=CrearIntestino(raiz,new Vector3(0,1.385f,.84f));
        var rinones=CrearRinones(raiz);rinones.gameObject.SetActive(false);
        var coagulo=CrearCoagulo(raiz,new Vector3(0,1.405f,.735f));
        var zonaCoagulo=C("Zona_de_Extraccion",raiz,new Vector3(-.16f,1.42f,.735f),new Vector3(.12f,.02f,.14f),verde);Accion(zonaCoagulo,"zona_extraccion");zonaCoagulo.GetComponent<Collider>().isTrigger=true;zonaCoagulo.SetActive(false);
        var zonaSangrado=C("Zona_de_Hemorragia",raiz,new Vector3(.18f,1.405f,.72f),new Vector3(.07f,.02f,.09f),rojo);Accion(zonaSangrado,"zona_hemorragia");zonaSangrado.GetComponent<Collider>().isTrigger=true;zonaSangrado.SetActive(false);
        var marcadorHemorragia=E("Pulso_de_hemorragia",raiz,new Vector3(.18f,1.415f,.72f),new Vector3(.05f,.018f,.05f),rojo);Accion(marcadorHemorragia,"zona_hemorragia");marcadorHemorragia.GetComponent<Collider>().isTrigger=true;marcadorHemorragia.SetActive(false);

        // Equipos del primer nivel: prioriza los modelos Blender exportados; sustituye con bloques si faltan.
        var mesaInst=Modelo("Mesa_Instrumental",new Vector3(-1.55f,0,.45f),Vector3.one);
        if(!mesaInst) mesaInst=CrearMesaInstrumental(raiz);
        zonaCoagulo.transform.localPosition=new Vector3(-1.55f,1.015f,.45f);
        var monitor=Modelo("Monitor_Signos_Vitales",new Vector3(1.75f,.9f,.65f),new Vector3(.85f,.85f,.85f));
        if(!monitor) monitor=C("Monitor_de_signos_vitales",raiz,new Vector3(1.65f,1.35f,.65f),new Vector3(.78f,.68f,.25f),petroleo);
        Accion(monitor,"monitor");
        var pantallaMonitor=C("Pantalla_del_monitor",raiz,new Vector3(1.65f,1.38f,.51f),new Vector3(.64f,.45f,.025f),pantalla);Accion(pantallaMonitor,"monitor");
        var trazo=C("Trazo_ECG",raiz,new Vector3(1.65f,1.38f,.49f),new Vector3(.26f,.025f,.018f),verde);Accion(trazo,"monitor");
        var ma=Modelo("Maquina_Anestesia",new Vector3(2.65f,0,1.45f),Vector3.one);
        if(!ma) C("Carro_de_anestesia",raiz,new Vector3(2.65f,.75f,1.45f),new Vector3(.72f,1.25f,.62f),blanco);
        var foco=Modelo("Lampara_Quirurgica",new Vector3(0,2.35f,.65f),new Vector3(.8f,.8f,.8f));
        if(!foco){var soporte=C("Soporte_de_lampara",raiz,new Vector3(0,2.75f,.65f),new Vector3(.09f,1.1f,.09f),acero);foco=C("Lampara_quirurgica",raiz,new Vector3(0,2.2f,.65f),new Vector3(1.1f,.17f,1.1f),blanco);}
        Accion(foco,"lampara");
        var bisturi=Modelo("Bisturi",new Vector3(-1.55f,1.05f,.28f),new Vector3(.65f,.65f,.65f));
        if(!bisturi) bisturi=C("Bisturi",raiz,new Vector3(-1.55f,1.05f,.28f),new Vector3(.42f,.06f,.06f),acero);
        var pinza=Modelo("Pinza_Hemostatica",new Vector3(-1.55f,1.05f,.53f),new Vector3(.65f,.65f,.65f));
        if(!pinza) pinza=C("Pinza_hemostatica",raiz,new Vector3(-1.55f,1.05f,.53f),new Vector3(.48f,.045f,.05f),acero);
        var tijera=Modelo("Tijeras_Quirurgicas",new Vector3(-1.55f,1.05f,.76f),new Vector3(.65f,.65f,.65f));
        if(!tijera) tijera=C("Tijeras_quirurgicas",raiz,new Vector3(-1.55f,1.05f,.76f),new Vector3(.42f,.045f,.05f),acero);
        Accion(bisturi,"bisturi"); Accion(pinza,"instrumental"); Accion(tijera,"instrumental");
        var mesaAccion=C("Punto_de_preparacion",raiz,new Vector3(-1.55f,1.02f,.45f),new Vector3(.72f,.035f,.48f),tela);Accion(mesaAccion,"instrumental");
        bisturi.transform.position=new Vector3(-1.55f,1.05f,.28f);
        var separador=CrearSeparador(raiz,new Vector3(-1.55f,1.06f,.98f));
        var pinzaExtraccion=Modelo("Pinza_Hemostatica",new Vector3(-1.55f,1.05f,.56f),new Vector3(.65f,.65f,.65f));
        if(!pinzaExtraccion)pinzaExtraccion=CrearPinza(raiz,new Vector3(-1.55f,1.05f,.56f));
        pinzaExtraccion.name="Pinza_de_Extraccion";Accion(pinzaExtraccion,"pinza_extraccion");
        var hemostato=CrearPinza(raiz,new Vector3(-1.55f,1.05f,.77f));hemostato.name="Pinza_Hemostatica_de_Control";Accion(hemostato,"pinza_hemostatica");
        var gasa=CrearGasa(raiz,new Vector3(-1.55f,1.04f,.12f));
        pinzaExtraccion.SetActive(false);hemostato.SetActive(false);
        Fisica(bisturi,.18f,true,false);Fisica(separador,.32f,true,false);Fisica(gasa,.06f,true,false);
        if(pinzaExtraccion.GetComponent<Rigidbody>()==null)Fisica(pinzaExtraccion,.2f,true,false);
        Fisica(hemostato,.2f,true,false);

        // Rótulo de orientación y estado del nivel
        var panel=C("Panel_de_instrucciones",raiz,new Vector3(0,2.7f,3.82f),new Vector3(3.5f,.9f,.08f),petroleo);
        var txt=Texto("Instrucciones", "PALANCA IZQ. MOVER · PALANCA DER. GIRAR\nGATILLO: USAR · AGARRE: TOMAR / SOLTAR\nSIGUE LOS PASOS DEL PANEL QUIRURGICO",new Vector3(0,2.79f,3.76f),.105f,blanco);
        var est=Texto("Estado_de_la_practica","Práctica iniciada",new Vector3(0,2.39f,3.76f),.1f,verde);
        var salida=C("Punto_de_salida",raiz,new Vector3(0,1.2f,3.7f),new Vector3(1.2f,.28f,.08f),verde);Accion(salida,"siguiente");
        Texto("Texto_de_salida","SIGUIENTE NIVEL",new Vector3(0,1.19f,3.64f),.09f,blanco);
        var luzObjeto=new GameObject("Luz_de_procedimiento");luzObjeto.transform.SetParent(foco.transform);luzObjeto.transform.localPosition=new Vector3(0,-.25f,0);var luz=luzObjeto.AddComponent<Light>();luz.type=LightType.Spot;luz.range=6;luz.intensity=3;luz.spotAngle=78;luz.color=new Color(.74f,.93f,1);luzObjeto.transform.rotation=Quaternion.Euler(90,0,0);
        var linea=new GameObject("Rayo_de_interaccion");var renderer=linea.AddComponent<LineRenderer>();renderer.positionCount=2;renderer.startWidth=.012f;renderer.endWidth=.004f;renderer.material=verde;renderer.useWorldSpace=true;
        Iluminar();
        var juego=new GameObject("Control_del_nivel").AddComponent<NivelCirugiaVR>();
        juego.jugador=jugador;juego.camara=camara;juego.manoIzquierda=manoI;juego.manoDerecha=manoD;juego.pechoPaciente=cuerpo.transform;juego.lampara=foco.transform;juego.trazoMonitor=trazo.transform;juego.bisturi=bisturi.transform;juego.separador=separador.transform;juego.gasa=gasa.transform;juego.pinzaExtraccion=pinzaExtraccion.transform;juego.hemostato=hemostato.transform;juego.cubiertaAbdominal=cubierta.transform;juego.cavidadAbdominal=cavidad.transform;juego.bordeIzquierdo=bordeIzquierdo.transform;juego.bordeDerecho=bordeDerecho.transform;juego.zonaIncision=zonaIncision.transform;juego.zonaRetraccion=zonaRetraccion.transform;juego.zonaHerida=zonaHerida.transform;juego.zonaCoagulo=zonaCoagulo.transform;juego.zonaSangrado=zonaSangrado.transform;juego.marcadorHemorragia=marcadorHemorragia.transform;juego.coagulo=coagulo.transform;juego.higado=higado.transform;juego.estomago=estomago.transform;juego.intestino=intestino.transform;juego.rinones=rinones;juego.instrucciones=txt;juego.estado=est;juego.rayo=renderer;juego.escenaSiguiente="Nivel_2_Urgencias";

        EditorSceneManager.SaveScene(sc,Base+"/Escenas/Nivel_1_Quirofano.unity");
        AssetDatabase.SaveAssets();
        Debug.Log("Nivel 1 listo: escena VR y controles de práctica creados.");
    }
    static void CrearJugador(Transform raiz,out Transform jugador,out Transform camara,out Transform manoI,out Transform manoD)
    {
        var g=new GameObject("Jugador_VR");g.transform.SetParent(raiz);g.transform.position=new Vector3(0,0,-2.25f);jugador=g.transform;
        var c=new GameObject("Camara_VR");c.tag="MainCamera";c.transform.SetParent(g.transform);c.transform.localPosition=new Vector3(0,1.62f,0);c.transform.localRotation=Quaternion.identity;var cam=c.AddComponent<Camera>();cam.stereoTargetEye=StereoTargetEyeMask.Both;cam.nearClipPlane=.03f;cam.farClipPlane=70;c.AddComponent<AudioListener>();var pose=c.AddComponent<TrackedPoseDriver>();pose.SetPoseSource(TrackedPoseDriver.DeviceType.GenericXRDevice,TrackedPoseDriver.TrackedPose.Head);camara=c.transform;
        manoI=CrearMano(g.transform,"Mano_izquierda",new Vector3(-.25f,1.25f,.4f));manoD=CrearMano(g.transform,"Mano_derecha",new Vector3(.25f,1.25f,.4f));
    }
    static Transform CrearMano(Transform p,string n,Vector3 pos){var g=new GameObject(n);g.transform.SetParent(p);g.transform.localPosition=pos;var material=CrearMaterial("Carcasa_mando",new Color(.075f,.1f,.12f));E("Empuñadura",g.transform,new Vector3(0,0,-.025f),new Vector3(.075f,.12f,.14f),material);var aro=E("Aro_de_seguimiento",g.transform,new Vector3(0,.055f,.045f),new Vector3(.18f,.018f,.18f),acero);aro.transform.localRotation=Quaternion.Euler(25,0,0);var gatillo=C("Gatillo",g.transform,new Vector3(0,.05f,.04f),new Vector3(.025f,.025f,.065f),rojo);gatillo.transform.localRotation=Quaternion.Euler(-20,0,0);E("Palanca",g.transform,new Vector3(0,.075f,-.005f),new Vector3(.035f,.022f,.035f),acero);foreach(var col in g.GetComponentsInChildren<Collider>())Object.DestroyImmediate(col);var rb=g.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;rb.interpolation=RigidbodyInterpolation.Interpolate;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;var zona=g.AddComponent<SphereCollider>();zona.radius=.045f;zona.isTrigger=true;return g.transform;}
    static void Iluminar(){var sol=new GameObject("Luz_general").AddComponent<Light>();sol.type=LightType.Directional;sol.intensity=1.15f;sol.transform.rotation=Quaternion.Euler(45,-25,0);RenderSettings.ambientLight=new Color(.42f,.48f,.51f);}
    static GameObject Modelo(string nombre,Vector3 posicion,Vector3 escala){string ruta="Assets/Modelos_MEDIVR/FBX_Para_Unity/"+nombre+".fbx";var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(ruta);if(!prefab)return null;var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab);g.name=nombre;g.transform.position=posicion;g.transform.localScale=escala;foreach(var f in g.GetComponentsInChildren<MeshFilter>())if(f.sharedMesh&&!f.GetComponent<Collider>())f.gameObject.AddComponent<MeshCollider>();return g;}
    static GameObject C(string n,Transform p,Vector3 v,Vector3 s,Material m){var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=n;o.transform.SetParent(p);o.transform.localPosition=v;o.transform.localScale=s;o.GetComponent<Renderer>().sharedMaterial=m;return o;}
    static GameObject E(string n,Transform p,Vector3 v,Vector3 s,Material m){var o=GameObject.CreatePrimitive(PrimitiveType.Sphere);o.name=n;o.transform.SetParent(p);o.transform.localPosition=v;o.transform.localScale=s;o.GetComponent<Renderer>().sharedMaterial=m;return o;}
    static Transform CrearHigado(Transform p,Vector3 v){higadoMat=CrearMaterial("Tejido_hepatico",new Color(.40f,.105f,.075f));var r=new GameObject("Higado");r.transform.SetParent(p);r.transform.localPosition=v;Accion(r,"organo_higado");E("Lobulo_derecho",r.transform,new Vector3(-.075f,0,0),new Vector3(.29f,.15f,.24f),higadoMat);E("Lobulo_izquierdo",r.transform,new Vector3(.105f,.012f,.025f),new Vector3(.21f,.125f,.19f),higadoMat);Fisica(r,.8f,true,true);return r.transform;}
    static Transform CrearEstomago(Transform p,Vector3 v){estomagoMat=CrearMaterial("Tejido_gastrico",new Color(.69f,.28f,.23f));var r=new GameObject("Estomago");r.transform.SetParent(p);r.transform.localPosition=v;Accion(r,"organo_estomago");E("Cuerpo_gastrico",r.transform,new Vector3(0,0,0),new Vector3(.19f,.14f,.26f),estomagoMat);E("Fundus_gastrico",r.transform,new Vector3(.075f,.015f,-.105f),new Vector3(.16f,.13f,.16f),estomagoMat);E("Antro_gastrico",r.transform,new Vector3(-.035f,-.005f,.14f),new Vector3(.115f,.095f,.17f),estomagoMat);Fisica(r,.45f,true,true);return r.transform;}
    static Transform CrearIntestino(Transform p,Vector3 v){intestinoMat=CrearMaterial("Tejido_intestinal",new Color(.73f,.43f,.32f));var r=new GameObject("Intestino_delgado");r.transform.SetParent(p);r.transform.localPosition=v;Accion(r,"organo_intestino");for(int i=0;i<18;i++){float x=-.14f+(i%3)*.14f;float z=-.15f+(i/3)*.052f;var l=E("Asa_intestinal_"+(i+1).ToString("00"),r.transform,new Vector3(x,Mathf.Abs((i%2)*.012f),z),new Vector3(.16f,.055f,.073f),intestinoMat);l.transform.localRotation=Quaternion.Euler(0,0,(i%2==0?22:-22));}Fisica(r,.5f,true,true);return r.transform;}
    static Transform CrearCoagulo(Transform p,Vector3 v){var r=new GameObject("Coagulo_Simulado");r.transform.SetParent(p);r.transform.localPosition=v;Accion(r,"coagulo");E("Fragmento_principal",r.transform,Vector3.zero,new Vector3(.105f,.075f,.09f),coaguloMat);E("Fragmento_secundario",r.transform,new Vector3(.045f,.008f,.025f),new Vector3(.055f,.045f,.052f),coaguloMat);Fisica(r,.12f,true,true);return r.transform;}
    static GameObject CrearCavidadAbdominal(Transform p){var r=new GameObject("Cavidad_Abdominal");r.transform.SetParent(p);C("Fondo_de_cavidad",r.transform,new Vector3(0,1.315f,.73f),new Vector3(.52f,.025f,.66f),goma);C("Pared_interna_izquierda",r.transform,new Vector3(-.28f,1.355f,.73f),new Vector3(.045f,.095f,.66f),goma);C("Pared_interna_derecha",r.transform,new Vector3(.28f,1.355f,.73f),new Vector3(.045f,.095f,.66f),goma);C("Pared_interna_superior",r.transform,new Vector3(0,1.355f,.38f),new Vector3(.52f,.095f,.045f),goma);C("Pared_interna_inferior",r.transform,new Vector3(0,1.355f,1.08f),new Vector3(.52f,.095f,.045f),goma);r.SetActive(false);return r;}
    static Transform CrearRinones(Transform p){var renalMat=CrearMaterial("Tejido_renal",new Color(.48f,.20f,.17f));var r=new GameObject("Rinones");r.transform.SetParent(p);E("Rinon_izquierdo",r.transform,new Vector3(-.22f,1.36f,.88f),new Vector3(.095f,.07f,.13f),renalMat);E("Rinon_derecho",r.transform,new Vector3(.22f,1.36f,.88f),new Vector3(.095f,.07f,.13f),renalMat);return r.transform;}
    static GameObject CrearSeparador(Transform p,Vector3 v){var m=new GameObject("Separador_Abdominal");m.transform.SetParent(p);m.transform.localPosition=v;Accion(m,"separador");var mango=E("Mango_del_separador",m.transform,new Vector3(0,0,-.12f),new Vector3(.055f,.055f,.22f),acero);var pala=C("Pala_del_separador",m.transform,new Vector3(0,0,.055f),new Vector3(.17f,.025f,.075f),acero);pala.transform.localRotation=Quaternion.Euler(0,0,-12);return m;}
    static GameObject CrearPinza(Transform p,Vector3 v){var m=new GameObject("Pinza_quirurgica");m.transform.SetParent(p);m.transform.localPosition=v;var a=C("Rama_izquierda",m.transform,new Vector3(-.018f,0,0),new Vector3(.012f,.012f,.24f),acero);var b=C("Rama_derecha",m.transform,new Vector3(.018f,0,0),new Vector3(.012f,.012f,.24f),acero);a.transform.localRotation=Quaternion.Euler(0,-7,0);b.transform.localRotation=Quaternion.Euler(0,7,0);E("Punta_izquierda",m.transform,new Vector3(-.018f,0,.12f),new Vector3(.018f,.018f,.04f),blanco);E("Punta_derecha",m.transform,new Vector3(.018f,0,.12f),new Vector3(.018f,.018f,.04f),blanco);return m;}
    static GameObject CrearMesaInstrumental(Transform p){var r=new GameObject("Mesa_instrumental");r.transform.SetParent(p);r.transform.position=new Vector3(-1.55f,0,.45f);C("Superficie_de_acero",r.transform,new Vector3(0,.92f,0),new Vector3(1.05f,.10f,.65f),acero);for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)C("Pata_de_mesa",r.transform,new Vector3(x*.43f,.47f,z*.25f),new Vector3(.055f,.86f,.055f),acero);C("Estante_inferior",r.transform,new Vector3(0,.34f,0),new Vector3(.92f,.045f,.52f),blanco);return r;}
    static GameObject CrearGasa(Transform p,Vector3 v){var m=C("Gasa_esteril",p,v,new Vector3(.13f,.022f,.13f),blanco);Accion(m,"gasa");return m;}
    static void Fisica(GameObject g,float masa,bool gravedad,bool isKinematic){var rb=g.GetComponent<Rigidbody>();if(!rb)rb=g.AddComponent<Rigidbody>();rb.mass=masa;rb.useGravity=gravedad;rb.isKinematic=isKinematic;rb.interpolation=RigidbodyInterpolation.Interpolate;rb.collisionDetectionMode=isKinematic?CollisionDetectionMode.ContinuousSpeculative:CollisionDetectionMode.ContinuousDynamic;foreach(var m in g.GetComponentsInChildren<MeshCollider>())m.convex=true;if(!g.GetComponent<NodoFisicoVR>())g.AddComponent<NodoFisicoVR>();}
    static void Accion(GameObject o,string tipo){var a=o.AddComponent<AccionQuirurgica>();a.tipo=tipo;}
    static TextMesh Texto(string n,string s,Vector3 p,float size,Material m){var g=new GameObject(n);g.transform.position=p;g.transform.rotation=Quaternion.Euler(0,180,0);var t=g.AddComponent<TextMesh>();t.text=s;t.fontSize=52;t.characterSize=size/52;t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.color=m.color;return t;}
    static Material CrearMaterial(string nombre,Color c,float metal=0){string ruta=Base+"/Materiales/"+nombre+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(ruta);if(m){m.color=c;if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",c);if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",metal);EditorUtility.SetDirty(m);return m;}var shader=Shader.Find("Universal Render Pipeline/Lit");if(!shader)shader=Shader.Find("Standard");m=new Material(shader){name=nombre,color=c};if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",c);if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",metal);AssetDatabase.CreateAsset(m,ruta);return m;}
    static void CrearMateriales(){pared=CrearMaterial("Pared_clara",new Color(.79f,.86f,.87f));piso=CrearMaterial("Piso_clinico",new Color(.33f,.48f,.51f));blanco=CrearMaterial("Blanco_medico",new Color(.92f,.96f,.95f));petroleo=CrearMaterial("Azul_petroleo",new Color(.025f,.14f,.17f));verde=CrearMaterial("Verde_estado",new Color(.1f,.72f,.53f));acero=CrearMaterial("Acero",new Color(.58f,.66f,.68f),.8f);goma=CrearMaterial("Goma_oscura",new Color(.003f,.006f,.009f));pantalla=CrearMaterial("Pantalla_oscura",new Color(.01f,.07f,.08f));tela=CrearMaterial("Tela_quirurgica",new Color(.025f,.27f,.25f));piel=CrearMaterial("Piel_simulada",new Color(.68f,.43f,.34f));rojo=CrearMaterial("Rojo_alerta",new Color(.72f,.035f,.05f));tejidoMat=CrearMaterial("Tejido_interno",new Color(.36f,.075f,.07f));coaguloMat=CrearMaterial("Coagulo_simulado",new Color(.22f,.018f,.025f));}
    static void AsegurarCarpeta(string ruta){if(AssetDatabase.IsValidFolder(ruta))return;var padre=Path.GetDirectoryName(ruta).Replace('\\','/');var nombre=Path.GetFileName(ruta);if(!AssetDatabase.IsValidFolder(padre)){if(padre=="Assets/NivelVR")AssetDatabase.CreateFolder("Assets","NivelVR");else AsegurarCarpeta(padre);}AssetDatabase.CreateFolder(padre,nombre);}
}
