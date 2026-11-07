# CityScooter

A Perth city-scooter endless runner used to teach Year 9 students micro:bit + MicroPython. Students program
their micro:bit as the controller (tilt to steer, buttons, sound, pins) and react to game events on the LEDs
and speaker. Unity 6000.3.28f1 (Unity 6.3 LTS) + URP 17.3, shipped as a Web build (Chrome/Edge, Web Serial).

- Sprint plan, ownership, how-tos: [docs/sprint.md](docs/sprint.md)
- Comic city prototype, asset recipe and rebuild workflow: [docs/art/comic-city-standard.md](docs/art/comic-city-standard.md)
- Audio kit, gameplay mix, source credits and rebuild workflow: [docs/audio/comic-city-audio-standard.md](docs/audio/comic-city-audio-standard.md)
- Bounded camera, speed progression, music rate and sidewalk riding: [docs/gameplay/comic-city-riding.md](docs/gameplay/comic-city-riding.md)
- Message protocol (micro:bit ↔ game): [docs/protocol.md](docs/protocol.md)
- Web shell ↔ Unity contract, simulator rules: [docs/bridge.md](docs/bridge.md)
- Task briefs: [docs/tasks/](docs/tasks/)
- Lab check page: https://cityscooter-labcheck.vercel.app (source in `tools/labcheck/`)
- Student MicroPython: `microbit/` (Lesson 1 starter, task answers T1–T6, co-op boards; V1 checks in `microbit/README.md`).
- Blocks → MicroPython editor (reusable for other games via block packs): [docs/blocks.md](docs/blocks.md)
- Web shell (USB, simulator mode, Blocks/Python editor, serial monitor): `Assets/WebGLTemplates/CityScooter/`. Develop it without Unity:
  `python -m http.server 8000` in the repo root, then open http://localhost:8000/tools/shell-dev/.
- No micro:bit? Press Play, F1 for the device overlay, and run `python tools/fake_microbit.py`.
- Real micro:bit in the Editor? Flash `tools/labcheck/labcheck.py`, double-click `tools/serial_bridge.cmd`, press Play,
  then press A on the board to start (the Editor does not open serial ports itself; the Web build uses Web Serial).
