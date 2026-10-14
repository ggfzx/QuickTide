#!/usr/bin/env python3
"""生成 QuickTidy 图标资源（无需第三方库）：
- assets/qt.ico          256x256 PNG-in-ICO（应用图标 + Win11 菜单图标）
- packaging/Assets/Logo.png  256x256 PNG（sparse package 清单 Logo）
图形：青色圆角方块 + 白色右向箭头（「移动/整理」隐喻）。
"""
import os
import struct
import zlib

SIZE = 256
RADIUS = 52
BG = (14, 154, 167, 255)        # 青色
BG_DARK = (11, 120, 130, 255)   # 底部渐变深色
FG = (255, 255, 255, 255)       # 白色箭头


def rounded_square_mask(x, y):
    """256x256 圆角方块掩码。"""
    r = RADIUS
    cx0, cy0 = r, r
    cx1, cy1 = SIZE - r, SIZE - r
    if cx0 <= x <= cx1 and cy0 <= y <= cy1:
        return True
    if x < cx0 and y < cy0:
        return (x - cx0) ** 2 + (y - cy0) ** 2 <= r * r
    if x > cx1 and y < cy0:
        return (x - cx1) ** 2 + (y - cy0) ** 2 <= r * r
    if x < cx0 and y > cy1:
        return (x - cx0) ** 2 + (y - cy1) ** 2 <= r * r
    if x > cx1 and y > cy1:
        return (x - cx1) ** 2 + (y - cy1) ** 2 <= r * r
    return True


def in_arrow(x, y):
    """白色右向箭头：矩形箭杆 + 三角箭头。"""
    # 箭杆
    if 62 <= x <= 138 and 108 <= y <= 148:
        return True
    # 箭头（三角）
    ax1, ay1 = 130, 74
    ax2, ay2 = 200, 128
    ax3, ay3 = 130, 182
    # 重心坐标法
    denom = (ay2 - ay3) * (ax1 - ax3) + (ax3 - ax2) * (ay1 - ay3)
    if denom == 0:
        return False
    w1 = ((ay2 - ay3) * (x - ax3) + (ax3 - ax2) * (y - ay3)) / denom
    w2 = ((ay3 - ay1) * (x - ax3) + (ax1 - ax3) * (y - ay3)) / denom
    w3 = 1 - w1 - w2
    return w1 >= 0 and w2 >= 0 and w3 >= 0


def render_png_bytes():
    raw = bytearray()
    for y in range(SIZE):
        raw.append(0)  # filter: None
        for x in range(SIZE):
            if not rounded_square_mask(x, y):
                raw += bytes((0, 0, 0, 0))
                continue
            if in_arrow(x, y):
                raw += bytes(FG)
            elif y > SIZE * 0.62:
                raw += bytes(BG_DARK)
            else:
                raw += bytes(BG)

    def chunk(tag, data):
        c = struct.pack(">I", len(data)) + tag + data
        c += struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)
        return c

    ihdr = struct.pack(">IIBBBBB", SIZE, SIZE, 8, 6, 0, 0, 0)  # 8bit RGBA
    png = b"\x89PNG\r\n\x1a\n"
    png += chunk(b"IHDR", ihdr)
    png += chunk(b"IDAT", zlib.compress(bytes(raw), 9))
    png += chunk(b"IEND", b"")
    return png


def render_ico_bytes(png):
    # ICONDIR
    ico = struct.pack("<HHH", 0, 1, 1)
    # ICONDIRENTRY（256 用 0 表示）
    ico += struct.pack("<BBBBHHII", 0, 0, 0, 0, 1, 32, len(png), 22)
    ico += png
    return ico


def main():
    base = os.path.dirname(os.path.abspath(__file__))
    root = os.path.dirname(base)
    png = render_png_bytes()

    os.makedirs(os.path.join(root, "assets"), exist_ok=True)
    with open(os.path.join(root, "assets", "qt.ico"), "wb") as f:
        f.write(render_ico_bytes(png))

    os.makedirs(os.path.join(root, "packaging", "Assets"), exist_ok=True)
    with open(os.path.join(root, "packaging", "Assets", "Logo.png"), "wb") as f:
        f.write(png)

    print("OK: assets/qt.ico, packaging/Assets/Logo.png")


if __name__ == "__main__":
    main()
