# 신성왕국 성문 v2 — 고딕 포털(계단식 아키볼트 + 기둥 + 팀파눔), 탑 장식, 베벨
import bpy, bmesh, math, sys
OUT = sys.argv[-1]
bpy.ops.wm.read_factory_settings(use_empty=True)
sc = bpy.context.scene
def desel(): bpy.ops.object.select_all(action='DESELECT')

def img_mat(name, color, metal, rough, tex=None):
    m = bpy.data.materials.new(name); m.use_nodes = True; nt = m.node_tree
    b = nt.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (*color, 1); b.inputs["Metallic"].default_value = metal; b.inputs["Roughness"].default_value = rough
    if tex:   # 미리보기 렌더용 (Unity에서는 PNG를 따로 연결)
        t = nt.nodes.new("ShaderNodeTexImage"); t.image = bpy.data.images.load(f"{OUT}/HolyGate_Marble_BaseColor.png")
        nt.links.new(t.outputs["Color"], b.inputs["Base Color"])
        n = nt.nodes.new("ShaderNodeTexImage"); n.image = bpy.data.images.load(f"{OUT}/HolyGate_Marble_Normal.png"); n.image.colorspace_settings.name = "Non-Color"
        nm = nt.nodes.new("ShaderNodeNormalMap"); nm.inputs["Strength"].default_value = 0.6
        nt.links.new(n.outputs["Color"], nm.inputs["Color"]); nt.links.new(nm.outputs["Normal"], b.inputs["Normal"])
    return m
MARBLE = img_mat("HolyGate_Marble", (0.9, 0.88, 0.84), 0.0, 0.38, tex=True)
GOLD   = img_mat("HolyGate_Gold",   (0.85, 0.63, 0.24), 1.0, 0.28)
DOOR   = img_mat("HolyGate_Door",   (0.80, 0.78, 0.74), 0.0, 0.5, tex=True)

def arch_pts(w, hs, seg=28):
    r = w / 2; pts = []
    for i in range(seg + 1):
        a = math.radians(180 - 60 * i / seg); pts.append((r + w * math.cos(a), hs + w * math.sin(a)))
    for i in range(1, seg + 1):
        a = math.radians(60 - 60 * i / seg); pts.append((-r + w * math.cos(a), hs + w * math.sin(a)))
    return pts                                  # 왼쪽 시작점(-r,hs) → 꼭대기 → 오른쪽(r,hs)
def arch_outline(w, hs, y0=0.0): return [(-w / 2, y0)] + arch_pts(w, hs) + [(w / 2, y0)]

def prism(name, outline, y0, y1, material):
    """(x, z) 외곽선을 y0~y1 두께로 (Blender: X 좌우, Y 앞뒤(-가 정면), Z 위)"""
    me = bpy.data.meshes.new(name); bm = bmesh.new()
    f = [bm.verts.new((x, y0, z)) for x, z in outline]; b = [bm.verts.new((x, y1, z)) for x, z in outline]
    bm.faces.new(f); bm.faces.new(list(reversed(b)))
    for i in range(len(outline)):
        j = (i + 1) % len(outline); bm.faces.new([f[i], f[j], b[j], b[i]])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces); bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new(name, me); sc.collection.objects.link(ob); ob.data.materials.append(material); return ob

def box(name, size, loc, material, rot=None):
    desel(); bpy.ops.mesh.primitive_cube_add(size=1, location=loc); ob = bpy.context.active_object
    ob.name = name; ob.scale = size; bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if rot: ob.rotation_euler = rot
    ob.data.materials.append(material); return ob

def cyl(name, r, h, loc, material, verts=16, rot=(0, 0, 0)):
    desel(); bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=r, depth=h, location=loc, rotation=rot)
    ob = bpy.context.active_object; ob.name = name; ob.data.materials.append(material); return ob

def cone(name, r, h, loc, material, verts=4, rot=(0, 0, math.radians(45))):
    desel(); bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=r, radius2=0, depth=h, location=loc, rotation=rot)
    ob = bpy.context.active_object; ob.name = name; ob.data.materials.append(material); return ob

def rib(name, pts, y, radius, material):
    """아치를 따라가는 둥근 몰딩 (커브 + 베벨 → 메쉬)"""
    cu = bpy.data.curves.new(name, 'CURVE'); cu.dimensions = '3D'; cu.bevel_depth = radius; cu.bevel_resolution = 2
    sp = cu.splines.new('POLY'); sp.points.add(len(pts) - 1)
    for i, (x, z) in enumerate(pts): sp.points[i].co = (x, y, z, 1)
    ob = bpy.data.objects.new(name, cu); sc.collection.objects.link(ob); cu.materials.append(material)
    desel(); ob.select_set(True); bpy.context.view_layer.objects.active = ob; bpy.ops.object.convert(target='MESH'); return ob

def cut(target, c):
    m = target.modifiers.new("cut", 'BOOLEAN'); m.operation = 'DIFFERENCE'; m.object = c; m.solver = 'EXACT'
    desel(); bpy.context.view_layer.objects.active = target; bpy.ops.object.modifier_apply(modifier=m.name)
    bpy.data.objects.remove(c, do_unlink=True)

def join(objs, name):
    desel()
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]; bpy.ops.object.join(); objs[0].name = name; return objs[0]

def bevel(ob, width):
    m = ob.modifiers.new("bevel", 'BEVEL'); m.width = width; m.segments = 2; m.limit_method = 'ANGLE'; m.angle_limit = math.radians(35)
    desel(); bpy.context.view_layer.objects.active = ob; bpy.ops.object.modifier_apply(modifier=m.name)


def triangulate(ob):
    """구멍 뚫린 n-gon(불리언 결과)은 Unity 임포터가 삼각형으로 못 쪼개서 면이 사라짐 → 미리 삼각화"""
    bm = bmesh.new(); bm.from_mesh(ob.data)
    bmesh.ops.triangulate(bm, faces=bm.faces[:], quad_method='BEAUTY', ngon_method='BEAUTY')
    bm.to_mesh(ob.data); bm.free(); ob.data.update()

