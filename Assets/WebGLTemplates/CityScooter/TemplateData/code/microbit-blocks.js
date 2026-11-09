/* micro:bit blocks -> MicroPython, for game shells (Claude, 2026-11-07). Game-agnostic: a game adds its own
 * blocks with MicrobitBlocks.registerPack() (see game-cityscooter.js).
 *
 * Needs Blockly (blockly_compressed.js, blocks_compressed.js, python_compressed.js, msg/en.js) loaded first.
 *
 * Program shape (every program the blocks make is real, readable MicroPython that runs on V1 and V2):
 *   from microbit import *         + imports the blocks need (music, random)
 *   VERSION = "v1" / "v2"          (V2-only blocks check it, so a V1 never crashes)
 *   uart.init(baudrate=115200)     (the game talks at 115200)
 *   variables, pack globals and helpers
 *   radio.on() / radio.config(group=radio_group, ...) (only with radio blocks)
 *   "on start" blocks
 *   while True:
 *       "forever" blocks, "when button pressed" blocks, the pack's loop tail (game messages)
 *       sleep(50)
 */
(() => {
  "use strict";
  const B = window.Blockly;
  const PY = window.python;
  if (!B || !PY) throw new Error("microbit-blocks.js needs Blockly and its Python generator loaded first");
  const gen = PY.pythonGenerator;
  const Order = PY.Order;
  gen.INDENT = "    "; // Python style taught in class (Blockly's default is 2 spaces)

  // ------------------------------------------------------------------ colours (comic palette from ComicCityRecipe)
  const COLOURS = {
    events: "#30738c", input: "#7a5f96", led: "#2f7d7d", sound: "#c4532f", basic: "#5a6b80",
    logic: "#4a7f3a", loops: "#a3542a", math: "#8a6a1f", text: "#7d6e58", variables: "#9b4f8c", game: "#e81f26",
    radio: "#b8407a",
  };

  const IMAGES = ["HAPPY", "SAD", "HEART", "SMILE", "CONFUSED", "ANGRY", "SURPRISED", "ASLEEP", "YES", "NO",
    "ARROW_N", "ARROW_E", "ARROW_S", "ARROW_W", "TRIANGLE", "SQUARE", "DIAMOND", "SKULL", "GHOST", "PACMAN", "DUCK", "TARGET"];
  const MELODIES = ["BA_DING", "WAWAWAWAA", "POWER_UP", "POWER_DOWN", "JUMP_UP", "JUMP_DOWN", "PUNCHLINE", "BADDY",
    "CHASE", "DADADADUM", "ENTERTAINER", "NYAN", "RINGTONE", "BIRTHDAY"];
  const nice = (s) => s.toLowerCase().replace(/_/g, " ");
  const HOLD_TIMES = [["0.5", "500"], ["1", "1000"], ["2", "2000"], ["3", "3000"]];
  const GESTURES = [["shaken", "shake"], ["turned face up", "face up"], ["turned face down", "face down"]];

  // ------------------------------------------------------------------ generic blocks
  const BLOCKS = [
    { type: "mb_on_start", message0: "on start %1 %2", args0: [{ type: "input_dummy" }, { type: "input_statement", name: "DO" }],
      style: "mb_events", tooltip: "Runs once when the micro:bit starts." },
    { type: "mb_forever", message0: "forever %1 %2", args0: [{ type: "input_dummy" }, { type: "input_statement", name: "DO" }],
      style: "mb_events", tooltip: "Runs again and again, about 20 times a second." },
    { type: "mb_on_button", message0: "when button %1 is pressed %2 %3",
      args0: [{ type: "field_dropdown", name: "BTN", options: [["A", "a"], ["B", "b"]] }, { type: "input_dummy" }, { type: "input_statement", name: "DO" }],
      style: "mb_events", tooltip: "Runs once for each press (Python: button_a.was_pressed())." },
    { type: "mb_on_presses", message0: "when button %1 is pressed %2 times in a row %3 %4",
      args0: [{ type: "field_dropdown", name: "BTN", options: [["A", "a"], ["B", "b"]] },
        { type: "field_dropdown", name: "TIMES", options: [["2", "2"], ["3", "3"], ["4", "4"], ["5", "5"]] },
        { type: "input_dummy" }, { type: "input_statement", name: "DO" }],
      style: "mb_events", tooltip: "Runs on the 2nd (3rd, ...) quick press in a row: each press less than 0.4 s after the one before. Look at the Python: it counts presses and remembers when the last one was (running_time)." },
    { type: "mb_broadcast", message0: "broadcast %1", args0: [{ type: "field_input", name: "NAME", text: "message1" }],
      previousStatement: null, nextStatement: null, style: "mb_events",
      tooltip: "Send a message to your own program: every \"when I receive\" block with the same name runs (later in this loop). Like broadcast in Scratch." },
    { type: "mb_on_receive", message0: "when I receive %1 %2 %3",
      args0: [{ type: "field_input", name: "NAME", text: "message1" }, { type: "input_dummy" }, { type: "input_statement", name: "DO" }],
      style: "mb_events", tooltip: "Runs when your program broadcasts this message. Several of these can listen to the same message." },
    { type: "mb_radio_group", message0: "radio: set group %1", args0: [{ type: "input_value", name: "GROUP", check: "Number" }],
      previousStatement: null, nextStatement: null, style: "mb_radio",
      tooltip: "micro:bits only hear each other in the same group (0-255). Put it in \"on start\". Pick a group number that no other team uses." },
    { type: "mb_radio_send", message0: "radio: send %1", args0: [{ type: "input_value", name: "VALUE" }],
      previousStatement: null, nextStatement: null, style: "mb_radio",
      tooltip: "Broadcast a short text by radio to every micro:bit in your group (it is not sent to the game). Python: radio.send(...)" },
    { type: "mb_on_radio", message0: "when radio receives a message %1 %2", args0: [{ type: "input_dummy" }, { type: "input_statement", name: "DO" }],
      style: "mb_radio", tooltip: "Runs when another micro:bit in your group sends something. Use \"radio message\" to see what it was." },
    { type: "mb_radio_msg", message0: "radio message", output: "String", style: "mb_radio",
      tooltip: "The last text received by radio (empty before the first one)." },
    { type: "mb_on_hold", message0: "when button %1 is held down for %2 seconds %3 %4",
      args0: [{ type: "field_dropdown", name: "BTN", options: [["A", "a"], ["B", "b"]] }, { type: "field_dropdown", name: "MS", options: HOLD_TIMES },
        { type: "input_dummy" }, { type: "input_statement", name: "DO" }],
      style: "mb_events", tooltip: "Runs once when the button has been held down this long (it does not repeat until you let go and press again). Look at the Python: it remembers when the press started (running_time)." },
    { type: "mb_on_release", message0: "when button %1 is released %2 %3",
      args0: [{ type: "field_dropdown", name: "BTN", options: [["A", "a"], ["B", "b"]] }, { type: "input_dummy" }, { type: "input_statement", name: "DO" }],
      style: "mb_events", tooltip: "Runs when you let go of the button. \"how long button A was held\" then tells you how long it was down: charge up, then let go!" },
    { type: "mb_held", message0: "how long button %1 is held (ms)",
      args0: [{ type: "field_dropdown", name: "BTN", options: [["A", "a"], ["B", "b"]] }],
      output: "Number", style: "mb_input", tooltip: "Milliseconds the button has been held down: grows while you hold it, 0 before the first press. After you let go it keeps the length of that last hold until the next press." },
    { type: "mb_presses", message0: "presses of button %1 in a row",
      args0: [{ type: "field_dropdown", name: "BTN", options: [["A", "a"], ["B", "b"]] }],
      output: "Number", style: "mb_input", tooltip: "How many quick presses in a row so far: 1, 2, 3, ... Starts again at 1 after a pause of 0.4 s. Even or odd? Use the remainder of dividing by 2." },
    { type: "mb_on_gesture", message0: "when %1 %2 %3",
      args0: [{ type: "field_dropdown", name: "GESTURE", options: GESTURES }, { type: "input_dummy" }, { type: "input_statement", name: "DO" }],
      style: "mb_events", tooltip: "Runs once each time the micro:bit is moved like this (Python: accelerometer.was_gesture(\"shake\"))." },
    { type: "mb_button_pressed", message0: "button %1 is pressed",
      args0: [{ type: "field_dropdown", name: "BTN", options: [["A", "a"], ["B", "b"], ["A+B", "ab"]] }],
      output: "Boolean", style: "mb_input", tooltip: "True while the button is held down right now (Python: button_a.is_pressed())." },
    { type: "mb_tilt", message0: "tilt %1",
      args0: [{ type: "field_dropdown", name: "AXIS", options: [["left / right (x)", "x"], ["forward / back (y)", "y"]] }],
      output: "Number", style: "mb_input", tooltip: "Accelerometer: about -1000 (tilted left) to 1000 (tilted right); 0 when flat." },
    { type: "mb_pin_touched", message0: "pin %1 is touched",
      args0: [{ type: "field_dropdown", name: "PIN", options: [["0", "0"], ["1", "1"], ["2", "2"]] }],
      output: "Boolean", style: "mb_input", tooltip: "Touch the pin (or foil wired to it). Hold GND with your other hand." },
    { type: "mb_logo_touched", message0: "logo is touched (V2)", output: "Boolean", style: "mb_input",
      tooltip: "V2 only. On a V1 this is always false." },
    { type: "mb_sound_level", message0: "sound level (V2)", output: "Number", style: "mb_input",
      tooltip: "V2 microphone, 0 (quiet) to 255 (loud). On a V1 this is always 0." },
    { type: "mb_version", message0: "board version", output: "String", style: "mb_input", tooltip: "\"v1\" or \"v2\"." },
    { type: "mb_running_time", message0: "running time (ms)", output: "Number", style: "mb_input",
      tooltip: "Milliseconds since the program started." },
    { type: "mb_show_icon", message0: "show icon %1",
      args0: [{ type: "field_dropdown", name: "ICON", options: IMAGES.map((i) => [nice(i), i]) }],
      previousStatement: null, nextStatement: null, style: "mb_led", tooltip: "Show a picture on the LEDs." },
    { type: "mb_show", message0: "show %1", args0: [{ type: "input_value", name: "VALUE" }],
      previousStatement: null, nextStatement: null, style: "mb_led",
      tooltip: "Show a number or text on the LEDs without pausing your program (no scrolling: scroll pauses steering)." },
    { type: "mb_clear", message0: "clear the LEDs", previousStatement: null, nextStatement: null, style: "mb_led", tooltip: "Turn all LEDs off." },
    { type: "mb_plot", message0: "light LED x %1 y %2 brightness %3",
      args0: [{ type: "input_value", name: "X", check: "Number" }, { type: "input_value", name: "Y", check: "Number" },
        { type: "input_value", name: "B", check: "Number" }],
      inputsInline: true, previousStatement: null, nextStatement: null, style: "mb_led",
      tooltip: "x and y go from 0 to 4 (top-left is 0, 0). Brightness 0 (off) to 9." },
    { type: "mb_play", message0: "play sound %1",
      args0: [{ type: "field_dropdown", name: "TUNE", options: MELODIES.map((m) => [nice(m), m]) }],
      previousStatement: null, nextStatement: null, style: "mb_sound",
      tooltip: "Plays without pausing your program. A V2 has a speaker; a V1 needs headphones or a speaker on pin 0." },
    { type: "mb_stop_sound", message0: "stop sound", previousStatement: null, nextStatement: null, style: "mb_sound", tooltip: "Stop the tune." },
    { type: "mb_pause", message0: "pause %1 ms", args0: [{ type: "input_value", name: "MS", check: "Number" }],
      previousStatement: null, nextStatement: null, style: "mb_basic",
      tooltip: "Wait. Careful: while it waits, nothing else happens — long pauses make steering slow." },
    { type: "mb_print", message0: "print %1", args0: [{ type: "input_value", name: "VALUE" }],
      previousStatement: null, nextStatement: null, style: "mb_basic",
      tooltip: "Send a line to the serial monitor. The game ignores lines it does not know." },
  ];

  // ------------------------------------------------------------------ program context used by hat generators
  // Hat blocks put their code in named sections instead of returning it; assemble() lays the program out.
  let ctx = null;
  function newContext() {
    return { sections: Object.create(null), flags: Object.create(null) };
  }
  function addSection(key, code) {
    if (!code || !code.trim()) return;
    (ctx.sections[key] = ctx.sections[key] || []).push(code);
  }
  const indent = (code, n) => code.replace(/^(?=.)/gm, " ".repeat(n));
  const dedent4 = (code) => code.replace(/^ {4}/gm, "");
  const body = (block, name) => gen.statementToCode(block, name); // indented by 4

  // ------------------------------------------------------------------ generic generators
  const G = gen.forBlock;
  G.mb_on_start = (b) => { addSection("start", body(b, "DO")); return ""; };
  G.mb_forever = (b) => { addSection("loop", body(b, "DO")); return ""; };
  G.mb_on_button = (b) => { addSection("button:" + b.getFieldValue("BTN"), body(b, "DO")); return ""; };
  // Quick presses in a row: counted in the main loop (see "presses" in generate()).
  const countPresses = (btn, g) => {
    ctx.flags["presses_" + btn] = true;
    g.definitions_["presses_" + btn] = "presses_" + btn + " = 0  # quick presses of button " + btn.toUpperCase() + " in a row\nlast_press_" + btn + " = -1000  # when it was last pressed (ms)";
  };
  // Scratch-style broadcasts inside one program: a list of messages handled at the end of each loop.
  const pyString = (s) => '"' + String(s).replace(/\\/g, "\\\\").replace(/"/g, '\\"') + '"';
  const useBroadcasts = (g) => { ctx.flags.broadcasts = true; g.definitions_.broadcasts = "broadcasts = []  # messages your blocks broadcast, handled at the end of each loop"; };
  G.mb_broadcast = (b, g) => { useBroadcasts(g); return "broadcasts.append(" + pyString(b.getFieldValue("NAME")) + ")\n"; };
  G.mb_on_receive = (b, g) => { useBroadcasts(g); addSection("receive:" + b.getFieldValue("NAME"), body(b, "DO")); return ""; };
  // Radio between micro:bits (V1 and V2). One radio_group variable for every block that sets the group ("radio: set
  // group", a pack's "join race"), so the last one wins and the program can print the group it is really in.
  // queue: how many messages the radio keeps until the program reads them (MicroPython's default is 3; a race needs 10).
  const useRadio = (g, queue) => {
    ctx.flags.radio = true;
    if (queue) ctx.flags.radioQueue = Math.max(ctx.flags.radioQueue || 0, queue);
    g.definitions_.import_radio = "import radio";
  };
  G.mb_radio_group = (b, g) => { useRadio(g); return "radio_group = int(" + (g.valueToCode(b, "GROUP", Order.NONE) || "1") + ")\nradio.config(group=radio_group)\n"; };
  G.mb_radio_send = (b, g) => { useRadio(g); return "radio.send(str(" + (g.valueToCode(b, "VALUE", Order.NONE) || '""') + "))\n"; };
  G.mb_on_radio = (b, g) => { useRadio(g); ctx.flags.radioMsg = true; addSection("radio", body(b, "DO")); return ""; };
  G.mb_radio_msg = (b, g) => { useRadio(g); ctx.flags.radioMsg = true; return ["radio_msg", Order.ATOMIC]; };

  // Holding a button: tracked in the main loop (see "held" in generate()).
  const trackHold = (btn, g) => {
    ctx.flags["held_" + btn] = true;
    g.definitions_["held_" + btn] = "held_since_" + btn + " = -1  # when button " + btn.toUpperCase() + " went down (ms), -1 while it is up\n"
      + "held_" + btn + " = 0  # how long it is (or last was) held down (ms)\nheld_done_" + btn + " = 0  # longest hold block already run for this press";
  };
  G.mb_on_hold = (b, g) => { const btn = b.getFieldValue("BTN"); trackHold(btn, g); addSection("hold:" + btn + ":" + b.getFieldValue("MS"), body(b, "DO")); return ""; };
  G.mb_on_release = (b, g) => { const btn = b.getFieldValue("BTN"); trackHold(btn, g); addSection("release:" + btn, body(b, "DO")); return ""; };
  G.mb_held = (b, g) => { const btn = b.getFieldValue("BTN"); trackHold(btn, g); return ["held_" + btn, Order.ATOMIC]; };
  // Blockly's "change [variable] by" imports numbers.Number, which MicroPython does not have (ImportError on
  // every micro:bit). Same meaning without it: a variable that is not a number yet counts from 0.
  G.math_change = (b, g) => {
    const v = g.getVariableName(b.getFieldValue("VAR"));
    const d = g.valueToCode(b, "DELTA", Order.ADDITIVE) || "0";
    return v + " = (" + v + " if type(" + v + ") in (int, float) else 0) + " + d + "\n";
  };
  G.mb_on_presses = (b, g) => {
    const btn = b.getFieldValue("BTN");
    countPresses(btn, g);
    addSection("presses:" + btn + ":" + b.getFieldValue("TIMES"), body(b, "DO"));
    return "";
  };
  G.mb_presses = (b, g) => { const btn = b.getFieldValue("BTN"); countPresses(btn, g); return ["presses_" + btn, Order.ATOMIC]; };
  G.mb_on_gesture = (b) => { addSection("gesture:" + b.getFieldValue("GESTURE"), body(b, "DO")); return ""; };
  G.mb_button_pressed = (b) => {
    const v = b.getFieldValue("BTN");
    return v === "ab" ? ["(button_a.is_pressed() and button_b.is_pressed())", Order.ATOMIC] : ["button_" + v + ".is_pressed()", Order.FUNCTION_CALL];
  };
  G.mb_tilt = (b) => ["accelerometer.get_" + b.getFieldValue("AXIS") + "()", Order.FUNCTION_CALL];
  G.mb_pin_touched = (b) => ["pin" + b.getFieldValue("PIN") + ".is_touched()", Order.FUNCTION_CALL];
  G.mb_logo_touched = () => ['(pin_logo.is_touched() if VERSION == "v2" else False)', Order.ATOMIC];
  G.mb_sound_level = () => ['(microphone.sound_level() if VERSION == "v2" else 0)', Order.ATOMIC];
  G.mb_version = () => ["VERSION", Order.ATOMIC];
  G.mb_running_time = () => ["running_time()", Order.FUNCTION_CALL];
  G.mb_show_icon = (b) => "display.show(Image." + b.getFieldValue("ICON") + ")\n";
  G.mb_show = (b, g) => "display.show(str(" + (g.valueToCode(b, "VALUE", Order.NONE) || '""') + "), wait=False)\n";
  G.mb_clear = () => "display.clear()\n";
  G.mb_plot = (b, g) => {
    const v = (n) => "int(" + (g.valueToCode(b, n, Order.NONE) || "0") + ")";
    return "display.set_pixel(" + v("X") + ", " + v("Y") + ", " + v("B") + ")\n";
  };
  // Sound normally drives pin 0, and then pin0.is_touched() fails ("Pin 0 in music mode"). A V2 plays on its own
  // speaker with pin=None, which keeps pin 0 free for touch; a V1 has no speaker, so its sound must use pin 0.
  const soundSetup = (g) => {
    g.definitions_.import_music = "import music";
    g.definitions_.sound_pin = 'SOUND_PIN = pin0 if VERSION == "v1" else None  # V2: speaker only, so pin 0 stays free for touch';
  };
  G.mb_play = (b, g) => { soundSetup(g); return "music.play(music." + b.getFieldValue("TUNE") + ", pin=SOUND_PIN, wait=False)\n"; };
  G.mb_stop_sound = (b, g) => { soundSetup(g); return "music.stop(SOUND_PIN)\n"; };
  G.mb_pause = (b, g) => "sleep(int(" + (g.valueToCode(b, "MS", Order.NONE) || "100") + "))\n";
  G.mb_print = (b, g) => "print(" + (g.valueToCode(b, "VALUE", Order.NONE) || '""') + ")\n";

  // Student variables must not reuse these names. Blockly builds its name table on the first init(), so the
  // words are added now and in registerPack(), never later.
  gen.addReservedWords("VERSION,SOUND_PIN,microbit,microphone,display,button_a,button_b,accelerometer,pin0,pin1,pin2,pin_logo,Image,music," +
    "uart,sleep,running_time,random,speaker,audio,Sound,radio,radio_msg,radio_group,got_radio,broadcasts,got,handled");

  // ------------------------------------------------------------------ packs
  let pack = null;

  function registerPack(p) {
    pack = p;
    if (p.reserved) gen.addReservedWords(p.reserved);
    B.common.defineBlocksWithJsonArray(p.blocks || []);
    Object.assign(G, p.generators || {});
  }

  // ------------------------------------------------------------------ program assembly
  function generate(workspace) {
    ctx = newContext();
    gen.init(workspace);
    for (const top of workspace.getTopBlocks(true)) {
      if (!top.isEnabled()) continue;
      if (!/^(mb_on_|mb_forever)/.test(top.type) && !(pack && pack.isHat && pack.isHat(top.type))) continue; // loose blocks do nothing
      gen.blockToCode(top);
    }
    const defs = Object.entries(gen.definitions_);
    const imports = defs.filter(([, v]) => /^(import|from)\s/.test(v)).map(([, v]) => v);
    const variables = defs.filter(([k]) => k === "variables").map(([, v]) => v);
    const helpers = defs.filter(([k, v]) => k !== "variables" && !/^(import|from)\s/.test(v)).map(([, v]) => v);
    const c = ctx;
    const s = (key) => (c.sections[key] || []).join("");
    const L = [];
    L.push("# Made with blocks" + (pack ? " in " + pack.name : "") + ". This is real MicroPython: you can also flash it from python.microbit.org.");
    L.push("from microbit import *");
    for (const i of imports) L.push(i);
    L.push("", "# Which micro:bit is this? Only V2 has a microphone.", "try:", "    from microbit import microphone",
      '    VERSION = "v2"', "except ImportError:", '    VERSION = "v1"', "",
      "uart.init(baudrate=115200)  # the game talks at 115200");
    const radioGlobals = !c.flags.radio ? [] : ["radio_group = 1  # micro:bits only hear others in the same group (0-255)",
      c.flags.radioMsg ? 'radio_msg = ""  # the last text received by radio' : null];
    const globals = [...variables, ...radioGlobals, ...(pack && pack.globals ? pack.globals(c) : [])].filter(Boolean);
    if (globals.length) L.push("", ...globals);
    for (const h of [...helpers, ...(pack && pack.helpers ? pack.helpers(c) : [])]) L.push("", "", h.replace(/\n+$/, ""));
    L.push("", "");
    if (c.flags.radio) {
      L.push("radio.on()  # micro:bits in the same group hear each other", c.flags.radioQueue
        ? "radio.config(group=radio_group, queue=" + c.flags.radioQueue + ")  # keep up to " + c.flags.radioQueue + " messages until the loop reads them (3 is too few for a race)"
        : "radio.config(group=radio_group)");
    }
    L.push("display.show(VERSION[1])  # 1 or 2 on the LEDs");
    if (s("start")) L.push("# on start", dedent4(s("start")).replace(/\n$/, ""));
    L.push("", "while True:");
    const loopParts = [];
    if (s("loop")) loopParts.push(s("loop"));
    for (const btn of ["a", "b"]) {
      const one = s("button:" + btn), counted = c.flags["presses_" + btn];
      if (!one && !counted) continue;
      let code = "    if button_" + btn + ".was_pressed():\n";
      if (counted) {
        const n = "presses_" + btn, last = "last_press_" + btn;
        code += "        if running_time() - " + last + " < 400:  # less than 0.4 s since the last press\n"
          + "            " + n + " += 1\n        else:\n            " + n + " = 1\n"
          + "        " + last + " = running_time()\n";
      }
      code += indent(one, 4);
      for (const times of ["2", "3", "4", "5"]) {
        const many = s("presses:" + btn + ":" + times);
        if (many) code += "        if presses_" + btn + " == " + times + ":\n" + indent(many, 8);
      }
      loopParts.push(code);
    }
    for (const btn of ["a", "b"]) {
      if (!c.flags["held_" + btn]) continue;
      const since = "held_since_" + btn, held = "held_" + btn, done = "held_done_" + btn;
      let code = "    if button_" + btn + ".is_pressed():\n"
        + "        if " + since + " < 0:  # it just went down\n"
        + "            " + since + " = running_time()\n"
        + "            " + done + " = 0\n"
        + "        " + held + " = running_time() - " + since + "\n";
      for (const [, ms] of HOLD_TIMES) {
        const hold = s("hold:" + btn + ":" + ms);
        if (hold) code += "        if " + held + " >= " + ms + " and " + done + " < " + ms + ":  # held for " + (ms / 1000) + " s\n"
          + "            " + done + " = " + ms + "\n" + indent(hold, 8);
      }
      code += "    elif " + since + " >= 0:  # it was just let go\n"
        + "        " + since + " = -1\n" + (indent(s("release:" + btn), 4) || "");
      loopParts.push(code);
    }
    for (const [, g] of GESTURES) {
      if (s("gesture:" + g)) loopParts.push('    if accelerometer.was_gesture("' + g + '"):\n' + indent(s("gesture:" + g), 4));
    }
    // A pack can look at every radio message too (CityScooter: "pass radio messages to the game"). Then the loop reads
    // the whole queue each time, so nothing piles up; otherwise one message a loop, as before.
    // radio.receive() raises ValueError for a packet that is not text (a MakeCode program, send_bytes) and drops that
    // packet first (V1 and V2), so skipping it is safe: one stray packet must not stop every board in the group.
    const NOT_TEXT = "  # not text (e.g. sent by a MakeCode program): skip it\n";
    const radioEach = c.flags.radio && pack && pack.radioEach ? pack.radioEach(c) : "";
    if (radioEach) {
      loopParts.push("    # radio from other micro:bits: read everything that arrived, every loop\n    while True:\n"
        + "        try:\n            got_radio = radio.receive()\n        except ValueError:" + NOT_TEXT + "            continue\n"
        + "        if not got_radio:\n            break\n"
        + indent(radioEach, 8) + (c.flags.radioMsg ? "        radio_msg = got_radio\n" + indent(s("radio"), 4) : ""));
    } else if (c.flags.radioMsg) {
      loopParts.push("    # radio from other micro:bits\n    try:\n        got_radio = radio.receive()\n    except ValueError:" + NOT_TEXT
        + "        got_radio = None\n    if got_radio:\n        radio_msg = got_radio\n" + indent(s("radio"), 4));
    }
    if (pack && pack.loopTail) loopParts.push(indent(pack.loopTail(c, s), 4));
    if (c.flags.broadcasts) {
      const names = Object.keys(c.sections).filter((k) => k.startsWith("receive:")).map((k) => k.slice(8));
      let code = "    # broadcasts: run the \"when I receive\" blocks (at most 20 a loop, so two blocks cannot ping-pong forever)\n"
        + "    handled = 0\n    while broadcasts and handled < 20:\n        handled += 1\n        got = broadcasts.pop(0)\n";
      names.forEach((name, i) => { code += "        " + (i ? "elif" : "if") + " got == " + pyString(name) + ":\n" + indent(s("receive:" + name), 8); });
      code += "    del broadcasts[:]  # anything left after 20\n";
      loopParts.push(code);
    }
    loopParts.push("    sleep(50)\n");
    L.push(loopParts.join("").replace(/\n$/, ""));
    gen.finish(""); // resets the generator's name database and definitions
    return L.join("\n") + "\n";
  }

  // ------------------------------------------------------------------ theme and toolbox
  function theme() {
    const style = (c) => ({ colourPrimary: c });
    const blockStyles = {
      logic_blocks: style(COLOURS.logic), loop_blocks: style(COLOURS.loops), math_blocks: style(COLOURS.math),
      text_blocks: style(COLOURS.text), variable_blocks: style(COLOURS.variables), variable_dynamic_blocks: style(COLOURS.variables),
      mb_events: style(COLOURS.events), mb_input: style(COLOURS.input), mb_led: style(COLOURS.led), mb_sound: style(COLOURS.sound),
      mb_basic: style(COLOURS.basic), mb_radio: style(COLOURS.radio), game_blocks: style(pack && pack.colour ? pack.colour : COLOURS.game),
    };
    const categoryStyles = {};
    for (const k of Object.keys(COLOURS)) categoryStyles[k + "_category"] = { colour: COLOURS[k] };
    if (pack && pack.colour) categoryStyles.game_category = { colour: pack.colour };
    return B.Theme.defineTheme("microbit-comic", {
      base: B.Themes.Classic, blockStyles, categoryStyles, startHats: true,
      componentStyles: {
        workspaceBackgroundColour: "#fff4dc", toolboxBackgroundColour: "#1a2638", toolboxForegroundColour: "#fff4dc",
        flyoutBackgroundColour: "#f1dfb6", flyoutForegroundColour: "#14161b", flyoutOpacity: 1, scrollbarColour: "#14161b",
        scrollbarOpacity: 0.35, insertionMarkerColour: "#14161b", insertionMarkerOpacity: 0.3, cursorColour: "#e81f26",
      },
      fontStyle: { family: 'system-ui, "Segoe UI", Roboto, sans-serif', weight: "700", size: 11 },
    });
  }

  const blk = (type, extra) => Object.assign({ kind: "block", type }, extra || {});
  const num = (n) => ({ shadow: { type: "math_number", fields: { NUM: n } } });

  function toolbox() {
    const cats = [];
    if (pack && pack.toolbox) cats.push(...pack.toolbox(blk, num));
    cats.push(
      { kind: "category", name: "Events", categorystyle: "events_category", contents: [blk("mb_on_start"), blk("mb_forever"), blk("mb_on_button"), blk("mb_on_presses"), blk("mb_on_hold"), blk("mb_on_release"), blk("mb_on_gesture"),
        blk("mb_broadcast"), blk("mb_on_receive")] },
      { kind: "category", name: "Input", categorystyle: "input_category", contents: [
        blk("mb_tilt"), blk("mb_button_pressed"), blk("mb_presses"), blk("mb_held"), blk("mb_pin_touched"), blk("mb_logo_touched"), blk("mb_sound_level"),
        blk("mb_version"), blk("mb_running_time")] },
      { kind: "category", name: "LEDs", categorystyle: "led_category", contents: [
        blk("mb_show_icon"), blk("mb_show", { inputs: { VALUE: num(5) } }), blk("mb_clear"),
        blk("mb_plot", { inputs: { X: num(0), Y: num(0), B: num(9) } })] },
      { kind: "category", name: "Sound", categorystyle: "sound_category", contents: [blk("mb_play"), blk("mb_stop_sound")] },
      { kind: "category", name: "Radio", categorystyle: "radio_category", contents: [
        blk("mb_radio_group", { inputs: { GROUP: num(1) } }), blk("mb_radio_send", { inputs: { VALUE: { shadow: { type: "text", fields: { TEXT: "hello" } } } } }),
        blk("mb_on_radio"), blk("mb_radio_msg")] },
      { kind: "category", name: "Basic", categorystyle: "basic_category", contents: [
        blk("mb_pause", { inputs: { MS: num(100) } }), blk("mb_print", { inputs: { VALUE: { shadow: { type: "text", fields: { TEXT: "hello" } } } } })] },
      { kind: "category", name: "Logic", categorystyle: "logic_category", contents: [
        blk("controls_if"), blk("controls_if", { extraState: { hasElse: true } }), blk("logic_compare"), blk("logic_operation"),
        blk("logic_negate"), blk("logic_boolean")] },
      { kind: "category", name: "Loops", categorystyle: "loops_category", contents: [
        blk("controls_repeat_ext", { inputs: { TIMES: num(5) } }),
        blk("controls_for", { inputs: { FROM: num(0), TO: num(4), BY: num(1) } }), blk("controls_whileUntil")] },
      { kind: "category", name: "Math", categorystyle: "math_category", contents: [
        blk("math_number"), blk("math_arithmetic", { inputs: { A: num(1), B: num(1) } }), blk("math_single"),
        blk("math_round"), blk("math_modulo", { inputs: { DIVIDEND: num(10), DIVISOR: num(3) } }),
        blk("math_constrain", { inputs: { LOW: num(-100), HIGH: num(100) } }),
        blk("math_random_int", { inputs: { FROM: num(1), TO: num(10) } })] },
      { kind: "category", name: "Text", categorystyle: "text_category", contents: [blk("text"), blk("text_join")] },
      { kind: "category", name: "Variables", categorystyle: "variables_category", custom: "VARIABLE" },
    );
    return { kind: "categoryToolbox", contents: cats };
  }

  // ------------------------------------------------------------------ editor
  B.common.defineBlocksWithJsonArray(BLOCKS);

  // Warn on blocks that will fail on real boards (shown as a warning icon on the block).
  function updateWarnings(ws) {
    const all = ws.getAllBlocks(false);
    const sound = all.some((b) => b.type === "mb_play" && b.isEnabled());
    for (const b of all) {
      if (b.type !== "mb_pin_touched") continue;
      b.setWarningText(sound && b.getFieldValue("PIN") === "0"
        ? "On a micro:bit V1, sound uses pin 0, so touching pin 0 will stop your program. Use pin 1 or 2." : null);
    }
  }

  function mount(container, options) {
    const opts = options || {};
    const ws = B.inject(container, {
      toolbox: toolbox(), theme: theme(), renderer: "zelos", media: opts.media, sounds: false, trashcan: true,
      grid: { spacing: 24, length: 3, colour: "#ead6aa", snap: false },
      zoom: { controls: true, wheel: true, startScale: 0.75, maxScale: 2, minScale: 0.4, scaleSpeed: 1.15 },
      move: { scrollbars: true, drag: true, wheel: false },
    });
    ws.addChangeListener(B.Events.disableOrphans);
    let timer = 0;
    ws.addChangeListener((e) => {
      if (e.isUiEvent) return;
      clearTimeout(timer);
      timer = setTimeout(() => { updateWarnings(ws); if (opts.onChange) opts.onChange(); }, 150);
    });
    const api = {
      workspace: ws,
      code: () => generate(ws),
      save: () => B.serialization.workspaces.save(ws),
      load(state) {
        ws.clear();
        B.serialization.workspaces.load(state, ws);
        api.fit();
      },
      loadStarter() { api.load(pack && pack.starter ? pack.starter() : { blocks: { blocks: [] } }); },
      resize: () => B.svgResize(ws),
      // Show every block (small screens), but never bigger than 0.85.
      fit() {
        if (!container.offsetWidth || !container.offsetHeight) { api.needsFit = true; return; }
        api.needsFit = false;
        B.svgResize(ws);
        ws.zoomToFit();
        ws.setScale(Math.min(0.85, Math.max(0.45, ws.scale)));
        ws.scrollCenter();
      },
      needsFit: false,
    };
    return api;
  }

  window.MicrobitBlocks = {
    registerPack, mount, generate, COLOURS, version: "1",
    // For pack generators: hat blocks file their body under a section; loopTail(ctx, section) reads them back.
    addSection,
    statements: body,
    setFlag: (name) => { ctx.flags[name] = true; },
    // For pack generators that use the radio: import radio, radio.on(), radio.config(group=radio_group, queue=...).
    useRadio: (g, queue) => useRadio(g, queue),
  };
})();
