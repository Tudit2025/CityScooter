# CityScooter - Task 1 answer: Steering
# Sensitivity, a dead zone and a direction switch. Works on V1 and V2.
from microbit import *

try:
    from microbit import microphone
    VERSION = "v2"
except ImportError:
    VERSION = "v1"

SENSITIVITY = 4   # smaller number = turns harder for the same tilt (starter used 5)
DEAD_ZONE = 10    # small tilts count as straight ahead
REVERSED = False  # set to True if the scooter turns the wrong way

uart.init(baudrate=115200)
display.show(VERSION[1])

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
    steer = accelerometer.get_x() // SENSITIVITY
    if abs(steer) < DEAD_ZONE:
        steer = 0
    if REVERSED:
        steer = -steer
    steer = max(-100, min(100, steer))  # keep it in the range the game understands
    print("st:" + str(steer))

    if button_a.was_pressed():
        print("jp")
    if button_b.is_pressed():
        print("br")

    loops += 1
    if loops % 20 == 0:
        print("hi:" + VERSION)

    for msg in messages():
        if msg == "crash":
            display.show(Image.SAD)
        elif msg == "gift":
            display.show(Image.HAPPY)
        elif msg == "go":
            display.clear()

    sleep(50)
