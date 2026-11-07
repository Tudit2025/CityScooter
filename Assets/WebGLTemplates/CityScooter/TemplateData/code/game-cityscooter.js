/* CityScooter block pack for MicrobitBlocks (Claude, 2026-11-07). Protocol: docs/protocol.md.
 * Blocks send st:/jp/br and react to the game's crash/gift/go/spd:N. The generated message reader is the same
 * buffered messages() as microbit/starter.py (uart.readline() can return half a line).
 * A pack for another game copies this file's shape: blocks, generators, isHat, globals, helpers, loopTail,
 * toolbox, starter. */
(() => {
  "use strict";
  const MB = window.MicrobitBlocks;
  const Order = window.python.Order;

  const BLOCKS = [
    { type: "cs_steer", message0: "steer %1", args0: [{ type: "input_value", name: "VALUE", check: "Number" }],
      previousStatement: null, nextStatement: null, style: "game_blocks",
      tooltip: "Tell the game how to steer: -100 is full left, 0 is straight, 100 is full right. Python: print(\"st:\" + str(steer))" },
    { type: "cs_jump", message0: "jump", previousStatement: null, nextStatement: null, style: "game_blocks",
      tooltip: "Jump over cones and railings. Python: print(\"jp\")" },
    { type: "cs_brake", message0: "brake", previousStatement: null, nextStatement: null, style: "game_blocks",
      tooltip: "Slow down for a moment. Send it every loop to keep braking. Python: print(\"br\")" },
    { type: "cs_on_message", message0: "when the game says %1 %2 %3",
      args0: [{ type: "field_dropdown", name: "MSG", options: [["crash", "crash"], ["gift", "gift"], ["go (new ride)", "go"]] },
        { type: "input_dummy" }, { type: "input_statement", name: "DO" }],
      style: "game_blocks", tooltip: "Runs when the game sends this message to your micro:bit." },
    { type: "cs_on_speed", message0: "when the game sends the speed %1 %2",
      args0: [{ type: "input_dummy" }, { type: "input_statement", name: "DO" }],
      style: "game_blocks", tooltip: "Runs when your speed changes. Use the \"game speed\" block inside: 0 (slow) to 9 (fastest)." },
    { type: "cs_speed", message0: "game speed (0-9)", output: "Number", style: "game_blocks",
      tooltip: "The last speed the game sent: 0 (slow) to 9 (fastest)." },
  ];

  const GENERATORS = {
    cs_steer: (b, g) => 'print("st:" + str(int(' + (g.valueToCode(b, "VALUE", Order.NONE) || "0") + ")))\n",
    cs_jump: () => 'print("jp")\n',
    cs_brake: () => 'print("br")\n',
    cs_speed: () => { MB.setFlag("usesSpeed"); return ["game_speed", Order.ATOMIC]; },
    cs_on_message: (b) => { MB.addSection("msg:" + b.getFieldValue("MSG"), MB.statements(b, "DO")); return ""; },
    cs_on_speed: (b) => { MB.setFlag("usesSpeed"); MB.addSection("msg:spd", MB.statements(b, "DO")); return ""; },
  };

  const MESSAGES_HELPER = [
    "def messages():",
    "    # uart.readline() can return half a line, so collect bytes until a newline arrives.",
    "    global buffer",
    "    lines = []",
    "    if uart.any():",
    "        data = uart.read()",
    "        if data:",
    '            buffer += str(data, "utf-8")',
    '            while "\\n" in buffer:',
    '                line, buffer = buffer.split("\\n", 1)',
    "                lines.append(line.strip())",
    "            if len(buffer) > 100:",
    '                buffer = ""',
    "    return lines",
  ].join("\n");

  const indent = (code, n) => code.replace(/^(?=.)/gm, " ".repeat(n));

  MB.registerPack({
    id: "cityscooter",
    name: "CityScooter",
    colour: "#e81f26",
    blocks: BLOCKS,
    generators: GENERATORS,
    reserved: "buffer,messages,msg,lines,line,data,loops,game_speed",
    isHat: (type) => type === "cs_on_message" || type === "cs_on_speed",
    globals: () => ['buffer = ""', "loops = 0", "game_speed = 0"],
    helpers: () => [MESSAGES_HELPER],
    loopTail: (ctx, section) => {
      const branches = [];
      for (const m of ["crash", "gift", "go"]) {
        if (section("msg:" + m)) branches.push('if msg == "' + m + '":\n' + indent(section("msg:" + m), 0));
      }
      if (section("msg:spd") || ctx.flags.usesSpeed) {
        branches.push('if msg.startswith("spd:"):\n    game_speed = int(msg[4:])\n' + section("msg:spd"));
      }
      let out = "";
      if (branches.length) {
        out += "# messages from the game\nfor msg in messages():\n" + indent(branches.map((b, i) => (i ? "el" : "") + b).join(""), 4);
      } else {
        out += "messages()  # read (and ignore) what the game sends\n";
      }
      out += 'loops += 1\nif loops % 20 == 0:\n    print("hi:" + VERSION)  # tell the game which micro:bit this is\n';
      return out;
    },
    toolbox: (blk, num) => [{
      kind: "category", name: "CityScooter", categorystyle: "game_category", contents: [
        blk("cs_steer", { inputs: { VALUE: num(0) } }), blk("cs_jump"), blk("cs_brake"),
        blk("cs_on_message"), blk("cs_on_message", { fields: { MSG: "gift" } }), blk("cs_on_speed"), blk("cs_speed"),
      ],
    }],
    // The Lesson 1 starter (microbit/starter.py) as blocks.
    starter: () => ({
      blocks: { languageVersion: 0, blocks: [
        { type: "mb_forever", x: 20, y: 20, inputs: { DO: { block: {
          type: "cs_steer", inputs: { VALUE: { block: { type: "math_arithmetic", fields: { OP: "DIVIDE" }, inputs: {
            A: { block: { type: "mb_tilt", fields: { AXIS: "x" } } },
            B: { shadow: { type: "math_number", fields: { NUM: 5 } } } } } } },
          next: { block: { type: "controls_if", inputs: {
            IF0: { block: { type: "mb_button_pressed", fields: { BTN: "b" } } },
            DO0: { block: { type: "cs_brake" } } } } } } } } },
        { type: "mb_on_button", x: 20, y: 320, fields: { BTN: "a" }, inputs: { DO: { block: { type: "cs_jump" } } } },
        { type: "cs_on_message", x: 560, y: 20, fields: { MSG: "crash" }, inputs: { DO: { block: { type: "mb_show_icon", fields: { ICON: "SAD" } } } } },
        { type: "cs_on_message", x: 560, y: 170, fields: { MSG: "gift" }, inputs: { DO: { block: { type: "mb_show_icon", fields: { ICON: "HAPPY" } } } } },
        { type: "cs_on_message", x: 560, y: 320, fields: { MSG: "go" }, inputs: { DO: { block: { type: "mb_clear" } } } },
      ] },
    }),
  });
})();
