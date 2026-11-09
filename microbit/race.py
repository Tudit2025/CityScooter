# CityScooter race - the Lesson 1 starter plus a radio race relay
# Works on micro:bit V1 and V2. Everyone in your race uses the same GROUP
# (look at the card on your computer) and a different NAME.
from microbit import *
import radio

GROUP = 12      # your race: the number on the card on your computer (0-255)
NAME = "AMY"    # your name in the race: 1-5 letters or digits, all different

# Which micro:bit is this? Only V2 has a microphone.
try:
    from microbit import microphone
    VERSION = "v2"
except ImportError:
    VERSION = "v1"

uart.init(baudrate=115200)  # the game talks at 115200 (plain uart.init() is too slow)
display.show(VERSION[1])     # shows 1 or 2 on the LEDs

# Radio: only micro:bits in the same group hear each other.
radio.on()
radio.config(group=GROUP, queue=10)  # keep up to 10 messages (the default 3 is too few for a race)

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
    steer = max(-100, min(100, accelerometer.get_x() // 5))
    print("st:" + str(steer))

    # Jump: was_pressed() remembers one press.
    if button_a.was_pressed():
        print("jp")

    # Brake: send "br" every loop while B is held down.
    if button_b.is_pressed():
        print("br")

    # About once a second (20 loops x 50 ms): say hello, and that you want to race.
    loops += 1
    if loops % 20 == 0:
        print("hi:" + VERSION)
        print("race:" + NAME + ":" + str(GROUP))

    # Messages from the game.
    for msg in messages():
        if msg == "crash":
            display.show(Image.SAD)
        elif msg == "gift":
            display.show(Image.HAPPY)
        elif msg == "go":
            display.clear()
        elif msg.startswith("pos:"):
            # pos:1234:-2 = you are 1234 m along, in lane -2. Tell everyone in your race.
            radio.send("p:" + NAME + ":" + msg[4:])
        elif msg.startswith("fin:"):
            # fin:72.35 = you crossed the finish line in 72.35 seconds.
            radio.send("f:" + NAME + ":" + msg[4:])

    # Radio: pass everything you heard to the game, so it can draw the others.
    # Read until the queue is empty: if it fills up, new messages are lost.
    while True:
        try:
            heard = radio.receive()
        except ValueError:  # not text (e.g. sent by a MakeCode program): skip it
            continue
        if not heard:
            break
        print("rx:" + heard)

    sleep(50)
