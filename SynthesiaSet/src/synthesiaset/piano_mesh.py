import trimesh
import numpy as np

from synthesiaset.key_group import KeyGroup
from synthesiaset.piano_config import PianoConfig

class PianoMesh:
    def __init__(self, config: PianoConfig):
        self.config = config

    def create(self):
        # piano = self._create_3_group()
        # piano = self._create_4_group()
        piano = self._create_88()
        return piano

    def _create_88(self):
        groups = [
            KeyGroup.create_2_group(self.config),
            KeyGroup.create_7_group(self.config),
            KeyGroup.create_7_group(self.config),
            KeyGroup.create_7_group(self.config),
            KeyGroup.create_7_group(self.config),
            KeyGroup.create_7_group(self.config),
            KeyGroup.create_7_group(self.config),
            KeyGroup.create_7_group(self.config),
            KeyGroup.create_1_group(self.config),
        ]
        merged = KeyGroup.merge_all(groups)
        white_mesh, black_mesh = merged.get_merged_mesh()

        # scale uniformly
        white_mesh.apply_scale(1.0 / merged.width)
        black_mesh.apply_scale(1.0 / merged.width)
        return white_mesh, black_mesh

    def _create_76(self):
        groups = [
            KeyGroup.create_1_group(self.config),
            KeyGroup.create_7_group(self.config),
            KeyGroup.create_7_group(self.config),
            KeyGroup.create_7_group(self.config),
            KeyGroup.create_7_group(self.config),
            KeyGroup.create_7_group(self.config),
            KeyGroup.create_7_group(self.config),
            KeyGroup.create_2_group(self.config),
        ]
        merged = KeyGroup.merge_all(groups)
        white_mesh, black_mesh = merged.get_merged_mesh()

        # scale uniformly
        white_mesh.apply_scale(1.0 / merged.width)
        black_mesh.apply_scale(1.0 / merged.width)
        return white_mesh, black_mesh

    def _create_61(self):
        groups = [
            KeyGroup.create_3_group(self.config),
            KeyGroup.create_7_group(self.config),
            KeyGroup.create_7_group(self.config),
            KeyGroup.create_7_group(self.config),
            KeyGroup.create_7_group(self.config),
            KeyGroup.create_4_group(self.config),
            KeyGroup.create_1_group(self.config),
        ]
        merged = KeyGroup.merge_all(groups)
        white_mesh, black_mesh = merged.get_merged_mesh()

        # scale uniformly
        white_mesh.apply_scale(1.0 / merged.width)
        black_mesh.apply_scale(1.0 / merged.width)
        return white_mesh, black_mesh

    def _create_49(self):
        groups = [
            KeyGroup.create_7_group(self.config),
            KeyGroup.create_7_group(self.config),
            KeyGroup.create_7_group(self.config),
            KeyGroup.create_7_group(self.config),
            KeyGroup.create_1_group(self.config),
        ]
        merged = KeyGroup.merge_all(groups)
        white_mesh, black_mesh = merged.get_merged_mesh()

        # scale uniformly
        white_mesh.apply_scale(1.0 / merged.width)
        black_mesh.apply_scale(1.0 / merged.width)
        return white_mesh, black_mesh

    def _create_32(self):
        groups = [
            KeyGroup.create_4_group(self.config),
            KeyGroup.create_7_group(self.config),
            KeyGroup.create_7_group(self.config),
            KeyGroup.create_1_group(self.config),
        ]
        merged = KeyGroup.merge_all(groups)
        white_mesh, black_mesh = merged.get_merged_mesh()

        # scale uniformly
        white_mesh.apply_scale(1.0 / merged.width)
        black_mesh.apply_scale(1.0 / merged.width)
        return white_mesh, black_mesh

def main():
    example_config = PianoConfig('configs/keyboard_min.yaml')
    # example_config = PianoConfig.create_random()
    piano = PianoMesh(example_config)
    white, black = piano._create_88()
    merged = trimesh.util.concatenate([white, black])
    merged.show()

if __name__ == '__main__':
    main()
