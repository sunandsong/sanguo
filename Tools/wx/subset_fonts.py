"""微信包瘦身：把中文字体裁到游戏里实际出现的字（只改 Build/_wxproj 那份拷贝，本地原字体不动）。
游戏里显示的字全写在代码和 Resources 的文本里，所以从这些文件收集字符；再加全部 ASCII 和常用中文标点兜底。
加了新台词不用管：每次 build_wx.sh 都重新收集、重新裁。
用法：python3 subset_fonts.py <微信工程目录>"""
import sys, pathlib
from fontTools import subset

root = pathlib.Path(sys.argv[1]) / 'Assets/LiuBeiRPG'
chars = set(chr(c) for c in range(0x20, 0x7F))
chars |= set('，。、；：？！…—～·“”‘’（）《》〈〉【】「」『』〔〕＋－×÷＝％＃＠＆＊／０１２３４５６７８９　')
for f in list(root.rglob('*.cs')) + list(root.rglob('*.txt')):
    chars |= set(f.read_text(encoding='utf-8', errors='ignore'))
chars = {c for c in chars if c.isprintable() or c == '　'}

for rel in ['Resources/ChineseFont.ttf', 'Resources/Fonts/BrushFont.ttf']:
    path = root / rel
    before = path.stat().st_size
    opts = subset.Options()
    opts.layout_features = ['*']
    opts.notdef_outline = True
    opts.name_IDs = ['*']
    font = subset.load_font(str(path), opts)
    s = subset.Subsetter(opts)
    s.populate(text=''.join(chars))
    s.subset(font)
    subset.save_font(font, str(path), opts)
    print(f'{rel}: {before // 1024} KB -> {path.stat().st_size // 1024} KB（{len(chars)} 个字）')
