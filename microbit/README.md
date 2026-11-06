# CityScooter micro:bit programs (MicroPython)

Open a file in https://python.microbit.org, then **Send to micro:bit** (or paste it into the game's
Simulator mode). Every program is standalone and speaks the protocol in `docs/protocol.md`:
it prints `st:-100..100`, `jp`, `br`, `hi:v1|v2` and reacts to `go`, `crash`, `gift`, `spd:0-9`.

| File | What it does | V1 |
|---|---|---|
| `starter.py` | Lesson 1 start: tilt steers, A jumps (`was_pressed`), hold B brakes, shows 1/2 for the board version, sad face on crash, happy face on gift, clears on go. `# TODO (Task N)` marks where students work. | yes |
| `solutions/t1_steering.py` | `SENSITIVITY`, `DEAD_ZONE`, `REVERSED` | yes |
| `solutions/t2_feedback.py` | `WAWAWAWAA` on crash, `BA_DING` on gift (`wait=False`) | silent |
| `solutions/t3_buttons.py` | `USE_POLLING` switches A between `was_pressed()` (1 jp per press) and `is_pressed()` (~16-20 jp per second held) | yes |
| `solutions/t4_speed_bar.py` | `spd:n` lights `n*5//9` rows from the bottom; the sad face stays until the next `go` | yes |
| `solutions/t5_shout.py` | prints `sound:N` 4x a second for calibrating; above `THRESHOLD` sends `jp`, 1 s cooldown | prints "This task needs a V2 micro:bit" every 5 s, controls still work |
| `solutions/t6_foil_keys.py` | pin 0 jump (once per touch), pin 1 left, pin 2 right, tilt when no pin is touched; V2 uses capacitive touch | yes, hold GND |
| `coop/steer.py` | co-op board 1: only `st:` (left arrow on the LEDs) | yes |
| `coop/jump.py` | co-op board 2: only `jp`, from A or B (up arrow) | yes |

Rules all files follow: V1-safe APIs in the core loop, `uart.init(baudrate=115200)`, the buffered
`messages()` reader (a game message can arrive in two halves), `sleep(50)` loops, no `display.scroll()`,
about 20-25 lines per second.

## Tested (2026-11-06)

Official simulator (python-simulator.usermbit.org, V2), headless, every file plus a "forced V1" run
of each one (the `microphone` import renamed so the V1 branch runs): 19/19 pass. Checked per run:
`hi:` line, steering sign follows tilt, A gives exactly one `jp`, B gives `br` while held, LED faces
for crash / gift / go (read from the simulator's LED grid), a `crash` split into `cr` + `ash` still
works, no `Traceback`, at most 25 lines per second. T1 dead zone, T3 polling (16 jp/s), T4 bar
(spd:9 full, spd:5 two rows, spd:0 blank, sad face kept after crash), T5 cooldown (3 jumps in a
2.3 s shout, none when quiet), T6 pins (-70 / 70, one jump per touch) checked as well.
**Not yet run on real hardware** (no board was connected).

## Teacher: check on a real V1 (and once on a real V2)

1. `hi:v1` really appears on a V1 (the `microphone` ImportError was only simulated).
2. The game receives lines after `uart.init(baudrate=115200)` and the reader handles `crash` / `gift`
   (sad / happy face). `str(bytes, "utf-8")` on the V1 firmware python.microbit.org flashes.
3. Memory: files are 1.3-2.3 KB; V1 has 16 KB RAM. Watch for `MemoryError` when students add code.
4. T2 on V1: no speaker, so it is silent unless a buzzer or headphones are on pin 0 + GND. Steering
   must not stutter while a tune plays.
5. T6 on V1: resistive touch only works while the other hand holds GND; try foil + crocodile clips
   on pins 0/1/2. T6 has no music because music also drives pin 0.
6. T6 on V2: `set_touch_mode()` needs a recent V2 MicroPython (python.microbit.org's is fine); an old
   firmware would raise `AttributeError`.
7. T5 on V2: 120 is a guess. Calibrate with the `sound:` numbers in the real classroom.
8. Loop speed on V1: the monitor should show about 20 `st:` per second with no lag.
9. T3 polling with A and B both held prints about 60 lines a second (above the usual 50); fine for the
   demo, but mention it.
