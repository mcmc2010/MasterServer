#!/bin/bash

# 配置参数
INPUT_DIR="./data/protocols"
OUTPUT_DIR="./src/protocols/generated"

# 创建目录并清空旧文件
mkdir -p "${OUTPUT_DIR}" 
# && rm -f "${OUTPUT_DIR}/*.cs"

# 获取所有proto文件列表
PROTO_FILES=$(find "${INPUT_DIR}" -name '*.proto')

# 
#./tools/google/bin/protoc --proto_path="${INPUT_DIR}" --csharp_out="${OUTPUT_DIR}" "${OUTPUT_DIR}/chat.proto"

# 批量编译
FAILED=0
for proto in ${PROTO_FILES}; do
  echo "正在编译：$(basename ${proto})"
  if ! "./tools/google/bin/protoc" \
    --proto_path="${INPUT_DIR}" \
    --csharp_out="${OUTPUT_DIR}" \
    "${proto}"; then
    echo "❌ 文件编译失败：${proto}"
    FAILED=$((FAILED+1))
  fi
done

