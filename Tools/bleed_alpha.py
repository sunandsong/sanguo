"""透明像素的颜色往外扩（同 Unity 导入的 alphaIsTransparency），alpha 不动。
运行时下载的原图没经过 Unity 导入，不扩的话双线性/mipmap 会把透明区的黑色带到边上，人物描一圈黑边。
用法：python3 bleed_alpha.py <目录>   —— 原地改目录下所有 png（只该对上传用的副本跑）"""
import sys, pathlib
import numpy as np
from PIL import Image


def bleed(a):
    rgb = a[..., :3].astype(np.float32)
    mask = a[..., 3] >= 64   # 几乎看不见的边缘（常是绿幕残留）也重新填色
    if mask.all() or not mask.any():
        return a
    for _ in range(64):   # ponytail: 只扩 64 像素，再远的透明区填平均色；mipmap 层数再多也够用
        if mask.all():
            break
        s = np.zeros_like(rgb); n = np.zeros(mask.shape, np.float32)
        mp = np.pad(mask, 1); rp = np.pad(rgb * mask[..., None], ((1, 1), (1, 1), (0, 0)))
        h, w = mask.shape
        for dy in (0, 1, 2):
            for dx in (0, 1, 2):
                s += rp[dy:dy + h, dx:dx + w]; n += mp[dy:dy + h, dx:dx + w]
        grow = ~mask & (n > 0)
        rgb[grow] = s[grow] / n[grow, None]
        mask = mask | grow
    rgb[~mask] = rgb[mask].mean(0)
    out = a.copy(); out[..., :3] = np.clip(rgb + 0.5, 0, 255).astype(np.uint8)
    return out


if __name__ == '__main__':
    t = np.zeros((5, 5, 4), np.uint8); t[2, 2] = (200, 100, 50, 255)
    r = bleed(t)
    assert tuple(r[0, 0]) == (200, 100, 50, 0) and tuple(r[2, 2]) == (200, 100, 50, 255), r[0, 0]
    for f in pathlib.Path(sys.argv[1]).rglob('*.png'):
        im = Image.open(f)
        if im.mode != 'RGBA':
            continue   # 没透明通道的不用管
        Image.fromarray(bleed(np.asarray(im))).save(f, optimize=False)
