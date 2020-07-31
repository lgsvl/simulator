FFMPEG_VERSION="4.1"

curl -fsSLO https://ffmpeg.org/releases/ffmpeg-${FFMPEG_VERSION}.tar.bz2
tar -xjf ffmpeg-${FFMPEG_VERSION}.tar.bz2
cd ffmpeg-${FFMPEG_VERSION}
mkdir build && cd build

../configure                       \
    --disable-shared               \
    --disable-debug                \
    --disable-doc                  \
    --disable-ffplay               \
    --disable-ffprobe              \
    --disable-everything           \
    --disable-schannel             \
    --disable-d3d11va              \
    --disable-dxva2                \
    --disable-vaapi                \
    --disable-bzlib                \
    --disable-iconv                \
    --disable-zlib                 \
    --enable-nvenc                 \
    --enable-encoder=h264_nvenc    \
    --enable-parser=h264           \
    --enable-demuxer=h264          \
    --enable-decoder=h264          \
    --enable-demuxer=rawvideo      \
    --enable-decoder=rawvideo      \
    --enable-muxer=mp4             \
    --enable-protocol=file         \
    --enable-protocol=pipe         \
    --enable-filter=vflip          \
    --enable-filter=scale
