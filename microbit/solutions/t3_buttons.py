# CityScooter - Task 3 answer: Buttons
# B brakes while held. A jumps, written two ways:
#   was_pressed() remembers a press  -> one "jp" per press (an event)
#   is_pressed()  is only "right now" -> "jp" every loop while held (polling)
# Hold A in each mode and compare the jp count in the serial monitor.
from microbit import *

try:
    from microbit import microphone
    VERSION = "v2"
except ImportError:
    VERSION = "v1"

USE_POLLING = False  # False = was_pressed(), True = is_pressed()

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
    steer = max(-100, min(100, accelerometer.get_x() // 5))
    print("st:" + str(steer))

    if USE_POLLING:
        if button_a.is_pressed():
            print("jp")  # about 20 per second while A is held
    else:
        if button_a.was_pressed():
            print("jp")  # one per press, even a very quick one

    # Brake has to be "right now": the game brakes for 0.3 s per "br".
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
