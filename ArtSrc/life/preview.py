"""按 Resources/Map/Decor/{地图}.txt 把底图 + 物件拼成一张静态预览，摆位置时对照用（游戏里是同一份数据）。

用法：python3 ArtSrc/life/preview.py zhuoxian_ground 输出.png [--grid]
"""
import sys, os
from PIL import Image, ImageDraw, ImageFilter

PROJ = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..'))
ART = os.path.join(PROJ, 'Assets/LiuBeiRPG/Resources/Map')
MARGIN = 0.5   # 原画左右各多半格；一格 = 1536/21 像素（和原来一屏的大地图一样）
TP = 1536 / 21


def parse(path):
    items = []
    for line in open(path, encoding='utf-8'):
        line = line.split('#')[0].split()
        if len(line) < 4:
            continue
        d = dict(sprite=line[0], x=float(line[1]), y=float(line[2]), w=float(line[3]), motion='none', frames=1, h=0, lift=0, shadow=None, ground=False)
        for tok in line[4:]:
            if tok == 'ground':
                d['ground'] = True
            elif '=' in tok:
                k, v = tok.split('=')
                d[k] = float(v) if k != 'motion' else v
            else:
                d['motion'] = tok
        if d['shadow'] is None:
            d['shadow'] = 0.7 if d['motion'] == 'plant' else 0
        items.append(d)
    return items


def main():
    name, out = sys.argv[1], sys.argv[2]
    ground = Image.open(os.path.join(ART, 'Art', name + '.png')).convert('RGBA')
    tp = TP
    COLS = round(ground.width / tp - 2 * MARGIN)
    canvas = ground.copy()
    shadow = Image.new('RGBA', canvas.size, (0, 0, 0, 0))
    sd = ImageDraw.Draw(shadow)
    items = sorted(parse(os.path.join(ART, 'Decor', name + '.txt')), key=lambda d: (not d['ground'], d['y']))   # 贴地的先画
    for d in items:
        if d['shadow']:
            sw = d['w'] * d['shadow'] * tp
            cx, cy = (d['x'] + MARGIN) * tp, d['y'] * tp
            sd.ellipse([cx - sw / 2, cy - sw * 0.12, cx + sw / 2, cy + sw * 0.12], fill=(0, 0, 0, 90))
    canvas.alpha_composite(shadow.filter(ImageFilter.GaussianBlur(4)))
    for d in items:
        p = d['sprite'] if '/' in d['sprite'] else 'Art/life/' + d['sprite']
        im = Image.open(os.path.join(ART, p + '.png')).convert('RGBA')
        f = int(d['frames'])
        if f > 1:
            im = im.crop((0, 0, im.width // f, im.height))
        w = d['w'] * tp
        h = d['h'] * tp if d['h'] else w * im.height / im.width
        im = im.resize((max(1, round(w)), max(1, round(h))), Image.LANCZOS)
        canvas.alpha_composite(im, (round((d['x'] + MARGIN) * tp - w / 2), round((d['y'] - d['lift']) * tp - h)))
    if '--grid' in sys.argv:
        # 过不去的格子（按 Chapter1.cs 里这张图的 Rows）蒙一层红
        import re
        src = open(os.path.join(PROJ, 'Assets/LiuBeiRPG/Scripts/World/Chapter1.cs'), encoding='utf-8').read()
        blk = src[src.index('Art = "%s"' % name):]
        blk = blk[blk.index('Rows = new[]'):blk.index('}')]
        rows_txt = re.findall(r'"([^"]+)"', blk)
        red = Image.new('RGBA', canvas.size, (0, 0, 0, 0))
        rd = ImageDraw.Draw(red)
        for y, row in enumerate(rows_txt):
            for x, ch in enumerate(row):
                col = {'w': (60, 140, 255, 70), 'g': (255, 255, 0, 80), 't': (255, 255, 0, 80), '=': None, '.': None, ',': None, 'f': None}.get(ch, (255, 0, 0, 70))
                if col:
                    rd.rectangle([(x + MARGIN) * tp, y * tp, (x + 1 + MARGIN) * tp, (y + 1) * tp], fill=col)
        canvas.alpha_composite(red)
        g = ImageDraw.Draw(canvas)
        rows = round(ground.height / tp)
        for x in range(COLS + 1):
            X = (x + MARGIN) * tp
            g.line([(X, 0), (X, ground.height)], fill=(255, 255, 255, 90))
        for y in range(rows + 1):
            g.line([(0, y * tp), (ground.width, y * tp)], fill=(255, 255, 255, 90))
        for y in range(rows):
            for x in range(COLS):
                g.text(((x + MARGIN) * tp + 2, y * tp + 2), '%d,%d' % (x, y), fill=(255, 255, 255, 200))
    canvas.convert('RGB').save(out)


if __name__ == '__main__':
    main()
