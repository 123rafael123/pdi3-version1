using UnityEngine;

/// <summary>Nodo físico reutilizable para herramientas y piezas anatómicas agarrables en VR.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class NodoFisicoVR : MonoBehaviour
{
    public Rigidbody cuerpo;
    [Tooltip("Crea colliders compuestos en las piezas visibles que no tengan uno propio.")]
    public bool completarColliders = true;
    public bool estaAgarrado { get; private set; }
    FixedJoint unionActual;
    Vector3 posicionSegura;
    Quaternion rotacionSegura;

    void Awake()
    {
        if (!cuerpo) cuerpo = GetComponent<Rigidbody>();
        posicionSegura = transform.position;
        rotacionSegura = transform.rotation;
        if (completarColliders) AsegurarCollidersCompuestos();
        cuerpo.mass = Mathf.Max(.05f, cuerpo.mass);
        cuerpo.drag = .08f;
        cuerpo.angularDrag = .12f;
        cuerpo.maxAngularVelocity = 18f;
        cuerpo.solverIterations = 12;
        cuerpo.solverVelocityIterations = 6;
        cuerpo.interpolation = RigidbodyInterpolation.Interpolate;
        cuerpo.collisionDetectionMode = cuerpo.isKinematic ? CollisionDetectionMode.ContinuousSpeculative : CollisionDetectionMode.ContinuousDynamic;
    }

    void FixedUpdate()
    {
        // Si una pieza atraviesa accidentalmente el suelo, vuelve cerca de su mesa
        // para que el jugador siempre pueda recuperarla.
        if (!estaAgarrado && transform.position.y < -1.5f)
        {
            cuerpo.velocity = Vector3.zero;
            cuerpo.angularVelocity = Vector3.zero;
            cuerpo.position = posicionSegura + Vector3.up * .18f;
            cuerpo.rotation = rotacionSegura;
        }
    }

    void AsegurarCollidersCompuestos()
    {
        foreach (var filtro in GetComponentsInChildren<MeshFilter>(true))
        {
            if (!filtro.sharedMesh || filtro.GetComponent<Collider>()) continue;
            Bounds limites = filtro.sharedMesh.bounds;
            if (limites.size.sqrMagnitude < .000001f) continue;
            var caja = filtro.gameObject.AddComponent<BoxCollider>();
            caja.center = limites.center;
            caja.size = limites.size;
        }

        foreach (var malla in GetComponentsInChildren<MeshCollider>(true))
        {
            if (!malla.sharedMesh || malla.convex) continue;
            malla.convex = true;
        }

        // Un objeto agarrable siempre necesita al menos una forma de colision.
        if (!GetComponentInChildren<Collider>(true))
        {
            var caja = gameObject.AddComponent<BoxCollider>();
            var renderizadores = GetComponentsInChildren<Renderer>(true);
            if (renderizadores.Length == 0) return;
            Bounds mundo = renderizadores[0].bounds;
            for (int i = 1; i < renderizadores.Length; i++) mundo.Encapsulate(renderizadores[i].bounds);
            caja.center = transform.InverseTransformPoint(mundo.center);
            Vector3 escala = transform.lossyScale;
            caja.size = new Vector3(
                mundo.size.x / Mathf.Max(Mathf.Abs(escala.x), .0001f),
                mundo.size.y / Mathf.Max(Mathf.Abs(escala.y), .0001f),
                mundo.size.z / Mathf.Max(Mathf.Abs(escala.z), .0001f));
        }
    }

    public bool Agarrar(Rigidbody cuerpoDeMano) => Agarrar(cuerpoDeMano, cuerpoDeMano ? cuerpoDeMano.position : transform.position);

    public bool Agarrar(Rigidbody cuerpoDeMano, Vector3 puntoDeAgarre)
    {
        if (!cuerpoDeMano || !cuerpo) return false;
        if (unionActual) Destroy(unionActual);
        cuerpo.isKinematic = false;
        cuerpo.useGravity = false;
        cuerpo.WakeUp();
        unionActual = gameObject.AddComponent<FixedJoint>();
        unionActual.connectedBody = cuerpoDeMano;
        unionActual.autoConfigureConnectedAnchor = false;
        unionActual.anchor = transform.InverseTransformPoint(puntoDeAgarre);
        unionActual.connectedAnchor = cuerpoDeMano.transform.InverseTransformPoint(puntoDeAgarre);
        unionActual.enableCollision = false;
        unionActual.breakForce = Mathf.Infinity;
        unionActual.breakTorque = Mathf.Infinity;
        estaAgarrado = true;
        return true;
    }

    public void Soltar()
    {
        if (unionActual)
        {
            Destroy(unionActual);
            unionActual = null;
        }
        if (cuerpo)
        {
            cuerpo.useGravity = true;
            cuerpo.isKinematic = false;
            cuerpo.WakeUp();
        }
        estaAgarrado = false;
    }

    public void GuardarEnCampo()
    {
        if (unionActual)
        {
            Destroy(unionActual);
            unionActual = null;
        }
        estaAgarrado = false;
        if (cuerpo)
        {
            cuerpo.useGravity = false;
            cuerpo.isKinematic = true;
            cuerpo.velocity = Vector3.zero;
            cuerpo.angularVelocity = Vector3.zero;
        }
    }
}
