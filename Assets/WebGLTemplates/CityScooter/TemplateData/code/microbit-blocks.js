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
  };

  const IMAGES = ["HAPPY", "SAD", "HEART", "SMILE", "CONFUSED", "ANGRY", "SURPRISED", "ASLEEP", "YES", "NO",
    "ARROW_N", "ARROW_E", "ARROW_S", "ARROW_W", "TRIANGLE", "SQUARE", "DIAMOND", "SKULL", "GHOST", "PACMAN", "DUCK", "TARGET"];
  const MELODIES = ["BA_DING", "WAWAWAWAA", "POWER_UP", "POWER_DOWN", "JUMP_UP", "JUMP_DOWN", "PUNCHLINE", "BADDY",
    "CHASE", "DADADADUM", "ENTERTAINER", "NYAN", "RINGTONE", "BIRTHDAY"];
  const nice = (s) => s.toLowerCase().replace(/_/g, " ");
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
    "uart,sleep,running_time,random,speaker,audio,Sound");

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
    const globals = [...variables, ...(pack && pack.globals ? pack.globals(c) : [])].filter(Boolean);
    if (globals.length) L.push("", ...globals);
    for (const h of [...helpers, ...(pack && pack.helpers ? pack.helpers(c) : [])]) L.push("", "", h.replace(/\n+$/, ""));
    L.push("", "", "display.show(VERSION[1])  # 1 or 2 on the LEDs");
    if (s("start")) L.push("# on start", dedent4(s("start")).replace(/\n$/, ""));
    L.push("", "while True:");
    const loopParts = [];
    if (s("loop")) loopParts.push(s("loop"));
    for (const btn of ["a", "b"]) {
      if (s("button:" + btn)) loopParts.push("    if button_" + btn + ".was_pressed():\n" + indent(s("button:" + btn), 4));
    }
    for (const [, g] of GESTURES) {
      if (s("gesture:" + g)) loopParts.push('    if accelerometer.was_gesture("' + g + '"):\n' + indent(s("gesture:" + g), 4));
    }
    if (pack && pack.loopTail) loopParts.push(indent(pack.loopTail(c, s), 4));
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
      mb_basic: style(COLOURS.basic), game_blocks: style(pack && pack.colour ? pack.colour : COLOURS.game),
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
      { kind: "category", name: "Events", categorystyle: "events_category", contents: [blk("mb_on_start"), blk("mb_forever"), blk("mb_on_button"), blk("mb_on_gesture")] },
      { kind: "category", name: "Input", categorystyle: "input_category", contents: [
        blk("mb_tilt"), blk("mb_button_pressed"), blk("mb_pin_touched"), blk("mb_logo_touched"), blk("mb_sound_level"),
        blk("mb_version"), blk("mb_running_time")] },
      { kind: "category", name: "LEDs", categorystyle: "led_category", contents: [
        blk("mb_show_icon"), blk("mb_show", { inputs: { VALUE: num(5) } }), blk("mb_clear"),
        blk("mb_plot", { inputs: { X: num(0), Y: num(0), B: num(9) } })] },
      { kind: "category", name: "Sound", categorystyle: "sound_category", contents: [blk("mb_play"), blk("mb_stop_sound")] },
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
  };
})();
