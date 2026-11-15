#!/bin/zsh
# 增量上传图片和背景音乐到云存储（只传改过的和新加的），逻辑见 upload_art.py。需要先 tcb login。
exec python3 "$(dirname $0)/upload_art.py" "$@"
