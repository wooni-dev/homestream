"""TV 모양 아이콘(tv.ico)을 표준 라이브러리만으로 생성한다.

Pillow 등 외부 의존성 없이 RGBA 픽셀 버퍼에 직접 그린 뒤 PNG → ICO 로 인코딩.
앱 테마색(보라 #8a8aff, 다크 배경)에 맞춘 단순한 TV 픽토그램.
"""

import struct
import zlib

# 앱 테마 색
ACCENT = (138, 138, 255, 255)   # #8a8aff 화면
BODY = (35, 35, 44, 255)        # #23232c 본체 테두리
SCREEN_BG = (14, 14, 18, 255)   # #0e0e12 화면 안쪽
STAND = (90, 90, 110, 255)      # 다리/안테나


def _blank(size):
    return [[(0, 0, 0, 0)] * size for _ in range(size)]


def _fill_rect(px, x0, y0, x1, y1, color):
    n = len(px)
    for y in range(max(0, y0), min(n, y1)):
        row = px[y]
        for x in range(max(0, x0), min(n, x1)):
            row[x] = color


def _line(px, x0, y0, x1, y1, color, thick):
    # 굵기를 가진 단순 브레젠험 선
    dx, dy = abs(x1 - x0), abs(y1 - y0)
    sx = 1 if x0 < x1 else -1
    sy = 1 if y0 < y1 else -1
    err = dx - dy
    n = len(px)
    while True:
        for ay in range(-thick, thick + 1):
            for ax in range(-thick, thick + 1):
                x, y = x0 + ax, y0 + ay
                if 0 <= x < n and 0 <= y < n:
                    px[y][x] = color
        if x0 == x1 and y0 == y1:
            break
        e2 = 2 * err
        if e2 > -dy:
            err -= dy
            x0 += sx
        if e2 < dx:
            err += dx
            y0 += sy


def draw_tv(size):
    px = _blank(size)
    u = size / 32.0  # 32 기준 단위

    def U(v):
        return int(round(v * u))

    # 안테나 (화면 위로 뻗는 두 선 + 꼭지)
    cx = size // 2
    _line(px, cx, U(9), U(9), U(2), STAND, max(1, U(0.6)))
    _line(px, cx, U(9), U(23), U(2), STAND, max(1, U(0.6)))

    # 본체(바깥 테두리)
    _fill_rect(px, U(3), U(9), U(29), U(26), BODY)
    # 화면 안쪽 배경
    _fill_rect(px, U(5), U(11), U(27), U(24), SCREEN_BG)
    # 화면(액센트) — 살짝 더 안쪽
    _fill_rect(px, U(6), U(12), U(23), U(23), ACCENT)
    # 스피커/버튼 칸 (화면 오른쪽 좁은 영역은 SCREEN_BG 로 남김)

    # 다리 두 개
    _fill_rect(px, U(7), U(26), U(10), U(29), STAND)
    _fill_rect(px, U(22), U(26), U(25), U(29), STAND)
    return px


def png_bytes(px):
    size = len(px)
    raw = bytearray()
    for row in px:
        raw.append(0)  # filter type 0
        for (r, g, b, a) in row:
            raw += bytes((r, g, b, a))
    comp = zlib.compress(bytes(raw), 9)

    def chunk(tag, data):
        c = tag + data
        return struct.pack(">I", len(data)) + c + struct.pack(">I", zlib.crc32(c) & 0xFFFFFFFF)

    ihdr = struct.pack(">IIBBBBB", size, size, 8, 6, 0, 0, 0)
    return (b"\x89PNG\r\n\x1a\n"
            + chunk(b"IHDR", ihdr)
            + chunk(b"IDAT", comp)
            + chunk(b"IEND", b""))


def write_ico(path, sizes=(256, 64, 48, 32, 16)):
    images = [(s, png_bytes(draw_tv(s))) for s in sizes]
    n = len(images)
    header = struct.pack("<HHH", 0, 1, n)
    offset = 6 + 16 * n
    entries = bytearray()
    blobs = bytearray()
    for s, data in images:
        w = 0 if s >= 256 else s
        entries += struct.pack("<BBBBHHII", w, w, 0, 0, 1, 32, len(data), offset)
        blobs += data
        offset += len(data)
    with open(path, "wb") as f:
        f.write(header + bytes(entries) + bytes(blobs))


if __name__ == "__main__":
    write_ico("tv.ico")
    print("tv.ico 생성 완료")
