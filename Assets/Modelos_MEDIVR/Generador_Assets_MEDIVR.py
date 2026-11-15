import bpy, math, os
from mathutils import Vector

OUT = os.path.join(os.path.dirname(__file__), 'assets_medicos')
os.makedirs(OUT, exist_ok=True)

def limpiar():
    bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)

def mat(n, color, metal=0.0, rough=.32, emission=None):
    m=bpy.data.materials.new(n); m.diffuse_color=(*color,1); m.use_nodes=True
    p=m.node_tree.nodes.get('Principled BSDF'); p.inputs['Base Color'].default_value=(*color,1); p.inputs['Metallic'].default_value=metal; p.inputs['Roughness'].default_value=rough
    if emission:
        p.inputs['Emission Color'].default_value=(*emission,1); p.inputs['Emission Strength'].default_value=1.2
    return m

MARFIL=mat('Porcelana sanitaria',(.82,.89,.89),.08,.26); PETROLEO=mat('Azul petróleo MEDIVR',(.018,.12,.15),.25,.25)
TURQ=mat('Turquesa MEDIVR',(.015,.52,.49),.22,.25); ACERO=mat('Acero inoxidable quirúrgico',(.55,.64,.67),.88,.2)
GOMA=mat('Polímero grafito',(.045,.065,.07),.18,.36); VIDRIO=mat('Vidrio azul de monitor',(.012,.09,.105),.35,.16)
LUZ=mat('Luz clínica',(.78,.96,1),.05,.18,(.65,.9,1)); ROJO=mat('Rojo emergencia',(.72,.025,.035),.15,.28)
PIEL=mat('Piel simulada',(.66,.39,.29),.03,.48); TELA=mat('Paño quirúrgico',(.018,.24,.23),.04,.62)

def asignar(o,m):
    if m:o.data.materials.append(m)
    return o
def bevel(o, amount=.04, seg=3):
    if o.type=='MESH':
        mod=o.modifiers.new('Bordes redondeados','BEVEL');mod.width=amount;mod.segments=seg
        o.modifiers.new('Normales suaves','WEIGHTED_NORMAL')
def caja(n,loc,dim,m,bev=.035):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc);o=bpy.context.object;o.name=n;o.dimensions=dim; bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);asignar(o,m);bevel(o,min(bev,min(dim)*.45));return o
def cilindro(n,loc,radio,depth,m,verts=48,rot=None):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts,radius=radio,depth=depth,location=loc,rotation=rot or (0,0,0));o=bpy.context.object;o.name=n;asignar(o,m);bevel(o,min(.025,radio*.3));return o
def esfera(n,loc,scale,m):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=20, location=loc);o=bpy.context.object;o.name=n;o.scale=scale;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);asignar(o,m);bpy.ops.object.shade_smooth();return o
def toro(n,loc,major,minor,m,rot=None):
    bpy.ops.mesh.primitive_torus_add(major_radius=major,minor_radius=minor,major_segments=48,minor_segments=12,location=loc,rotation=rot or (0,0,0));o=bpy.context.object;o.name=n;asignar(o,m);bpy.ops.object.shade_smooth();return o
def tubo(n,a,b,r,m):
    mid=(Vector(a)+Vector(b))/2; d=Vector(b)-Vector(a);o=cilindro(n,mid,r,d.length,m,24);o.rotation_mode='QU quaternion' if False else 'XYZ';o.rotation_quaternion=d.to_track_quat('Z','Y');return o
def texto(n,s,loc,size,m):
    c=bpy.data.curves.new(n,'FONT');c.body=s;c.size=size;c.extrude=.001;o=bpy.data.objects.new(n,c);bpy.context.collection.objects.link(o);o.location=loc;o.rotation_euler=(math.pi/2,0,0);o.data.materials.append(m);return o
def preparar(nombre):
    limpiar(); sc=bpy.context.scene;sc.render.engine='BLENDER_EEVEE';sc.render.resolution_x=1200;sc.render.resolution_y=900;sc.render.resolution_percentage=100
    sc.world.color=(.12,.12,.12);sc.view_settings.view_transform='AgX'
    bpy.ops.object.camera_add(location=(5,-8,5.2));cam=bpy.context.object;cam.name='Camara de producto';cam.data.type='ORTHO';cam.data.ortho_scale=8;cam.rotation_euler=(Vector((0,0,1))-cam.location).to_track_quat('-Z','Y').to_euler();sc.camera=cam
    for loc,power,size in [((1,-4,6),1000,5),((-4,-1,3),650,4),((2,3,4),900,3)]:
        bpy.ops.object.light_add(type='AREA',location=loc);l=bpy.context.object;l.data.energy=power;l.data.shape='DISK';l.data.size=size;l.rotation_euler=(Vector((0,0,1))-l.location).to_track_quat('-Z','Y').to_euler()
    sc.render.image_settings.file_format='PNG';sc.render.filepath=os.path.join(OUT,nombre+'.png')
def guardar(nombre):
    # Organizar por colecciones semánticas y guardar un .blend independiente.
    bpy.ops.object.select_all(action='DESELECT')
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,nombre+'.blend'))
def modelo(nombre, build):
    preparar(nombre)
    # Each independent file receives local copies of the palette materials.
    global MARFIL,PETROLEO,TURQ,ACERO,GOMA,VIDRIO,LUZ,ROJO,PIEL,TELA
    MARFIL=MARFIL.copy();PETROLEO=PETROLEO.copy();TURQ=TURQ.copy();ACERO=ACERO.copy();GOMA=GOMA.copy();VIDRIO=VIDRIO.copy();LUZ=LUZ.copy();ROJO=ROJO.copy();PIEL=PIEL.copy();TELA=TELA.copy()
    build();guardar(nombre)

def bisturi():
    caja('Mango del bisturí',(-.12,0,0),(.95,.13,.1),PETROLEO,.045)
    for x in [-.42,-.28,-.14,0,.14,.28]: caja('Estría antideslizante',(x,-.069,0),(.025,.009,.075),TURQ,.004)
    # Hoja afilada facetada
    verts=[(.34,-.02,.035),(.34,.02,.035),(.75,0,.002),(.34,-.02,-.035),(.34,.02,-.035),(.75,0,.002)]
    faces=[(0,1,2),(3,5,4),(0,2,5,3),(1,4,5,2),(0,3,4,1)]
    me=bpy.data.meshes.new('Geometría hoja de acero');me.from_pydata(verts,[],faces);o=bpy.data.objects.new('Hoja afilada de acero',me);bpy.context.collection.objects.link(o);asignar(o,ACERO)
    toro('Aro de unión',(.35,0,0),.055,.012,ACERO,(0,math.pi/2,0))
def pinza():
    # Dos ramas ligeramente cruzadas y anillas ergonómicas
    for z,ang in [(-.025,-.08),(.025,.08)]:
        o=caja('Rama de pinza',(-.02,0,z),(.84,.045,.025),ACERO,.018);o.rotation_euler[1]=ang
        toro('Anilla para dedo',(-.48,0,z),.095,.022,ACERO,(math.pi/2,0,0))
    cilindro('Articulación',(0,-.03,0),.055,.07,PETROLEO,32,(math.pi/2,0,0))
    caja('Traba dentada',(.18,.035,0),(.28,.025,.035),ACERO,.008)
    caja('Punta hemostática',(.42,0,0),(.2,.03,.025),ACERO,.01)
def tijeras():
    for ang in [-.18,.18]:
        o=caja('Hoja de tijera',(.15,0,0),(.67,.045,.028),ACERO,.012);o.rotation_euler[1]=ang
        toro('Aro ergonómico',(-.38,0,0),.105,.026,ACERO,(math.pi/2,0,0))
    cilindro('Eje de tijera',(.02,-.035,0),.055,.08,PETROLEO,32,(math.pi/2,0,0))
def monitor():
    caja('Carcasa del monitor',(0,0,.65),(1.2,.38,1.05),PETROLEO,.11)
    caja('Marco de pantalla',(0,-.202,.73),(1.04,.04,.72),GOMA,.045);caja('Pantalla de signos vitales',(0,-.23,.75),(.94,.018,.61),VIDRIO,.025)
    # trazado ECG escalonado
    pts=[(-.42,.7),(-.3,.7),(-.22,.74),(-.17,.69),(-.12,.89),(-.06,.55),(.02,.77),(.18,.77),(.24,.72),(.3,.76),(.42,.76)]
    for i in range(len(pts)-1):
        a=(pts[i][0],-.246,pts[i][1]);b=(pts[i+1][0],-.246,pts[i+1][1]);tubo('Trazado electrocardiográfico',a,b,.012,LUZ)
    for z,label in [(0.58,'72'),(.47,'98'),(.36,'16')]:
        texto('Lectura de signos',label,(.22,-.26,z),.09,LUZ)
    caja('Panel de controles',(0,-.212,.13),(.9,.08,.2),PETROLEO,.025)
    for x,m in [(-.32,TURQ),(0,ROJO),(.32,LUZ)]: esfera('Botón de control',(x,-.27,.14),(.045,.025,.045),m)
    cilindro('Columna de soporte',(0,.08,-.13),.075,.53,ACERO)
    caja('Base del monitor',(0,.08,-.42),(.74,.52,.09),PETROLEO,.035)
    texto('Marca MEDIVR','MEDIVR',(0,-.251,.12),.085,LUZ)
def mesa_quirurgica():
    caja('Bastidor superior',(0,0,.72),(1.7,2.6,.18),PETROLEO,.12)
    caja('Colchon segmentado',(0,0,.84),(1.58,2.48,.16),MARFIL,.13)
    caja('Sección de cabeza',(0,-.93,.94),(1.34,.5,.07),TURQ,.055)
    caja('Sección de piernas',(0,.83,.94),(1.4,.78,.07),MARFIL,.055)
    cilindro('Columna hidráulica',(0,0,.38),.17,.62,ACERO)
    caja('Base móvil',(0,0,.08),(1.12,1.35,.14),PETROLEO,.09)
    for x in [-.42,.42]:
        for y in [-.48,.48]:
            cilindro('Rueda con freno',(x,y,.02),.11,.12,GOMA,32,(math.pi/2,0,0));caja('Pedal de freno',(x,y-.06,.14),(.13,.2,.035),ACERO,.015)
    for x in [-.89,.89]: caja('Riel lateral',(x,0,.72),(.055,2.25,.065),ACERO,.025)
    texto('Placa de marca','MEDIVR',(0,-1.315,.75),.1,LUZ)
def lampara():
    cilindro('Anclaje de techo',(0,0,1.8),.2,.16,PETROLEO)
    tubo('Brazo articulado superior',(0,0,1.7),(0,0,1.15),.045,ACERO)
    toro('Articulación giratoria',(0,0,1.13),.11,.035,TURQ,(math.pi/2,0,0))
    tubo('Brazo articulado inferior',(0,0,1.1),(.18,0,.72),.038,ACERO)
    cilindro('Carcasa circular de lámpara',(.18,0,.62),.68,.18,MARFIL,64)
    toro('Borde cromado',(.18,0,.51),.61,.035,ACERO)
    cilindro('Difusor de luz clínica',(.18,0,.505),.54,.035,LUZ,64)
    for i in range(8):
        a=2*math.pi*i/8;esfera('Lente LED',(.18+.37*math.cos(a),.37*math.sin(a),.475),(.055,.055,.025),LUZ)
    esfera('Cámara central',(.18,0,.472),(.09,.09,.035),VIDRIO)
    texto('Marca MEDIVR','MEDIVR',(-.15,-.64,.61),.1,PETROLEO)
def maquina_anestesia():
    caja('Gabinete de anestesia',(0,0,.75),(.85,.68,1.2),MARFIL,.09)
    caja('Panel frontal',(0,-.355,1.15),(.67,.045,.38),PETROLEO,.04)
    caja('Pantalla anestesia',(-.13,-.386,1.2),(.38,.02,.27),VIDRIO,.02)
    texto('Etiqueta de anestesia','ANESTESIA',(-.13,-.402,1.16),.065,LUZ)
    for x in [.12,.25]: cilindro('Caudalímetro',(x,-.39,.75),.055,.05,ACERO,32,(math.pi/2,0,0))
    for x in [-.25,0,.25]: esfera('Perilla de control',(x,-.4,.54),(.055,.045,.055),TURQ)
    caja('Plataforma superior',(0,0,1.39),(.94,.72,.08),PETROLEO,.04)
    for x in [-.28,.28]: cilindro('Rueda de carro',(x,.2,.12),.12,.09,GOMA,32,(math.pi/2,0,0))
    for x in [-.32,.32]: tubo('Asa lateral',(x,0,.5),(x,0,1.25),.025,ACERO)
    tubo('Manguera respiratoria',(.34,-.32,.94),(.48,-.38,.45),.035,GOMA)
    tubo('Manguera paciente',(.48,-.38,.45),(.28,-.42,.3),.025,TURQ)
    texto('Marca MEDIVR','MEDIVR',(0,-.352,.27),.09,PETROLEO)
def camilla_urgencias():
    mesa_quirurgica()
    # Revestimiento rojo de emergencia y barandales abatibles
    caja('Colchon de emergencia',(0,0,.94),(1.48,2.42,.07),ROJO,.08)
    for x in [-.78,.78]:
        tubo('Barandal lateral',(x,-.76,.8),(x,.76,.8),.035,ACERO)
        for y in [-.65,.65]: tubo('Soporte de barandal',(x,y,.72),(x,y,.83),.025,ACERO)
    texto('Identificación de camilla','URGENCIAS',(0,-1.315,.65),.09,LUZ)
def desfibrilador():
    caja('Carcasa desfibrilador',(0,0,.65),(.9,.48,.75),MARFIL,.11)
    caja('Pantalla desfibrilador',(0,-.252,.78),(.53,.025,.32),VIDRIO,.025)
    texto('Etiqueta desfibrilador','DESFIBRILADOR',(0,-.273,.75),.055,LUZ)
    esfera('Botón de descarga',(.28,-.27,.52),(.09,.045,.09),ROJO)
    esfera('Botón de energía',(-.28,-.27,.52),(.07,.04,.07),TURQ)
    for side in [-1,1]:
        caja('Paleta de desfibrilación',(side*.25,.42,.17),(.19,.34,.1),ACERO,.045)
        tubo('Cable de paleta',(side*.25,.25,.21),(side*.32,.05,.35),.018,GOMA)
    caja('Asa de transporte',(0,.01,1.07),(.48,.12,.1),PETROLEO,.045)
    texto('Marca MEDIVR','MEDIVR',(0,-.248,.39),.07,PETROLEO)
def tanque_oxigeno():
    cilindro('Cuerpo de tanque',(0,0,.68),.28,1.18,TURQ,64)
    esfera('Hombro de tanque',(0,0,1.28),(.28,.28,.18),TURQ)
    cilindro('Cuello de tanque',(0,0,1.43),.1,.18,ACERO)
    caja('Regulador',(0,-.12,1.48),(.24,.16,.15),ACERO,.025)
    toro('Manómetro',(0,-.23,1.55),.075,.012,ACERO,(math.pi/2,0,0))
    tubo('Aguja manómetro',(0,-.245,1.55),(.04,-.25,1.59),.008,ROJO)
    caja('Etiqueta oxígeno',(0,-.282,.72),(.34,.018,.34),MARFIL,.02)
    texto('Texto oxígeno','O2',(0,-.297,.69),.15,PETROLEO)
    cilindro('Base de tanque',(0,0,.08),.3,.12,PETROLEO)
def mesa_instrumental():
    caja('Bandeja de instrumental',(0,0,.85),(1.3,.78,.12),ACERO,.045)
    caja('Alfombrilla de silicona',(0,0,.92),(1.18,.65,.025),GOMA,.035)
    for x in [-.53,.53]:
        for y in [-.27,.27]: cilindro('Pata de mesa',(x,y,.43),.035,.78,ACERO)
    # Instrumentos miniatura colocados ordenadamente sobre la bandeja
    for i,n in enumerate(['Bisturi de mesa','Pinza hemostatica','Tijeras quirurgicas']):
        y=-.19+i*.19;caja(n,(-.05,y,.96),(.76,.045,.035),ACERO,.015)
        caja('Mango de '+n,(-.33,y,.98),(.19,.055,.05),PETROLEO,.02)
    caja('Bandeja auxiliar',(0,.52,.7),(.62,.28,.08),MARFIL,.035)
    toro('Asa de bandeja',(0,.69,.73),.08,.014,ACERO,(math.pi/2,0,0))

MODELOS=[('Bisturi',bisturi),('Pinza_Hemostatica',pinza),('Tijeras_Quirurgicas',tijeras),('Monitor_Signos_Vitales',monitor),('Mesa_Quirurgica',mesa_quirurgica),('Lampara_Quirurgica',lampara),('Maquina_Anestesia',maquina_anestesia),('Camilla_Emergencia',camilla_urgencias),('Desfibrilador',desfibrilador),('Tanque_Oxigeno',tanque_oxigeno),('Mesa_Instrumental',mesa_instrumental)]
for n,fn in MODELOS: modelo(n,fn)
print('Modelos generados:', OUT)

