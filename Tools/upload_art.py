"""增量上传图片和背景音乐到云存储 liubei/（游戏运行时从这里下，见 Remote.cs）。
每个源文件记一个内容指纹（md5）在 Tools/art_uploaded.json，下次只处理指纹变了的和新加的：
  图片：扩色（bleed_alpha）→ 压缩（compress_art：不透明整图转 JPG、其余 pngquant）→ 上传
  背景音乐 bgm_*：转 128k mp3 → 上传
最后按记录重写 Resources/RemoteFiles.txt（云上全部文件清单，打进包里）。删掉的源文件只从清单去掉，云上不删。
用法：python3 upload_art.py            需要先 tcb login
      python3 upload_art.py --dry-run  只列出要传的，不传"""
import sys, json, hashlib, pathlib, shutil, subprocess, tempfile

ENV = 'renshengqingdan-d9fc03opf3bac6ba'
TOOLS = pathlib.Path(__file__).resolve().parent
RES = TOOLS.parent / 'Assets/LiuBeiRPG/Resources'
ENGINE = pathlib.Path.home() / 'Desktop/sanguoguge/Packages/com.sunandsong.puppet/Runtime/Resources'
RECORD = TOOLS / 'art_uploaded.json'
dry = '--dry-run' in sys.argv


def sources():
    """源文件：(相对路径, 绝对路径)。相对路径就是云上 liubei/ 下的路径（压缩前的扩展名）。"""
    for base, pat in [(RES, 'Map/**/*.png'), (RES, 'Map/**/*.jpg'), (ENGINE, 'Puppets/**/*.png'), (RES, 'Audio/bgm_*')]:
        for f in sorted(base.glob(pat)):
            if f.suffix != '.meta':
                yield f.relative_to(base).as_posix(), f


def md5(f):
    return hashlib.md5(f.read_bytes()).hexdigest()


def upload(local, remote):
    for _ in range(5):
        out = subprocess.run(['tcb', 'storage', 'upload', str(local), 'liubei/' + remote, '-e', ENV], capture_output=True, text=True).stdout
        if '✖' not in out:
            return True
    return False


record = json.loads(RECORD.read_text()) if RECORD.exists() else {}   # 相对路径 → [md5, 云上路径]
src = dict(sources())
changed = [rel for rel, f in src.items() if rel not in record or record[rel][0] != md5(f)]
print(f'共 {len(src)} 个源文件，要处理 {len(changed)} 个')
for rel in changed:
    print('  ', rel)
if dry or not changed:
    sys.exit(0)

stage = pathlib.Path(tempfile.mkdtemp())
try:
    imgs = [r for r in changed if not r.startswith('Audio/')]
    for rel in imgs:
        (stage / rel).parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(src[rel], stage / rel)
    if imgs:
        subprocess.run([sys.executable, str(TOOLS / 'bleed_alpha.py'), str(stage)], check=True)   # 透明区扩色，免得缩小出黑边
        subprocess.run([sys.executable, str(TOOLS / 'compress_art.py'), str(stage), str(stage / '_list.txt')], check=True)
    failed = []
    for rel in changed:
        if rel.startswith('Audio/'):
            remote = str(pathlib.PurePosixPath(rel).with_suffix('.mp3'))
            out = stage / remote
            out.parent.mkdir(parents=True, exist_ok=True)
            subprocess.run(['ffmpeg', '-v', 'error', '-y', '-i', str(src[rel]), '-codec:a', 'libmp3lame', '-b:a', '128k', '-ar', '44100', str(out)], check=True)
        else:
            png = stage / rel
            remote = rel if png.exists() else str(pathlib.PurePosixPath(rel).with_suffix('.jpg'))   # compress_art 可能转成了 jpg
        if upload(stage / remote, remote):
            record[rel] = [md5(src[rel]), remote]
            print('已上传', remote)
        else:
            failed.append(remote)
finally:
    shutil.rmtree(stage, ignore_errors=True)

for rel in [r for r in record if r not in src]:   # 源文件删了：清单里去掉
    del record[rel]
RECORD.write_text(json.dumps(record, ensure_ascii=False, indent=0, sort_keys=True))
files = sorted(v[1] for r, v in record.items() if not r.startswith('Audio/'))
(RES / 'RemoteFiles.txt').write_text('\n'.join(files) + '\n')
print(f'清单 {len(files)} 个文件' + (f'；失败 {len(failed)} 个，再跑一次会重传：{failed}' if failed else ''))
