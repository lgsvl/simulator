curl -LO https://ffmpeg.org/releases/ffmpeg-4.2.tar.bz2
tar xf ffmpeg-4.2.tar.bz2
cd ffmpeg-4.2
mkdir build && cd build

# --arch=x86                         \
# --target-os=mingw32                \
# --cross-prefix=x86_64-w64-mingw32- \

CFLAGS="-mtune=skylake" \
LDFLAGS="-Wl,-Bstatic,--whole-archive -lwinpthread -Wl,--no-whole-archive" \
../configure                         \
  --enable-lto                       \
  --enable-static                    \
  --disable-shared                   \
  --disable-debug                    \
  --disable-doc                      \
  --disable-ffplay                   \
  --disable-ffprobe                  \
  --disable-everything               \
  --disable-swscale                  \
  --disable-schannel                 \
  --disable-d3d11va                  \
  --disable-dxva2                    \
  --enable-indev=lavfi               \
  --enable-parser=h264               \
  --enable-demuxer=h264              \
  --enable-decoder=h264              \
  --enable-decoder=pcm_u8            \
  --enable-encoder=aac               \
  --enable-muxer=flv                 \
  --enable-muxer=mp4                 \
  --enable-protocol=file             \
  --enable-protocol=pipe             \
  --enable-protocol=rtmp             \
  --enable-filter=anullsrc           \
  --enable-filter=aresample          \
  --enable-bsf=extract_extradata     \
  \
  --disable-bzlib \
  --disable-iconv \
  --disable-zlib  \
  --disable-cuda-llvm
