import bpy, os
from pathlib import Path

carpeta=Path(r'C:\Users\Rafo\Universidad\6ToSemestre\PDI\1ra\ProyectoMedicos\Assets\Modelos_MEDIVR')
exportados=carpeta/'FBX_Para_Unity'
exportados.mkdir(exist_ok=True)

def nombre_normal(nombre):
    mapa={
      'Camera':'Camara','Camera.001':'Camara_Auxiliar','Camara de producto':'Camara','Cámara de producto':'Camara',
      'Area':'Luz_Principal','Area.001':'Luz_Relleno','Area.002':'Luz_Contorno',
      'Point':'Luz_Puntual','Spot':'Luz_Dirigida','Sun':'Luz_Solar',
      'Aro de unión':'Anillo_de_fijacion','Hoja afilada de acero':'Hoja_del_Bisturi',
      'Mango del bisturí':'Mango_del_Bisturi','Estría antideslizante':'Muesca_de_mango','Articulación':'Eje_de_la_Pinza',
      'Rama de pinza':'Brazo_de_la_Pinza','Anilla para dedo':'Anilla_de_la_Pinza',
      'Hoja de tijera':'Hoja_de_Tijera','Aro ergonómico':'Anilla_de_Tijera',
      'Anillo_de_Union':'Aro','Hoja_del_Bisturi':'Hoja','Mango_del_Bisturi':'Mango',
    }
    if nombre in mapa:return mapa[nombre]
    # Blender's automatic numeric suffixes are replaced with an intentional part number.
    import re
    estria=re.match(r'^Estria_antideslizante(?:_Parte_(\d+))?$',nombre)
    if estria:return 'Estria_{:02d}'.format(int(estria.group(1)) if estria.group(1) else 1)
    m=re.match(r'^(.*)\.(\d{3})$',nombre)
    if m and m.group(1)=='Estría antideslizante':return 'Estria_{:02d}'.format(int(m.group(2))+1)
    if m:return f'{m.group(1)}_Parte_{int(m.group(2))+1:02d}'
    return nombre.replace(' ','_').replace('í','i').replace('é','e').replace('á','a').replace('ó','o').replace('ú','u')

for archivo in sorted(carpeta.glob('*.blend')):
    bpy.ops.wm.open_mainfile(filepath=str(archivo))
    for obj in bpy.data.objects:
        obj.name=nombre_normal(obj.name)
        if obj.type=='CAMERA': obj.name='Camara'
        elif obj.type=='LIGHT' and obj.name.startswith('Area'): obj.name='Luz_Area'
    # Avoid collisions after translating generic auto names.
    vistos={}
    for obj in bpy.data.objects:
        base=obj.name
        if base in vistos:
            vistos[base]+=1; obj.name=f'{base}_{vistos[base]:02d}'
        else: vistos[base]=1
    bpy.ops.wm.save_as_mainfile(filepath=str(archivo))
    # Unity receives only mesh parts; the Blender camera and studio lamps stay in .blend.
    bpy.ops.object.select_all(action='DESELECT')
    for obj in bpy.data.objects:
        if obj.type=='MESH': obj.select_set(True)
    if bpy.context.selected_objects:
        bpy.context.view_layer.objects.active=bpy.context.selected_objects[0]
        bpy.ops.export_scene.fbx(filepath=str(exportados/(archivo.stem+'.fbx')),use_selection=True,apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',object_types={'MESH'},bake_space_transform=False,add_leaf_bones=False,path_mode='AUTO',mesh_smooth_type='FACE')
print('Archivos Blender normalizados y FBX creados en',exportados)
