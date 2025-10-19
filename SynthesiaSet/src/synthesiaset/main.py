import random
import os
from pathlib import Path

import numpy as np
import drjit as dr
import trimesh
import yaml
import mitsuba as mi

from synthesiaset.piano_mesh import PianoMesh
from synthesiaset.piano_config import PianoConfig

mi.set_variant("scalar_rgb")

# Temporary file names for the meshes
BLACK_PLY = 'tmp_black.ply'
WHITE_PLY = 'tmp_white.ply'

# TODO
def project_points_to_pixels(sensor: mi.Sensor, points_3d: np.ndarray) -> np.ndarray:
    # Get the 4x4 world->clip matrix
    proj_matrix = np.array(sensor.projection_transform().matrix, dtype=np.float32)

    # Convert points to homogeneous coordinates (Nx4)
    points_h = np.hstack([points_3d, np.ones((len(points_3d), 1), dtype=np.float32)])

    # Multiply by projection matrix
    clip = proj_matrix @ points_h  # (N,4)

    # Perspective divide
    ndc = clip[:, :3] / clip[:, 3:4]  # (N,3) x/w, y/w, z/w

    x_ndc = ndc[:, 0]
    y_ndc = ndc[:, 1]
    z_ndc = ndc[:, 2]  # can be used for depth / mask

    # Convert NDC [-1,1] → pixel coordinates
    film = sensor.film()
    width, height = map(float, film.size())

    u = (x_ndc + 1.0) * 0.5 * width
    v = (1.0 - (y_ndc + 1.0) * 0.5) * height  # flip y-axis for top-left origin

    pixels = np.stack([u, v], axis=-1)
    return pixels


def get_white_key_top_corners(white_mesh_file: str) -> np.ndarray:
    mesh = trimesh.load_mesh(white_mesh_file, process=False)
    verts = mesh.vertices

    min_x, _, min_z = verts.min(axis=0)
    max_x, max_y, max_z = verts.max(axis=0)
    corners = np.array([
        [min_x, max_y, min_z],
        [max_x, max_y, max_z],
        [max_x, max_y, min_z],
        [min_x, max_y, max_z]
    ])
    return corners


def load_random_piano_mesh():
    config = PianoConfig.create_random()
    piano = PianoMesh(config)

    whites, blacks = piano._create_88()

    # to be able to set a BSDF for the mesh, we need to first store it temporarily
    whites.export(WHITE_PLY, file_type='ply')
    blacks.export(BLACK_PLY, file_type='ply')


def add_corner_cubes(scene_dict: dict, corners: np.ndarray, size: float = 0.01, color=None):
    if color is None:
        color = [1.0, 0.0, 0.0]
    bsdf = {"type": "diffuse", "reflectance": {"type": "rgb", "value": color}}

    for i, corner in enumerate(corners):
        scene_dict[f"corner_{i}"] = {
            "type": "cube",
            "to_world": mi.ScalarAffineTransform4f.translate(corner.tolist()) @ mi.ScalarTransform4f.scale(size),
            "bsdf": bsdf
        }


def create_scene(sensor: mi.Sensor, white_corners_3d: np.ndarray, envmap_dir="envmaps"):
    # fetch random environment map
    envmap = random.choice([
        os.path.join(envmap_dir, f)
        for f in os.listdir(envmap_dir)
        if f.endswith(".exr")
    ])

    # BSDFs
    white_bsdf = {"type": "plastic",
                  "diffuse_reflectance": {"type": "rgb", "value": [1.0, 1.0, 1.0]},
                  "specular_reflectance": {"type": "rgb", "value": [0.1, 0.1, 0.1]}}
    black_bsdf = {"type": "plastic",
                  "diffuse_reflectance": {"type": "rgb", "value": [0.1, 0.1, 0.1]},
                  "specular_reflectance": {"type": "rgb", "value": [0.1, 0.1, 0.1]}}

    scene_dict = {
        "type": "scene",
        "emitter": {"type": "envmap", "filename": envmap, "scale": 1.0},
        "sensor": sensor,
        "integrator": {"type": "path"},
        "white_keys": {"type": "ply", "filename": WHITE_PLY, "bsdf": white_bsdf},
        "black_keys": {"type": "ply", "filename": BLACK_PLY, "bsdf": black_bsdf},
    }

    # Add corner cubes
    if white_corners_3d is not None:
        add_corner_cubes(scene_dict, white_corners_3d, size=0.01)

    scene = mi.load_dict(scene_dict)
    return scene, envmap


def random_camera_pose():
    # TODO: figure out better parameters
    distance = random.uniform(1.5, 2.5)
    azimuth = random.uniform(-20, 20)
    elevation = random.uniform(20, 60)

    az, el = np.radians(azimuth), np.radians(elevation)
    x = distance * np.sin(az) * np.cos(el)
    y = distance * np.sin(el)
    z = distance * np.cos(az) * np.cos(el)

    origin = mi.Point3f([x, y, z])
    target = mi.Point3f([0, 0, 0])
    up = mi.Vector3f([0, 1, 0])

    camera = mi.load_dict({
        "type": "perspective",
        "to_world": mi.ScalarTransform4f.look_at(origin=origin, target=target, up=up),
        "fov": 45,
        "film": {"type": "hdrfilm", "width": 640, "height": 480, "rfilter": {"type": "gaussian"}},
        "sampler": {"type": "independent", "sample_count": 64}
    })
    meta = dict(distance=distance, azimuth=azimuth, elevation=elevation)
    return camera, meta


def render_and_save(scene, meta, out_dir="outputs", idx=0):
    out_dir = Path(out_dir)
    out_dir.mkdir(exist_ok=True, parents=True)

    img = mi.render(scene)
    img_path = out_dir / f"img_{idx:04d}.png"
    meta_path = out_dir / f"img_{idx:04d}_meta.yaml"

    mi.util.write_bitmap(str(img_path), img)
    with open(meta_path, "w") as f:
        yaml.dump(meta, f)

def main(n_samples=1, envmap_dir="envmaps", out_dir="outputs"):
    for i in range(n_samples):
        load_random_piano_mesh()
        white_corners_3d = get_white_key_top_corners(WHITE_PLY)

        sensor, sensor_meta = random_camera_pose()
        scene, envmap_file = create_scene(sensor, white_corners_3d, envmap_dir)

        # Project corners into 2D pixels
        white_corners_px = project_points_to_pixels(sensor, white_corners_3d)

        # TODO: better meta file
        meta = {
            "envmap_file": envmap_file,
            "camera": sensor_meta,
            "white_key_corners_3d": white_corners_3d.tolist(),
            "white_key_corners_px": white_corners_px.tolist()
        }

        render_and_save(scene, meta, out_dir=out_dir, idx=i)
        print(f"[{i+1}/{n_samples}]")


if __name__ == "__main__":
    main()
