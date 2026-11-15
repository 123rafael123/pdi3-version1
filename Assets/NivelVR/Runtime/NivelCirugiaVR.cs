using UnityEngine;
using UnityEngine.XR;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>Controles Touch y práctica quirúrgica simulada del primer nivel.</summary>
public class NivelCirugiaVR : MonoBehaviour
{
    static readonly RaycastHit[] resultadosRayo=new RaycastHit[32];
    public Transform jugador, camara, manoIzquierda, manoDerecha;
    public Transform pechoPaciente, lampara, trazoMonitor;
    public Transform bisturi, separador, gasa;
    public Transform pinzaExtraccion, hemostato;
    public Transform cubiertaAbdominal, cavidadAbdominal, bordeIzquierdo, bordeDerecho, zonaIncision, zonaRetraccion, zonaHerida;
    public Transform zonaCoagulo, zonaSangrado, marcadorHemorragia, coagulo;
    public Transform higado, estomago, intestino, rinones;
    public TextMesh instrucciones, estado;
    public LineRenderer rayo;
    public string escenaSiguiente;

    readonly List<InputDevice> dispositivos=new List<InputDevice>();
    // Fases: 0 monitor, 1 corte, 2 separador, 3 órganos, 4 hemostasia, 5 sujetar coágulo, 6 depositarlo, 7 cierre, 8 salida.
    int paso, organoActual;
    bool gatilloDerecho, gripDerecho, gripIzquierdo;
    bool gatilloDerechoAnterior, gripDerechoAnterior, gripIzquierdoAnterior;
    bool bisturiEnMano, separadorEnMano, gasaEnMano, pinzaEnMano, hemostatoEnMano, coaguloEnPinza, sangradoControlado;
    Transform organoEnMano;
    bool organoEnManoIzquierda;
    Transform trazoCorteVisual;
    bool animarOrganos;
    bool organoResaltadoHigado,organoResaltadoEstomago,organoResaltadoIntestino;
    bool giroAnterior, tienePosicionCorte;
    float tiempo, pausaGiro, avanceCorte, tiempoHemorragia, progresoHemostasia, estabilidad=100f, tiempoTexto, progresoApertura;
    Vector3 escalaPecho, posicionTrazo, ultimaPosicionCorte, bordeIzqInicial, bordeDerInicial;
    Quaternion giroLampara, giroHerramienta;
    Renderer[] renderHigado,renderEstomago,renderIntestino;
    MaterialPropertyBlock bloqueMaterial;
    Vector3 destinoManoIzquierda,destinoManoDerecha;
    Quaternion giroManoIzquierda,giroManoDerecha;
    bool manoIzquierdaRastreada,manoDerechaRastreada;
    const float LargoCorte=.42f, LimiteHemorragia=38f, TiempoHemostasia=1.35f;

    void Start()
    {
        PrepararEscenaFisica();
        CrearVisualDeCorte();
        if(!FindObjectOfType<EstiloVisualQuest>())gameObject.AddComponent<EstiloVisualQuest>();
        if(pechoPaciente) escalaPecho=pechoPaciente.localScale;
        if(trazoMonitor) posicionTrazo=trazoMonitor.localPosition;
        if(lampara) giroLampara=lampara.localRotation;
        if(higado)renderHigado=higado.GetComponentsInChildren<Renderer>();if(estomago)renderEstomago=estomago.GetComponentsInChildren<Renderer>();if(intestino)renderIntestino=intestino.GetComponentsInChildren<Renderer>();
        bloqueMaterial=new MaterialPropertyBlock();
        if(bordeIzquierdo)bordeIzqInicial=bordeIzquierdo.localPosition;if(bordeDerecho)bordeDerInicial=bordeDerecho.localPosition;
        Mostrar("AGARRE D.: TOMAR · GATILLO D.: USAR\nAGARRE I.: TOMAR SEPARADOR · PALANCAS: MOVER / GIRAR");
        Estado("FASE 1/8 · estabiliza al paciente revisando el monitor");
        if(cavidadAbdominal)cavidadAbdominal.gameObject.SetActive(false);
        MostrarOrganos(false);
        if(bordeIzquierdo)bordeIzquierdo.gameObject.SetActive(false);if(bordeDerecho)bordeDerecho.gameObject.SetActive(false);
        if(coagulo)coagulo.gameObject.SetActive(false);if(zonaCoagulo)zonaCoagulo.gameObject.SetActive(false);
        if(zonaSangrado)zonaSangrado.gameObject.SetActive(false);if(marcadorHemorragia)marcadorHemorragia.gameObject.SetActive(false);if(rinones)rinones.gameObject.SetActive(false);
        if(pinzaExtraccion)pinzaExtraccion.gameObject.SetActive(false);if(hemostato)hemostato.gameObject.SetActive(false);
    }
    void PrepararEscenaFisica()
    {
        Transform raiz=higado?higado.parent:transform;
        if(!raiz)raiz=transform;
        if(!bisturi)bisturi=Buscar("Bisturi");if(!separador)separador=Buscar("Separador_Abdominal");if(!gasa)gasa=Buscar("Gasa_esteril");
        if(!cubiertaAbdominal)cubiertaAbdominal=Buscar("Cubierta_Abdominal");if(!cavidadAbdominal)cavidadAbdominal=Buscar("Cavidad_Abdominal");
        if(!zonaIncision)zonaIncision=Buscar("Zona_de_Incision");if(!zonaRetraccion)zonaRetraccion=Buscar("Zona_de_Separacion");if(!zonaHerida)zonaHerida=Buscar("Zona_de_Herida");
        if(!higado)higado=Buscar("Higado");if(!estomago)estomago=Buscar("Estomago");if(!intestino)intestino=Buscar("Intestino_delgado");
        PrepararCavidadAntigua();
        if(!bordeIzquierdo)bordeIzquierdo=CrearEsfera("Borde_de_tejido_izquierdo",raiz,new Vector3(-.20f,1.302f,.73f),new Vector3(.14f,.022f,.5f),new Color(.36f,.075f,.07f)).transform;
        if(!bordeDerecho)bordeDerecho=CrearEsfera("Borde_de_tejido_derecho",raiz,new Vector3(.20f,1.302f,.73f),new Vector3(.14f,.022f,.5f),new Color(.36f,.075f,.07f)).transform;
        if(!pinzaExtraccion)pinzaExtraccion=CrearPinzaFisica("Pinza_de_Extraccion",raiz,new Vector3(-1.55f,1.02f,.52f),"pinza_extraccion").transform;
        if(!hemostato)hemostato=CrearPinzaFisica("Pinza_Hemostatica_de_Control",raiz,new Vector3(-1.55f,1.02f,.76f),"pinza_hemostatica").transform;
        if(!gasa){gasa=CrearCubo("Gasa_esteril",raiz,new Vector3(-1.55f,1.025f,.12f),new Vector3(.13f,.022f,.13f),new Color(.92f,.96f,.95f)).transform;var a=gasa.gameObject.AddComponent<AccionQuirurgica>();a.tipo="gasa";}
        if(!coagulo)coagulo=CrearCoaguloFisico(raiz).transform;
        if(!zonaCoagulo)zonaCoagulo=CrearZonaFisica("Zona_de_Extraccion",raiz,new Vector3(-1.55f,1.015f,.45f),new Vector3(.16f,.025f,.15f),"zona_extraccion",new Color(.1f,.72f,.53f)).transform;
        if(!zonaSangrado)zonaSangrado=CrearZonaFisica("Zona_de_Hemorragia",raiz,new Vector3(.18f,1.405f,.72f),new Vector3(.09f,.03f,.1f),"zona_hemorragia",new Color(.78f,.035f,.05f)).transform;
        if(!marcadorHemorragia)marcadorHemorragia=CrearEsfera("Pulso_de_hemorragia",raiz,new Vector3(.18f,1.42f,.72f),new Vector3(.055f,.025f,.055f),new Color(.9f,.02f,.02f)).transform;
        if(zonaRetraccion)zonaRetraccion.gameObject.SetActive(false);if(zonaIncision)zonaIncision.gameObject.SetActive(false);if(zonaHerida)zonaHerida.gameObject.SetActive(false);
        if(zonaCoagulo)zonaCoagulo.gameObject.SetActive(false);if(zonaSangrado)zonaSangrado.gameObject.SetActive(false);if(marcadorHemorragia)marcadorHemorragia.gameObject.SetActive(false);
        if(pinzaExtraccion)pinzaExtraccion.gameObject.SetActive(false);if(hemostato)hemostato.gameObject.SetActive(false);if(coagulo)coagulo.gameObject.SetActive(false);
        PrepararCuerpoFisico(bisturi,.18f,true);PrepararCuerpoFisico(separador,.32f,true);PrepararCuerpoFisico(gasa,.06f,true);
        PrepararCuerpoFisico(pinzaExtraccion,.2f,true);PrepararCuerpoFisico(hemostato,.2f,true);
        PrepararCuerpoFisico(higado,.7f,true);PrepararCuerpoFisico(estomago,.4f,true);PrepararCuerpoFisico(intestino,.5f,true);PrepararCuerpoFisico(coagulo,.12f,true);
        PrepararMano(manoIzquierda);PrepararMano(manoDerecha);
    }
    Transform Buscar(string nombre){var g=GameObject.Find(nombre);return g?g.transform:null;}
    void PrepararCavidadAntigua()
    {
        if(!cavidadAbdominal)return;
        var renderizadores=cavidadAbdominal.GetComponentsInChildren<Renderer>();
        if(renderizadores.Length>1)return;
        foreach(var render in renderizadores)render.enabled=false;
        foreach(var col in cavidadAbdominal.GetComponents<Collider>())col.enabled=false;
        cavidadAbdominal.localScale=Vector3.one;
        CrearCubo("Fondo_de_cavidad",cavidadAbdominal,new Vector3(0,.041f,0),new Vector3(.52f,.025f,.66f),new Color(.003f,.006f,.009f));
        CrearCubo("Pared_interna_izquierda",cavidadAbdominal,new Vector3(-.28f,.081f,0),new Vector3(.045f,.095f,.66f),new Color(.003f,.006f,.009f));
        CrearCubo("Pared_interna_derecha",cavidadAbdominal,new Vector3(.28f,.081f,0),new Vector3(.045f,.095f,.66f),new Color(.003f,.006f,.009f));
        CrearCubo("Pared_interna_superior",cavidadAbdominal,new Vector3(0,.081f,-.35f),new Vector3(.52f,.095f,.045f),new Color(.003f,.006f,.009f));
        CrearCubo("Pared_interna_inferior",cavidadAbdominal,new Vector3(0,.081f,.35f),new Vector3(.52f,.095f,.045f),new Color(.003f,.006f,.009f));
    }
    void PrepararMano(Transform mano)
    {
        if(!mano)return;var rb=mano.GetComponent<Rigidbody>();if(!rb)rb=mano.gameObject.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;rb.interpolation=RigidbodyInterpolation.Interpolate;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
        var col=mano.GetComponent<SphereCollider>();if(!col)col=mano.gameObject.AddComponent<SphereCollider>();col.radius=.085f;col.isTrigger=true;
    }
    void PrepararCuerpoFisico(Transform raiz,float masa,bool gravedad)
    {
        if(!raiz)return;var rb=raiz.GetComponent<Rigidbody>();if(!rb)rb=raiz.gameObject.AddComponent<Rigidbody>();rb.mass=masa;rb.useGravity=gravedad;rb.isKinematic=false;rb.interpolation=RigidbodyInterpolation.Interpolate;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
        foreach(var m in raiz.GetComponentsInChildren<MeshCollider>())m.convex=true;
        if(!raiz.GetComponentInChildren<Collider>())raiz.gameObject.AddComponent<SphereCollider>();
        if(!raiz.GetComponent<NodoFisicoVR>())raiz.gameObject.AddComponent<NodoFisicoVR>();
    }
    GameObject CrearPinzaFisica(string nombre,Transform raiz,Vector3 posicion,string tipo)
    {
        var m=new GameObject(nombre);m.transform.SetParent(raiz);m.transform.position=posicion;
        var rama1=CrearCubo("Rama_izquierda",m.transform,new Vector3(-.018f,0,0),new Vector3(.012f,.012f,.24f),new Color(.58f,.66f,.68f));
        var rama2=CrearCubo("Rama_derecha",m.transform,new Vector3(.018f,0,0),new Vector3(.012f,.012f,.24f),new Color(.58f,.66f,.68f));
        rama1.transform.localRotation=Quaternion.Euler(0,-7,0);rama2.transform.localRotation=Quaternion.Euler(0,7,0);
        var accion=m.AddComponent<AccionQuirurgica>();accion.tipo=tipo;return m;
    }
    GameObject CrearCoaguloFisico(Transform raiz)
    {
        var m=new GameObject("Coagulo_Simulado");m.transform.SetParent(raiz);m.transform.position=new Vector3(0,1.39f,.735f);var accion=m.AddComponent<AccionQuirurgica>();accion.tipo="coagulo";
        CrearEsfera("Fragmento_principal",m.transform,Vector3.zero,new Vector3(.105f,.075f,.09f),new Color(.22f,.018f,.025f));CrearEsfera("Fragmento_secundario",m.transform,new Vector3(.045f,.008f,.025f),new Vector3(.055f,.045f,.052f),new Color(.22f,.018f,.025f));return m;
    }
    GameObject CrearZonaFisica(string nombre,Transform raiz,Vector3 posicion,Vector3 escala,string tipo,Color color)
    {
        var m=CrearCubo(nombre,raiz,posicion,escala,color);m.GetComponent<Collider>().isTrigger=true;var accion=m.AddComponent<AccionQuirurgica>();accion.tipo=tipo;return m;
    }
    GameObject CrearCubo(string nombre,Transform raiz,Vector3 posicion,Vector3 escala,Color color)
    {
        var m=GameObject.CreatePrimitive(PrimitiveType.Cube);m.name=nombre;m.transform.SetParent(raiz);m.transform.localPosition=posicion;m.transform.localScale=escala;var r=m.GetComponent<Renderer>();var mat=new Material(Shader.Find("Universal Render Pipeline/Lit")){color=color};r.sharedMaterial=mat;return m;
    }
    GameObject CrearEsfera(string nombre,Transform raiz,Vector3 posicion,Vector3 escala,Color color)
    {
        var m=GameObject.CreatePrimitive(PrimitiveType.Sphere);m.name=nombre;m.transform.SetParent(raiz);m.transform.localPosition=posicion;m.transform.localScale=escala;var r=m.GetComponent<Renderer>();var mat=new Material(Shader.Find("Universal Render Pipeline/Lit")){color=color};r.sharedMaterial=mat;return m;
    }
    void CrearVisualDeCorte()
    {
        if(!zonaIncision||trazoCorteVisual)return;
        var linea=GameObject.CreatePrimitive(PrimitiveType.Cube);linea.name="Apertura_Visual_del_Corte";
        linea.transform.SetParent(zonaIncision.parent,true);linea.transform.position=zonaIncision.position+zonaIncision.up*.018f;linea.transform.rotation=zonaIncision.rotation;
        linea.transform.localScale=new Vector3(.006f,.012f,.028f);
        var col=linea.GetComponent<Collider>();if(col)col.enabled=false;
        var render=linea.GetComponent<Renderer>();render.sharedMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit")){color=new Color(.48f,.015f,.02f)};
        trazoCorteVisual=linea.transform;trazoCorteVisual.gameObject.SetActive(false);
    }
    void ActualizarVisualDeCorte()
    {
        if(!trazoCorteVisual||!zonaIncision)return;
        float p=Mathf.Clamp01(avanceCorte/LargoCorte);float largo=Mathf.Lerp(.006f,LargoCorte,p);
        trazoCorteVisual.position=zonaIncision.position+zonaIncision.right*(-LargoCorte*.5f+largo*.5f)+zonaIncision.up*.018f;
        trazoCorteVisual.rotation=zonaIncision.rotation;trazoCorteVisual.localScale=new Vector3(largo,.012f,.028f);
    }
    void Update()
    {
        tiempo+=Time.deltaTime;
        if(pechoPaciente)pechoPaciente.localScale=escalaPecho+new Vector3(0,0,Mathf.Sin(tiempo*1.15f)*.018f);
        if(trazoMonitor)trazoMonitor.localPosition=posicionTrazo+new Vector3(Mathf.PingPong(tiempo*1.1f,.22f)-.11f,0,0);
        if(lampara)lampara.localRotation=giroLampara*Quaternion.Euler(0,0,Mathf.Sin(tiempo*.3f)*.7f);
        if(animarOrganos){AnimarOrgano(renderHigado,organoResaltadoHigado);AnimarOrgano(renderEstomago,organoResaltadoEstomago);AnimarOrgano(renderIntestino,organoResaltadoIntestino);}
        ActualizarHemorragia();

        LeerDispositivos();
        ActualizarMano(XRNode.LeftHand,manoIzquierda);
        ActualizarMano(XRNode.RightHand,manoDerecha);
        MantenerHerramientas();
        DibujarRayo();
        ActualizarGestoDeCorte();
        RevisarSeparador();

        if(Input.GetMouseButtonDown(0))Interactuar(camara, false);
        if(Input.GetKeyDown(KeyCode.M))RevisarMonitor();
        if(Input.GetKeyDown(KeyCode.E))TomarHerramientaDerecha();
        if(Input.GetKeyDown(KeyCode.Q))TomarSeparador();
        if(Input.GetKeyDown(KeyCode.R)&&paso==9)SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        if(Input.GetKeyDown(KeyCode.Space))AccionGatilloDerecho();
        if(Input.GetKeyDown(KeyCode.W))Mover(Vector2.up);
        if(Input.GetKeyDown(KeyCode.S))Mover(Vector2.down);
        if(Input.GetKeyDown(KeyCode.A))Mover(Vector2.left);
        if(Input.GetKeyDown(KeyCode.D))Mover(Vector2.right);
        if(Input.GetKeyDown(KeyCode.LeftArrow))jugador.Rotate(0,-30,0);
        if(Input.GetKeyDown(KeyCode.RightArrow))jugador.Rotate(0,30,0);
        if(progresoApertura>0&&progresoApertura<1)
        {
            progresoApertura=Mathf.MoveTowards(progresoApertura,1,Time.deltaTime*1.8f);
            if(bordeIzquierdo)bordeIzquierdo.localPosition=Vector3.Lerp(bordeIzqInicial,bordeIzqInicial+Vector3.left*.18f,progresoApertura);
            if(bordeDerecho)bordeDerecho.localPosition=Vector3.Lerp(bordeDerInicial,bordeDerInicial+Vector3.right*.18f,progresoApertura);
        }
    }
    void FixedUpdate()
    {
        MoverMandoFisico(manoIzquierda,manoIzquierdaRastreada,destinoManoIzquierda,giroManoIzquierda);
        MoverMandoFisico(manoDerecha,manoDerechaRastreada,destinoManoDerecha,giroManoDerecha);
    }
    void MoverMandoFisico(Transform mano,bool rastreada,Vector3 destino,Quaternion giro)
    {
        if(!mano||!rastreada||!mano.gameObject.activeInHierarchy)return;
        var rb=mano.GetComponent<Rigidbody>();if(rb){rb.MovePosition(destino);rb.MoveRotation(giro);}else mano.SetPositionAndRotation(destino,giro);
    }
    void LeerDispositivos()
    {
        gatilloDerecho=false;gripDerecho=false;gripIzquierdo=false;
        dispositivos.Clear();InputDevices.GetDevicesAtXRNode(XRNode.RightHand,dispositivos);
        foreach(var d in dispositivos)if(d.isValid)
        {
            bool boton=false,agarre=false;float trigger=0,grip=0;
            d.TryGetFeatureValue(CommonUsages.triggerButton,out boton);d.TryGetFeatureValue(CommonUsages.trigger,out trigger);
            d.TryGetFeatureValue(CommonUsages.gripButton,out agarre);d.TryGetFeatureValue(CommonUsages.grip,out grip);
            gatilloDerecho=boton||trigger>.55f;gripDerecho=agarre||grip>.55f;
            if(gatilloDerecho&&!gatilloDerechoAnterior)AccionGatilloDerecho();
            if(gripDerecho&&!gripDerechoAnterior)TomarHerramientaDerecha();
            if(!gripDerecho&&gripDerechoAnterior)SoltarHerramientaDerecha();
            gripDerechoAnterior=gripDerecho;gatilloDerechoAnterior=gatilloDerecho;break;
        }
        dispositivos.Clear();InputDevices.GetDevicesAtXRNode(XRNode.LeftHand,dispositivos);
        foreach(var d in dispositivos)if(d.isValid)
        {
            bool agarre=false;float grip=0;d.TryGetFeatureValue(CommonUsages.gripButton,out agarre);d.TryGetFeatureValue(CommonUsages.grip,out grip);
            gripIzquierdo=agarre||grip>.55f;
            if(gripIzquierdo&&!gripIzquierdoAnterior)TomarConManoIzquierda();
            if(!gripIzquierdo&&gripIzquierdoAnterior)SoltarManoIzquierda();
            gripIzquierdoAnterior=gripIzquierdo;
            if(d.TryGetFeatureValue(CommonUsages.primary2DAxis,out Vector2 eje)&&eje.sqrMagnitude>.03f)Mover(eje);
            break;
        }
        pausaGiro-=Time.deltaTime;dispositivos.Clear();InputDevices.GetDevicesAtXRNode(XRNode.RightHand,dispositivos);
        foreach(var d in dispositivos)if(d.isValid&&d.TryGetFeatureValue(CommonUsages.primary2DAxis,out Vector2 eje))
        {bool girando=Mathf.Abs(eje.x)>.72f;if(girando&&!giroAnterior&&pausaGiro<=0){jugador.Rotate(0,eje.x>0?30:-30,0);pausaGiro=.28f;}giroAnterior=girando;break;}
    }
    void ActualizarMano(XRNode nodo,Transform mano)
    {
        if(!mano||!jugador)return;dispositivos.Clear();InputDevices.GetDevicesAtXRNode(nodo,dispositivos);
        foreach(var d in dispositivos)if(d.isValid&&d.TryGetFeatureValue(CommonUsages.devicePosition,out Vector3 p)&&d.TryGetFeatureValue(CommonUsages.deviceRotation,out Quaternion q))
        {
            if(nodo==XRNode.LeftHand){destinoManoIzquierda=jugador.TransformPoint(p);giroManoIzquierda=jugador.rotation*q;manoIzquierdaRastreada=true;}
            else{destinoManoDerecha=jugador.TransformPoint(p);giroManoDerecha=jugador.rotation*q;manoDerechaRastreada=true;}
            mano.gameObject.SetActive(true);return;
        }
        if(nodo==XRNode.LeftHand)manoIzquierdaRastreada=false;else manoDerechaRastreada=false;
        mano.gameObject.SetActive(false);
    }
    void DibujarRayo()
    {
        if(!rayo)return;
        if(!manoDerecha||!manoDerecha.gameObject.activeInHierarchy){if(camara){rayo.SetPosition(0,camara.position);rayo.SetPosition(1,camara.position+camara.forward*3.2f);}return;}
        Vector3 inicio=manoDerecha.position;Vector3 fin=inicio+manoDerecha.forward*3.2f;
        if(LanzarRayo(manoDerecha,out RaycastHit h,3.2f))fin=h.point;
        rayo.SetPosition(0,inicio);rayo.SetPosition(1,fin);
    }
    bool LanzarRayo(Transform origen,out RaycastHit golpe,float distancia=3.2f)
    {
        golpe=default(RaycastHit);if(!origen)return false;
        int cantidad=Physics.RaycastNonAlloc(origen.position+origen.forward*.045f,origen.forward,resultadosRayo,distancia,~0,QueryTriggerInteraction.Collide);
        int indiceCercano=-1;float distanciaCercana=float.MaxValue;
        for(int i=0;i<cantidad;i++)
        {
            var h=resultadosRayo[i];var accion=h.collider.GetComponentInParent<AccionQuirurgica>();
            if(accion&&EsHerramientaEnMano(accion.transform))continue;
            if(h.distance<distanciaCercana){distanciaCercana=h.distance;indiceCercano=i;}
        }
        if(indiceCercano<0)return false;golpe=resultadosRayo[indiceCercano];return true;
    }
    bool EsHerramientaEnMano(Transform t){return (bisturiEnMano&&bisturi&&t==bisturi)||(separadorEnMano&&separador&&t==separador)||(pinzaEnMano&&pinzaExtraccion&&t==pinzaExtraccion)||(hemostatoEnMano&&hemostato&&t==hemostato)||(gasaEnMano&&gasa&&t==gasa);}
    AccionQuirurgica BuscarParaAgarrar(Transform mano,float distanciaRayo=1.8f)
    {
        if(!mano)return null;
        if(LanzarRayo(mano,out RaycastHit golpe,distanciaRayo))
        {
            var porRayo=golpe.collider.GetComponentInParent<AccionQuirurgica>();
            if(porRayo)return porRayo;
        }
        var cercanos=Physics.OverlapSphere(mano.position,.30f,~0,QueryTriggerInteraction.Collide);
        AccionQuirurgica mejor=null;float menor=float.MaxValue;
        foreach(var col in cercanos)
        {
            var accion=col.GetComponentInParent<AccionQuirurgica>();if(!accion)continue;
            float d=(col.ClosestPoint(mano.position)-mano.position).sqrMagnitude;
            if(d<menor){menor=d;mejor=accion;}
        }
        return mejor;
    }
    void AccionGatilloDerecho()
    {
        if(paso==6&&coaguloEnPinza&&coagulo&&zonaCoagulo&&Vector3.Distance(coagulo.position,zonaCoagulo.position)<.24f){DepositarCoagulo();return;}
        if(!LanzarRayo(manoDerecha,out RaycastHit h))return;
        var a=h.collider.GetComponentInParent<AccionQuirurgica>();if(!a)return;
        if(paso==0&&a.tipo=="monitor"){RevisarMonitor();return;}
        if(paso==3&&a.tipo.StartsWith("organo_")){if(organoEnMano!=a.transform){Estado("Sujeta el órgano correcto con el agarre antes de revisarlo");return;}RevisarOrgano(a.transform);return;}
        if(paso==4&&hemostatoEnMano&&a.tipo=="zona_hemorragia"){ControlarHemorragia();return;}
        if(paso==5&&pinzaEnMano&&a.tipo=="coagulo"){TomarCoagulo();return;}
        if(paso==6&&pinzaEnMano&&coaguloEnPinza&&a.tipo=="zona_extraccion"){DepositarCoagulo();return;}
        if(paso==7&&gasaEnMano&&a.tipo=="zona_herida")
        {
            if(!gasa||Vector3.Distance(gasa.position,zonaHerida.position)>.24f){Estado("Acerca la gasa hasta tocar el área de cierre");return;}
            paso=8;gasaEnMano=false;SoltarFisico(gasa);MostrarOrganos(false);if(zonaHerida)zonaHerida.gameObject.SetActive(false);Estado("Cierre completado · estabilidad "+Mathf.RoundToInt(estabilidad)+"% · puedes salir al nivel 2");return;
        }
        if(a.tipo=="siguiente"){Continuar();return;}
        if(paso==1&&a.tipo=="zona_incision"&&bisturiEnMano)Estado("Mantén el gatillo y desliza el bisturí de izquierda a derecha por la guía");
    }
    void Interactuar(Transform origen,bool visor)
    {
        if(!LanzarRayo(origen,out RaycastHit h))return;var a=h.collider.GetComponentInParent<AccionQuirurgica>();if(!a)return;
        if(a.tipo=="monitor")RevisarMonitor();else if(a.tipo=="siguiente")Continuar();
    }
    void TomarHerramientaDerecha()
    {
        if(!manoDerecha)return;var a=BuscarParaAgarrar(manoDerecha);if(!a)return;
        if(paso==1&&a.tipo=="bisturi"&&bisturi){if(!TomarFisico(bisturi,manoDerecha)){Estado("Acércate a la mesa para tomar el bisturí");return;}bisturiEnMano=true;giroHerramienta=Quaternion.Inverse(manoDerecha.rotation)*bisturi.rotation;Estado("Bisturí tomado · haz un corte continuo y preciso");}
        else if(paso==3&&a.tipo.StartsWith("organo_")&&a.transform==OrganoEsperado()){if(!TomarFisico(a.transform,manoDerecha)){Estado("Acércate al órgano y usa el agarre para sujetarlo");return;}organoEnMano=a.transform;Estado("Órgano sujeto · inspecciónalo con cuidado y pulsa gatillo");}
        else if(paso==4&&a.tipo=="pinza_hemostatica"&&hemostato){if(!TomarFisico(hemostato,manoDerecha)){Estado("Acércate a la mesa para tomar el hemostato");return;}hemostatoEnMano=true;giroHerramienta=Quaternion.Inverse(manoDerecha.rotation)*hemostato.rotation;Estado("Hemostato tomado · pulsa y mantén sobre la fuga");}
        else if((paso==5||paso==6)&&a.tipo=="pinza_extraccion"&&pinzaExtraccion){if(!TomarFisico(pinzaExtraccion,manoDerecha)){Estado("Acércate a la mesa para tomar la pinza de extracción");return;}pinzaEnMano=true;giroHerramienta=Quaternion.Inverse(manoDerecha.rotation)*pinzaExtraccion.rotation;Estado(paso==5?"Pinza tomada · acerca las puntas al coágulo y pulsa gatillo":"Vuelve a tomar la pinza y sujeta el coágulo");}
        else if(paso==7&&a.tipo=="gasa"&&gasa){if(!TomarFisico(gasa,manoDerecha)){Estado("Acércate a la mesa para tomar la gasa");return;}gasaEnMano=true;Estado("Gasa tomada · cúbrela sobre la zona de cierre y pulsa gatillo");}
        else if(paso==0&&a.tipo=="monitor")RevisarMonitor();
    }
    bool TomarFisico(Transform objeto,Transform mano)
    {
        if(!objeto||!mano||Vector3.Distance(objeto.position,mano.position)>.62f)return false;
        var nodo=objeto.GetComponent<NodoFisicoVR>();var cuerpoMano=mano.GetComponent<Rigidbody>();return nodo&&nodo.Agarrar(cuerpoMano);
    }
    void TomarSeparador()
    {
        if(paso!=2||!manoIzquierda)return;
        var a=BuscarParaAgarrar(manoIzquierda);if(a&&a.tipo=="separador"&&separador){if(!TomarFisico(separador,manoIzquierda)){Estado("Acércate a la mesa para tomar el separador");return;}separadorEnMano=true;Estado("Separador tomado · apóyalo en el punto de retracción");}
    }
    void TomarConManoIzquierda()
    {
        if(paso==2){TomarSeparador();return;}
        if(paso!=3||!manoIzquierda)return;var a=BuscarParaAgarrar(manoIzquierda);if(!a||!a.tipo.StartsWith("organo_")||a.transform!=OrganoEsperado())return;
        if(!TomarFisico(a.transform,manoIzquierda)){Estado("Acércate al órgano y usa el agarre para sujetarlo");return;}
        organoEnMano=a.transform;organoEnManoIzquierda=true;Estado("Órgano sujeto con la mano izquierda · apunta con la derecha y pulsa gatillo");
    }
    void SoltarHerramientaDerecha()
    {
        if(bisturiEnMano){bisturiEnMano=false;SoltarFisico(bisturi);}
        if(gasaEnMano){gasaEnMano=false;SoltarFisico(gasa);}
        if(hemostatoEnMano){hemostatoEnMano=false;SoltarFisico(hemostato);}
        if(pinzaEnMano){pinzaEnMano=false;SoltarFisico(pinzaExtraccion);}
        var nodoCoagulo=coagulo?coagulo.GetComponent<NodoFisicoVR>():null;if(nodoCoagulo&&nodoCoagulo.estaAgarrado){SoltarFisico(coagulo);coaguloEnPinza=false;}
        if(organoEnMano&&!organoEnManoIzquierda){SoltarFisico(organoEnMano);organoEnMano=null;}
    }
    void SoltarFisico(Transform objeto){var nodo=objeto?objeto.GetComponent<NodoFisicoVR>():null;if(nodo)nodo.Soltar();}
    void SoltarSeparador(){if(separadorEnMano){separadorEnMano=false;SoltarFisico(separador);}}
    void SoltarManoIzquierda(){SoltarSeparador();if(organoEnManoIzquierda&&organoEnMano){SoltarFisico(organoEnMano);organoEnMano=null;}organoEnManoIzquierda=false;}
    void MantenerHerramientas()
    {
        // Las herramientas siguen al controlador mediante articulaciones FixedJoint físicas.
    }
    void ActualizarGestoDeCorte()
    {
        if(paso!=1||!bisturiEnMano||!gatilloDerecho||!manoDerecha||!LanzarRayo(manoDerecha,out RaycastHit h,1.1f)||h.collider.GetComponentInParent<AccionQuirurgica>()?.tipo!="zona_incision")
        {tienePosicionCorte=false;return;}
        Vector3 local=zonaIncision.InverseTransformPoint(h.point);
        if(Mathf.Abs(local.z)>.12f||Mathf.Abs(local.x)>.28f){tienePosicionCorte=false;return;}
        if(tienePosicionCorte)
        {
            float delta=local.x-ultimaPosicionCorte.x;
            if(delta>.001f&&delta<.06f)avanceCorte+=delta;
            else if(delta>.06f)tienePosicionCorte=false;
        }
        ultimaPosicionCorte=local;tienePosicionCorte=true;
        ActualizarVisualDeCorte();
        if(estado)estado.text="Corte guiado · "+Mathf.Clamp01(avanceCorte/LargoCorte).ToString("P0");
        if(avanceCorte>=LargoCorte)AbrirCampo();
    }
    void AbrirCampo()
    {
        if(cubiertaAbdominal)cubiertaAbdominal.gameObject.SetActive(false);
        if(cavidadAbdominal)cavidadAbdominal.gameObject.SetActive(true);
        if(bordeIzquierdo)bordeIzquierdo.gameObject.SetActive(true);if(bordeDerecho)bordeDerecho.gameObject.SetActive(true);if(zonaRetraccion)zonaRetraccion.gameObject.SetActive(true);
        if(zonaIncision)zonaIncision.gameObject.SetActive(false);
        if(trazoCorteVisual)trazoCorteVisual.gameObject.SetActive(false);
        if(pinzaExtraccion)pinzaExtraccion.gameObject.SetActive(true);if(hemostato)hemostato.gameObject.SetActive(true);
        if(zonaHerida)zonaHerida.gameObject.SetActive(true);
        bisturiEnMano=false;SoltarFisico(bisturi);paso=2;avanceCorte=0;
        Estado("Capas abiertas · toma el separador con el agarre izquierdo");
    }
    void AbrirConSeparador()
    {
        if(paso!=2)return;
        paso=3;progresoApertura=.001f;MostrarOrganos(true);animarOrganos=true;
        if(zonaSangrado)zonaSangrado.gameObject.SetActive(true);if(marcadorHemorragia)marcadorHemorragia.gameObject.SetActive(true);
        Estado("Retracción estable · revisa los tres órganos en el orden indicado");
    }
    void RevisarSeparador()
    {
        if(paso!=2||!separadorEnMano||!separador||!zonaRetraccion)return;
        if(Vector3.Distance(separador.position,zonaRetraccion.position)<.38f)
        {
            SoltarSeparador();AbrirConSeparador();
        }
    }
    void RevisarOrgano(Transform organo)
    {
        Transform esperado=organoActual==0?higado:organoActual==1?estomago:intestino;
        if(organo!=esperado){Estado(organoActual==0?"Revisa primero el hígado":organoActual==1?"Sigue con el estómago":"Sigue con el intestino");return;}
        if(organo==higado)organoResaltadoHigado=true;else if(organo==estomago)organoResaltadoEstomago=true;else organoResaltadoIntestino=true;
        organoActual++;
        if(organoActual>=3){paso=4;tiempoHemorragia=0;progresoHemostasia=0;if(hemostato)hemostato.gameObject.SetActive(true);Estado("¡Hemorragia simulada! Toma el hemostato y mantén el gatillo sobre la fuga");}
        else Estado("Órgano revisado · continúa con "+(organoActual==1?"estómago":"intestino"));
    }
    void RevisarMonitor(){if(paso!=0)return;paso=1;if(zonaIncision)zonaIncision.gameObject.SetActive(true);if(trazoCorteVisual)trazoCorteVisual.gameObject.SetActive(true);Estado("Monitor estable · toma el bisturí y sigue la guía de corte");}
    void Continuar()
    {
        if(paso<8){Estado("Completa primero todas las fases quirúrgicas");return;}
        if(!string.IsNullOrEmpty(escenaSiguiente))SceneManager.LoadScene(escenaSiguiente);
    }
    Transform OrganoEsperado(){return organoActual==0?higado:organoActual==1?estomago:intestino;}
    void MostrarOrganos(bool visible)
    {
        var organos=new[]{higado,estomago,intestino,rinones};
        foreach(var organo in organos)if(organo)
        {
            organo.gameObject.SetActive(visible);
            var rb=organo.GetComponent<Rigidbody>();if(rb&&visible){rb.isKinematic=false;rb.useGravity=true;rb.WakeUp();}
        }
        if(!visible)animarOrganos=false;
    }
    void ActualizarHemorragia()
    {
        if(paso!=4||sangradoControlado)return;
        tiempoHemorragia+=Time.deltaTime;estabilidad=Mathf.Clamp(100f-tiempoHemorragia/LimiteHemorragia*100f,0,100);
        bool bienColocado=hemostatoEnMano&&hemostato&&zonaSangrado&&Vector3.Distance(hemostato.position,zonaSangrado.position)<.22f&&gatilloDerecho;
        progresoHemostasia=bienColocado?progresoHemostasia+Time.deltaTime:Mathf.Max(0,progresoHemostasia-Time.deltaTime*.65f);
        if(marcadorHemorragia)marcadorHemorragia.localScale=Vector3.one*(.045f+Mathf.PingPong(tiempo*7f,.035f));
        if(progresoHemostasia>=TiempoHemostasia){ControlarHemorragia();return;}
        if(tiempo-tiempoTexto>.6f){tiempoTexto=tiempo;Estado("Hemorragia · estabilidad "+Mathf.RoundToInt(estabilidad)+"% · hemostasia "+Mathf.RoundToInt(progresoHemostasia/TiempoHemostasia*100)+"%");}
        if(estabilidad<=0){paso=9;Estado("Paciente inestable · pulsa R para reiniciar la simulación");}
    }
    void ControlarHemorragia()
    {
        if(paso!=4||!hemostatoEnMano||!hemostato||!zonaSangrado||Vector3.Distance(hemostato.position,zonaSangrado.position)>.22f)return;
        if(progresoHemostasia<TiempoHemostasia){Estado("Mantén el gatillo y fija el hemostato sobre la fuga");return;}
        sangradoControlado=true;paso=5;SoltarHerramientaDerecha();if(zonaSangrado)zonaSangrado.gameObject.SetActive(false);if(marcadorHemorragia)marcadorHemorragia.gameObject.SetActive(false);
        if(coagulo){coagulo.gameObject.SetActive(true);var rb=coagulo.GetComponent<Rigidbody>();if(rb){rb.isKinematic=false;rb.useGravity=true;rb.WakeUp();}}Estado("Hemorragia controlada · ahora extrae el coágulo simulado con la pinza");
    }
    void TomarCoagulo()
    {
        if(!pinzaEnMano||!pinzaExtraccion||!coagulo)return;
        if(Vector3.Distance(pinzaExtraccion.position+pinzaExtraccion.forward*.14f,coagulo.position)>.24f){Estado("Acerca las puntas de la pinza al coágulo");return;}
        var pieza=coagulo.GetComponent<NodoFisicoVR>();var rbPinza=pinzaExtraccion.GetComponent<Rigidbody>();
        Vector3 punta=pinzaExtraccion.position+pinzaExtraccion.forward*.14f;
        coaguloEnPinza=pieza&&pieza.Agarrar(rbPinza,punta);
        if(coaguloEnPinza){paso=6;if(zonaCoagulo)zonaCoagulo.gameObject.SetActive(true);Estado("Coágulo sujeto · deposítalo en la bandeja marcada fuera de la cavidad");}
    }
    void DepositarCoagulo()
    {
        if(!coaguloEnPinza||!coagulo||!zonaCoagulo||Vector3.Distance(coagulo.position,zonaCoagulo.position)>.24f){Estado("Lleva el coágulo hasta la bandeja verde");return;}
        var nodo=coagulo.GetComponent<NodoFisicoVR>();if(nodo)nodo.GuardarEnCampo();coagulo.position=zonaCoagulo.position;coaguloEnPinza=false;paso=7;
        if(hemostato)hemostato.gameObject.SetActive(false);if(pinzaExtraccion)pinzaExtraccion.gameObject.SetActive(false);if(gasa)gasa.gameObject.SetActive(true);
        Estado("Extracción completa · toma la gasa y cierra el campo");
    }
    void AnimarOrgano(Renderer[] renderizadores,bool revisado)
    {
        if(renderizadores==null)return;float pulso=1f+Mathf.Sin(tiempo*1.8f)*.04f;
        foreach(var render in renderizadores)if(render&&render.sharedMaterial)
        {
            Color baseColor=render.sharedMaterial.color;
            if(revisado)baseColor=Color.Lerp(baseColor,new Color(.88f,.61f,.35f),.55f);
            Color color=baseColor*pulso;render.GetPropertyBlock(bloqueMaterial);
            bloqueMaterial.SetColor("_BaseColor",color);bloqueMaterial.SetColor("_Color",color);render.SetPropertyBlock(bloqueMaterial);
        }
    }
    void ConfigurarColision(Transform t,bool activa){if(!t)return;foreach(var c in t.GetComponentsInChildren<Collider>())c.enabled=activa;}
    void Mover(Vector2 eje){if(!jugador||!camara)return;Vector3 f=Vector3.ProjectOnPlane(camara.forward,Vector3.up).normalized;Vector3 d=Vector3.ProjectOnPlane(camara.right,Vector3.up).normalized;jugador.position+=(f*eje.y+d*eje.x)*(Time.deltaTime*1.05f);jugador.position=new Vector3(Mathf.Clamp(jugador.position.x,-3.1f,3.1f),0,Mathf.Clamp(jugador.position.z,-3.1f,3.1f));}
    void Mostrar(string s){if(instrucciones)instrucciones.text=s;}
    void Estado(string s){if(estado)estado.text=s;}
}

