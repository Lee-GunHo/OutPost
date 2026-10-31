"""Procedural SFX in a Core Keeper-ish style: short, punchy, warm, slightly lo-fi.
Usage (프로젝트 루트에서): python3 Tools/Sfx/synth_sfx.py Assets/Music/Sfx
같은 이름으로 덮어쓰므로 .meta(guid)는 유지되어 SfxLibrary 연결이 끊기지 않음. numpy 필요.
"""
import sys, os, wave
import numpy as np

SR = 44100
rng = np.random.default_rng(1234)


def t_axis(dur):
    return np.arange(int(SR * dur)) / SR


def env_exp(dur, decay, attack=0.002):
    t = t_axis(dur)
    a = np.clip(t / attack, 0, 1) if attack > 0 else 1.0
    return a * np.exp(-t / decay)


def noise(dur):
    return rng.uniform(-1, 1, int(SR * dur))


def onepole_lp(x, cutoff):
    cutoff = np.broadcast_to(np.asarray(cutoff, dtype=float), x.shape)
    a = np.exp(-2 * np.pi * cutoff / SR)
    y = np.empty_like(x)
    s = 0.0
    for i in range(len(x)):
        s = (1 - a[i]) * x[i] + a[i] * s
        y[i] = s
    return y


def lp(x, cutoff, order=2):
    for _ in range(order):
        x = onepole_lp(x, cutoff)
    return x


def hp(x, cutoff):
    return x - onepole_lp(x, cutoff)


def bp(x, lo, hi, order=2):
    return lp(hp(x, lo), hi, order)


def sweep_sine(dur, f0, f1, curve=8.0, shape="sine"):
    t = t_axis(dur)
    f = f1 + (f0 - f1) * np.exp(-t * curve)
    ph = 2 * np.pi * np.cumsum(f) / SR
    if shape == "square":
        return np.sign(np.sin(ph)) * 0.6
    if shape == "tri":
        return 2 / np.pi * np.arcsin(np.sin(ph))
    return np.sin(ph)


def tone(dur, freq, decay, shape="sine", attack=0.002):
    t = t_axis(dur)
    ph = 2 * np.pi * freq * t
    if shape == "tri":
        w = 2 / np.pi * np.arcsin(np.sin(ph))
    elif shape == "square":
        w = np.sign(np.sin(ph)) * 0.5
    else:
        w = np.sin(ph)
    return w * env_exp(dur, decay, attack)


def mix(*parts):
    n = max(len(p) for p in parts)
    out = np.zeros(n)
    for p in parts:
        out[:len(p)] += p
    return out


def delay(x, sec):
    return np.concatenate([np.zeros(int(SR * sec)), x])


def saturate(x, drive=2.0):
    return np.tanh(x * drive) / np.tanh(drive)


def crush(x, bits=10, down=2):
    """Light lo-fi: sample-and-hold + quantize."""
    y = np.repeat(x[::down], down)[:len(x)]
    q = 2 ** (bits - 1)
    return np.round(y * q) / q


def finish(x, fade=0.004, peak=0.89):
    x = x - np.mean(x)
    n = int(SR * fade)
    if n > 0 and len(x) > n:
        x[-n:] *= np.linspace(1, 0, n)
        x[:16] *= np.linspace(0, 1, 16)
    m = np.max(np.abs(x))
    return x / m * peak if m > 0 else x


def write(path, x):
    data = (np.clip(x, -1, 1) * 32767).astype(np.int16)
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(data.tobytes())


def jit(v, amt=0.08):
    return v * (1 + rng.uniform(-amt, amt))


# ------------------------------------------------------------------ sounds

def swing(i):
    d = 0.17
    t = t_axis(d)
    n = noise(d)
    center = jit(1400) * (0.6 + 1.2 * np.sin(np.pi * np.clip(t / d, 0, 1)))
    w = bp(n, center * 0.5, center * 1.6)
    e = np.sin(np.pi * np.clip(t / d, 0, 1)) ** 1.5
    return finish(saturate(w * e, 1.5))


def hit_enemy(i):
    d = 0.22
    thump = sweep_sine(d, jit(240), jit(70), 30) * env_exp(d, 0.06)
    crack = bp(noise(d), 800, 3500) * env_exp(d, 0.018, 0.0005)
    body = lp(noise(d), 900) * env_exp(d, 0.05) * 0.8
    click = tone(d, jit(1800), 0.006) * 0.35
    return finish(crush(saturate(mix(thump * 1.1, crack * 0.9, body, click), 2.5), 11, 2))


def enemy_die(i):
    d = 0.42
    pop = sweep_sine(d, jit(520), jit(90), 9, "tri") * env_exp(d, 0.12)
    squish = bp(noise(d), 300, 1600) * env_exp(d, 0.09) * 0.7
    puff = lp(noise(d), 2500) * env_exp(d, 0.18, 0.02) * 0.35
    thump = sweep_sine(d, 160, 50, 20) * env_exp(d, 0.08) * 0.8
    return finish(crush(saturate(mix(pop, squish, puff, thump), 2.0), 10, 2))


def boss_die(i):
    d = 1.3
    rumble = lp(noise(d), 260) * env_exp(d, 0.55, 0.01) * 1.4
    boom = sweep_sine(d, 120, 32, 4) * env_exp(d, 0.45)
    crack = bp(noise(d), 700, 4000) * env_exp(d, 0.05) * 0.8
    tail = mix(delay(enemy_die(0) * 0.6, 0.08), delay(enemy_die(1) * 0.4, 0.22))
    return finish(saturate(mix(rumble, boom * 1.2, crack, tail), 1.8))


def player_hurt(i):
    d = 0.32
    thud = sweep_sine(d, jit(180), 55, 18) * env_exp(d, 0.09)
    grunt = sweep_sine(d, jit(330), jit(200), 6, "square") * env_exp(d, 0.08, 0.01)
    grunt = lp(grunt, 1200)
    crack = bp(noise(d), 1000, 4000) * env_exp(d, 0.02) * 0.6
    return finish(crush(saturate(mix(thud * 1.2, grunt * 0.7, crack), 2.2), 10, 2))


def player_death(i):
    d = 1.0
    notes = [392, 330, 262, 196]
    parts = []
    for k, f in enumerate(notes):
        parts.append(delay(lp(tone(0.35, f, 0.16, "square"), 2200) * 0.6, k * 0.14))
    thud = sweep_sine(d, 150, 40, 10) * env_exp(d, 0.15)
    return finish(crush(mix(*parts, thud), 9, 2))


def dig_dirt(i):
    d = 0.2
    crunch = lp(noise(d), jit(1100)) * env_exp(d, 0.06, 0.001)
    grains = bp(noise(d) * (rng.random(int(SR * d)) > 0.93), 1500, 5000) * env_exp(d, 0.08) * 1.5
    thud = sweep_sine(d, jit(150), 60, 25) * env_exp(d, 0.05)
    return finish(crush(saturate(mix(crunch * 1.2, grains * 0.5, thud * 0.9), 2.0), 11, 2))


def dig_stone(i):
    d = 0.28
    f = jit(1350, 0.12)
    ring = (tone(d, f, 0.045) + tone(d, f * 2.71, 0.025) * 0.5 + tone(d, f * 4.1, 0.015) * 0.3)
    crack = bp(noise(d), 1500, 6000) * env_exp(d, 0.012, 0.0003)
    chunk = lp(noise(d), 700) * env_exp(d, 0.04) * 0.9
    thud = sweep_sine(d, 200, 80, 30) * env_exp(d, 0.035) * 0.6
    return finish(crush(saturate(mix(ring * 0.55, crack, chunk, thud), 2.0), 11, 2))


def dig_ore(i):
    d = 0.42
    base = dig_stone(i)
    f = jit(2400, 0.06)
    shimmer = (tone(d, f, 0.12, "tri") + tone(d, f * 1.5, 0.09, "tri") * 0.6) * 0.35
    return finish(mix(base, delay(shimmer, 0.015)))


def chop_wood(i):
    d = 0.26
    knock = (tone(d, jit(380), 0.05) + tone(d, jit(610), 0.03) * 0.6 + tone(d, jit(960), 0.02) * 0.4)
    crack = bp(noise(d), 900, 3500) * env_exp(d, 0.015, 0.0005)
    body = lp(noise(d), 1200) * env_exp(d, 0.04) * 0.7
    return finish(crush(saturate(mix(knock, crack, body), 2.2), 11, 2))


def break_dirt(i):
    d = 0.5
    parts = [dig_dirt(i) * 0.9]
    for k in range(4):
        parts.append(delay(lp(noise(0.12), jit(1500)) * env_exp(0.12, 0.03) * jit(0.5, 0.3), 0.05 + k * jit(0.06, 0.3)))
    thud = sweep_sine(d, 120, 45, 12) * env_exp(d, 0.1)
    return finish(saturate(mix(*parts, thud), 1.6))


def break_stone(i):
    d = 0.6
    parts = [dig_stone(i)]
    for k in range(5):
        pebble = (tone(0.1, jit(1800, 0.3), 0.02) * 0.5 + bp(noise(0.1), 1500, 5000) * env_exp(0.1, 0.012))
        parts.append(delay(pebble * jit(0.45, 0.3), 0.06 + k * jit(0.055, 0.35)))
    thud = sweep_sine(d, 140, 45, 10) * env_exp(d, 0.12) * 1.2
    return finish(crush(saturate(mix(*parts, thud), 1.8), 10, 2))


def break_wood(i):
    d = 0.6
    creak = lp(sweep_sine(0.25, jit(260), jit(140), 4, "square"), 900) * env_exp(0.25, 0.12, 0.02) * 0.5
    snap = bp(noise(0.2), 600, 4000) * env_exp(0.2, 0.03) * 1.2
    thud = sweep_sine(d, 110, 45, 9) * env_exp(d, 0.14) * 1.2
    return finish(crush(saturate(mix(chop_wood(i) * 0.7, delay(creak, 0.02), delay(snap, 0.12), delay(thud, 0.14)), 1.8), 10, 2))


def pickup(i):
    # soft "bloop" pop, rising
    d = 0.16
    f0 = jit(520, 0.05) * (1 + 0.06 * i)
    t = t_axis(d)
    f = f0 * (1 + 1.3 * (1 - np.exp(-t * 40)))
    b = np.sin(2 * np.pi * np.cumsum(f) / SR) * env_exp(d, 0.045, 0.003)
    sparkle = tone(d, f0 * 3, 0.03, "tri") * 0.25
    return finish(mix(b, delay(sparkle, 0.03)))


def craft(i):
    d = 0.75
    parts = []
    for k, f in enumerate([900, 1050, 1300]):
        hammer = (tone(0.2, f, 0.05) + tone(0.2, f * 2.6, 0.02) * 0.4) + bp(noise(0.2), 2000, 7000) * env_exp(0.2, 0.008)
        parts.append(delay(hammer * 0.8, k * 0.11))
    chime = mix(tone(0.4, 1047, 0.14, "tri"), delay(tone(0.4, 1568, 0.16, "tri"), 0.06))
    parts.append(delay(chime * 0.6, 0.34))
    return finish(crush(mix(*parts), 10, 2))


def craft_fail(i):
    d = 0.25
    buzz = lp(mix(tone(d, 140, 0.12, "square"), tone(d, 147, 0.12, "square")), 1400)
    return finish(crush(buzz, 8, 2))


def level_up(i):
    notes = [523, 659, 784, 1047, 1319]
    parts = []
    for k, f in enumerate(notes):
        parts.append(delay(lp(tone(0.5, f, 0.2, "square"), 3500) * 0.5 + tone(0.5, f * 2, 0.12, "tri") * 0.2, k * 0.075))
    shimmer = bp(noise(0.9), 4000, 9000) * env_exp(0.9, 0.3, 0.2) * 0.15
    return finish(crush(mix(*parts, delay(shimmer, 0.3)), 9, 2))


def upgrade(i):
    notes = [659, 988, 1319]
    parts = [delay(tone(0.35, f, 0.13, "tri"), k * 0.06) for k, f in enumerate(notes)]
    return finish(crush(mix(*parts), 10, 2))


def place(i):
    d = 0.2
    thud = sweep_sine(d, jit(260), 90, 30) * env_exp(d, 0.05)
    tap = bp(noise(d), 600, 2500) * env_exp(d, 0.02) * 0.8
    return finish(crush(saturate(mix(thud, tap), 2.0), 11, 2))


def plant(i):
    d = 0.25
    pat = lp(noise(d), 800) * env_exp(d, 0.05) * 0.8
    pop = tone(d, jit(700), 0.04, "tri") * 0.5
    return finish(mix(pat, delay(pop, 0.04)))


def harvest(i):
    d = 0.35
    rip = bp(noise(0.15), 700, 3000) * env_exp(0.15, 0.04)
    return finish(mix(rip, delay(pickup(i) * 0.9, 0.05)))


def dash(i):
    d = 0.25
    t = t_axis(d)
    center = 600 + 2400 * (t / d)
    w = bp(noise(d), center * 0.5, center * 1.4)
    e = np.sin(np.pi * np.clip(t / d, 0, 1)) ** 2
    return finish(w * e)


def nexus_hit(i):
    d = 0.5
    f = 420
    ring = (tone(d, f, 0.18) + tone(d, f * 2.4, 0.1) * 0.5 + tone(d, f * 3.9, 0.06) * 0.3)
    thud = sweep_sine(d, 160, 60, 15) * env_exp(d, 0.08)
    return finish(crush(saturate(mix(ring * 0.7, thud), 1.8), 10, 2))


def projectile_charge(i):
    d = 0.5
    t = t_axis(d)
    f = 300 + 900 * (t / d) ** 1.5
    whine = np.sin(2 * np.pi * np.cumsum(f) / SR) * 0.4 + np.sign(np.sin(2 * np.pi * np.cumsum(f * 0.5) / SR)) * 0.15
    hiss = bp(noise(d), 2000, 6000) * 0.25
    e = np.clip(t / d, 0, 1) ** 1.2 * np.clip((d - t) / 0.04, 0, 1)
    return finish(crush(lp(whine + hiss, 3500) * e, 9, 2))


def projectile_fire(i):
    d = 0.3
    t = t_axis(d)
    center = jit(2200) * np.exp(-t * 6) + 400
    whoosh = bp(noise(d), center * 0.5, center * 1.3) * env_exp(d, 0.1, 0.003)
    pew = sweep_sine(d, jit(1400), 350, 18, "tri") * env_exp(d, 0.06) * 0.6
    thump = sweep_sine(d, 180, 70, 25) * env_exp(d, 0.04) * 0.5
    return finish(crush(saturate(mix(whoosh, pew, thump), 1.8), 10, 2))


def projectile_impact(i):
    d = 0.4
    boom = sweep_sine(d, jit(220), 50, 14) * env_exp(d, 0.09)
    crackle = bp(noise(d) * (rng.random(int(SR * d)) > 0.85), 1500, 6000) * env_exp(d, 0.12) * 1.4
    burst = lp(noise(d), 1800) * env_exp(d, 0.05, 0.001)
    return finish(crush(saturate(mix(boom * 1.1, crackle * 0.5, burst), 2.2), 10, 2))


SOUNDS = {
    "swing": (swing, 3),
    "hit_enemy": (hit_enemy, 3),
    "enemy_die": (enemy_die, 2),
    "boss_die": (boss_die, 1),
    "player_hurt": (player_hurt, 2),
    "player_death": (player_death, 1),
    "dig_dirt": (dig_dirt, 3),
    "dig_stone": (dig_stone, 3),
    "dig_ore": (dig_ore, 2),
    "chop_wood": (chop_wood, 3),
    "break_dirt": (break_dirt, 2),
    "break_stone": (break_stone, 2),
    "break_wood": (break_wood, 2),
    "pickup": (pickup, 3),
    "craft": (craft, 1),
    "craft_fail": (craft_fail, 1),
    "level_up": (level_up, 1),
    "upgrade": (upgrade, 1),
    "place": (place, 2),
    "plant": (plant, 1),
    "harvest": (harvest, 1),
    "dash": (dash, 1),
    "nexus_hit": (nexus_hit, 1),
    "projectile_charge": (projectile_charge, 1),
    "projectile_fire": (projectile_fire, 2),
    "projectile_impact": (projectile_impact, 2),
}

if __name__ == "__main__":
    out = sys.argv[1]
    os.makedirs(out, exist_ok=True)
    for name, (fn, count) in SOUNDS.items():
        for i in range(count):
            path = os.path.join(out, f"sfx_{name}_{i + 1:02d}.wav")
            write(path, fn(i))
            print(path)
