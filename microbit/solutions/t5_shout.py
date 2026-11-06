# CityScooter - Task 5 answer (bonus, V2 only): Shout to jump
# 1. Watch the "sound:" numbers in the serial monitor while the room is normal,
#    then while you shout. 2. Pick a THRESHOLD between them.
# The cooldown stops one long shout from sending many jumps.
# On a V1 (no microphone) the normal controls still work.
from microbit import *

try:
    from microbit import microphone
    VERSION = "v2"
except ImportError:
    VERSION = "v1"

THRESHOLD = 120   # 0 = silent, 255 = very loud. Calibrate this in your room!
COOLDOWN = 1000   # milliseconds between shout jumps

uart.init(baudrate=115200)
display.show(VERSION[1])

loops = 0
buffer = ""
last_shout = -COOLDOWN


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
    print("st:" + str(steer))

    if button_a.was_pressed():
        print("jp")
    if button_b.is_pressed():
        print("br")

    if VERSION == "v2":
        level = microphone.sound_level()
        if loops % 5 == 0:
            print("sound:" + str(level))  # 4 times a second, for calibrating
        if level > THRESHOLD and running_time() - last_shout > COOLDOWN:
            print("jp")
            last_shout = running_time()
    elif loops % 100 == 0:
        print("This task needs a V2 micro:bit")  # every 5 seconds

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
