# SynthesiaSet
Synthetic piano dataset generation.

# Installation
```bash
python -m venv .venv        # create virtual environment
source .venv/bin/activate   # activate virtual environment
pip install -e .            # install project with dependencies
```

# How to use
```bash
python src/synthesiaset/piano_mesh.py   # demo of 3D created mesh
python src/synthesiaset/main.py         # creates the dataset
python src/synthesiaset/view_gt.py      # shows the projected points
```