"""ArtSrc/map/*.png → Resources/Map/Art/。
_fg 图只当遮罩用：品红处透明，其余像素直接取对应地图原图，保证前景和底图零色差。
用法：python3 Tools/import_map_art.py"""
import glob, os
from PIL import Image
import numpy as np

DST = 'Assets/LiuBeiRPG/Resources/Map/Art'
for src in glob.glob('ArtSrc/map/*.png'):
    img = Image.open(src).convert('RGB')
    if src.endswith('_fg.png'):
        a = np.asarray(img).astype(np.int32)
        m = np.minimum(a[..., 0], a[..., 2]) - a[..., 1]   # 品红程度；桃花粉约 40，底色约 250
        alpha = np.clip((150 - m) * 255 // 60, 0, 255)     # m≤90 不透明，m≥150 全透明，中间渐变
        base = np.asarray(Image.open(src.replace('_fg', '')).convert('RGB'))
        img = Image.fromarray(np.dstack([base, alpha]).astype(np.uint8))
    img.save(os.path.join(DST, os.path.basename(src)))
    print(src, '→', DST)
