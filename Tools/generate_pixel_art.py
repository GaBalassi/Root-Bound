"""Generatore deterministico di sprite pixel art originali per ROOTBOUND (Foresta Antica)."""

import math
import os
import random
import struct
import zlib

ROOT_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ART_DIR = os.path.join(ROOT_DIR, "Assets", "Art")


def write_png(path, width, height, pixels):
    """Scrive un PNG RGBA 32-bit con pixel (top-to-bottom) in formato (r, g, b, a)."""
    os.makedirs(os.path.dirname(path), exist_ok=True)
    raw = bytearray()
    for y in range(height):
        raw.append(0)  # filter type 0
        row_offset = y * width
        for x in range(width):
            r, g, b, a = pixels[row_offset + x]
            raw.extend((r & 255, g & 255, b & 255, a & 255))

    def chunk(tag, data):
        return (
            struct.pack("!I", len(data))
            + tag
            + data
            + struct.pack("!I", zlib.crc32(tag + data) & 0xFFFFFFFF)
        )

    ihdr = struct.pack("!IIBBBBB", width, height, 8, 6, 0, 0, 0)
    idat = zlib.compress(bytes(raw), 9)
    png_bytes = b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", ihdr) + chunk(b"IDAT", idat) + chunk(b"IEND", b"")
    with open(path, "wb") as f:
        f.write(png_bytes)


class Canvas:
    def __init__(self, w, h, fill=(0, 0, 0, 0)):
        self.w = w
        self.h = h
        self.px = [fill] * (w * h)

    def set(self, x, y, col):
        xi = int(round(x))
        yi = int(round(y))
        if 0 <= xi < self.w and 0 <= yi < self.h:
            if len(col) == 3:
                col = (col[0], col[1], col[2], 255)
            sa = col[3]
            if sa <= 0:
                return
            idx = yi * self.w + xi
            if sa >= 255 or self.px[idx][3] == 0:
                self.px[idx] = col
            else:
                dr, dg, db, da = self.px[idx]
                a = sa / 255.0
                ia = 1.0 - a
                nr = int(col[0] * a + dr * ia)
                ng = int(col[1] * a + dg * ia)
                nb = int(col[2] * a + db * ia)
                na = min(255, int(sa + da * ia))
                self.px[idx] = (nr, ng, nb, na)

    def get(self, x, y):
        if 0 <= x < self.w and 0 <= y < self.h:
            return self.px[y * self.w + x]
        return (0, 0, 0, 0)

    def rect(self, x, y, w, h, col):
        for yy in range(int(y), int(y + h)):
            for xx in range(int(x), int(x + w)):
                self.set(xx, yy, col)

    def ellipse(self, cx, cy, rx, ry, col):
        for yy in range(int(cy - ry - 1), int(cy + ry + 2)):
            for xx in range(int(cx - rx - 1), int(cx + rx + 2)):
                nx = (xx - cx) / max(0.5, rx)
                ny = (yy - cy) / max(0.5, ry)
                if nx * nx + ny * ny <= 1.0:
                    self.set(xx, yy, col)

    def line(self, x0, y0, x1, y1, col):
        steps = max(1, int(max(abs(x1 - x0), abs(y1 - y0)) * 2))
        for i in range(steps + 1):
            t = i / steps
            self.set(x0 + (x1 - x0) * t, y0 + (y1 - y0) * t, col)

    def outline(self, outline_col=(20, 18, 28, 255)):
        copy = list(self.px)
        for y in range(self.h):
            for x in range(self.w):
                if copy[y * self.w + x][3] == 0:
                    for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 1)):
                        nx, ny = x + dx, y + dy
                        if 0 <= nx < self.w and 0 <= ny < self.h and copy[ny * self.w + nx][3] > 20:
                            self.px[y * self.w + x] = outline_col
                            break

    def save(self, rel_path):
        write_png(os.path.join(ART_DIR, rel_path), self.w, self.h, self.px)


def hex_col(h, a=255):
    h = h.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)


def lerp_col(c1, c2, t):
    t = max(0.0, min(1.0, t))
    return (
        int(c1[0] + (c2[0] - c1[0]) * t),
        int(c1[1] + (c2[1] - c1[1]) * t),
        int(c1[2] + (c2[2] - c1[2]) * t),
        int(c1[3] + (c2[3] - c1[3]) * t),
    )


# Palette comune
OUTLINE = hex_col("#17141F")
LEAF_HI = hex_col("#76EE7C")
LEAF_MID = hex_col("#3AC156")
LEAF_DARK = hex_col("#1F7A38")
SCARF_HI = hex_col("#FFD166")
SCARF_MID = hex_col("#F08A24")
BARK_HI = hex_col("#A8704B")
BARK_MID = hex_col("#754830")
BARK_DARK = hex_col("#46281A")
EYE_GOLD = hex_col("#FFF394")
CORRUPT_HI = hex_col("#FF6BC9")
CORRUPT_MID = hex_col("#D12A82")
CORRUPT_DARK = hex_col("#6E154E")
ROOT_GLOW = hex_col("#54FFAB")
ROOT_MID = hex_col("#24B878")


def draw_player_frame(pose, frame):
    c = Canvas(24, 24)
    bob = 0
    squash_y = 0
    leg_l = 0
    leg_r = 0
    scarf_dy = 0
    lean = 0
    arm_pose = "rest"

    if pose == "idle":
        bob = 1 if frame in (1, 2) else 0
        scarf_dy = 1 if frame in (2, 3) else 0
    elif pose == "run":
        lean = 1
        bob = -1 if frame in (1, 4) else 0
        cycle = [(-2, 2), (-1, 1), (1, -1), (2, -2), (1, -1), (-1, 1)][frame % 6]
        leg_l, leg_r = cycle
        scarf_dy = -1 if frame % 2 == 0 else 1
    elif pose == "jump":
        squash_y = -1
        leg_l, leg_r = -1, 1
        scarf_dy = 2
    elif pose == "fall":
        squash_y = 1
        scarf_dy = -2
    elif pose == "dash":
        lean = 3
        arm_pose = "dash"
        scarf_dy = -1
    elif pose == "attack1":
        lean = 1
        arm_pose = "slash_down" if frame == 0 else "slash_follow"
    elif pose == "attack2":
        lean = 2
        arm_pose = "slash_up" if frame == 0 else "slash_follow"
    elif pose == "attack3":
        lean = 2
        arm_pose = "heavy"
    elif pose == "charge":
        bob = 1
        arm_pose = "charge"
    elif pose == "root":
        lean = 1
        arm_pose = "root"
    elif pose == "hurt":
        lean = -2
        arm_pose = "hurt"

    cx = 11 + lean
    foot_y = 22

    # Stivali / gambe
    c.rect(cx - 3 + leg_l, foot_y - 3, 2, 3, BARK_DARK)
    c.rect(cx + 1 + leg_r, foot_y - 3, 2, 3, BARK_MID)

    # Tunica di corteccia
    body_top = 13 + bob + squash_y
    c.rect(cx - 4, body_top, 8, 7 - squash_y, BARK_MID)
    c.rect(cx - 3, body_top + 1, 5, 4, BARK_HI)

    # Sciarpa ambrata svolazzante dietro
    c.rect(cx - 6, body_top + 1 + scarf_dy, 3, 2, SCARF_MID)
    c.rect(cx - 8, body_top + 2 + scarf_dy, 2, 2, SCARF_HI)
    if pose in ("run", "dash"):
        c.rect(cx - 10, body_top + 2 + scarf_dy, 2, 1, SCARF_MID)

    # Mantello e cappuccio di foglie
    hood_y = 6 + bob + squash_y
    c.ellipse(cx, hood_y + 4, 5.2, 4.5, LEAF_MID)
    c.ellipse(cx - 0.5, hood_y + 3, 4.2, 3.2, LEAF_HI)
    c.rect(cx - 4, hood_y + 6, 8, 3, LEAF_DARK)
    # Foglie punta del cappuccio (corna/ciuffo fogliare)
    c.line(cx - 1, hood_y, cx - 4, hood_y - 2, LEAF_MID)
    c.set(cx - 4, hood_y - 2, LEAF_HI)
    c.line(cx + 1, hood_y, cx + 3, hood_y - 2, LEAF_HI)

    # Viso ombra + occhi dorati luminosi rivolti a destra
    c.rect(cx - 1, hood_y + 3, 5, 4, OUTLINE)
    c.set(cx + 1, hood_y + 4, EYE_GOLD)
    c.set(cx + 3, hood_y + 4, EYE_GOLD)

    # Braccio e lama di rovo
    if arm_pose == "rest":
        c.rect(cx + 3, body_top + 2, 3, 2, LEAF_MID)
    elif arm_pose == "dash":
        c.rect(cx + 4, body_top + 1, 5, 2, LEAF_HI)
        c.line(cx + 6, body_top + 1, cx + 10, body_top + 1, ROOT_GLOW)
    elif arm_pose == "slash_down":
        c.line(cx + 3, body_top - 2, cx + 10, body_top + 3, LEAF_HI)
        c.line(cx + 4, body_top - 1, cx + 10, body_top + 4, ROOT_GLOW)
    elif arm_pose == "slash_up":
        c.line(cx + 3, body_top + 4, cx + 10, body_top - 2, LEAF_HI)
        c.line(cx + 4, body_top + 3, cx + 10, body_top - 3, ROOT_GLOW)
    elif arm_pose == "slash_follow":
        c.line(cx + 4, body_top + 2, cx + 9, body_top + 2, ROOT_MID)
    elif arm_pose == "heavy":
        c.rect(cx + 3, body_top + 1, 7, 3, SCARF_HI)
        c.rect(cx + 8, body_top, 3, 5, ROOT_GLOW)
    elif arm_pose == "charge":
        c.rect(cx - 6, body_top + 1, 3, 3, SCARF_HI)
        c.set(cx - 6, body_top, EYE_GOLD)
    elif arm_pose == "root":
        c.line(cx + 3, body_top + 2, cx + 10, body_top + 1, ROOT_GLOW)
        c.set(cx + 11, body_top + 1, EYE_GOLD)
    elif arm_pose == "hurt":
        for i in range(len(c.px)):
            if c.px[i][3] > 0:
                c.px[i] = lerp_col(c.px[i], (255, 110, 110, 255), 0.45)

    c.outline(OUTLINE)
    return c


def generate_player():
    for i in range(4):
        draw_player_frame("idle", i).save(f"Player/idle_{i}.png")
    for i in range(6):
        draw_player_frame("run", i).save(f"Player/run_{i}.png")
    for i in range(2):
        draw_player_frame("jump", i).save(f"Player/jump_{i}.png")
        draw_player_frame("fall", i).save(f"Player/fall_{i}.png")
        draw_player_frame("dash", i).save(f"Player/dash_{i}.png")
        draw_player_frame("attack1", i).save(f"Player/attack1_{i}.png")
        draw_player_frame("attack2", i).save(f"Player/attack2_{i}.png")
        draw_player_frame("attack3", i).save(f"Player/attack3_{i}.png")
        draw_player_frame("charge", i).save(f"Player/charge_{i}.png")
        draw_player_frame("root", i).save(f"Player/root_{i}.png")
    draw_player_frame("hurt", 0).save("Player/hurt_0.png")


def generate_enemies():
    # 1. Slime di Muschio (20x16)
    for i, (rx, ry, dy, telegraph) in enumerate([
        (7.2, 5.2, 0, False),
        (7.8, 4.6, 1, False),
        (6.4, 6.2, -1, False),
        (7.5, 5.0, 0, False),
        (8.4, 3.6, 2, True),
    ]):
        c = Canvas(20, 16)
        cy = 10 + dy
        c.ellipse(10, cy, rx, ry, LEAF_DARK if not telegraph else CORRUPT_DARK)
        c.ellipse(10, cy - 0.5, rx - 1.4, ry - 1.2, LEAF_MID if not telegraph else CORRUPT_MID)
        c.ellipse(8.5, cy - 1.5, rx * 0.45, ry * 0.45, LEAF_HI if not telegraph else CORRUPT_HI)
        # Nucleo corrotto brillante interno
        c.ellipse(10, cy, 2.2, 2.0, CORRUPT_MID)
        c.set(10, cy, CORRUPT_HI)
        # Occhi
        c.set(12, cy - 1, EYE_GOLD)
        c.set(14, cy - 1, EYE_GOLD)
        c.outline(OUTLINE)
        name = "slime_telegraph.png" if telegraph else f"slime_{i}.png"
        c.save(f"Enemies/{name}")

    # 2. Lupo Corrotto (28x20)
    wolf_dark = hex_col("#2C263B")
    wolf_mid = hex_col("#463C5C")
    for i in range(5):
        telegraph = (i == 4)
        c = Canvas(28, 20)
        dy = 2 if telegraph else (1 if i in (1, 3) else 0)
        leg_off = (0, 0) if telegraph else [(-2, 2), (0, 0), (2, -2), (1, -1)][i]
        # Corpo
        c.ellipse(13, 12 + dy, 7.5, 4.2, wolf_mid)
        c.rect(7, 9 + dy, 12, 5, wolf_dark)
        # Criniera acuminata viola
        for sx in (8, 11, 14, 17):
            c.line(sx, 8 + dy, sx - 2, 5 + dy, CORRUPT_MID if not telegraph else CORRUPT_HI)
            c.set(sx - 2, 5 + dy, CORRUPT_HI)
        # Testa e muso rivolto a destra
        c.rect(17, 7 + dy, 7, 5, wolf_mid)
        c.rect(22, 9 + dy, 4, 3, wolf_dark)
        c.line(18, 7 + dy, 17, 4 + dy, wolf_mid)
        c.set(22, 8 + dy, CORRUPT_HI if telegraph else EYE_GOLD)
        c.set(24, 11 + dy, hex_col("#FFF5D6"))
        # Coda
        c.line(6, 11 + dy, 2, 8 + dy, CORRUPT_MID)
        # Zampe
        c.rect(8 + leg_off[0], 14 + dy, 2, 5 - dy, wolf_dark)
        c.rect(18 + leg_off[1], 14 + dy, 2, 5 - dy, wolf_mid)
        c.outline(OUTLINE)
        c.save("Enemies/wolf_telegraph.png" if telegraph else f"Enemies/wolf_{i}.png")

    # 3. Pianta Carnivora (22x26)
    for i in range(5):
        spit = (i == 4)
        c = Canvas(22, 26)
        sway = -1 if i == 1 else (1 if i == 3 else 0)
        # Radici base
        c.rect(7, 22, 8, 3, BARK_DARK)
        c.rect(5, 23, 12, 2, LEAF_DARK)
        # Stelo sinuoso
        c.line(11, 22, 11 + sway, 13, LEAF_MID)
        c.line(12, 22, 12 + sway, 13, LEAF_DARK)
        c.line(10 + sway, 17, 6 + sway, 15, LEAF_HI)
        c.line(12 + sway, 18, 16 + sway, 16, LEAF_MID)
        # Bocca bulbosa
        hx = 12 + sway
        hy = 9
        jaw_open = 3 if spit else (2 if i == 2 else 1)
        c.ellipse(hx, hy - jaw_open // 2, 5.5, 4.0, CORRUPT_DARK)
        c.ellipse(hx, hy - jaw_open // 2 - 1, 4.5, 3.0, CORRUPT_MID)
        c.ellipse(hx, hy + 2 + jaw_open // 2, 5.0, 2.6, LEAF_DARK)
        # Denti
        for tx in (hx - 2, hx + 1, hx + 4):
            c.set(tx, hy, hex_col("#FFF3DA"))
        if spit:
            c.ellipse(hx + 5, hy, 2.2, 2.2, CORRUPT_HI)
        c.set(hx - 1, hy - 3, EYE_GOLD)
        c.outline(OUTLINE)
        c.save("Enemies/plant_spit.png" if spit else f"Enemies/plant_{i}.png")


def draw_boss_frame(mode, frame):
    c = Canvas(48, 52)
    bob = 1 if (mode == "idle" and frame in (1, 2)) else 0
    is_slam = (mode == "slam")
    is_summon = (mode == "summon")
    is_stun = (mode == "stun")

    dy = 3 if is_slam else (2 if is_stun else bob)
    # Tronco inferiore / radici gambe
    c.rect(14, 38, 8, 12, BARK_DARK)
    c.rect(26, 38, 8, 12, BARK_MID)
    c.rect(11, 46, 12, 4, BARK_MID)
    c.rect(25, 46, 12, 4, BARK_DARK)

    # Torso massiccio
    c.ellipse(24, 28 + dy, 13, 13, BARK_DARK)
    c.ellipse(24, 27 + dy, 11, 11, BARK_MID)
    c.ellipse(21, 25 + dy, 6, 7, BARK_HI)

    # Muschio sulle spalle
    c.ellipse(15, 19 + dy, 6, 3.5, LEAF_MID)
    c.ellipse(33, 19 + dy, 6, 3.5, LEAF_MID)
    c.ellipse(24, 16 + dy, 8, 4.0, LEAF_DARK)

    # Testa + corna ramificate
    c.ellipse(24, 14 + dy, 7.5, 6.0, BARK_MID)
    c.line(18, 10 + dy, 10, 3 + dy, BARK_HI)
    c.line(13, 6 + dy, 11, 1 + dy, LEAF_HI)
    c.line(30, 10 + dy, 38, 3 + dy, BARK_HI)
    c.line(35, 6 + dy, 37, 1 + dy, LEAF_HI)
    # Cristalli viola corrotti sulle corna
    c.rect(10, 4 + dy, 3, 4, CORRUPT_MID)
    c.set(11, 4 + dy, CORRUPT_HI)
    c.rect(36, 4 + dy, 3, 4, CORRUPT_MID)
    c.set(37, 4 + dy, CORRUPT_HI)

    # Occhi
    eye_c = hex_col("#80FFE0") if is_stun else CORRUPT_HI
    c.rect(20, 13 + dy, 2, 2, eye_c)
    c.rect(26, 13 + dy, 2, 2, eye_c)

    # Cuore ametista centrale (espone debolezza durante stun)
    core_r = 4.6 if is_stun else (4.0 if is_summon else 3.4)
    c.ellipse(24, 27 + dy, core_r, core_r, CORRUPT_DARK)
    c.ellipse(24, 27 + dy, core_r - 1.1, core_r - 1.1, CORRUPT_MID if not is_stun else CORRUPT_HI)
    c.ellipse(24, 26 + dy, 1.4, 1.4, hex_col("#FFFFFF") if is_stun else CORRUPT_HI)

    # Braccia radice possenti
    arm_y = 35 if is_slam else (14 if is_summon else (31 if is_stun else 26 + dy))
    c.rect(6, arm_y, 7, 11, BARK_MID)
    c.rect(35, arm_y, 7, 11, BARK_DARK)
    c.rect(5, arm_y + 7, 8, 5, LEAF_DARK)
    c.rect(35, arm_y + 7, 8, 5, LEAF_MID)

    c.outline(OUTLINE)
    return c


def generate_boss():
    for i in range(4):
        draw_boss_frame("idle", i).save(f"Boss/root_guardian_idle_{i}.png")
    draw_boss_frame("slam", 0).save("Boss/root_guardian_slam.png")
    draw_boss_frame("summon", 0).save("Boss/root_guardian_summon.png")
    draw_boss_frame("stun", 0).save("Boss/root_guardian_stun.png")

    # Root spike emerging (16x32)
    for i, h_frac in enumerate((0.35, 0.72, 1.0)):
        c = Canvas(16, 32)
        top_y = int(31 - 28 * h_frac)
        for y in range(top_y, 32):
            t = (y - top_y) / max(1, (32 - top_y))
            half_w = max(1, int(1 + t * 5.2))
            for x in range(8 - half_w, 8 + half_w):
                col = CORRUPT_HI if y < top_y + 5 else (CORRUPT_MID if (x + y) % 3 == 0 else BARK_MID)
                c.set(x, y, col)
        c.outline(OUTLINE)
        c.save(f"Boss/root_spike_{i}.png")


def generate_tiles():
    rng = random.Random(42)
    # grass_top_0..2 (16x16)
    dirt_c1 = hex_col("#543528")
    dirt_c2 = hex_col("#3E251B")
    dirt_hi = hex_col("#6E4836")
    for idx in range(3):
        c = Canvas(16, 16)
        c.rect(0, 0, 16, 16, dirt_c1)
        for _ in range(28):
            c.set(rng.randint(0, 15), rng.randint(4, 15), dirt_c2)
        for _ in range(12):
            c.set(rng.randint(0, 15), rng.randint(5, 14), dirt_hi)
        # Muschio superiore verde vivo
        c.rect(0, 0, 16, 4, LEAF_MID)
        c.rect(0, 0, 16, 2, LEAF_HI)
        for x in range(16):
            hang = rng.randint(3, 6)
            for y in range(3, hang):
                c.set(x, y, LEAF_DARK if y == hang - 1 else LEAF_MID)
        c.save(f"Tiles/grass_top_{idx}.png")

    # dirt_0..2 (16x16)
    for idx in range(3):
        c = Canvas(16, 16)
        c.rect(0, 0, 16, 16, dirt_c1)
        for _ in range(32):
            c.set(rng.randint(0, 15), rng.randint(0, 15), dirt_c2)
        for _ in range(14):
            c.set(rng.randint(0, 15), rng.randint(0, 15), dirt_hi)
        c.save(f"Tiles/dirt_{idx}.png")

    # stone_wall (16x16)
    stone_1 = hex_col("#4A5559")
    stone_2 = hex_col("#353F45")
    stone_hi = hex_col("#6A777A")
    c = Canvas(16, 16)
    c.rect(0, 0, 16, 16, stone_2)
    c.rect(1, 1, 6, 6, stone_1)
    c.rect(8, 1, 7, 6, stone_1)
    c.rect(0, 8, 4, 7, stone_1)
    c.rect(5, 8, 10, 7, stone_1)
    c.rect(1, 1, 6, 1, stone_hi)
    c.rect(5, 8, 10, 1, stone_hi)
    c.rect(2, 1, 3, 2, LEAF_DARK)
    c.save("Tiles/stone_wall.png")

    # wood_platform (16x16, parte alta solida + supporto)
    c = Canvas(16, 16)
    c.rect(0, 0, 16, 6, BARK_MID)
    c.rect(0, 0, 16, 2, BARK_HI)
    c.rect(0, 5, 16, 2, BARK_DARK)
    c.rect(2, 0, 3, 2, LEAF_MID)
    c.rect(11, 0, 3, 2, LEAF_MID)
    c.outline(OUTLINE)
    c.save("Tiles/wood_platform.png")

    # village_ground (16x16)
    c = Canvas(16, 16)
    c.rect(0, 0, 16, 16, hex_col("#5E4332"))
    c.rect(0, 0, 16, 3, hex_col("#91B854"))
    c.rect(0, 0, 16, 1, hex_col("#BBE06C"))
    c.rect(2, 5, 5, 3, hex_col("#7B6859"))
    c.rect(9, 6, 5, 3, hex_col("#6E5B4C"))
    c.save("Tiles/village_ground.png")

    # thorns_hazard (16x16)
    c = Canvas(16, 16)
    for bx in (2, 8, 13):
        c.line(bx - 2, 15, bx, 3, CORRUPT_DARK)
        c.line(bx + 2, 15, bx, 3, CORRUPT_MID)
        c.line(bx, 15, bx, 2, CORRUPT_HI)
    c.outline(OUTLINE)
    c.save("Tiles/thorns_hazard.png")

    # bridge_tile (16x16)
    c = Canvas(16, 16)
    c.rect(0, 0, 16, 5, hex_col("#94603C"))
    c.rect(0, 0, 16, 2, hex_col("#BA7E52"))
    c.rect(3, 5, 2, 10, BARK_DARK)
    c.rect(11, 5, 2, 10, BARK_DARK)
    c.outline(OUTLINE)
    c.save("Tiles/bridge_tile.png")


def generate_props():
    # root_anchor_0..1 (16x16 center pivot)
    for f in range(2):
        c = Canvas(16, 16)
        r = 5.5 if f == 0 else 6.3
        c.ellipse(8, 8, r, r, hex_col("#19593B", 200))
        c.ellipse(8, 8, 4.2, 4.2, ROOT_MID)
        c.ellipse(8, 8, 2.6, 2.6, ROOT_GLOW)
        c.set(7, 7, hex_col("#FFFFFF"))
        c.outline(OUTLINE)
        c.save(f"Props/root_anchor_{f}.png")

    # natural_switch_off / on (16x24)
    for on in (False, True):
        c = Canvas(16, 24)
        c.rect(4, 6, 8, 17, hex_col("#465054"))
        c.rect(5, 7, 6, 15, hex_col("#616E73"))
        c.rect(3, 20, 10, 4, LEAF_DARK)
        gem = ROOT_GLOW if on else hex_col("#4C545E")
        c.ellipse(8, 12, 2.5, 3.5, gem)
        if on:
            c.set(8, 11, hex_col("#FFFFFF"))
        c.outline(OUTLINE)
        c.save("Props/natural_switch_on.png" if on else "Props/natural_switch_off.png")

    # chest_closed / open (20x16)
    for open_st in (False, True):
        c = Canvas(20, 16)
        c.rect(3, 7, 14, 8, BARK_MID)
        c.rect(3, 7, 14, 2, SCARF_HI)
        if not open_st:
            c.rect(3, 4, 14, 4, BARK_HI)
            c.rect(8, 6, 4, 4, SCARF_HI)
        else:
            c.rect(2, 1, 16, 4, BARK_HI)
            c.rect(6, 5, 8, 3, ROOT_GLOW)
        c.outline(OUTLINE)
        c.save("Props/chest_open.png" if open_st else "Props/chest_closed.png")

    # portal_0..2 (32x48)
    for f in range(3):
        c = Canvas(32, 48)
        c.ellipse(16, 26, 12, 20, hex_col("#445056"))
        c.ellipse(16, 26, 9, 17, hex_col("#192E33"))
        swirl = ROOT_GLOW if f % 2 == 0 else hex_col("#76FFF5")
        c.ellipse(16, 26, 6.5 - f * 0.5, 13.5, ROOT_MID)
        c.ellipse(16, 26, 3.8, 9.0 + f, swirl)
        c.rect(4, 43, 24, 5, hex_col("#5B676D"))
        c.rect(6, 10, 5, 4, LEAF_MID)
        c.rect(21, 10, 5, 4, LEAF_MID)
        c.outline(OUTLINE)
        c.save(f"Props/portal_{f}.png")

    # exit doors (28x40)
    door_styles = [
        ("door_easy", LEAF_MID, LEAF_HI),
        ("door_hard", CORRUPT_MID, CORRUPT_HI),
        ("door_merchant", SCARF_MID, SCARF_HI),
        ("door_boss", hex_col("#911C4A"), hex_col("#FF4D6D")),
    ]
    for name, c1, c2 in door_styles:
        c = Canvas(28, 40)
        c.ellipse(14, 22, 11, 16, hex_col("#434C52"))
        c.rect(3, 22, 22, 18, hex_col("#434C52"))
        c.ellipse(14, 23, 7.5, 12, c1)
        c.rect(6, 23, 16, 16, c1)
        c.ellipse(14, 24, 4.2, 8, c2)
        c.rect(12, 6, 4, 4, c2)
        c.outline(OUTLINE)
        c.save(f"Props/{name}.png")

    # altar_seeds (32x36)
    c = Canvas(32, 36)
    c.rect(6, 28, 20, 8, hex_col("#5A656B"))
    c.rect(9, 16, 14, 12, hex_col("#75838A"))
    c.ellipse(16, 10, 5.5, 6.5, ROOT_MID)
    c.ellipse(16, 10, 3.0, 4.0, ROOT_GLOW)
    c.rect(4, 32, 24, 4, LEAF_MID)
    c.outline(OUTLINE)
    c.save("Props/altar_seeds.png")

    # weapon_rack (32x28)
    c = Canvas(32, 28)
    c.rect(4, 6, 3, 22, BARK_MID)
    c.rect(25, 6, 3, 22, BARK_MID)
    c.rect(3, 9, 26, 3, BARK_HI)
    c.line(10, 5, 10, 23, LEAF_HI)
    c.rect(15, 7, 5, 6, hex_col("#9DA8AE"))
    c.line(17, 13, 17, 25, BARK_DARK)
    c.line(23, 8, 22, 22, ROOT_MID)
    c.outline(OUTLINE)
    c.save("Props/weapon_rack.png")

    # elder_npc_0..1 & merchant_npc_0..1 (24x28)
    for f in range(2):
        c = Canvas(24, 28)
        dy = f
        c.ellipse(12, 18 + dy, 6.5, 8.5, hex_col("#2C6E49"))
        c.ellipse(12, 10 + dy, 4.8, 4.5, hex_col("#4C956C"))
        c.rect(10, 11 + dy, 4, 5, hex_col("#E8E6D9"))  # barba
        c.set(10, 9 + dy, EYE_GOLD)
        c.set(13, 9 + dy, EYE_GOLD)
        c.line(18, 8, 18, 27, BARK_HI)
        c.set(18, 7, ROOT_GLOW)
        c.outline(OUTLINE)
        c.save(f"Props/elder_npc_{f}.png")

        m = Canvas(24, 28)
        m.ellipse(12, 19 + dy, 6.5, 8.0, hex_col("#8A4E2A"))
        m.ellipse(12, 10 + dy, 5.2, 4.6, SCARF_MID)
        m.rect(9, 10 + dy, 6, 3, OUTLINE)
        m.set(10, 11 + dy, SCARF_HI)
        m.set(13, 11 + dy, SCARF_HI)
        m.rect(4, 15 + dy, 5, 7, hex_col("#C99648"))
        m.outline(OUTLINE)
        m.save(f"Props/merchant_npc_{f}.png")

    # tree_bg_prop (48x64)
    c = Canvas(48, 64)
    c.rect(19, 28, 10, 36, BARK_DARK)
    c.rect(21, 28, 5, 36, BARK_MID)
    c.ellipse(24, 22, 17, 15, LEAF_DARK)
    c.ellipse(18, 18, 11, 10, LEAF_MID)
    c.ellipse(29, 20, 11, 10, LEAF_MID)
    c.ellipse(24, 13, 10, 8, LEAF_HI)
    c.outline(OUTLINE)
    c.save("Props/tree_bg_prop.png")

    # mushroom_glow (16x16)
    c = Canvas(16, 16)
    c.rect(6, 8, 4, 7, hex_col("#E5DFC5"))
    c.ellipse(8, 7, 5.5, 3.5, hex_col("#2AD49E"))
    c.set(6, 6, hex_col("#B8FFE6"))
    c.set(10, 7, hex_col("#B8FFE6"))
    c.outline(OUTLINE)
    c.save("Props/mushroom_glow.png")

    # signpost (16x20)
    c = Canvas(16, 20)
    c.rect(7, 8, 2, 12, BARK_DARK)
    c.rect(2, 3, 12, 7, BARK_HI)
    c.rect(3, 5, 10, 1, BARK_DARK)
    c.rect(3, 7, 8, 1, BARK_DARK)
    c.outline(OUTLINE)
    c.save("Props/signpost.png")


def generate_backgrounds():
    # sky_forest (320x180)
    c = Canvas(320, 180)
    top = hex_col("#0E2925")
    mid = hex_col("#1B4D3E")
    bot = hex_col("#2E7052")
    for y in range(180):
        t = y / 179.0
        col = lerp_col(top, mid, t * 2) if t < 0.5 else lerp_col(mid, bot, (t - 0.5) * 2)
        for x in range(320):
            c.px[y * 320 + x] = col
    # Dappled mist
    for x in range(320):
        glow = int(12 * math.sin(x * math.pi * 2 / 160))
        for y in range(85, 155):
            r, g, b, a = c.px[y * 320 + x]
            c.px[y * 320 + x] = (min(255, max(0, r + glow)), min(255, max(0, g + glow)), b, 255)
    c.save("Backgrounds/sky_forest.png")

    # sky_village (320x180)
    v = Canvas(320, 180)
    v_top = hex_col("#2E5378")
    v_mid = hex_col("#7697A0")
    v_bot = hex_col("#F2C48D")
    for y in range(180):
        t = y / 179.0
        col = lerp_col(v_top, v_mid, t * 1.8) if t < 0.55 else lerp_col(v_mid, v_bot, (t - 0.55) / 0.45)
        for x in range(320):
            v.px[y * 320 + x] = col
    v.ellipse(245, 58, 22, 22, hex_col("#FFF1BF", 190))
    v.save("Backgrounds/sky_village.png")

    # parallax_far_trees (320x180, seamless)
    far = Canvas(320, 180)
    far_col = hex_col("#16352E", 238)
    far_hi = hex_col("#214D40", 238)
    far_deep = hex_col("#102923", 240)
    # Catena di alberi lontani seamless con chiome frastagliate
    for x in range(320):
        base = int(
            54
            + 16 * math.sin(x * 2 * math.pi / 80)
            + 9 * math.cos(x * 2 * math.pi / 32)
            + 5 * math.sin(x * 2 * math.pi / 16)
        )
        for y in range(180 - base, 180):
            far.set(x, y, far_col if y > 180 - base + 4 else far_hi)
    # Silhouette Albero-Cuore antico lontano al centro (x=160) con radici svasate e chioma organica
    for y in range(62, 180):
        t = (y - 62) / 118.0
        half_w = int(7 + (t * t) * 14 + (3 if y < 78 else 0))
        for dx in range(-half_w, half_w + 1):
            far.set(160 + dx, y, far_deep if dx > -2 else far_col)
    for bx0, by0, bx1, by1 in ((155, 72, 128, 52), (165, 70, 192, 50), (158, 66, 142, 40), (162, 66, 178, 42)):
        for k in range(24):
            u = k / 23.0
            far.ellipse(bx0 + (bx1 - bx0) * u, by0 + (by1 - by0) * u, 2.5, 2.5, far_col)
    for cx, cy, rx, ry, col in (
        (160, 44, 24, 15, far_col),
        (136, 52, 17, 12, far_col),
        (184, 52, 17, 12, far_col),
        (148, 36, 15, 11, far_hi),
        (172, 37, 15, 11, far_hi),
        (160, 29, 16, 10, far_hi),
    ):
        far.ellipse(cx, cy, rx, ry, col)
    # Cuore luminoso dell'Albero-Cuore
    far.ellipse(160, 52, 7, 7, hex_col("#33C98F", 110))
    far.ellipse(160, 52, 3.5, 3.5, hex_col("#8CFFD0", 190))
    far.save("Backgrounds/parallax_far_trees.png")

    # parallax_mid_trunks (320x180, seamless)
    mid_c = Canvas(320, 180)
    trunk = hex_col("#122921", 238)
    trunk_hi = hex_col("#1E4436", 238)
    for tx in (25, 95, 175, 255):
        for y in range(12, 180):
            flare = 4 if y > 158 else (2 if y > 142 else 0)
            mid_c.rect(tx - flare, y, 16 + flare * 2, 1, trunk)
            mid_c.rect(tx + 2, y, 4, 1, trunk_hi)
        mid_c.ellipse(tx + 8, 18, 26, 16, trunk)
        mid_c.ellipse(tx - 4, 25, 14, 10, trunk_hi)
        mid_c.ellipse(tx + 20, 24, 14, 10, trunk_hi)
    # Soft godrays diagonali
    for rx in (55, 145, 235):
        for y in range(0, 172):
            x_off = int(y * 0.28)
            for dx in range(11):
                mid_c.set((rx + x_off + dx) % 320, y, hex_col("#C8FFE0", 18))
    mid_c.save("Backgrounds/parallax_mid_trunks.png")

    # parallax_near_canopy (320x180, seamless)
    near = Canvas(320, 180)
    n_col = hex_col("#0D211A", 230)
    for x in range(320):
        top_h = int(16 + 7 * math.sin(x * 2 * math.pi / 64))
        for y in range(top_h):
            near.set(x, y, n_col)
        if x % 32 == 0:
            vine_len = 26 + ((x * 7) % 18)
            near.line(x, top_h, x + 2, top_h + vine_len, LEAF_DARK)
    near.save("Backgrounds/parallax_near_canopy.png")


def generate_vfx_and_icons():
    # slash_arc_0..2 (28x20)
    for i in range(3):
        c = Canvas(28, 20)
        col = hex_col("#80FFBD") if i < 2 else SCARF_HI
        for a in range(-65, 70, 5):
            rad = math.radians(a)
            rx = 10 + math.cos(rad) * 12
            ry = 10 + math.sin(rad) * 8
            c.set(rx, ry, col)
            c.set(rx - 1, ry, hex_col("#FFFFFF"))
            c.set(rx - 2, ry, ROOT_MID)
        c.save(f"VFX/slash_arc_{i}.png")

    # hammer_smash (36x28)
    c = Canvas(36, 28)
    c.ellipse(18, 20, 16, 7, hex_col("#FFBF5E", 210))
    c.ellipse(18, 21, 11, 4, hex_col("#FFFFFF", 220))
    c.save("VFX/hammer_smash.png")

    # whip_arc (40x14)
    c = Canvas(40, 14)
    for x in range(2, 38):
        y = 7 + math.sin(x * 0.28) * 3.5
        c.set(x, y, ROOT_GLOW)
        c.set(x, y + 1, LEAF_MID)
    c.save("VFX/whip_arc.png")

    # root_vine_segment (16x8)
    c = Canvas(16, 8)
    c.rect(0, 2, 16, 4, ROOT_MID)
    c.rect(0, 3, 16, 2, ROOT_GLOW)
    c.set(4, 1, LEAF_HI)
    c.set(11, 6, LEAF_HI)
    c.save("VFX/root_vine_segment.png")

    # root_vine_tip (12x12)
    c = Canvas(12, 12)
    c.ellipse(6, 6, 4, 4, ROOT_GLOW)
    c.set(6, 6, hex_col("#FFFFFF"))
    c.outline(OUTLINE)
    c.save("VFX/root_vine_tip.png")

    # root_reticle (16x16)
    c = Canvas(16, 16)
    for ox, oy in ((2, 2), (11, 2), (2, 11), (11, 11)):
        c.rect(ox, oy, 3, 1, ROOT_GLOW)
        c.rect(ox, oy, 1, 3, ROOT_GLOW)
    c.rect(11, 2, 3, 1, ROOT_GLOW)
    c.rect(13, 2, 1, 3, ROOT_GLOW)
    c.rect(2, 13, 3, 1, ROOT_GLOW)
    c.rect(11, 13, 3, 1, ROOT_GLOW)
    c.rect(13, 11, 1, 3, ROOT_GLOW)
    c.save("VFX/root_reticle.png")

    # projectile_spore (10x10)
    c = Canvas(10, 10)
    c.ellipse(5, 5, 3.8, 3.8, CORRUPT_MID)
    c.ellipse(5, 5, 2.0, 2.0, CORRUPT_HI)
    c.set(4, 4, hex_col("#FFFFFF"))
    c.outline(OUTLINE)
    c.save("VFX/projectile_spore.png")

    # particle_circle (8x8)
    c = Canvas(8, 8)
    c.ellipse(4, 4, 3.0, 3.0, hex_col("#FFFFFF", 210))
    c.ellipse(4, 4, 1.5, 1.5, hex_col("#FFFFFF", 255))
    c.save("VFX/particle_circle.png")

    # particle_leaf (8x6)
    c = Canvas(8, 6)
    c.ellipse(4, 3, 3.2, 1.8, LEAF_HI)
    c.line(1, 3, 6, 3, LEAF_MID)
    c.save("VFX/particle_leaf.png")

    # warn_marker (16x16)
    c = Canvas(16, 16)
    c.ellipse(8, 8, 6.5, 6.5, hex_col("#FF2A55", 220))
    c.rect(7, 3, 2, 6, hex_col("#FFFFFF"))
    c.rect(7, 11, 2, 2, hex_col("#FFFFFF"))
    c.outline(OUTLINE)
    c.save("VFX/warn_marker.png")

    # ui_panel (24x24 9-slice border 4)
    c = Canvas(24, 24)
    c.rect(0, 0, 24, 24, hex_col("#161D21", 238))
    c.rect(0, 0, 24, 2, hex_col("#D9A84E"))
    c.rect(0, 22, 24, 2, hex_col("#8C6229"))
    c.rect(0, 0, 2, 24, hex_col("#C49342"))
    c.rect(22, 0, 2, 24, hex_col("#8C6229"))
    c.rect(2, 2, 20, 1, hex_col("#334742"))
    c.save("VFX/ui_panel.png")

    # Icons (16x16)
    def make_heart(full):
        h = Canvas(16, 16)
        fill = hex_col("#FF3B5C") if full else hex_col("#3B2B35")
        hi = hex_col("#FF9BB0") if full else hex_col("#54404C")
        h.ellipse(5, 5, 3.2, 3.2, fill)
        h.ellipse(11, 5, 3.2, 3.2, fill)
        for y in range(6, 14):
            w = int(6 - (y - 6) * 0.85)
            h.rect(8 - w, y, w * 2, 1, fill)
        h.set(4, 4, hi)
        h.set(5, 4, hi)
        h.outline(OUTLINE)
        return h

    make_heart(True).save("Icons/heart_full.png")
    make_heart(False).save("Icons/heart_empty.png")

    # seed
    s = Canvas(16, 16)
    s.ellipse(8, 9, 4.2, 5.0, ROOT_MID)
    s.ellipse(7, 8, 2.2, 3.0, ROOT_GLOW)
    s.line(8, 4, 11, 2, LEAF_HI)
    s.outline(OUTLINE)
    s.save("Icons/seed.png")

    # coin
    co = Canvas(16, 16)
    co.ellipse(8, 8, 5.2, 5.2, SCARF_MID)
    co.ellipse(8, 8, 3.8, 3.8, SCARF_HI)
    co.rect(7, 5, 2, 6, hex_col("#FFF7B2"))
    co.outline(OUTLINE)
    co.save("Icons/coin.png")

    # 5 Relics
    relics = {
        "relic_poison": (LEAF_MID, hex_col("#76FF33")),
        "relic_fury": (CORRUPT_MID, hex_col("#FF4554")),
        "relic_wind": (hex_col("#56D8FF"), hex_col("#D4FAFF")),
        "relic_sap": (SCARF_HI, hex_col("#FF8833")),
        "relic_oak": (BARK_HI, LEAF_HI),
    }
    for name, (c1, c2) in relics.items():
        r = Canvas(16, 16)
        r.ellipse(8, 8, 5.5, 5.5, c1)
        r.ellipse(7, 7, 3.0, 3.0, c2)
        r.set(6, 6, hex_col("#FFFFFF"))
        r.outline(OUTLINE)
        r.save(f"Icons/{name}.png")

    # 3 Weapons
    wb = Canvas(16, 16)
    wb.line(3, 13, 13, 3, LEAF_HI)
    wb.line(4, 13, 13, 4, ROOT_GLOW)
    wb.rect(2, 11, 4, 3, BARK_HI)
    wb.outline(OUTLINE)
    wb.save("Icons/weapon_blade.png")

    wh = Canvas(16, 16)
    wh.line(3, 13, 11, 5, BARK_MID)
    wh.rect(7, 2, 7, 6, BARK_HI)
    wh.rect(8, 3, 5, 2, LEAF_MID)
    wh.outline(OUTLINE)
    wh.save("Icons/weapon_hammer.png")

    ww = Canvas(16, 16)
    for x in range(3, 14):
        y = 8 + math.sin(x * 0.7) * 3.5
        ww.set(x, y, ROOT_GLOW)
        ww.set(x, y + 1, LEAF_MID)
    ww.rect(2, 9, 3, 3, BARK_HI)
    ww.outline(OUTLINE)
    ww.save("Icons/weapon_whip.png")


def main():
    generate_player()
    generate_enemies()
    generate_boss()
    generate_tiles()
    generate_props()
    generate_backgrounds()
    generate_vfx_and_icons()
    print("Pixel art generata con successo in:", ART_DIR)


if __name__ == "__main__":
    main()
