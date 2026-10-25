#!/bin/zsh
# 在 iPhone 模拟器里运行游戏。复制一份项目去打包，不影响正开着的 Unity 编辑器。
# 用法：zsh Tools/run_ios_sim.sh [模拟器机型，默认 "iPhone 16"]
set -e
cd "$(dirname "$0")/.."
ROOT=$PWD
SIM=${1:-iPhone 16}
UNITY=/Applications/Unity/Hub/Editor/6000.3.22f1/Unity.app/Contents/MacOS/Unity
COPY=$ROOT/Build/_simproj

echo "== 1/4 复制项目"
mkdir -p $COPY
rsync -a --delete Assets Packages ProjectSettings $COPY/
[ -d $COPY/Library ] || rsync -a Library $COPY/

echo "== 2/4 Unity 导出 Xcode 工程（第一次要切 iOS 平台，较慢）"
$UNITY -batchmode -nographics -quit -projectPath $COPY -buildTarget iOS \
  -executeMethod BuildPreview.IOSSimulator -logFile $ROOT/Build/ios_unity.log \
  || { tail -40 $ROOT/Build/ios_unity.log; exit 1; }

echo "== 3/4 Xcode 编译"
xcodebuild -project $COPY/Build/iOS/Unity-iPhone.xcodeproj -scheme Unity-iPhone -configuration Release \
  -sdk iphonesimulator -destination 'generic/platform=iOS Simulator' -derivedDataPath $ROOT/Build/_xcode \
  CODE_SIGNING_ALLOWED=NO -quiet > $ROOT/Build/ios_xcode.log 2>&1 \
  || { grep -E "error" $ROOT/Build/ios_xcode.log | head -30; exit 1; }
APP=$(find $ROOT/Build/_xcode/Build/Products -maxdepth 2 -name "*.app" | head -1)
BID=$(/usr/libexec/PlistBuddy -c "Print CFBundleIdentifier" "$APP/Info.plist")

echo "== 4/4 启动 $SIM 模拟器"
xcrun simctl boot "$SIM" 2>/dev/null || true
open -a Simulator
xcrun simctl install "$SIM" "$APP"
xcrun simctl launch "$SIM" "$BID"
echo "完成：$APP ($BID)"
