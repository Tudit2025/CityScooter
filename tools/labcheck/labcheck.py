# CityScooter lab check - works on micro:bit V1 and V2
from microbit import *
import music

try:
    from microbit import microphone
    VER = "v2"
except ImportError:
    VER = "v1"

uart.init(baudrate=115200)
display.show(VER[1])
n = 0
buf = ""


def messages():
    # uart.readline() can return half a line, so collect bytes until a newline arrives.
    global buf
    out = []
    if uart.any():
        data = uart.read()
        if data:
            buf += str(data, "utf-8")
            while "\n" in buf:
                line, buf = buf.split("\n", 1)
                out.append(line.strip())
            if len(buf) > 100:
                buf = ""
    return out


while True:
    steer = max(-100, min(100, accelerometer.get_x() // 5))
    print("st:" + str(steer))
    if button_a.was_pressed():
        print("jp")
    if button_b.is_pressed():
        print("br")
    n += 1
    if n % 20 == 0:
        print("hi:" + VER)
    for msg in messages():
        print("echo:" + msg)
        if msg == "crash":
            display.show(Image.SAD)
            music.play(music.WAWAWAWAA, wait=False)
        elif msg == "gift":
            display.show(Image.HAPPY)
            music.play(music.BA_DING, wait=False)
    sleep(50)
