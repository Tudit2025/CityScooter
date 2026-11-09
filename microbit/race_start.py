# CityScooter race starter - for the teacher's micro:bit (V1 and V2)
#   A    count down 3-2-1 on the LEDs, then start the race in the group shown
#   B    next group: 11, 12, 13, 14, 15, then 20 (the final)
#   A+B  start every race at once: groups 11 to 15, one after the other
# Every game in that group then counts down 3-2-1 too, and the race begins.
from microbit import *
import radio
import random

RACES = [11, 12, 13, 14, 15]  # one radio group per race (the cards on the computers)
FINAL = 20                    # the final: the winners of each race
START = "uwa"                 # where races start: uwa, kingspark, cbd, quay, rockingham, ...
METRES = 2000                 # how long a race is (300-5000 m)

uart.init(baudrate=115200)
radio.on()
group = RACES[0]
radio.config(group=group)


def new_go():
    # go:<seed>:<start>:<metres>. The same seed makes the same road in every game.
    return "go:" + str(random.randint(1, 99999)) + ":" + START + ":" + str(METRES)


def send_go(g, go):
    # Say it 5 times in half a second: a radio message can get lost.
    radio.config(group=g)
    for i in range(5):
        radio.send(go)
        sleep(100)
    print("race " + str(g) + ": " + go)


def show_group():
    # 1 then 2 for group 12 (the last digit stays). wait=False: the buttons keep working while it shows,
    # so pressing B quickly never skips a group.
    display.show(str(group), wait=False)


show_group()

while True:
    a = button_a.was_pressed()
    b = button_b.was_pressed()
    if a or b:
        sleep(200)  # both buttons pressed together? give the other one a moment
        a = a or button_a.was_pressed()
        b = b or button_b.was_pressed()
        if a and b:
            # Every race at once, all on the same road, so times can be compared.
            go = new_go()
            for g in RACES:
                display.show(str(g)[-1])
                send_go(g, go)
            radio.config(group=group)  # back to the group shown
            display.show(Image.YES)
        elif a:
            for n in "321":
                display.show(n)
                sleep(1000)
            send_go(group, new_go())
            display.show(Image.YES)
        else:
            choices = RACES + [FINAL]
            group = choices[(choices.index(group) + 1) % len(choices)]
            radio.config(group=group)
            show_group()
    sleep(20)
