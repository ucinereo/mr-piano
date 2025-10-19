from PIL import Image, ImageDraw
import numpy as np
import yaml
import matplotlib.pyplot as plt

config = None
with open('outputs/img_0000_meta.yaml', 'r') as f:
    config = yaml.safe_load(f)

img = Image.open('outputs/img_0000.png')
draw = ImageDraw.Draw(img)
width, height = img.size
print(width, height)

for [x, y] in config["white_key_corners_px"]:
    draw.circle((width - x, y), 2.0, fill='blue')

draw.circle((width / 2, height / 2), 1.0, fill='blue')

plt.imshow(img)
plt.show()
