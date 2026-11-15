using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using System.Collections.Generic;

/// <summary>Recorrido, interacciones por rayo y secuencia de objetivos para los niveles 2–5.</summary>
public class ControlNivelVR : MonoBehaviour
{
    public Transform jugador, camara, manoIzquierda, manoDerecha;
    public Transform pechoPaciente, trazoMonitor, lampara, luzAlerta;
    public TextMesh instrucciones, estado;
    public LineRenderer rayo;
    public string[] secuencia;
    public string escenaSiguiente;
    public bool nivelFinal;

    readonly List<InputDevice> dispositivos=new List<InputDevice>();
    int paso;
    bool gatilloAnterior, gatilloMantenido, agarreDerecho, agarreAnterior, giroAnterior, esUrgencias;
    float tiempo, pausaGiro, tiempoRescate=135f, cargaDescarga, tiempoCartel;
    Vector3 escalaPaciente, posicionTrazo;
    Transform tanqueOxigeno, desfibrilador, zonaOxigeno, zonaDescarga, puntoReinicio, puntoCamilla, objetoAgarrado;
    Vector3 posicionMandoDerecho;
    Quaternion rotacionMandoDerecho;

    void Start()
    {
        if(pechoPaciente) escalaPaciente=pechoPaciente.localScale;
        if(trazoMonitor) posicionTrazo=trazoMonitor.localPosition;
        esUrgencias=secuencia!=null&&secuencia.Length==4&&secuencia[0]=="monitor"&&secuencia[1]=="oxigeno"&&secuencia[2]=="desfibrilador"&&secuencia[3]=="camilla";
        if(esUrgencias)PrepararUrgencias();
        if(SceneManager.GetActiveScene().name=="Nivel_2_Urgencias"&&!FindObjectOfType<EstiloVisualQuest>())gameObject.AddComponent<EstiloVisualQuest>();
        if(instrucciones) instrucciones.text="JOYSTICK IZQ. MOVER · JOYSTICK DER. GIRAR\\nAPUNTA CON EL CONTROL Y APRIETA EL GATILLO";
        if(esUrgencias){if(instrucciones)instrucciones.text="RESCATE EN URGENCIAS · AGARRE: TOMAR / SOLTAR\\nGATILLO: INTERACTUAR · PALANCA IZQ.: MOVER";ActualizarEstado("Tiempo de rescate: 2:15 · revisa primero el monitor");}
        else ActualizarEstado("Objetivo 1: "+NombrePasoActual());
    }
    void Update()
    {
        tiempo+=Time.deltaTime;
        if(pechoPaciente) pechoPaciente.localScale=escalaPaciente+new Vector3(0,0,Mathf.Sin(tiempo*1.15f)*.025f);
        if(trazoMonitor) trazoMonitor.localPosition=posicionTrazo+new Vector3(Mathf.PingPong(tiempo*.85f,.24f)-.12f,0,0);
        if(lampara) lampara.localRotation=Quaternion.Euler(0,0,Mathf.Sin(tiempo*.28f)*.7f);
        if(luzAlerta) luzAlerta.localScale=Vector3.one*(.8f+Mathf.PingPong(tiempo*1.8f,.35f));
        if(esUrgencias)ActualizarRetoUrgencias();
        ActualizarMano(XRNode.LeftHand,manoIzquierda);ActualizarMano(XRNode.RightHand,manoDerecha);
        if(rayo&&manoDerecha){rayo.SetPosition(0,manoDerecha.position);rayo.SetPosition(1,manoDerecha.position+manoDerecha.forward*4f);}
        LeerControlesVisor();
        if(Input.GetMouseButtonDown(0)||Input.GetKeyDown(KeyCode.Space)) Interactuar(false);
        if(Input.GetKey(KeyCode.W)) Mover(Vector2.up); if(Input.GetKey(KeyCode.S)) Mover(Vector2.down);
        if(Input.GetKey(KeyCode.A)) Mover(Vector2.left); if(Input.GetKey(KeyCode.D)) Mover(Vector2.right);
        if(Input.GetKeyDown(KeyCode.LeftArrow)) jugador.Rotate(0,-30,0);
        if(Input.GetKeyDown(KeyCode.RightArrow)) jugador.Rotate(0,30,0);
    }
    void FixedUpdate()
    {
        if(esUrgencias&&manoDerecha&&manoDerecha.gameObject.activeInHierarchy){var rb=manoDerecha.GetComponent<Rigidbody>();if(rb){rb.MovePosition(posicionMandoDerecho);rb.MoveRotation(rotacionMandoDerecho);}}
    }
    void ActualizarMano(XRNode nodo,Transform mano)
    {
        if(!mano)return;dispositivos.Clear();InputDevices.GetDevicesAtXRNode(nodo,dispositivos);
        foreach(var d in dispositivos)if(d.isValid&&d.TryGetFeatureValue(CommonUsages.devicePosition,out Vector3 p)&&d.TryGetFeatureValue(CommonUsages.deviceRotation,out Quaternion q)){if(esUrgencias&&nodo==XRNode.RightHand){posicionMandoDerecho=jugador.TransformPoint(p);rotacionMandoDerecho=jugador.rotation*q;}else mano.SetPositionAndRotation(jugador.TransformPoint(p),jugador.rotation*q);mano.gameObject.SetActive(true);return;}
        mano.gameObject.SetActive(false);
    }
    void LeerControlesVisor()
    {
        gatilloMantenido=false;agarreDerecho=false;
        dispositivos.Clear();InputDevices.GetDevicesAtXRNode(XRNode.RightHand,dispositivos);
        foreach(var d in dispositivos)if(d.isValid)
        {
            bool boton=false,agarre=false;float gatillo=0,agarreValor=0;
            d.TryGetFeatureValue(CommonUsages.triggerButton,out boton);d.TryGetFeatureValue(CommonUsages.trigger,out gatillo);
            d.TryGetFeatureValue(CommonUsages.gripButton,out agarre);d.TryGetFeatureValue(CommonUsages.grip,out agarreValor);
            gatilloMantenido=boton||gatillo>.55f;agarreDerecho=agarre||agarreValor>.55f;
            if(gatilloMantenido&&!gatilloAnterior)Interactuar(true);
            if(esUrgencias&&agarreDerecho&&!agarreAnterior)IntentarTomarEquipo();
            if(esUrgencias&&!agarreDerecho&&agarreAnterior)SoltarEquipo();
            gatilloAnterior=gatilloMantenido;agarreAnterior=agarreDerecho;break;
        }
        dispositivos.Clear();InputDevices.GetDevicesAtXRNode(XRNode.LeftHand,dispositivos);
        foreach(var d in dispositivos)if(d.isValid&&d.TryGetFeatureValue(CommonUsages.primary2DAxis,out Vector2 eje)&&eje.sqrMagnitude>.01f){Mover(eje);break;}
        pausaGiro-=Time.deltaTime;dispositivos.Clear();InputDevices.GetDevicesAtXRNode(XRNode.RightHand,dispositivos);
        foreach(var d in dispositivos)if(d.isValid&&d.TryGetFeatureValue(CommonUsages.primary2DAxis,out Vector2 eje)){bool gira=Mathf.Abs(eje.x)>.72f;if(gira&&!giroAnterior&&pausaGiro<=0){jugador.Rotate(0,eje.x>0?30:-30,0);pausaGiro=.25f;}giroAnterior=gira;break;}
    }
    void Mover(Vector2 eje)
    {
        if(!jugador||!camara)return;Vector3 frente=Vector3.ProjectOnPlane(camara.forward,Vector3.up).normalized;Vector3 derecha=Vector3.ProjectOnPlane(camara.right,Vector3.up).normalized;
        jugador.position+=(frente*eje.y+derecha*eje.x)*(Time.deltaTime*1.05f);jugador.position=new Vector3(Mathf.Clamp(jugador.position.x,-3.25f,3.25f),0,Mathf.Clamp(jugador.position.z,-3.25f,3.25f));
    }
    void Interactuar(bool visor)
    {
        if(esUrgencias){InteractuarUrgencias(visor);return;}
        RaycastHit golpe=default(RaycastHit);bool acierto=false;
        if(visor&&manoDerecha)acierto=Physics.Raycast(manoDerecha.position,manoDerecha.forward,out golpe,4f);
        else if(camara)acierto=Physics.Raycast(camara.position,camara.forward,out golpe,4f);
        if(!acierto)return;var accion=golpe.collider.GetComponentInParent<AccionQuirurgica>();if(!accion)return;
        if(accion.tipo=="continuar")
        {
            if(paso<secuencia.Length){ActualizarEstado("Completa primero: "+NombrePasoActual());return;}
            if(nivelFinal){ActualizarEstado("Nivel 5 completado · práctica terminada");return;}
            if(!string.IsNullOrEmpty(escenaSiguiente))SceneManager.LoadScene(escenaSiguiente);return;
        }
        if(paso>=secuencia.Length){ActualizarEstado(nivelFinal?"Nivel completado":"Ve a la salida para continuar");return;}
        if(accion.tipo==secuencia[paso])
        {
            var render=accion.GetComponentInChildren<Renderer>();if(render)render.material.color=new Color(.12f,.72f,.52f);
            paso++;ActualizarEstado(paso<secuencia.Length?"Correcto · siguiente: "+NombrePasoActual():nivelFinal?"Objetivos listos · práctica completada":"Objetivos listos · busca la salida");
        }
        else ActualizarEstado("Ahora completa: "+NombrePasoActual());
    }
    void PrepararUrgencias()
    {
        tanqueOxigeno=BuscarObjetivo("oxigeno");desfibrilador=BuscarObjetivo("desfibrilador");puntoCamilla=BuscarObjetivo("camilla");
        OcultarDuplicados("Tanque_Oxigeno",tanqueOxigeno);OcultarDuplicados("Desfibrilador",desfibrilador);
        PrepararCuerpo(tanqueOxigeno,.9f);PrepararCuerpo(desfibrilador,1.2f);PrepararMano(manoDerecha);
        Vector3 centroCamilla=puntoCamilla?puntoCamilla.position-new Vector3(0,.92f,.08f):new Vector3(0,0,.7f);
        zonaOxigeno=CrearMarcaFisica("Zona_de_Oxigeno_del_paciente",centroCamilla+new Vector3(-.34f,.82f,0),new Vector3(.34f,.16f,.3f),"zona_oxigeno",new Color(.1f,.72f,.53f)).transform;
        zonaOxigeno.gameObject.SetActive(false);
        zonaDescarga=CrearMarcaFisica("Zona_de_Descarga_del_paciente",centroCamilla+new Vector3(.31f,.86f,0),new Vector3(.35f,.12f,.28f),"zona_descarga",new Color(.95f,.48f,.08f)).transform;
        puntoReinicio=CrearMarcaFisica("Punto_de_reinicio",new Vector3(0,1.05f,-3.25f),new Vector3(.48f,.18f,.1f),"reiniciar",Color.red).transform;
        puntoReinicio.gameObject.SetActive(false);if(puntoCamilla){var aviso=puntoCamilla.GetComponentInChildren<TextMesh>();if(aviso)aviso.text="ACERCA LA CAMILLA";}
    }
    Transform BuscarObjetivo(string tipo)
    {
        foreach(var accion in FindObjectsOfType<AccionQuirurgica>())if(accion.gameObject.scene.IsValid()&&accion.tipo==tipo)return accion.transform;
        return null;
    }
    void OcultarDuplicados(string nombre,Transform principal)
    {
        if(!principal)return;
        foreach(var raiz in FindObjectsOfType<Transform>())if(raiz!=principal&&raiz.name==nombre)
        {
            foreach(var col in raiz.GetComponentsInChildren<Collider>())col.enabled=false;
            foreach(var render in raiz.GetComponentsInChildren<Renderer>())render.enabled=false;
        }
    }
    void PrepararMano(Transform mano)
    {
        if(!mano)return;var rb=mano.GetComponent<Rigidbody>();if(!rb)rb=mano.gameObject.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;rb.interpolation=RigidbodyInterpolation.Interpolate;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
        var zona=mano.GetComponent<SphereCollider>();if(!zona)zona=mano.gameObject.AddComponent<SphereCollider>();zona.radius=.055f;zona.isTrigger=true;
    }
    void PrepararCuerpo(Transform equipo,float masa)
    {
        if(!equipo)return;var rb=equipo.GetComponent<Rigidbody>();if(!rb)rb=equipo.gameObject.AddComponent<Rigidbody>();rb.mass=masa;rb.useGravity=true;rb.isKinematic=false;rb.interpolation=RigidbodyInterpolation.Interpolate;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
        foreach(var col in equipo.GetComponentsInChildren<MeshCollider>())col.convex=true;
        if(!equipo.GetComponentInChildren<Collider>())equipo.gameObject.AddComponent<SphereCollider>();
        if(!equipo.GetComponent<NodoFisicoVR>())equipo.gameObject.AddComponent<NodoFisicoVR>();
    }
    GameObject CrearMarcaFisica(string nombre,Vector3 posicion,Vector3 escala,string tipo,Color color)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=nombre;g.transform.position=posicion;g.transform.localScale=escala;g.GetComponent<Collider>().isTrigger=true;
        var shader=Shader.Find("Universal Render Pipeline/Lit");if(shader)g.GetComponent<Renderer>().sharedMaterial=new Material(shader){color=color};var accion=g.AddComponent<AccionQuirurgica>();accion.tipo=tipo;return g;
    }
    void IntentarTomarEquipo()
    {
        if(paso!=1&&paso!=2||!manoDerecha)return;
        Transform equipo=paso==1?tanqueOxigeno:desfibrilador;
        if(!equipo||!RayoDeControl(manoDerecha,1.6f,out RaycastHit golpe)||golpe.collider.GetComponentInParent<AccionQuirurgica>()==null||golpe.collider.GetComponentInParent<AccionQuirurgica>().transform!=equipo)return;
        if(Vector3.Distance(equipo.position,manoDerecha.position)>.72f){ActualizarEstado("Acércate al equipo antes de agarrarlo");return;}
        var nodo=equipo.GetComponent<NodoFisicoVR>();if(nodo&&nodo.Agarrar(manoDerecha.GetComponent<Rigidbody>())){objetoAgarrado=equipo;ActualizarEstado(paso==1?"Oxígeno en mano · acóplalo junto a la camilla":"Desfibrilador en mano · llévalo hasta el pecho del paciente");}
    }
    void SoltarEquipo()
    {
        if(!objetoAgarrado)return;var nodo=objetoAgarrado.GetComponent<NodoFisicoVR>();if(nodo)nodo.Soltar();objetoAgarrado=null;
    }
    void InteractuarUrgencias(bool visor)
    {
        Transform origen=visor?manoDerecha:camara;
        if(!RayoDeControl(origen,4f,out RaycastHit golpe))return;
        var accion=golpe.collider.GetComponentInParent<AccionQuirurgica>();if(!accion)return;
        if(paso==99&&accion.tipo=="reiniciar"){SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);return;}
        if(paso==0&&accion.tipo=="monitor"){paso=1;if(zonaOxigeno)zonaOxigeno.gameObject.SetActive(true);ActualizarEstado("Monitor revisado · agarra el tanque de oxígeno con el mando derecho");return;}
        if(paso==1&&accion.tipo=="zona_oxigeno"&&objetoAgarrado==tanqueOxigeno&&Vector3.Distance(tanqueOxigeno.position,zonaOxigeno.position)<.42f)
        {GuardarEquipo(tanqueOxigeno);objetoAgarrado=null;paso=2;ActualizarEstado("Oxígeno conectado · toma el desfibrilador y acércalo al pecho");return;}
        if(paso==2&&accion.tipo=="zona_descarga"&&objetoAgarrado==desfibrilador&&Vector3.Distance(desfibrilador.position,zonaDescarga.position)<.42f){ActualizarEstado("Mantén el gatillo para cargar · "+Mathf.RoundToInt(cargaDescarga/2.5f*100)+"%");return;}
        if(paso==3&&accion.tipo=="camilla")
        {
            if(Vector3.Distance(jugador.position,accion.transform.position)>1.5f){ActualizarEstado("Acércate y asegura la camilla junto al paciente");return;}
            paso=4;SoltarEquipo();if(puntoCamilla){var aviso=puntoCamilla.GetComponentInChildren<TextMesh>();if(aviso)aviso.text="PACIENTE ASEGURADO";}ActualizarEstado("Rescate estabilizado · puedes continuar al siguiente nivel");return;
        }
        if(accion.tipo=="continuar")
        {
            if(paso<4){ActualizarEstado("Completa primero: "+NombrePasoUrgencias());return;}
            if(!string.IsNullOrEmpty(escenaSiguiente))SceneManager.LoadScene(escenaSiguiente);
        }
    }
    bool RayoDeControl(Transform origen,float distancia,out RaycastHit golpe)
    {
        golpe=default(RaycastHit);if(!origen)return false;return Physics.Raycast(origen.position+origen.forward*.07f,origen.forward,out golpe,distancia,~0,QueryTriggerInteraction.Collide);
    }
    void ActualizarRetoUrgencias()
    {
        if(paso<4&&paso<99)
        {
            if(paso==0)return;
            tiempoRescate-=Time.deltaTime;
            if(tiempoRescate<=0){paso=99;if(puntoReinicio)puntoReinicio.gameObject.SetActive(true);ActualizarEstado("Tiempo agotado · paciente crítico · pulsa el panel de reinicio");return;}
        }
        if(paso==2&&objetoAgarrado==desfibrilador&&gatilloMantenido&&RayoDeControl(manoDerecha,4f,out RaycastHit h)&&h.collider.GetComponentInParent<AccionQuirurgica>()?.tipo=="zona_descarga"&&Vector3.Distance(desfibrilador.position,zonaDescarga.position)<.42f)
        {
            if(zonaDescarga&&!zonaDescarga.gameObject.activeSelf)zonaDescarga.gameObject.SetActive(true);
            cargaDescarga+=Time.deltaTime;
            if(cargaDescarga>=2.5f){if(zonaDescarga)zonaDescarga.gameObject.SetActive(false);GuardarEquipo(desfibrilador);objetoAgarrado=null;paso=3;ActualizarEstado("Descarga aplicada · acércate y asegura la camilla");}
        }
        else if(paso==2)cargaDescarga=Mathf.Max(0,cargaDescarga-Time.deltaTime*.5f);
        if(tiempo-tiempoCartel>1f)
        {
            tiempoCartel=tiempo;int restante=Mathf.CeilToInt(tiempoRescate);if(paso<4)ActualizarEstado("Tiempo "+(restante/60).ToString("00")+":"+(restante%60).ToString("00")+" · "+NombrePasoUrgencias());
        }
    }
    void GuardarEquipo(Transform equipo){var nodo=equipo?equipo.GetComponent<NodoFisicoVR>():null;if(nodo)nodo.GuardarEnCampo();}
    string NombrePasoUrgencias(){return paso==0?"revisa el monitor":paso==1?"conecta el oxígeno":paso==2?"carga el desfibrilador":paso==3?"asegura la camilla":"rescate completo";}
    string NombrePasoActual(){if(secuencia==null||paso>=secuencia.Length)return "todos los objetivos";return secuencia[paso].Replace('_',' ');}
    void ActualizarEstado(string s){if(estado)estado.text=s;}
}







