"""上传前压图（只对上传用的副本跑，原图不动）：
  Map/Art 下完全不透明的手绘整图 → JPG（质量 90），删掉 png，Remote.cs 按清单（云上全部文件，带扩展名）知道请求 .jpg 还是 .png
  其余 png → pngquant 有损调色板（质量够不上 80 或反而变大就保持原样）
像素图块（Map/ 下非 Art）不转 JPG：JPG 的块状噪点在点采样下很扎眼。
用法：python3 compress_art.py <目录> <清单输出路径>"""
import sys, pathlib, subprocess
import numpy as np
from PIL import Image

root, out = pathlib.Path(sys.argv[1]), pathlib.Path(sys.argv[2])
jpgs, pngs = [], []
for f in sorted(root.rglob('*.png')):
    rel = f.relative_to(root).with_suffix('').as_posix()
    im = Image.open(f)
    if rel.startswith('Map/Art/') and (im.mode == 'RGB' or np.asarray(im.convert('RGBA'))[..., 3].min() == 255):
        im.convert('RGB').save(f.with_suffix('.jpg'), 'JPEG', quality=90)
        f.unlink()
        jpgs.append(rel)
    else:
        pngs.append(str(f))
for i in range(0, len(pngs), 100):
    # 退出码 98/99 = 压了反而大 / 质量不够，原文件保留，不算错
    subprocess.run(['pngquant', '--quality=80-98', '--speed', '1', '--force', '--skip-if-larger', '--ext', '.png'] + pngs[i:i + 100])
out.write_text('\n'.join(sorted(f.relative_to(root).as_posix() for f in root.rglob('*') if f.is_file())) + '\n')   # 云上全部文件清单
print(len(jpgs), '张转 JPG，', len(pngs), '张 pngquant')
