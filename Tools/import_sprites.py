"""品红底素材 → 透明图入库。
物品图标：ArtSrc/items/sheetN.png 按格子切开 → Resources/Map/Art/item_<Id>.png（256 方图，居中留边）
地图物件：ArtSrc/life/<名>.png → Resources/Map/Art/life/<名>.png（贴边裁切，最长边不超过 1000）
用法：python3 Tools/import_sprites.py"""
import os
import numpy as np
from PIL import Image

ART = 'Assets/LiuBeiRPG/Resources/Map/Art'
# 图标表：文件 → (列, 行, 按从左到右、从上到下排的 Id)
SHEETS = {
    'sheet8.png': (2, 2, ['jinchuangyao', 'zhixuesan', 'qingjiu', 'pojia']),
    'sheet9.png': (2, 2, ['sunzibingfa', 'sanlue', 'suozijia', 'yulinjia']),
    'sheet10.png': (2, 1, ['shoumiankai', 'baguadaopao']),
    'sheet11.png': (2, 2, ['baima', 'junma', 'chitu', 'dilu']),
}
LIFE = ['pass_gate', 'tent_command', 'house_hut', 'bamboo_0', 'bamboo_1']

def key(img):
    a = np.asarray(img.convert('RGB')).astype(np.int32)
    m = np.minimum(a[..., 0], a[..., 2]) - a[..., 1]           # 品红程度
    alpha = np.clip((150 - m) * 255 // 60, 0, 255)
    spill = np.clip(m, 0, None) * (255 - alpha) // 255         # 半透明边上去掉品红
    a[..., 0] -= spill; a[..., 2] -= spill
    return Image.fromarray(np.dstack([np.clip(a, 0, 255), alpha]).astype(np.uint8))

def trim(im, pad=4):
    box = Image.fromarray((np.asarray(im)[..., 3] > 40).astype(np.uint8) * 255).getbbox()
    im = im.crop(box)
    out = Image.new('RGBA', (im.width + pad * 2, im.height + pad * 2))
    out.paste(im, (pad, pad))
    return out

for f, (cols, rows, ids) in SHEETS.items():
    src = 'ArtSrc/items/' + f
    if not os.path.exists(src): continue
    im = key(Image.open(src))
    cw, ch = im.width // cols, im.height // rows
    for i, iid in enumerate(ids):
        c = trim(im.crop(((i % cols) * cw, (i // cols) * ch, (i % cols + 1) * cw, (i // cols + 1) * ch)), 0)
        s = 240 / max(c.size)
        c = c.resize((max(1, round(c.width * s)), max(1, round(c.height * s))), Image.LANCZOS)
        out = Image.new('RGBA', (256, 256)); out.paste(c, ((256 - c.width) // 2, (256 - c.height) // 2))
        out.save(f'{ART}/item_{iid}.png'); print(src, '→', iid)

for n in LIFE:
    src = f'ArtSrc/life/{n}.png'
    if not os.path.exists(src): continue
    im = trim(key(Image.open(src)))
    if max(im.size) > 1000:
        s = 1000 / max(im.size); im = im.resize((round(im.width * s), round(im.height * s)), Image.LANCZOS)
    im.save(f'{ART}/life/{n}.png'); print(src, '→', im.size)
