# CityScooter co-op - STEER board
# Two micro:bits on the same computer: this one only steers, the other one
# (coop/jump.py) only jumps. Connect both on the game page. V1 and V2.
from microbit import *

try:
    from microbit import microphone
    VERSION = "v2"
except ImportError:
    VERSION = "v1"

uart.init(baudrate=115200)
display.show(Image.ARROW_W)  # "I steer"

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
    steer = max(-100, min(100, accelerometer.get_x() // 5))
    print("st:" + str(steer))  # the only command this board sends

    loops += 1
    if loops % 20 == 0:
        print("hi:" + VERSION)

    # Both boards hear every game message.
    for msg in messages():
        if msg == "crash":
            display.show(Image.SAD)
        elif msg == "gift":
            display.show(Image.HAPPY)
        elif msg == "go":
            display.show(Image.ARROW_W)

    sleep(50)
