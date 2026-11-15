#!/bin/zsh
# 打微信小游戏：把工程同步进 Build/_wxproj（那边装了微信转换插件、WebGL 设置，ProjectSettings 不动），
# 同步时剥掉 Resources 里的 png（运行时从云上下，见 Remote.cs、upload_art.sh），再命令行导出到 Build/wxgame。
set -e
P=$(cd "$(dirname $0)/.." && pwd)
W=$P/Build/_wxproj
ENGINE=~/Desktop/sanguoguge/Packages/com.sunandsong.puppet
NOPNG=(--exclude='/Resources/**.png' --exclude='/Resources/**.png.meta' --delete-excluded)
rsync -a --delete $NOPNG --exclude='/Resources/Audio/bgm_*' $P/Assets/LiuBeiRPG/ $W/Assets/LiuBeiRPG/   # 背景音乐也在云上（Remote.Audio）
rsync -a --delete $NOPNG $ENGINE/Runtime/ $W/Packages/com.sunandsong.puppet/Runtime/   # 引擎嵌进来，才能只剥这边的图
rsync -a --delete $ENGINE/ --exclude=/Runtime $W/Packages/com.sunandsong.puppet/
sed -i '' '/"com.sunandsong.puppet": "file:/d' $W/Packages/manifest.json
python3 $P/Tools/wx/subset_fonts.py $W   # 字体裁到游戏里用到的字（22MB → 1.5MB）
cp $P/Tools/wx/WxBuild.cs $W/Assets/Editor/
cp $P/Tools/wx/WxGlue.cs $W/Assets/
WX_CDN=${WX_CDN:-https://7265-renshengqingdan-d9fc03opf3bac6ba-1478597699.tcb.qcloud.la/liubei/wxgame} /Applications/Unity/Hub/Editor/6000.3.22f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath $W -executeMethod WxBuild.Build -logFile $P/Build/wx_unity.log
python3 $P/Tools/wx/patch_minigame.py $P   # 启动加载页：背景图 + 进度条颜色
# 资源包也传到云存储（DATA_CDN 指向这里）
cd $P/Build/wxgame/webgl && F=$(ls *.webgl.data.unityweb.bin.txt)
for i in 1 2 3 4 5 6; do tcb storage upload "$F" "liubei/wxgame/$F" -e renshengqingdan-d9fc03opf3bac6ba 2>&1 | grep -q "✖" || { echo "已上传 $F"; break; }; done
