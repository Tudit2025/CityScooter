"""A fake micro:bit with a MicroPython raw REPL, on stdin/stdout, for send.test.js ("Send to micro:bit").

    python fake_repl_board.py <outdir> <id> [--no-repl] [--full]

Running: prints hi:v2 and st:0 (st:7 after a new main.py was sent) like a student program. Ctrl-C stops it and
shows the >>> REPL; Ctrl-A enters the raw REPL; each command up to Ctrl-D is executed by CPython with a fake
open() and a fake microbit module, and answered as MicroPython does: OK <stdout> 0x04 <stderr> 0x04 >.
microbit.reset() restarts the program. main.py is saved to <outdir>/<id>_main.py, raw commands to <id>_log.txt.
--no-repl: behaves like a MakeCode program (ignores Ctrl-C). --full: writing main.py fails with OSError 28.
"""
import io
import os
import sys
import threading
import time
import traceback
import types
import contextlib

OUT, BID = sys.argv[1], sys.argv[2]
NO_REPL = "--no-repl" in sys.argv
FULL = "--full" in sys.argv

lock = threading.Lock()
state = "running"          # running | repl | raw
program = "old"            # old | new
fs = {}
log = open(os.path.join(OUT, BID + "_log.txt"), "w", encoding="utf-8")


def out(text):
    with lock:
        sys.stdout.buffer.write(text.encode("utf-8") if isinstance(text, str) else text)
        sys.stdout.buffer.flush()


def runner():
    n = 0
    while True:
        time.sleep(0.1)
        if state == "running":
            n += 1
            out("st:%d\r\n" % (7 if program == "new" else 0))
            if n % 10 == 1:
                out("hi:v2\r\n")


class Reset(Exception):
    pass


class FakeFile:
    def __init__(self, name, mode):
        self.name, self.mode = name, mode
        if "w" in mode:
            self.buf = io.BytesIO()
        else:
            if name not in fs:
                raise OSError(2)
            self.buf = io.BytesIO(fs[name])

    def write(self, b):
        if FULL:
            raise OSError(28)
        return self.buf.write(b)

    def read(self, n=-1):
        return self.buf.read(n)

    def close(self):
        if "w" in self.mode:
            fs[self.name] = self.buf.getvalue()
            with open(os.path.join(OUT, BID + "_main.py"), "wb") as f:
                f.write(fs[self.name])


microbit = types.ModuleType("microbit")


def _reset():
    raise Reset()


microbit.reset = _reset
sys.modules["microbit"] = microbit
ns = {"open": FakeFile}


def execute(src):
    global state, program
    log.write(repr(src) + "\n")
    log.flush()
    out("OK")
    so, se = io.StringIO(), io.StringIO()
    try:
        with contextlib.redirect_stdout(so):
            exec(compile(src, "<stdin>", "exec"), ns)
    except Reset:
        state, program = "running", ("new" if "main.py" in fs else "old")
        return
    except Exception as e:
        se.write("Traceback (most recent call last):\n  File \"<stdin>\", line 1, in <module>\n")
        se.write("%s: %s\n" % (type(e).__name__, e))
    out(so.getvalue().replace("\n", "\r\n") + "\x04" + se.getvalue().replace("\n", "\r\n") + "\x04>")


def main():
    global state
    threading.Thread(target=runner, daemon=True).start()
    cmd = bytearray()
    while True:
        data = os.read(0, 4096)
        if not data:
            return
        for c in data:
            if state == "running":
                if c == 3 and not NO_REPL:
                    state = "repl"
                    out("Traceback (most recent call last):\r\n  File \"main.py\", line 40, in <module>\r\n"
                        "KeyboardInterrupt: \r\nMicroPython v2.1.2 on 2023-10-11; micro:bit v2.0.0 with nRF52833\r\n"
                        "Type \"help()\" for more information.\r\n>>> ")
                else:
                    log.write("program got %r\n" % chr(c))
            elif state == "repl":
                if c == 1:
                    state = "raw"
                    cmd.clear()
                    out("\r\nraw REPL; CTRL-B to exit\r\n>")
                elif c == 4:
                    state = "running"
                elif c == 3:
                    out("\r\nKeyboardInterrupt\r\n>>> ")
            elif state == "raw":
                if c == 4:
                    src = cmd.decode("utf-8")
                    cmd.clear()
                    if not src:
                        state = "running"  # soft reset
                    else:
                        execute(src)
                elif c == 2:
                    state = "repl"
                    out("\r\nMicroPython v2.1.2\r\nType \"help()\" for more information.\r\n>>> ")
                elif c == 3:
                    cmd.clear()
                elif c == 1:
                    cmd.clear()
                    out("\r\nraw REPL; CTRL-B to exit\r\n>")
                else:
                    cmd.append(c)


try:
    main()
except Exception:
    traceback.print_exc(file=log)
