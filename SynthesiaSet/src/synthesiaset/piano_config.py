import yaml
import random
from dataclasses import dataclass

random.seed(1337)

@dataclass
class PianoConfig:
    # Explicitc params, set by the config
    white_length_total: float = 10.0
    white_height: float = 0.5
    white_thick_bottom: float = 2.0
    white_thin_bottom: float = 1.8

    black_length_ratio: float = 0.7
    black_height: float = 1.0
    black_width: float = 1.0

    space: float = 0.1

    group_3_ratio: float = 0.7
    group_4_ratio: float = 0.5

    # Implicit param calculated for the explicit params
    white_height_offset: float = None
    black_height_offset: float = None
    white_length_bottom: float = None
    white_length_top: float = None
    black_length: float = None
    white_top_offset: float = None
    black_offset: float = None
    half_space: float = None

    def __init__(self, config_file: str = None):
        # Load defaults first
        if not config_file:
            return

        with open(config_file, 'r') as f:
            config = yaml.safe_load(f)

        # Overwrite any provided fields
        for key, value in config.items():
            if hasattr(self, key):
                setattr(self, key, value)
            else:
                raise KeyError(f"Unknown parameter in config: '{key}'")

        self._recompute_implicit()

    def _recompute_implicit(self):
        self.white_height_offset = self.white_height / 2
        self.black_height_offset = self.black_height / 2
        self.white_length_bottom = (1 - self.black_length_ratio) * self.white_length_total
        self.white_length_top = self.black_length_ratio * self.white_length_total
        self.black_length = self.white_length_top
        self.white_top_offset = - self.white_length_total / 2
        self.black_offset = self.white_top_offset
        self.half_space = self.space / 2

    @classmethod
    def create_random(cls):
        config = cls()

        # white keys
        config.white_length_total = random.uniform(6, 12)
        if random.random() > 0.5: # change for flat keyboard is 50%
            config.white_height = random.uniform(0.3, 0.7)
        else:
            config.white_height = random.uniform(2, 3)
        config.white_thick_bottom = random.uniform(1.8, 2.2)
        config.white_thin_bottom = config.white_thick_bottom - random.uniform(0.0, 0.2)

        # black keys
        config.black_length_ratio = random.uniform(0.5, 0.7)
        config.black_height = config.white_height + random.uniform(0.1, 0.7)
        config.black_width = config.white_thin_bottom / 2 + random.uniform(0.0, 0.4)

        # other params
        config.space = random.uniform(0.05, 0.2)
        config.group_3_ratio = random.uniform(0.3, 0.7)
        config.group_4_ratio = random.uniform(0.3, 0.7)
        config._recompute_implicit()
        return config
