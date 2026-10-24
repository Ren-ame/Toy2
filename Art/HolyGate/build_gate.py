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

# ---------- 치수 (m)
OPEN_W, OPEN_HS = 7.0, 7.0
WALL_W, WALL_H, WALL_D = 15.0, 22.0, 4.0
FRONT = -WALL_D / 2
TOWER_W, TOWER_D, TOWER_H = 3.6, WALL_D + 1.4, 27.0
STEPS = [(3.0, 0.45), (2.0, 0.9), (1.0, 1.35)]          # (아치 폭 추가분, 파고 들어가는 깊이) — 바깥일수록 얕게
ROSE_Z = 19.0

marble, gold = [], []

# ---------- 중앙 벽 + 계단식 아키볼트 + 통로
wall = box("Wall", (WALL_W, WALL_D, WALL_H), (0, 0, WALL_H / 2), MARBLE)
for extra, depth in STEPS:
    cut(wall, prism("c", arch_outline(OPEN_W + extra, OPEN_HS, -1), FRONT - 1, FRONT + depth, MARBLE))
cut(wall, prism("c", arch_outline(OPEN_W, OPEN_HS, -1), -WALL_D, WALL_D, MARBLE))
cut(wall, cyl("rc", 2.2, 1.2, (0, FRONT, ROSE_Z), MARBLE, 48, (math.radians(90), 0, 0)))      # 장미창 구멍
marble.append(wall)

# 아키볼트 둥근 갈비(리브) + 각 단 모서리의 작은 기둥(콜로넷) + 기둥머리·받침
for k, (extra, depth) in enumerate(STEPS):
    w = OPEN_W + extra; y = FRONT + depth - 0.22
    marble.append(rib(f"Rib{k}", arch_pts(w - 0.5, OPEN_HS), y, 0.22, MARBLE))
    for sx in (-1, 1):
        x = sx * (w / 2 - 0.25)
        marble.append(cyl(f"Col{k}{sx}", 0.2, OPEN_HS - 1.2, (x, y, 0.95 + (OPEN_HS - 1.2) / 2), MARBLE, 16))
        gold.append(box(f"Cap{k}{sx}", (0.6, 0.6, 0.35), (x, y, OPEN_HS - 0.1), GOLD))         # 기둥머리(금)
        marble.append(box(f"Base{k}{sx}", (0.6, 0.6, 0.4), (x, y, 0.95), MARBLE))
gold.append(rib("GoldArch", arch_pts(OPEN_W + 3.0 + 0.25, OPEN_HS), FRONT - 0.05, 0.12, GOLD))   # 가장 바깥 금 테두리

# 팀파눔(문 위 반원 판) + 상인방 + 금색 해 문장
tym = prism("Tympanum", [(-OPEN_W / 2, OPEN_HS)] + arch_pts(OPEN_W, OPEN_HS) , -0.2, 0.2, MARBLE)
marble.append(tym)
gold.append(box("Lintel", (OPEN_W + 0.1, 0.6, 0.45), (0, -0.05, OPEN_HS), GOLD))
sun_z = OPEN_HS + 2.6
gold.append(cyl("Sun", 0.9, 0.3, (0, -0.3, sun_z), GOLD, 40, (math.radians(90), 0, 0)))
for i in range(12):
    L = 3.0 if i % 2 == 0 else 2.2
    gold.append(box(f"SunRay{i}", (0.14, 0.18, L), (0, -0.28, sun_z), GOLD, (0, math.radians(i * 15), 0)))

# 띠 몰딩 + 박공 + 박공 테두리(코핑) + 작은 첨탑
marble.append(box("CorniceTop", (WALL_W + 0.5, WALL_D + 0.6, 0.6), (0, 0, WALL_H - 0.3), MARBLE))
marble.append(box("CorniceMid", (WALL_W + 0.3, WALL_D + 0.4, 0.4), (0, 0, 15.9), MARBLE))
gold.append(box("GoldLine", (WALL_W + 0.3, 0.08, 0.12), (0, FRONT - 0.32, WALL_H - 0.62), GOLD))
GH = 7.0
marble.append(prism("Gable", [(-WALL_W / 2, WALL_H), (0, WALL_H + GH), (WALL_W / 2, WALL_H)], FRONT + 0.2, -FRONT, MARBLE))
slope = math.atan2(GH, WALL_W / 2); L = math.hypot(GH, WALL_W / 2)
for sx in (-1, 1):
    marble.append(box(f"Coping{sx}", (L + 0.4, 0.7, 0.45), (sx * WALL_W / 4, FRONT + 0.2, WALL_H + GH / 2 + 0.15), MARBLE, (0, sx * slope, 0)))
    marble.append(box(f"GPinBase{sx}", (0.9, 0.9, 1.4), (sx * (WALL_W / 2 - 0.2), FRONT + 0.3, WALL_H + 0.7), MARBLE))
    marble.append(cone(f"GPin{sx}", 0.62, 2.6, (sx * (WALL_W / 2 - 0.2), FRONT + 0.3, WALL_H + 2.7), MARBLE))
# 박공 꼭대기 빛살 문장
for i in range(8):
    gold.append(box(f"Ray{i}", (0.22, 0.3, 2.8 if i % 2 == 0 else 1.7), (0, FRONT - 0.05, WALL_H + 4.2), GOLD, (0, math.radians(i * 22.5), 0)))
gold.append(cyl("RayHub", 0.6, 0.4, (0, FRONT - 0.1, WALL_H + 4.2), GOLD, 32, (math.radians(90), 0, 0)))
marble.append(box("ApexBlock", (1.0, 1.0, 0.9), (0, FRONT + 0.2, WALL_H + GH + 0.2), MARBLE))
marble.append(cone("ApexPin", 0.55, 2.2, (0, FRONT + 0.2, WALL_H + GH + 1.75), MARBLE))
desel(); bpy.ops.mesh.primitive_uv_sphere_add(radius=0.28, segments=16, ring_count=8, location=(0, FRONT + 0.2, WALL_H + GH + 3.0))
o = bpy.context.active_object; o.data.materials.append(GOLD); gold.append(o)
# 장미창: 금 테 + 바큇살 + 안쪽 원
desel(); bpy.ops.mesh.primitive_torus_add(major_radius=2.2, minor_radius=0.2, major_segments=48, minor_segments=8, location=(0, FRONT + 0.05, ROSE_Z), rotation=(math.radians(90), 0, 0))
t = bpy.context.active_object; t.data.materials.append(GOLD); gold.append(t)
for i in range(8):
    gold.append(box(f"Spoke{i}", (0.14, 0.2, 4.2), (0, FRONT + 0.6, ROSE_Z), GOLD, (0, math.radians(i * 22.5), 0)))
gold.append(cyl("RoseHub", 0.45, 0.3, (0, FRONT + 0.55, ROSE_Z), GOLD, 24, (math.radians(90), 0, 0)))
marble.append(cyl("RoseBack", 2.3, 0.2, (0, FRONT + 0.75, ROSE_Z), MARBLE, 48, (math.radians(90), 0, 0)))

# ---------- 양옆 탑: 모서리 버트레스, 층 띠, 아치 홈, 꼭대기 작은 첨탑 4개 + 큰 첨탑
for sx in (-1, 1):
    x = sx * (WALL_W / 2 + TOWER_W / 2 - 0.2)
    t = box(f"Tower{sx}", (TOWER_W, TOWER_D, TOWER_H), (x, 0, TOWER_H / 2), MARBLE)
    for h in (4.5, 12.5, 19.5):
        c = prism("n", arch_outline(1.5, 3.2, 0), -TOWER_D / 2 - 1, -TOWER_D / 2 + 0.45, MARBLE)
        c.location = (x, 0, h); cut(t, c)
    marble.append(t)
    for z in (9.8, 17.0, TOWER_H - 0.3):
        marble.append(box(f"Course{sx}{z}", (TOWER_W + 0.4, TOWER_D + 0.4, 0.5), (x, 0, z), MARBLE))
    gold.append(box(f"TBand{sx}", (TOWER_W + 0.45, TOWER_D + 0.45, 0.14), (x, 0, TOWER_H - 0.62), GOLD))
    for cx in (-1, 1):                                                 # 모서리 버트레스(층층이 좁아짐)
        for i, (hh, ww) in enumerate(((10.0, 0.9), (17.5, 0.7))):
            marble.append(box(f"Butt{sx}{cx}{i}", (ww, ww, hh), (x + cx * (TOWER_W / 2), -TOWER_D / 2, hh / 2), MARBLE))
    for cx in (-1, 1):                                                 # 꼭대기 모서리 작은 첨탑
        for cy in (-1, 1):
            px, py = x + cx * (TOWER_W / 2 - 0.35), cy * (TOWER_D / 2 - 0.35)
            marble.append(box(f"PinB{sx}{cx}{cy}", (0.7, 0.7, 1.2), (px, py, TOWER_H + 0.6), MARBLE))
            marble.append(cone(f"Pin{sx}{cx}{cy}", 0.48, 2.4, (px, py, TOWER_H + 2.4), MARBLE))
    marble.append(box(f"SpireBase{sx}", (TOWER_W * 0.75, TOWER_D * 0.6, 1.4), (x, 0, TOWER_H + 0.7), MARBLE))
    marble.append(cone(f"Spire{sx}", TOWER_W * 0.52, 9.0, (x, 0, TOWER_H + 5.9), MARBLE, 8, (0, 0, math.radians(22.5))))
    gold.append(cyl(f"SpireRing{sx}", TOWER_W * 0.45, 0.25, (x, 0, TOWER_H + 1.55), GOLD, 8, (0, 0, math.radians(22.5))))
    desel(); bpy.ops.mesh.primitive_uv_sphere_add(radius=0.42, segments=16, ring_count=8, location=(x, 0, TOWER_H + 10.7))
    o = bpy.context.active_object; o.data.materials.append(GOLD); gold.append(o)

# ---------- 받침 계단
for i in range(3):
    marble.append(box(f"Step{i}", (WALL_W + 2 * TOWER_W + 3.0 - i * 1.4, WALL_D + 5.0 - i * 1.2, 0.3), (0, -0.6 + i * 0.35, 0.15 + i * 0.3), MARBLE))

# ---------- 베벨: 대리석 큰 덩어리만 (모서리에 빛이 맺혀 훨씬 사실적으로 보임)
for o in marble:
    if o.type == 'MESH' and len(o.data.polygons) < 2000 and max(o.dimensions) > 0.8: bevel(o, 0.05)
gate = join(marble + gold, "HolyGate")

# ---------- 문 2짝: 판넬(돋을 틀) + 금 경첩 띠 + 징
door_parts = []
DH = OPEN_HS - 0.95 - 0.25
for sx in (-1, 1):
    cx = sx * OPEN_W / 4
    d = box(f"Door{sx}", (OPEN_W / 2 - 0.04, 0.35, DH), (cx, 0.0, 0.95 + DH / 2), DOOR); bevel(d, 0.03); door_parts.append(d)
    for row in range(3):                                              # 판넬 3단
        pz = 0.95 + DH * (row + 0.5) / 3
        fr = box(f"Panel{sx}{row}", (OPEN_W / 2 - 0.7, 0.12, DH / 3 - 0.5), (cx, -0.22, pz), DOOR); bevel(fr, 0.04); door_parts.append(fr)
    for z in (1.8, 0.95 + DH - 0.85):                                 # 경첩 띠(금)
        door_parts.append(box(f"Hinge{sx}{z}", (OPEN_W / 2 - 0.3, 0.1, 0.28), (cx, -0.3, z), GOLD))
        for k in range(5):
            door_parts.append(box(f"Stud{sx}{z}{k}", (0.14, 0.14, 0.14), (cx - (OPEN_W / 2 - 0.9) / 2 + k * (OPEN_W / 2 - 0.9) / 4, -0.38, z), GOLD))
door_parts.append(box("Seam", (0.18, 0.45, DH), (0, -0.05, 0.95 + DH / 2), GOLD))
door_parts.append(cyl("Knocker", 0.7, 0.2, (0, -0.32, 0.95 + DH * 0.55), GOLD, 32, (math.radians(90), 0, 0)))
doors = join(door_parts, "HolyGate_Doors")

for ob in (gate, doors):
    desel(); ob.select_set(True); bpy.context.view_layer.objects.active = ob
    bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT'); bpy.ops.uv.cube_project(cube_size=4.0); bpy.ops.object.mode_set(mode='OBJECT')
    bpy.ops.object.shade_auto_smooth(angle=math.radians(35))
for ob in (gate, doors): triangulate(ob)
tris = sum(len(p.vertices) - 2 for o in (gate, doors) for p in o.data.polygons)
print("TRIS", tris)
bpy.ops.wm.save_as_mainfile(filepath=OUT + "/HolyGate.blend")
desel(); gate.select_set(True); doors.select_set(True)
bpy.ops.export_scene.fbx(filepath=OUT + "/HolyGate.fbx", use_selection=True, apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS',
                         axis_forward='-Z', axis_up='Y', bake_space_transform=True, object_types={'MESH'}, mesh_smooth_type='FACE', path_mode='STRIP')
print("DONE")
