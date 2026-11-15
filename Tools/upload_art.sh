#!/bin/zsh
# 把 Resources 里的 png（本工程 Map/ + 引擎 Puppets/）压缩后传到云存储 liubei/ 下，游戏运行时从这里下（见 Remote.cs）。
# 加了新图/改了图就跑一次。需要先 tcb login。
set -e
ENV=renshengqingdan-d9fc03opf3bac6ba
STAGE=$(mktemp -d)
TOOLS=$(cd "$(dirname $0)" && pwd)
cd "$TOOLS/../Assets/LiuBeiRPG/Resources" && find Map -name '*.png' -print0 | rsync -a --from0 --files-from=- . $STAGE/
cd ~/Desktop/sanguoguge/Packages/com.sunandsong.puppet/Runtime/Resources && find Puppets -name '*.png' -print0 | rsync -a --from0 --files-from=- . $STAGE/
python3 "$TOOLS/bleed_alpha.py" $STAGE   # 透明区扩色，免得运行时缩小出黑边
python3 "$TOOLS/compress_art.py" $STAGE "$TOOLS/../Assets/LiuBeiRPG/Resources/RemoteFiles.txt"   # 整图转 JPG、其余 pngquant；文件清单打进包里
echo "上传 $(find $STAGE -type f | wc -l) 张图（$(du -sh $STAGE | cut -f1)）…"
# 一张一张传：这边到上海网慢，并发或整目录传大图会被 COS 判"网速太慢"
cd $STAGE && find . -type f | sed 's|^\./||' | while read f; do
  for i in 1 2 3 4 5; do tcb storage upload "$f" "liubei/$f" -e $ENV 2>&1 | grep -q "✖" || break; [ $i = 5 ] && echo "失败 $f"; done
done
# 背景音乐：转 128k mp3（微信 iOS 不认 ogg）传到 liubei/Audio/
mkdir -p $STAGE/Audio && cd "$TOOLS/../Assets/LiuBeiRPG/Resources/Audio"
for f in bgm_*.(wav|ogg|mp3); do
  ffmpeg -v error -y -i $f -codec:a libmp3lame -b:a 128k -ar 44100 $STAGE/Audio/${f%.*}.mp3
  for i in 1 2 3 4 5; do tcb storage upload $STAGE/Audio/${f%.*}.mp3 liubei/Audio/${f%.*}.mp3 -e $ENV 2>&1 | grep -q "✖" || break; done
done
rm -rf $STAGE
