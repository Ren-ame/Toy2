import bpy, math, sys
D = sys.argv[-1]
bpy.ops.wm.open_mainfile(filepath=D + "/HolyGate.blend")
sc = bpy.context.scene
with bpy.data.libraries.load(D + "/walls/HolyWalls.blend") as (src, dst): dst.objects = [n for n in src.objects if n.startswith("HolyWall")]
lib = {o.name: o for o in dst.objects}
def put(name, x, y=0, sx=1, rz=0):
    o = lib[name].copy(); sc.collection.objects.link(o); o.location = (x, y, 0); o.scale = (sx, 1, 1); o.rotation_euler = (0, 0, math.radians(rz))
E = 10.9
put("HolyWall_Intact", E); put("HolyWall_BrokenA", E + 8); put("HolyWall_Stub", E + 19)
put("HolyWall_Rubble", E + 15.5, -2.5); put("HolyWall_Rubble", E + 21, -2.2, 1, 70)
put("HolyWall_Intact", -E, sx=-1); put("HolyWall_BrokenB", -E - 8, sx=-1); put("HolyWall_BrokenA", -E - 16, sx=-1)
put("HolyWall_Rubble", -E - 12, -2.4, 1, 40); put("HolyWall_Rubble", -E - 21, -2.6)
bpy.ops.mesh.primitive_plane_add(size=300, location=(0, -10, 0))
g = bpy.context.active_object; m = bpy.data.materials.new("ground"); m.use_nodes = True
m.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.30, 0.28, 0.25, 1); g.data.materials.append(m)
bpy.ops.mesh.primitive_cylinder_add(radius=0.3, depth=1.8, location=(2.5, -9, 0.9))
p = bpy.context.active_object; pm = bpy.data.materials.new("p"); pm.use_nodes = True
pm.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.05, 0.05, 0.06, 1); p.data.materials.append(pm)
bpy.ops.object.light_add(type='SUN', rotation=(math.radians(68), 0, math.radians(-40))); s = bpy.context.active_object.data
s.energy = 5; s.color = (1.0, 0.86, 0.68); s.angle = math.radians(2)
w = bpy.data.worlds.new("w"); sc.world = w; w.use_nodes = True
bg = w.node_tree.nodes["Background"]; bg.inputs[0].default_value = (0.42, 0.52, 0.68, 1); bg.inputs[1].default_value = 0.7
sc.view_settings.view_transform = 'AgX'
bpy.ops.object.empty_add(location=(0, 0, 11)); tgt = bpy.context.active_object
bpy.ops.object.camera_add(location=(14, -62, 6)); cam = bpy.context.active_object; sc.camera = cam; cam.data.lens = 24
c = cam.constraints.new('TRACK_TO'); c.target = tgt; c.track_axis = 'TRACK_NEGATIVE_Z'; c.up_axis = 'UP_Y'
sc.render.engine = 'CYCLES'; sc.cycles.samples = 32; sc.cycles.device = 'CPU'; sc.cycles.use_denoising = True
sc.render.resolution_x, sc.render.resolution_y = 1400, 700
sc.render.filepath = D + "/walls/preview_walls.png"; bpy.ops.render.render(write_still=True)
cam.location = (36, -24, 3); tgt.location = (22, 0, 6); cam.data.lens = 30
sc.render.resolution_x, sc.render.resolution_y = 1100, 750
sc.render.filepath = D + "/walls/preview_walls_close.png"; bpy.ops.render.render(write_still=True)
print("RENDERED")
