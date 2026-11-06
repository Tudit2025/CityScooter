# CityScooter starter - Lesson 1
# Works on micro:bit V1 and V2. Flash it from python.microbit.org,
# then connect the micro:bit on the game page.
from microbit import *

# Which micro:bit is this? Only V2 has a microphone.
try:
    from microbit import microphone
    VERSION = "v2"
except ImportError:
    VERSION = "v1"

uart.init(baudrate=115200)  # the game talks at 115200 (plain uart.init() is too slow)
display.show(VERSION[1])     # shows 1 or 2 on the LEDs

loops = 0
buffer = ""


def messages():
    # uart.readline() can return half a line, so collect bytes until a newline arrives.
    global buffer
    lines = []
    if uart.any():
        data = uart.read()
        if data:
            buffer += str(data, "utf-8")
            while "\n" in buffer:
                line, buffer = buffer.split("\n", 1)
                lines.append(line.strip())
            if len(buffer) > 100:
                buffer = ""
    return lines


while True:
    # Steering: tilt left/right. -100 is full left, 100 is full right.
    # TODO (Task 1): change the sensitivity, add a dead zone, flip the direction.
    steer = max(-100, min(100, accelerometer.get_x() // 5))
    print("st:" + str(steer))

    # Jump: was_pressed() remembers one press.
    # TODO (Task 3): try is_pressed() instead and compare in the serial monitor.
    if button_a.was_pressed():
        print("jp")

    # Brake: send "br" every loop while B is held down.
    if button_b.is_pressed():
        print("br")

    # Say hello about once a second (20 loops x 50 ms).
    loops += 1
    if loops % 20 == 0:
        print("hi:" + VERSION)

    # Messages from the game.
    for msg in messages():
        if msg == "crash":
            display.show(Image.SAD)
            # TODO (Task 2): play a sound when you crash.
        elif msg == "gift":
            display.show(Image.HAPPY)
            # TODO (Task 2): play a sound when you get a gift.
        elif msg == "go":
            display.clear()
        elif msg.startswith("spd:"):
            pass  # TODO (Task 4): show the speed (0-9) as a bar on the LEDs.

    sleep(50)
