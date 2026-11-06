# CityScooter - Task 6 answer (bonus): Foil keys
# Wire foil (or a banana) to the big pins with crocodile clips:
#   pin 0 = jump, pin 1 = left, pin 2 = right. No pin touched = tilt steering.
# V1 pins are resistive: hold the GND pin with your other hand while you touch.
# V2 can switch to capacitive touch, which works without holding GND.
# No music here: music also uses pin 0 and would confuse the jump key.
from microbit import *

try:
    from microbit import microphone
    VERSION = "v2"
except ImportError:
    VERSION = "v1"

TURN = 70  # how hard a foil key turns (0-100)

uart.init(baudrate=115200)
display.show(VERSION[1])

if VERSION == "v2":
    pin0.set_touch_mode(pin0.CAPACITIVE)
    pin1.set_touch_mode(pin1.CAPACITIVE)
    pin2.set_touch_mode(pin2.CAPACITIVE)
else:
    print("V1: hold GND with your other hand when you touch a pin")

loops = 0
buffer = ""
jump_was_touched = False


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
    if pin1.is_touched():
        steer = -TURN
    elif pin2.is_touched():
        steer = TURN
    else:
        steer = max(-100, min(100, accelerometer.get_x() // 5))
    print("st:" + str(steer))

    # Jump once when pin 0 is first touched (is_touched() is "right now",
    # so remember the last loop to turn it into an event).
    jump_touched = pin0.is_touched()
    if jump_touched and not jump_was_touched:
        print("jp")
    jump_was_touched = jump_touched

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
