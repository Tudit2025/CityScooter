# Web shell tests (headless Chrome)

Uses the installed Google Chrome (`C:/Program Files/Google/Chrome/Application/chrome.exe`) through puppeteer-core,
and the real official MicroPython simulator (needs internet). Screenshots go to `out/`.

    cd tools/shell-tests
    npm install
    mkdir out
    npm run shell    # tools/shell-dev: modes, fake Web Serial (USB), simulator, serial monitor, help
    npm run blocks   # Blocks editor: generated MicroPython (CPython syntax check), runs in the simulator on V2 and forced V1
    npm run send     # "Send to micro:bit": Web Serial bridged to fake MicroPython boards (fake_repl_board.py, needs python)
    npm run race     # radio race: microbit/race.py and race_start.py compile and run in the simulator; robot racers;
                     # the simulator's radio_input format (\x01\x00\x01 header)
    npm run build    # a Unity Web build in Builds/Web: Unity loads, USB and Simulator rides, messages back to devices
                     # or: node build.test.js https://cityscooter.vercel.app ../labcheck/labcheck.py out

`blocks` needs `python` on PATH for the syntax check. What each test covers: docs/blocks.md ("测试").
