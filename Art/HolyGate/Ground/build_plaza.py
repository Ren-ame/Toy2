# 성문 앞 포석 광장: 1m 칸 단위로 깔고 가장자리는 무너진 듯 들쭉날쭉, 바깥에 흩어진 포석 몇 개
import bpy, bmesh, math, random, sys
OUT = sys.argv[-1]
bpy.ops.wm.read_factory_settings(use_empty=True)
random.seed(3)
mat = bpy.data.materials.new("Plaza_Flagstone")
TOP, TH = 0.04, 0.08                 # 바닥(Plane) 위로 4cm, 두께 8cm
CX, CY, R = 0.0, -15.0, 15.0         # 중심 (성문 정면 -Y 쪽), 반경
bm = bmesh.new()
def slab(x0, y0, x1, y1, z, tilt=0.0):
    vs = [bm.verts.new((x0, y0, z)), bm.verts.new((x1, y0, z + tilt)), bm.verts.new((x1, y1, z + tilt)), bm.verts.new((x0, y1, z))]
    bm.faces.new(vs)
def wob(a): return 0.12 * math.sin(3 * a + 1.3) + 0.08 * math.sin(7 * a + 0.4) + 0.05 * math.sin(13 * a)
cells = set()
for ix in range(-16, 17):
    for iy in range(-31, -2):
        x, y = ix + 0.5, iy + 0.5
        a = math.atan2(y - CY, x - CX); d = math.hypot(x - CX, y - CY)
        lim = R * (1 + wob(a)) * (1.35 if y > CY else 1.0)          # 성문 쪽으로는 더 길게 → 계단 앞까지 닿음
        if d < lim - random.uniform(0, 2.2): cells.add((ix, iy))
for ix, iy in cells: slab(ix, iy, ix + 1, iy + 1, TOP)
bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-4)
top = bm.faces[:]
ext = bmesh.ops.extrude_face_region(bm, geom=top)
for v in [e for e in ext["geom"] if isinstance(e, bmesh.types.BMVert)]: v.co.z -= TH
bmesh.ops.reverse_faces(bm, faces=top)   # 돌출 후 원래 면이 아래가 되므로 뒤집기
bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
# 가장자리 바깥으로 흩어진 포석 (살짝 기울고 회전)
for _ in range(40):
    a = random.uniform(0, 2 * math.pi); d = R * (1 + wob(a)) * random.uniform(0.95, 1.25)
    x, y = CX + d * math.cos(a), CY + d * math.sin(a)
    if y > -3: continue
    w, h = random.uniform(0.6, 1.1), random.uniform(0.5, 0.9); rot = random.uniform(-0.5, 0.5)
    c, s = math.cos(rot), math.sin(rot)
    pts = [(-w / 2, -h / 2), (w / 2, -h / 2), (w / 2, h / 2), (-w / 2, h / 2)]
    top_v = [bm.verts.new((x + px * c - py * s, y + px * s + py * c, TOP + random.uniform(-0.02, 0.03))) for px, py in pts]
    bot_v = [bm.verts.new((v.co.x, v.co.y, -0.04)) for v in top_v]
    bm.faces.new(top_v); bm.faces.new(list(reversed(bot_v)))
    for i in range(4): j = (i + 1) % 4; bm.faces.new([top_v[i], bot_v[i], bot_v[j], top_v[j]])
bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
uv = bm.loops.layers.uv.new()
for f in bm.faces:
    for l in f.loops: l[uv].uv = (l.vert.co.x / 4.0, l.vert.co.y / 4.0)   # 4m 마다 텍스처 1장 (포석 한 줄 = 1m)
bmesh.ops.triangulate(bm, faces=bm.faces[:])
me = bpy.data.meshes.new("HolyGate_Plaza"); bm.to_mesh(me); bm.free()
ob = bpy.data.objects.new("HolyGate_Plaza", me); bpy.context.scene.collection.objects.link(ob); me.materials.append(mat)
print("TRIS", len(me.polygons))
ob.select_set(True); bpy.context.view_layer.objects.active = ob
bpy.ops.export_scene.fbx(filepath=OUT + "/HolyGate_Plaza.fbx", use_selection=True, apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS',
                         axis_forward='-Z', axis_up='Y', bake_space_transform=True, object_types={'MESH'}, mesh_smooth_type='FACE', path_mode='STRIP')
bpy.ops.wm.save_as_mainfile(filepath=OUT + "/HolyGate_Plaza.blend")
print("DONE")
