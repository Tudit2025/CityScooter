# CityScooter - Task 4 answer: Speed bar
# The game sends "spd:0" to "spd:9". Light up rows of LEDs from the bottom.
# The LEDs are shared: after a crash the bar would cover the sad face, so this
# answer keeps the sad face until the next run starts ("go"). Works on V1 and V2.
from microbit import *

try:
    from microbit import microphone
    VERSION = "v2"
except ImportError:
    VERSION = "v1"

uart.init(baudrate=115200)
display.show(VERSION[1])

loops = 0
buffer = ""
crashed = False


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


def show_speed(level):
    rows = level * 5 // 9  # speed 0-9 becomes 0-5 lit rows
    for y in range(5):
        if y >= 5 - rows:
            brightness = 9   # this row is part of the bar
        else:
            brightness = 0
        for x in range(5):
            display.set_pixel(x, y, brightness)


while True:
    steer = max(-100, min(100, accelerometer.get_x() // 5))
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
            crashed = True
            display.show(Image.SAD)
        elif msg == "gift":
            display.show(Image.HAPPY)
        elif msg == "go":
            crashed = False
            display.clear()
        elif msg.startswith("spd:") and not crashed:
            try:
                level = int(msg[4:])  # the text after "spd:"
            except ValueError:
                level = 0
            show_speed(max(0, min(9, level)))

    sleep(50)
