# 성문 양옆 성벽 모듈 키트: 온전한 벽 / 무너진 벽 A·B / 잔해 기둥 / 돌무더기
import sys, math, random
exec(open(__file__.rsplit('/',1)[0] + "/gate_lib.py").read())
random.seed(7)
L, H, T = 8.0, 12.0, 2.5           # 모듈 길이·높이·두께 (x: 0→8 로 이어붙임)
F = -T / 2                         # 정면(-Y)

def wall_parts(length=L, height=H):
    P = []
    P.append(box("Body", (length, T, height), (length / 2, 0, height / 2), MARBLE))
    P.append(box("Plinth", (length - 0.004, T + 0.8, 1.2), (length / 2, 0, 0.6), MARBLE))          # 아래 받침
    P.append(box("PlinthTop", (length - 0.004, T + 0.5, 0.3), (length / 2, 0, 1.35), MARBLE))
    P.append(box("Course", (length - 0.004, T + 0.4, 0.45), (length / 2, 0, height * 0.72), MARBLE))   # 띠 몰딩
    P.append(box("Cornice", (length - 0.004, T + 0.6, 0.6), (length / 2, 0, height - 0.3), MARBLE))
    P.append(box("GoldLine", (length - 0.004, 0.06, 0.1), (length / 2, F - 0.21, height * 0.72 - 0.12), GOLD))
    for x in (0.0,):                                                                 # 시작점 버트레스(이어붙이면 모듈마다 기둥 하나)
        P.append(box("Butt", (1.1, 1.0, height * 0.72), (x, F - 0.4, height * 0.36), MARBLE))
        P.append(cone("ButtCap", 0.78, 1.2, (x, F - 0.4, height * 0.72 + 0.6), MARBLE))
    n = int(length // 2)                                                             # 성가퀴(톱니 모양 흉벽)
    for i in range(n):
        P.append(box("Merlon", (1.1, T, 1.6), (1.0 + i * 2.0, 0, height + 0.8), MARBLE))
    return P

def niche(target_parts, length=L):
    body = target_parts[0]
    c = prism("n", arch_outline(2.6, 5.6, 0), F - 1, F + 0.35, MARBLE); c.location = (length / 2, 0, 1.5); cut(body, c)
    return [rib("NicheRib", arch_pts(2.4, 5.6), F + 0.05, 0.1, MARBLE)]
def place(objs, loc):
    for o in objs: o.location = (o.location[0] + loc[0], o.location[1] + loc[1], o.location[2] + loc[2])

def broken_cutter(profile):
    """profile: [(x, z), ...] 무너진 윗선. 그 위쪽을 전부 잘라냄"""
    pts = [(-2, profile[0][1])] + profile + [(L + 2, profile[-1][1]), (L + 2, 40), (-2, 40)]
    return prism("brk", pts, -6, 6, MARBLE)

def jag(xs_zs, amp=0.35):
    """돌 블록이 빠진 듯한 계단식 파단선 (가로 0.6~1.1m, 세로 0.5m 단위)"""
    def zat(x):
        for (x0, z0), (x1, z1) in zip(xs_zs, xs_zs[1:]):
            if x0 <= x <= x1: return z0 + (z1 - z0) * (x - x0) / (x1 - x0)
        return xs_zs[-1][1]
    out, x = [], xs_zs[0][0]
    while x < xs_zs[-1][0]:
        nx = min(x + random.uniform(0.6, 1.1), xs_zs[-1][0])
        z = round((zat((x + nx) / 2) + random.uniform(-amp, amp)) / 0.5) * 0.5
        out += [(x, z), (nx, z)]; x = nx
    return out
def _old_jag(xs_zs, amp=0.35):
    out = []
    for i in range(len(xs_zs) - 1):
        (x0, z0), (x1, z1) = xs_zs[i], xs_zs[i + 1]
        n = max(2, int(abs(x1 - x0) / 0.45))
        for k in range(n):
            t = k / n; out.append((x0 + (x1 - x0) * t, z0 + (z1 - z0) * t + random.uniform(-amp, amp)))
    out.append(xs_zs[-1]); return out

def finish(parts, name, profile=None):
    parts = [p for p in parts]
    if profile:
        kept = []
        for i, p in enumerate(parts):    # 본체 외 부품은 2cm 더 낮게 잘라 단면이 겹치지 않게 (z-fighting 방지)
            c = broken_cutter(profile if i == 0 else [(x, z - 0.02) for x, z in profile]); cut(p, c)
            if len(p.data.polygons) > 0: kept.append(p)
            else: bpy.data.objects.remove(p, do_unlink=True)
        parts = kept
    for p in parts:
        if p.type == 'MESH' and max(p.dimensions) > 0.8 and len(p.data.polygons) < 3000: bevel(p, 0.04)
    ob = join(parts, name)
    desel(); ob.select_set(True); bpy.context.view_layer.objects.active = ob
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)   # 피벗 = 왼쪽 아래 (0,0,0)
    desel(); ob.select_set(True); bpy.context.view_layer.objects.active = ob
    bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT'); bpy.ops.uv.cube_project(cube_size=4.0); bpy.ops.object.mode_set(mode='OBJECT')
    bpy.ops.object.shade_auto_smooth(angle=math.radians(35))
    return ob

def chunks(name, n, area, size=(0.4, 1.4), zmax=0.6):
    P = []
    for i in range(n):
        s = random.uniform(*size)
        b = box(f"R{i}", (s * random.uniform(0.8, 1.6), s * random.uniform(0.7, 1.3), s * random.uniform(0.5, 1.0)),
                (random.uniform(area[0], area[1]), random.uniform(area[2], area[3]), random.uniform(0, zmax) + s * 0.3), MARBLE,
                (random.uniform(-0.5, 0.5), random.uniform(-0.5, 0.5), random.uniform(0, 3.14)))
        bevel(b, 0.06); P.append(b)
    return P

kit = []
# 1) 온전한 벽 (성문 탑에 바로 붙는 구간)
p = wall_parts(); p += niche(p); kit.append(finish(p, "HolyWall_Intact"))
# 2) 무너진 벽 A — 오른쪽으로 갈수록 낮아짐
p = wall_parts(); p += niche(p)
kit.append(finish(p, "HolyWall_BrokenA", jag([(0, 12.6), (2.5, 11.0), (4.5, 8.0), (6.5, 6.5), (8, 5.5)])))
# 3) 무너진 벽 B — 가운데가 V자로 무너짐
p = wall_parts()
kit.append(finish(p, "HolyWall_BrokenB", jag([(0, 9.5), (2.5, 6.0), (4.0, 3.2), (5.5, 5.0), (8, 7.8)])))
# 4) 잔해 기둥(끝부분 그루터기) — 4m짜리 낮은 벽
p = wall_parts(4.0, 9.0)
pr = jag([(0, 6.5), (1.5, 5.0), (3.0, 3.0), (4.0, 2.2)])
kit.append(finish(p, "HolyWall_Stub", pr))
# 5) 돌무더기
kit.append(finish(chunks("Rubble", 22, (-2.0, 2.0, -1.5, 1.5)), "HolyWall_Rubble"))

for o in kit: triangulate(o)
tris = {o.name: sum(len(q.vertices) - 2 for q in o.data.polygons) for o in kit}
print("TRIS", tris)
for o in kit:
    desel(); o.select_set(True); bpy.context.view_layer.objects.active = o
    bpy.ops.export_scene.fbx(filepath=f"{OUT}/walls/{o.name}.fbx", use_selection=True, apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS',
                             axis_forward='-Z', axis_up='Y', bake_space_transform=True, object_types={'MESH'}, mesh_smooth_type='FACE', path_mode='STRIP')
bpy.ops.wm.save_as_mainfile(filepath=OUT + "/walls/HolyWalls.blend")
print("DONE")
