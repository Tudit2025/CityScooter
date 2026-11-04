#!/usr/bin/env python3
"""Pretend to be micro:bits so the Unity Editor can be tested without hardware.

    python tools/fake_microbit.py              # one board: gentle weaving + random jumps
    python tools/fake_microbit.py --boards 2   # co-op: b1 steers, b2 jumps
    python tools/fake_microbit.py --spam-jump  # what a polling loop with is_pressed() looks like

Sends protocol v0 lines (docs/protocol.md) over UDP to 127.0.0.1:47800, where the Editor's
UdpDeviceTransport listens while in Play mode, and prints whatever the game sends back.
Standard library only.
"""
import argparse
import math
import random
import socket
import time


def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--port", type=int, default=47800)
    ap.add_argument("--boards", type=int, default=1, choices=(1, 2))
    ap.add_argument("--rate", type=float, default=20.0, help="loop rate in Hz (a micro:bit with sleep(50) is ~20)")
    ap.add_argument("--spam-jump", action="store_true", help="send jp every loop for 1 s at a time")
    args = ap.parse_args()

    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.setblocking(False)
    dest = ("127.0.0.1", args.port)
    start = time.time()
    next_jump = start + 2.0
    next_hello = start
    spam_until = 0.0
    print(f"fake micro:bit x{args.boards} -> {dest}  (press Play in Unity; Ctrl+C to stop)")

    try:
        while True:
            now = time.time()
            lines = [f"b1|st:{int(80 * math.sin((now - start) * 0.8))}"]
            if now >= next_hello:
                lines += [f"b{i + 1}|hi:v2" for i in range(args.boards)]
                next_hello = now + 1.0
            jumper = "b2" if args.boards == 2 else "b1"
            if args.spam_jump and now >= next_jump:
                spam_until, next_jump = now + 1.0, now + random.uniform(3, 5)
            if now < spam_until:
                lines.append(f"{jumper}|jp")
            elif not args.spam_jump and now >= next_jump:
                lines.append(f"{jumper}|jp")
                next_jump = now + random.uniform(1.5, 4.0)
            try:
                sock.sendto(("\n".join(lines) + "\n").encode(), dest)
            except OSError:
                pass
            try:
                while True:
                    data, _ = sock.recvfrom(2048)
                    for line in data.decode(errors="replace").splitlines():
                        print("game ->", line)
            except (BlockingIOError, ConnectionResetError):
                pass  # nothing to read / Windows reports "port unreachable" while Unity isn't playing
            time.sleep(1.0 / args.rate)
    except KeyboardInterrupt:
        print()


if __name__ == "__main__":
    main()
