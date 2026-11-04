# CityScooter

A Perth city-scooter endless runner used to teach Year 9 students micro:bit + MicroPython. Students program
their micro:bit as the controller (tilt to steer, buttons, sound, pins) and react to game events on the LEDs
and speaker. Unity 6000.0.58f2 + URP, shipped as a Web build (Chrome/Edge, Web Serial).

- Sprint plan, ownership, how-tos: [docs/sprint.md](docs/sprint.md)
- Message protocol (micro:bit ↔ game): [docs/protocol.md](docs/protocol.md)
- Web shell ↔ Unity contract, simulator rules: [docs/bridge.md](docs/bridge.md)
- Task briefs: [docs/tasks/](docs/tasks/)
- Lab check page: https://cityscooter-labcheck.vercel.app (source in `tools/labcheck/`)
- No micro:bit? Press Play, F1 for the device overlay, and run `python tools/fake_microbit.py`.
