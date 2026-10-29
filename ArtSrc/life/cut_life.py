"""把 ArtSrc/life/ 下的图集切成游戏用的单张图 → Assets/LiuBeiRPG/Resources/Map/Art/life/。

物件（树、房子……）：一件一张，裁掉空白，底边是脚底。
动画（小动物、篝火）：同一排的几帧拼成一张横条，每帧格子一样大、底边（或中心）对齐。
底图：缩到和原地图一样大（1536×1024）直接放。

用法：python3 ArtSrc/life/cut_life.py
"""
import sys, uuid, re, os
import numpy as np
from PIL import Image
from scipy import ndimage

sys.path.insert(0, '/Users/songzhang/Desktop/sanguoguge/Tools')
from cut_sheet import key_out

ROOT = os.path.dirname(os.path.abspath(__file__))
PROJ = os.path.abspath(os.path.join(ROOT, '..', '..'))
OUT = os.path.join(PROJ, 'Assets/LiuBeiRPG/Resources/Map/Art/life')
META = open(os.path.join(PROJ, 'Assets/LiuBeiRPG/Resources/Map/Art/battle_disc.png.meta')).read()

# 图集 → 每排的名字（从上到下、每排从左到右）。动画排写成 ('名字', 'bottom'|'center')
SHEETS = {
    'trees_green': [['tree_green_0', 'tree_green_1', 'tree_green_2', 'tree_green_3']],
    'trees_color': [['tree_peach_0', 'tree_peach_1', 'tree_peach_2'], ['tree_autumn_0', 'tree_autumn_1', 'tree_autumn_2']],
    'plants': [['bush_0', 'bush_1', 'bush_2', 'grass_0'], ['reed_0', 'reed_1', 'lotus_0', 'cattail_0']],
    'cloth': [['lantern', 'banner_blue', 'curtain_red', 'awning'], ['flag_big', 'flag_small']],
    'houses': [['house_liubei', 'house_tavern', 'house_inn', 'gate']],
    'camp': [['tent_big', 'tent_small', 'watchtower', 'palisade'], ['firepit', 'weapon_rack', 'crates']],
    'village': [['cottage_0', 'cottage_1', 'haystack', 'well'], ['bridge', 'fence', 'signpost']],
    'fish': [('fish_move', 'center', 4), ('fish_jump', 'center', 3)],
    'rabbit': [('rabbit_idle', 'bottom', 2), ('rabbit_move', 'bottom', 4)],
    'chicken': [('chicken_move', 'bottom', 4), ('chicken_idle', 'bottom', 2)],
    'sparrow': [('sparrow_idle', 'bottom', 2), ('sparrow_move', 'center', 4)],
    'duck': [('duck_idle', 'bottom', 2), ('duck_dive', 'bottom', 2)],
    'bugs': [('butterfly_idle', 'center', 2), ('butterfly2_idle', 'center', 2), ('dragonfly_idle', 'center', 2)],
}
BUG_ROWS = [4, 2]   # bugs.png 实际排法：上排两只蝴蝶各 2 帧，下排蜻蜓 2 帧

MAX_W = 520   # 单张最宽像素，够用就行


def save(img, name):
    os.makedirs(OUT, exist_ok=True)
    p = os.path.join(OUT, name + '.png')
    img.save(p)
    if not os.path.exists(p + '.meta'):
        open(p + '.meta', 'w').write(re.sub(r'guid: \w+', 'guid: ' + uuid.uuid4().hex, META, 1))


def objects(rgba, count):
    """按连通块找出 count 件东西：先膨胀把碎叶、须子并进主体，膨胀到正好 count 件为止。"""
    alpha = rgba[..., 3] > 24
    for r in range(2, 60, 2):
        grown = ndimage.binary_dilation(alpha, iterations=r)
        lab, n = ndimage.label(grown)
        sizes = ndimage.sum(grown, lab, range(1, n + 1))
        big = [i + 1 for i, s in enumerate(sizes) if s > 1500]
        if len(big) == count:
            out = []
            for b in big:
                m = (lab == b) & alpha
                ys, xs = np.nonzero(m)
                out.append(dict(mask=m, x0=xs.min(), x1=xs.max() + 1, y0=ys.min(), y1=ys.max() + 1,
                                cx=xs.mean(), cy=ys.mean()))
            return out
    raise SystemExit('切不出 %d 件' % count)


def rows_of(objs, counts):
    """按底边从上到下分排，每排按 x 排序。"""
    objs = sorted(objs, key=lambda o: o['y1'])
    rows, i = [], 0
    for c in counts:
        rows.append(sorted(objs[i:i + c], key=lambda o: o['cx']))
        i += c
    return rows


def crop(rgba, o, pad=6):
    a = rgba.copy()
    a[..., 3] = np.where(o['mask'], a[..., 3], 0)
    img = Image.fromarray(a).crop((o['x0'] - pad, o['y0'] - pad, o['x1'] + pad, o['y1'] + pad))
    return img


def fit(img, max_w):
    if img.width <= max_w:
        return img
    return img.resize((max_w, round(img.height * max_w / img.width)), Image.LANCZOS)


def strip(frames, align):
    """几帧拼成横条：格子取最大宽高，底边对齐或中心对齐。"""
    cw = max(f.width for f in frames)
    ch = max(f.height for f in frames)
    out = Image.new('RGBA', (cw * len(frames), ch), (0, 0, 0, 0))
    for i, f in enumerate(frames):
        x = i * cw + (cw - f.width) // 2
        y = ch - f.height if align == 'bottom' else (ch - f.height) // 2
        out.paste(f, (x, y))
    return out


def load_keyed(name):
    rgb = np.array(Image.open(os.path.join(ROOT, name + '.png')).convert('RGB')).astype(float)
    rgba, _ = key_out(rgb)
    return rgba


def cut_sheet(name, spec):
    rgba = load_keyed(name)
    anim = isinstance(spec[0], tuple)
    if name == 'bugs':
        counts = BUG_ROWS
    elif anim:
        counts = [s[2] for s in spec]
    else:
        counts = [len(r) for r in spec]
    rows = rows_of(objects(rgba, sum(counts)), counts)
    if name == 'bugs':   # 上排 4 个拆成两只蝴蝶
        rows = [rows[0][:2], rows[0][2:], rows[1]]
    if not anim:
        for names, objs in zip(spec, rows):
            for n, o in zip(names, objs):
                save(fit(crop(rgba, o), MAX_W), n)
                print(n)
        return
    for (n, align, _), objs in zip(spec, rows):
        frames = [crop(rgba, o, 4) for o in objs]
        s = strip(frames, align)
        # 不单独缩放：同一只动物的几排保持原图同一比例，游戏里按 idle 那排换算大小
        save(s, n)
        print(n, len(frames), s.size)


def cut_fire():
    """黑底火焰：亮度当透明度，颜色除以亮度还原。"""
    rgb = np.array(Image.open(os.path.join(ROOT, 'fire.png')).convert('RGB')).astype(float)
    a = rgb.max(-1)
    col = rgb / np.maximum(a[..., None], 1) * 255
    rgba = np.dstack([col, a]).astype(np.uint8)
    rows = rows_of(objects(rgba, 6), [6])
    frames = [crop(rgba, o, 4) for o in rows[0]]
    s = strip(frames, 'bottom')
    save(s, 'fire')
    print('fire', s.size)


def ground(name):
    img = Image.open(os.path.join(ROOT, name + '.png')).convert('RGB').resize((1536, 1024), Image.LANCZOS)
    p = os.path.join(PROJ, 'Assets/LiuBeiRPG/Resources/Map/Art', name + '.png')
    img.save(p)
    if not os.path.exists(p + '.meta'):
        open(p + '.meta', 'w').write(re.sub(r'guid: \w+', 'guid: ' + uuid.uuid4().hex, META, 1))
    print(name)


if __name__ == '__main__':
    only = sys.argv[1:]   # 只切某几张：python3 cut_life.py village
    for k, v in SHEETS.items():
        if not only or k in only:
            cut_sheet(k, v)
    if only:
        sys.exit()
    cut_fire()
    ground('zhuoxian_ground')
    ground('field_ground')
