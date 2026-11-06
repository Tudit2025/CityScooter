# CityScooter co-op - JUMP board
# Two micro:bits on the same computer: this one only jumps, the other one
# (coop/steer.py) only steers. Connect both on the game page. V1 and V2.
from microbit import *

try:
    from microbit import microphone
    VERSION = "v2"
except ImportError:
    VERSION = "v1"

uart.init(baudrate=115200)
display.show(Image.ARROW_N)  # "I jump"

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
    # No "st:" from this board, so it never fights the steer board.
    if button_a.was_pressed() or button_b.was_pressed():
        print("jp")  # the only command this board sends

    loops += 1
    if loops % 20 == 0:
        print("hi:" + VERSION)

    for msg in messages():
        if msg == "crash":
            display.show(Image.SAD)
        elif msg == "gift":
            display.show(Image.HAPPY)
        elif msg == "go":
            display.show(Image.ARROW_N)

    sleep(50)
