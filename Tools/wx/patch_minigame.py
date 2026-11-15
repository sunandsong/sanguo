"""导出后改微信小游戏的启动加载页（转换插件每次导出都会重新生成 game.js，所以由 build_wx.sh 每次跑）：
  背景图 = ArtSrc/loading_bg.png 天空处叠标题 logo → minigame/images/background.jpg
  进度条暖金色、文字白色、隐藏 Unity 标志、首页两张图预下载
用法：python3 patch_minigame.py <工程根目录>"""
import sys, re, pathlib
from PIL import Image

root = pathlib.Path(sys.argv[1])
mg = root / 'Build/wxgame/minigame'
bg = Image.open(root / 'ArtSrc/loading_bg.png').convert('RGB')
logo = Image.open(root / 'Assets/LiuBeiRPG/Resources/Map/Art/title_logo.png').convert('RGBA')
w = int(bg.width * 0.66)
logo = logo.resize((w, w * logo.height // logo.width), Image.LANCZOS)
bg.paste(logo, ((bg.width - w) // 2, int(bg.height * 0.12)), logo)   # 顶部约 11% 是微信胶囊按钮和状态栏，logo 放它下面
bg.save(mg / 'images/background.jpg', quality=85)   # 算首包体积，别用 png

js = (mg / 'game.js').read_text()
js = js.replace("backgroundColor: '#07C160'", "backgroundColor: '#D9A441'")
js = re.sub(r"(iconConfig: \{\s*visible: )true", r"\1false", js)
# 预下载：wasm 编译的同时先把首页要的图下进微信缓存（Remote 之后请求同一地址直接命中缓存）
base = 'https://7265-renshengqingdan-d9fc03opf3bac6ba-1478597699.tcb.qcloud.la/liubei/Map/Art/'
pre = ''.join(f"\n        '{base}{f}'," for f in ['title_bg.jpg', 'title_logo.png'])
js = re.sub(r"(preloadDataList: \[)", lambda m: m.group(1) + pre, js, count=1) if base not in js else js
assert "#D9A441" in js and re.search(r"iconConfig: \{\s*visible: false", js), '转换插件的 game.js 格式变了，改这里'
(mg / 'game.js').write_text(js)
# 开发版每次启动弹的「优化建议」框关掉（只在开发版 / 体验版检测，线上本来就不弹）；想看时改回 true
ns = mg / 'unity-namespace.js'
t = ns.read_text()
t = re.sub(r"showSuggestModal: true", "showSuggestModal: false", t)
assert "showSuggestModal: false" in t, 'unity-namespace.js 格式变了，改这里'
ns.write_text(t)
print('加载页已替换')
