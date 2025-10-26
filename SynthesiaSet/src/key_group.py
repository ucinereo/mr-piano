from typing import Self

import numpy as np
import trimesh
from trimesh import Trimesh

from src.piano_types import KeyGroupType
from src.piano_utils import PianoMeasure


# todo: @Nicola, clean this up xD
class KeyGroup:
    def __init__(self, measure: PianoMeasure):
        self.width = 0
        self.n_black_keys = 0
        self.n_white_keys = 0
        self.white_key_meshes = []
        self.black_key_meshes = []
        self.measure = measure

    @classmethod
    def materialize(cls, key_group_type: KeyGroupType, measure: PianoMeasure) -> Self:
        return {
            KeyGroupType.GROUP_1: cls.create_1_group,
            KeyGroupType.GROUP_2: cls.create_2_group,
            KeyGroupType.GROUP_3: cls.create_3_group,
            KeyGroupType.GROUP_4: cls.create_4_group,
            KeyGroupType.GROUP_7: cls.create_7_group,
        }[key_group_type](measure)

    @classmethod
    def merge(cls, left: Self, right: Self) -> Self:
        total_width = left.width + right.width

        left_shift = -(total_width - left.width) / 2
        right_shift = (total_width - right.width) / 2

        left.shift_all(left_shift)
        right.shift_all(right_shift)

        group = cls(left.measure)
        group.width = total_width
        group.n_black_keys = left.n_black_keys + right.n_black_keys
        group.n_white_keys = left.n_white_keys + right.n_white_keys
        group.white_key_meshes = left.white_key_meshes + right.white_key_meshes
        group.black_key_meshes = left.black_key_meshes + right.black_key_meshes

        return group

    @classmethod
    def merge_all(cls, groups: list[Self]) -> Self:
        merged_group = cls(groups[0].measure)
        for group in groups:
            merged_group = cls.merge(merged_group, group)
        return merged_group

    def shift_all(self, x: float):
        for mesh in self.white_key_meshes:
            mesh.apply_translation([x, 0, 0])
        for mesh in self.black_key_meshes:
            mesh.apply_translation([x, 0, 0])

    def _add_white_key(
        self,
        bottom_width: float,
        z_translation: float,
        left_indent: float,
        right_indent: float,
    ):
        # Create the easy bottom part (need to remove one half_space height)
        bottom = trimesh.creation.box(
            extents=[
                bottom_width - self.measure.space,
                self.measure.white_height,
                self.measure.white_length_bottom - self.measure.half_space,
            ]
        )
        # space translation (z is positive, as we need to move down)
        bottom.apply_translation([0, 0, self.measure.half_space / 2])

        # Create the top part
        top_width = bottom_width - left_indent - right_indent
        # need to add halfspace to z, as it gets removed from the bottom half
        top = trimesh.creation.box(
            extents=[
                top_width - self.measure.space,
                self.measure.white_height,
                self.measure.white_length_top + self.measure.half_space,
            ]
        )
        top_offset = -bottom_width / 2 + top_width / 2 + left_indent
        # + half_space in z as we want to move down
        top.apply_translation([top_offset, 0, self.measure.white_top_offset + self.measure.half_space / 2])

        # Merge together and position globally
        key = trimesh.boolean.boolean_manifold([bottom, top], operation="union")
        key.apply_translation([z_translation, self.measure.white_height_offset, 0])

        # Color the key white
        normalized_color = np.array([1.0, 1.0, 1.0], dtype=np.float32)
        key.visual.vertex_colors = np.tile(normalized_color, (len(key.vertices), 1))
        self.white_key_meshes.append(key)
        self.width += bottom_width

    def _add_black_key(self, top_offset):
        black = trimesh.creation.box(
            extents=[
                self.measure.black_width - self.measure.space,
                self.measure.black_height,
                self.measure.black_length - self.measure.half_space,
            ]
        )
        black.apply_translation([top_offset, self.measure.black_height_offset, self.measure.black_offset])

        # spacing translation (note that z is negative)
        black.apply_translation([0, 0, -self.measure.half_space / 2])

        # Color the key black
        normalized_color = np.array([0.0, 0.0, 0.0], dtype=np.float32)
        black.visual.vertex_colors = np.tile(normalized_color, (len(black.vertices), 1))
        self.black_key_meshes.append(black)

    @classmethod
    def create_3_group(cls, measure: PianoMeasure) -> Self:
        black_offset = (
            measure.white_thin_bottom - measure.black_width
        ) / 2 + measure.group_3_ratio * measure.black_width
        inner_offset = (1 - measure.group_3_ratio) * measure.black_width
        outer_offset = measure.group_3_ratio * measure.black_width

        white_1_translation = -(measure.white_thick_bottom + measure.white_thin_bottom) / 2
        white_3_translation = -white_1_translation

        group = cls(measure)
        group._add_white_key(measure.white_thick_bottom, white_1_translation, 0, outer_offset)
        group._add_white_key(measure.white_thin_bottom, 0, inner_offset, inner_offset)
        group._add_white_key(measure.white_thick_bottom, white_3_translation, outer_offset, 0)

        group._add_black_key(black_offset)
        group._add_black_key(-black_offset)
        return group

    @classmethod
    def create_4_group(cls, measure: PianoMeasure) -> Self:
        black_offset = measure.white_thin_bottom + (measure.group_4_ratio - 0.5) * measure.black_width
        inner_offset = (1 - measure.group_4_ratio) * measure.black_width
        outer_offset = measure.group_4_ratio * measure.black_width

        white_translation_1 = -(measure.white_thin_bottom + measure.white_thick_bottom / 2)
        white_translation_2 = -(measure.white_thin_bottom / 2)
        white_translation_3 = -white_translation_2
        white_translation_4 = -white_translation_1

        group = cls(measure)

        group._add_black_key(0)
        group._add_black_key(black_offset)
        group._add_black_key(-black_offset)

        group._add_white_key(measure.white_thick_bottom, white_translation_1, 0, outer_offset)
        group._add_white_key(measure.white_thin_bottom, white_translation_2, inner_offset, measure.black_width / 2)
        group._add_white_key(measure.white_thin_bottom, white_translation_3, measure.black_width / 2, inner_offset)
        group._add_white_key(measure.white_thick_bottom, white_translation_4, outer_offset, 0)

        return group

    @classmethod
    def create_7_group(cls, measure: PianoMeasure) -> Self:
        left = cls.create_3_group(measure)
        right = cls.create_4_group(measure)
        return cls.merge(left, right)

    @classmethod
    def create_1_group(cls, measure: PianoMeasure) -> Self:
        group = cls(measure)
        group._add_white_key(measure.white_thick_bottom, 0, 0, 0)
        return group

    @classmethod
    def create_2_group(cls, measure: PianoMeasure) -> Self:
        white_translation_1 = -measure.white_thin_bottom / 2
        white_translation_2 = -white_translation_1

        group = cls(measure)

        group._add_black_key(0)
        group._add_white_key(measure.white_thin_bottom, white_translation_1, 0, measure.black_width / 2)
        group._add_white_key(measure.white_thin_bottom, white_translation_2, measure.black_width / 2, 0)
        return group

    def get_merged_mesh(self) -> tuple[Trimesh, Trimesh]:
        return trimesh.util.concatenate(self.white_key_meshes), trimesh.util.concatenate(self.black_key_meshes)
