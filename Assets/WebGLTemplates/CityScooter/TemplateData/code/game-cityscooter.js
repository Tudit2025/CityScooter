/* CityScooter block pack for MicrobitBlocks (Claude, 2026-11-07). Protocol: docs/protocol.md.
 * Blocks send st:/jp/br (v1: pu/bl, police: sur) and react to the game's crash/gift/go/spd:N (v1: ko/warn:/turn:;
 * police 11/8: wanted:N, chase:, cop:N, heli:, inc:, arrested, busted, lost; v0.3 ramps: trick, do:ramp, air:, land:;
 * v0.3 districts: zone:, and start:<id> to pick where rides start; radio race 11/9: race:NAME:GROUP, pos:/fin: from
 * the game relayed by radio as p:/f:, rx:<what the radio heard>, go: from the host, see docs/radio-race.md;
 * item race 11/10: item:<name> / hit:<name> from the game, "use", tx:<radio message> relayed by radio, see
 * docs/item-race.md).
 * The generated message reader is the same buffered messages() as microbit/starter.py (uart.readline() can return
 * half a line).
 * A pack for another game copies this file's shape: blocks, generators, isHat, globals, helpers, loopTail,
 * toolbox, starter. */
(() => {
  "use strict";
  const MB = window.MicrobitBlocks;
  const Order = window.python.Order;

  // What the game sends (docs/protocol.md). v1 added ko, warn:* and turn:*; old programs simply ignore them.
  const MESSAGE_OPTIONS = [["crash", "crash"], ["gift", "gift"], ["go (new ride)", "go"], ["ko (punch landed)", "ko"],
    ["warn:car (car ahead)", "warn:car"], ["warn:ped (person ahead)", "warn:ped"], ["warn:hole (hole ahead)", "warn:hole"],
    ["turn:-1 (road turns left)", "turn:-1"], ["turn:1 (road turns right)", "turn:1"], ["turn:0 (road straight)", "turn:0"],
    ["boing (bumped someone in cartoon mode)", "boing"], ["tired (out of punches: wait a moment)", "tired"],
    // WA police (11/8). wanted:N and cop:N carry a number: see the "wanted level" and "police distance" blocks.
    ["chase:1 (the police are after you)", "chase:1"], ["chase:0 (police chase over)", "chase:0"],
    ["heli:1 (police helicopter!)", "heli:1"], ["heli:0 (helicopter gone)", "heli:0"],
    ["inc:C (net or sniper aiming AT you: move!)", "inc:C"], ["inc:L (net or sniper on your left)", "inc:L"], ["inc:R (net or sniper on your right)", "inc:R"],
    ["arrested", "arrested"], ["busted", "busted"], ["lost (you lost the police!)", "lost"],
    // Ramps and air (v0.3): flying off ramps, tricks
    ["air:1 (you took off from a ramp!)", "air:1"], ["air:0 (back on the ground)", "air:0"],
    ["land:ok (landed)", "land:ok"], ["land:trick (landed after a trick!)", "land:trick"]];

  // ---- Districts (v0.3): zone:<id> at the start of a ride and when you ride into the next district (route order).
  const DISTRICTS = [["UWA", "uwa"], ["Matilda Bay", "matilda"], ["Kings Park", "kingspark"], ["West Perth", "westperth"],
    ["Perth CBD", "cbd"], ["Elizabeth Quay", "quay"], ["Mounts Bay Road", "mountsbay"],
    // v0.4 south line: down the Kwinana Freeway to Rockingham
    ["Canning Bridge", "canningbridge"], ["Murdoch", "murdoch"], ["Cockburn Central", "cockburn"], ["Aubin Grove", "aubingrove"],
    ["Kwinana", "kwinana"], ["Wellard", "wellard"], ["Rockingham", "rockingham"], ["Warnbro", "warnbro"],
    // v0.5 east line: along the Midland train line (after Perth CBD a ride takes the south line or this one)
    ["McIver", "mciver"], ["Claisebrook", "claisebrook"], ["East Perth", "eastperth"], ["Mt Lawley", "mtlawley"],
    ["Maylands", "maylands"], ["Meltham", "meltham"], ["Bayswater", "bayswater"], ["Ashfield", "ashfield"],
    ["Bassendean", "bassendean"], ["Success Hill", "successhill"], ["Guildford", "guildford"],
    ["East Guildford", "eastguildford"], ["Woodbridge", "woodbridge"], ["Midland", "midland"]];
  for (const [name, id] of DISTRICTS) MESSAGE_OPTIONS.push(["zone:" + id + " (entering " + name + ")", "zone:" + id]);
  // ---- end Districts (v0.3)

  // Words of your own that the game repeats to every board ("say:party" -> "party").
  const sayWord = (w) => String(w || "").replace(/[^A-Za-z0-9_!?-]/g, "").slice(0, 16) || "party";

  // ---- Race (radio race v1, 11/9; docs/radio-race.md): a name in race:NAME:GROUP and p:NAME:… is 1-5 letters or
  // digits in capitals, the same rule the game applies.
  const raceName = (s) => String(s == null ? "" : s).replace(/[^A-Za-z0-9]/g, "").toUpperCase().slice(0, 5);
  window.Blockly.Extensions.register("cs_race_name", function () {
    this.getField("NAME").setValidator((v) => raceName(v) || null); // "amy!" shows as AMY; an empty name is refused
  });
  const intField = (b, name, lo, hi) => String(Math.max(lo, Math.min(hi, Math.round(Number(b.getFieldValue(name)) || 0))));

  // ---- Items (item race, 11/10; docs/item-race.md): in a race the game gives you an item (item:rocket) when you ride
  // through an item box. Nothing happens until the program says "use". hit:<name> = an item hit you.
  const ITEMS = ["boost", "shield", "oil", "rocket", "bolt", "police"];
  // Student variables must not reuse these names (the same as the pack's `reserved` list below).
  window.python.pythonGenerator.addReservedWords("my_item,hit_item");

  const BLOCKS = [
    { type: "cs_steer", message0: "steer %1", args0: [{ type: "input_value", name: "VALUE", check: "Number" }],
      previousStatement: null, nextStatement: null, style: "game_blocks",
      tooltip: "Tell the game how to steer: -100 is full left, 0 is straight, 100 is full right. Python: print(\"st:\" + str(steer))" },
    { type: "cs_jump", message0: "jump", previousStatement: null, nextStatement: null, style: "game_blocks",
      tooltip: "Jump over cones and railings. Python: print(\"jp\")" },
    { type: "cs_brake", message0: "brake", previousStatement: null, nextStatement: null, style: "game_blocks",
      tooltip: "Slow down for a moment. Send it every loop to keep braking. Python: print(\"br\")" },
    { type: "cs_punch", message0: "punch", previousStatement: null, nextStatement: null, style: "game_blocks",
      tooltip: "Punch boxes, bins and cones out of your way. One punch at a time: punching again within 0.4 s does nothing. Python: print(\"pu\")" },
    { type: "cs_bell", message0: "ring the bell", previousStatement: null, nextStatement: null, style: "game_blocks",
      tooltip: "Ring your bell: people ahead step out of your way. Python: print(\"bl\")" },
    { type: "cs_boost", message0: "boost", previousStatement: null, nextStatement: null, style: "game_blocks",
      tooltip: "Go 35% faster than the speed limit for 2 seconds. It recharges for 6 seconds: boosts sent before that are ignored. Python: print(\"bo\")" },
    { type: "cs_toon", message0: "cartoon mode %1", args0: [{ type: "field_dropdown", name: "ON", options: [["on", "1"], ["off", "0"]] }],
      previousStatement: null, nextStatement: null, style: "game_blocks",
      tooltip: "Cartoon mode on: people you bump into go flying (BOING!) and you only slow down. Off: bumping into someone ends the ride. Works in \"on start\" too: your program says it again every second, so the game always knows. Python: print(\"toon:1\")" },
    { type: "cs_do", message0: "make the game start %1",
      args0: [{ type: "field_dropdown", name: "EVENT", options: [["a police chase", "chase"], ["the police helicopter (3 stars)", "heli"],
        ["calm: the police give up", "calm"], ["a turn to the left", "turn:L"], ["a turn to the right", "turn:R"], ["an oncoming car", "car"],
        ["a slow car ahead", "slowcar"], ["roadworks", "roadworks"], ["a police stop", "police"], ["people crossing", "crossing"], ["an open manhole", "manhole"],
        ["a ramp ahead", "ramp"]] }],
      previousStatement: null, nextStatement: null, style: "game_blocks",
      tooltip: "Your program makes one of the game's own events happen: e.g. count punches (\"ko\") in a variable and start a police chase after 3. During a ride: at once, one per second per micro:bit. In \"on start\": at the start of every ride. Python: print(\"do:chase\"), every ride: print(\"open:chase\")" },
    { type: "cs_stars", message0: "set the wanted stars to %1", args0: [{ type: "input_value", name: "N", check: "Number" }],
      previousStatement: null, nextStatement: null, style: "game_blocks",
      tooltip: "0 to 5 police stars: 1 starts a chase, 3 brings the helicopter, 0 makes the police give up. In \"on start\": every ride starts with these stars. Python: print(\"do:stars:\" + str(n)), every ride: print(\"open:stars:\" + str(n))" },
    { type: "cs_say", message0: "make the game say %1", args0: [{ type: "field_input", name: "WORD", text: "party" }],
      previousStatement: null, nextStatement: null, style: "game_blocks",
      tooltip: "The game repeats your word to EVERY connected micro:bit (this one too), and shows it on screen. Use \"when the game says word\" to react. One word: letters, digits, _ - ! ? (up to 16), not a game word like crash. At most 2 a second. Python: print(\"say:party\")" },
    { type: "cs_on_word", message0: "when the game says word %1 %2 %3",
      args0: [{ type: "field_input", name: "WORD", text: "party" }, { type: "input_dummy" }, { type: "input_statement", name: "DO" }],
      style: "game_blocks", tooltip: "Runs when the game repeats this word, because some micro:bit used \"make the game say\". Two boards can talk through the game this way." },
    { type: "cs_on_message", message0: "when the game says %1 %2 %3",
      args0: [{ type: "field_dropdown", name: "MSG", options: MESSAGE_OPTIONS },
        { type: "input_dummy" }, { type: "input_statement", name: "DO" }],
      style: "game_blocks", tooltip: "Runs when the game sends this message to your micro:bit." },
    { type: "cs_on_speed", message0: "when the game sends the speed %1 %2",
      args0: [{ type: "input_dummy" }, { type: "input_statement", name: "DO" }],
      style: "game_blocks", tooltip: "Runs when your speed changes. Use the \"game speed\" block inside: 0 (slow) to 9 (fastest)." },
    { type: "cs_speed", message0: "game speed (0-9)", output: "Number", style: "game_blocks",
      tooltip: "The last speed the game sent: 0 (slow) to 9 (fastest)." },
    // WA police (11/8)
    { type: "cs_surrender", message0: "pull over (surrender)", previousStatement: null, nextStatement: null, style: "game_blocks",
      tooltip: "Give up when the police chase you: the scooter brakes to a stop and you are ARRESTED (the ride ends, your score stays). Python: print(\"sur\")" },
    { type: "cs_on_wanted", message0: "when the wanted level changes %1 %2",
      args0: [{ type: "input_dummy" }, { type: "input_statement", name: "DO" }],
      style: "game_blocks", tooltip: "Runs when your wanted level changes. Use the \"wanted level\" block inside: 0 (nobody is after you) to 5 stars." },
    { type: "cs_wanted", message0: "wanted level (0-5)", output: "Number", style: "game_blocks",
      tooltip: "How many police stars you have: 0 to 5. Punching police adds a star; losing the police takes them away." },
    { type: "cs_cop", message0: "police distance (0-9)", output: "Number", style: "game_blocks",
      tooltip: "How far behind the police car is while it chases you: 0 is right next to you, 9 is far away (or no police car)." },
    // Ramps and air (v0.3)
    { type: "cs_trick", message0: "do a trick", previousStatement: null, nextStatement: null, style: "game_blocks",
      tooltip: "While you fly off a ramp (the game says air:1): a spin, then a flip, 50 points each, more for 2 in one flight. Land in the middle of one and it is SKETCHY (no points). On the ground it does nothing; sending it in a fast loop makes you dizzy. Python: print(\"trick\")" },
    // ---- Districts (v0.3)
    { type: "cs_on_zone", message0: "when entering %1 %2 %3",
      args0: [{ type: "field_dropdown", name: "ZONE", options: DISTRICTS.map(([name, id]) => [name, id]) },
        { type: "input_dummy" }, { type: "input_statement", name: "DO" }],
      style: "game_blocks",
      tooltip: "Runs when your ride starts in this part of Perth, and each time you ride into it (from UWA through the city, then down the freeway to Rockingham and Warnbro). The game sends zone:<name>, e.g. zone:kingspark." },
    { type: "cs_start", message0: "start rides in %1",
      args0: [{ type: "field_dropdown", name: "ZONE", options: DISTRICTS.map(([name, id]) => [name, id]) }],
      previousStatement: null, nextStatement: null, style: "game_blocks",
      tooltip: "Choose where your rides start on the route from UWA through the city to Rockingham and Warnbro. Put it in \"on start\" (or on a button). On the start screen the game moves there straight away; during a ride, your next ride starts there. Your program sends it again every second, in case the game was not listening yet. Python: print(\"start:kingspark\")" },
    // ---- Race (radio race v1): three setup blocks for "on start" (they only set variables; the main loop does the work)
    { type: "cs_race_join", message0: "join race %1 as %2",
      args0: [{ type: "field_number", name: "GROUP", value: 12, min: 0, max: 255, precision: 1 },
        { type: "field_input", name: "NAME", text: "AMY" }],
      previousStatement: null, nextStatement: null, style: "game_blocks", extensions: ["cs_race_name"],
      tooltip: "Put it in \"on start\". Your race number is your radio group (look at the card on your computer): only micro:bits in the same group hear each other. About once a second your program tells the game you want to race, with your name (1-5 letters or digits, everyone in a race needs a different one). Python: print(\"race:AMY:12\")" },
    { type: "cs_race_send", message0: "send my position by radio", previousStatement: null, nextStatement: null, style: "game_blocks",
      tooltip: "Put it in \"on start\". In a race the game tells your micro:bit where you are 4 times a second (pos:1234:-2); this sends it by radio to everyone in your race (p:AMY:1234:-2). After the finish line it sends your time (f:AMY:72.35). It also sends the items you use to the others: the game says tx:…, your micro:bit sends it by radio. Python: radio.send(\"p:\" + race_name + \":\" + msg[4:])" },
    { type: "cs_race_relay", message0: "pass radio messages to the game", previousStatement: null, nextStatement: null, style: "game_blocks",
      tooltip: "Put it in \"on start\". Every loop your program reads everything your micro:bit heard by radio and passes it on to the game (rx:p:BEN:1200:0), so the game can draw the other riders. Without it you race alone. Python: print(\"rx:\" + got_radio)" },
    { type: "cs_race_start", message0: "start a race from %1 for %2 m",
      args0: [{ type: "field_dropdown", name: "ZONE", options: DISTRICTS.map(([name, id]) => [name, id]) },
        { type: "field_number", name: "METRES", value: 2000, min: 300, max: 5000, precision: 1 }],
      previousStatement: null, nextStatement: null, style: "game_blocks",
      tooltip: "Put it inside \"when shaken\" (or a button you don't use for riding). Only the host of each race needs it. It sends go: by radio 5 times in half a second: every game in your race (yours too) counts down 3-2-1 and everyone rides the same road, from this place, this many metres (300-5000). Python: radio.send(\"go:4821:uwa:2000\")" },
    // ---- Items (item race): two hats, three values and "use my item"
    { type: "cs_on_item", message0: "when I get an item %1 %2",
      args0: [{ type: "input_dummy" }, { type: "input_statement", name: "DO" }],
      style: "game_blocks",
      tooltip: "Runs when you pick up an item from an item box in a race. The item is NOT used by itself: it waits until your program says \"use my item\" (for example when button B is pressed). \"my item\" tells you which one you got. The game sends item:rocket. Python: my_item = msg[5:]" },
    { type: "cs_item", message0: "my item", output: "String", style: "game_blocks",
      tooltip: "The item you hold in a race: boost, shield, oil, rocket, bolt or police. It is none when you have no item. Holding an item does nothing until your program says \"use my item\". Python: my_item" },
    { type: "cs_item_is", message0: "my item is %1",
      args0: [{ type: "field_dropdown", name: "ITEM", options: ITEMS.concat("none").map((i) => [i, i]) }],
      output: "Boolean", style: "game_blocks",
      tooltip: "True while you hold this item. Example: if my item is shield, keep it for later; else use my item. An item is only used when your program says \"use my item\". Python: my_item == \"rocket\"" },
    { type: "cs_use", message0: "use my item", previousStatement: null, nextStatement: null, style: "game_blocks",
      tooltip: "Use the item you hold in a race, for example inside \"when button B is pressed\". Items are never used by themselves: your program has to say use. With no item it does nothing. Python: print(\"use\")" },
    { type: "cs_on_hit", message0: "when an item hits me %1 %2",
      args0: [{ type: "input_dummy" }, { type: "input_statement", name: "DO" }],
      style: "game_blocks",
      tooltip: "Runs when an item hits you in a race: a rocket, a bolt, the police, or oil you rode over. \"item that hit me\" tells you which one. The game sends hit:rocket. Python: hit_item = msg[4:]" },
    { type: "cs_hit_item", message0: "item that hit me", output: "String", style: "game_blocks",
      tooltip: "The last item that hit you in a race: rocket, bolt, police or oil. It is none before the first hit. Python: hit_item" },
  ];

  // In "on start", "make the game start ..." and "set the wanted stars to ..." set up EVERY ride (11/9): "on start" runs
  // once, but A after a crash starts a new ride. The program says open:<event> at once and again about once a second
  // (like start:), so the game also hears it when it was not listening yet or a ride was already going.
  const inOnStart = (b) => { const root = b.getRootBlock(); return !!root && root.type === "mb_on_start"; };
  const everyRide = (event) => {
    MB.setFlag("usesOpening");
    return "every_ride.append(" + event + ')\nprint("open:" + every_ride[-1])  # every ride starts with it\n';
  };

  const GENERATORS = {
    cs_steer: (b, g) => 'print("st:" + str(int(' + (g.valueToCode(b, "VALUE", Order.NONE) || "0") + ")))\n",
    cs_jump: () => 'print("jp")\n',
    cs_brake: () => 'print("br")\n',
    cs_punch: () => 'print("pu")\n',
    cs_bell: () => 'print("bl")\n',
    cs_boost: () => 'print("bo")\n',
    cs_do: (b) => (inOnStart(b) ? everyRide('"' + b.getFieldValue("EVENT") + '"') : 'print("do:' + b.getFieldValue("EVENT") + '")\n'),
    cs_stars: (b, g) => {
      const n = "str(max(0, min(5, int(" + (g.valueToCode(b, "N", Order.NONE) || "0") + "))))";
      return inOnStart(b) ? everyRide('"stars:" + ' + n) : 'print("do:stars:" + ' + n + ")\n";
    },
    cs_say: (b) => 'print("say:' + sayWord(b.getFieldValue("WORD")) + '")\n',
    cs_on_word: (b) => { MB.addSection("word:" + sayWord(b.getFieldValue("WORD")), MB.statements(b, "DO")); return ""; },
    // Remembered and said again with hi: (about once a second): the game forgets cartoon mode when the page is
    // refreshed or the board reconnects, and "on start" does not run again.
    cs_toon: (b) => { MB.setFlag("usesToon"); return 'toon_mode = "' + b.getFieldValue("ON") + '"\nprint("toon:" + toon_mode)\n'; },
    cs_speed: () => { MB.setFlag("usesSpeed"); return ["game_speed", Order.ATOMIC]; },
    cs_on_message: (b) => { MB.addSection("msg:" + b.getFieldValue("MSG"), MB.statements(b, "DO")); return ""; },
    cs_on_speed: (b) => { MB.setFlag("usesSpeed"); MB.addSection("msg:spd", MB.statements(b, "DO")); return ""; },
    cs_surrender: () => 'print("sur")\n',
    cs_wanted: () => { MB.setFlag("usesWanted"); return ["wanted_level", Order.ATOMIC]; },
    cs_on_wanted: (b) => { MB.setFlag("usesWanted"); MB.addSection("msg:wanted", MB.statements(b, "DO")); return ""; },
    cs_cop: () => { MB.setFlag("usesCop"); return ["police_distance", Order.ATOMIC]; },
    cs_trick: () => 'print("trick")\n',
    // ---- Districts (v0.3): the same branch as "when the game says zone:<id>" (compact: one elif per district used)
    cs_on_zone: (b) => { MB.addSection("msg:zone:" + b.getFieldValue("ZONE"), MB.statements(b, "DO")); return ""; },
    // Remembered in start_zone and sent again with hi: (about once a second): a print in "on start" can run before the
    // game is listening.
    cs_start: (b) => { MB.setFlag("usesStart"); return 'start_zone = "' + b.getFieldValue("ZONE") + '"\nprint("start:" + start_zone)\n'; },
    // ---- Race (radio race v1). The setup blocks only set variables, so they are harmless in "forever" too; the main
    // loop prints race: with hi:, relays pos:/fin: as p:/f:, and passes the radio queue on as rx:.
    cs_race_join: (b, g) => {
      MB.useRadio(g, 10);
      MB.setFlag("race");
      const group = intField(b, "GROUP", 0, 255);
      return 'race_name = "' + (raceName(b.getFieldValue("NAME")) || "ME") + '"\n'
        + "if radio_group != " + group + ":  # a new group: tell the radio\n    radio_group = " + group + "\n    radio.config(group=radio_group)\n";
    },
    cs_race_send: (b, g) => { MB.useRadio(g, 10); MB.setFlag("raceSend"); return "send_position = True\n"; },
    cs_race_relay: (b, g) => { MB.useRadio(g, 10); MB.setFlag("raceRelay"); return "pass_radio = True\n"; },
    cs_race_start: (b, g) => {
      MB.useRadio(g);
      MB.setFlag("raceStart");
      g.definitions_.import_random = "import random";
      return 'start_race("' + b.getFieldValue("ZONE") + '", ' + intField(b, "METRES", 300, 5000) + ")\n";
    },
    // ---- Items (item race). my_item follows every item: message (item:none too); hit_item every hit: message. The
    // variables exist only when a block uses them, like wanted_level.
    cs_on_item: (b) => { MB.setFlag("usesItem"); MB.addSection("msg:item", MB.statements(b, "DO")); return ""; },
    cs_item: () => { MB.setFlag("usesItem"); return ["my_item", Order.ATOMIC]; },
    cs_item_is: (b) => { MB.setFlag("usesItem"); return ['(my_item == "' + b.getFieldValue("ITEM") + '")', Order.ATOMIC]; },
    cs_use: () => 'print("use")\n',
    cs_on_hit: (b) => { MB.setFlag("usesHit"); MB.addSection("msg:hit", MB.statements(b, "DO")); return ""; },
    cs_hit_item: () => { MB.setFlag("usesHit"); return ["hit_item", Order.ATOMIC]; },
  };

  // "start a race from … for … m": the same road for everyone (a random seed), sent 5 times because radio messages
  // can get lost. A micro:bit does not hear its own radio, so the host's game gets the go: over USB.
  const START_RACE_HELPER = [
    "def start_race(place, metres):",
    "    # go:<seed>:<place>:<metres> starts the race in every game in your group: the same seed makes the same road.",
    '    go = "go:" + str(random.randint(1, 99999)) + ":" + place + ":" + str(metres)',
    '    print("rx:" + go)  # your own game too: a micro:bit does not hear its own radio',
    "    for i in range(5):  # 5 times in half a second, in case one gets lost",
    "        radio.send(go)",
    "        sleep(100)",
  ].join("\n");

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
    reserved: "buffer,messages,msg,lines,line,data,loops,game_speed,wanted_level,police_distance,start_zone,"
      + "race_name,send_position,pass_radio,start_race,every_ride,ride_event,toon_mode",
    isHat: (type) => type === "cs_on_message" || type === "cs_on_word" || type === "cs_on_speed" || type === "cs_on_wanted"
      || type === "cs_on_zone" // ---- Districts (v0.3)
      || type === "cs_on_item" || type === "cs_on_hit", // ---- Items (item race)
    // wanted_level / police_distance (and the race variables) only when a block uses them, so Lesson 1 programs look
    // the same as before.
    globals: (ctx) => ['buffer = ""', "loops = 0", "game_speed = 0",
      ctx.flags.usesWanted ? "wanted_level = 0" : null, ctx.flags.usesCop ? "police_distance = 9  # far away" : null,
      ctx.flags.usesStart ? 'start_zone = ""  # where rides start (the "start rides in" block)' : null,
      ctx.flags.usesOpening ? 'every_ride = []  # game events for the start of every ride (set in "on start")' : null,
      ctx.flags.usesToon ? 'toon_mode = ""  # cartoon mode: "1" on, "0" off ("" = never set)' : null,
      ctx.flags.race || ctx.flags.raceSend ? 'race_name = ""  # your name in the race ("join race")' : null,
      ctx.flags.raceSend ? "send_position = False  # True: send where you are by radio" : null,
      ctx.flags.raceRelay ? "pass_radio = False  # True: pass what the radio hears to the game" : null,
      ctx.flags.usesItem ? 'my_item = "none"  # the item you hold in a race ("none": no item)' : null,
      ctx.flags.usesHit ? 'hit_item = "none"  # the last item that hit you' : null].filter(Boolean),
    helpers: (ctx) => [MESSAGES_HELPER, ctx.flags.raceStart ? START_RACE_HELPER : null].filter(Boolean),
    // ---- Race: every radio message, when "pass radio messages to the game" is on (the generic loop reads them all)
    radioEach: (ctx) => (ctx.flags.raceRelay ? 'if pass_radio:\n    print("rx:" + got_radio)  # pass it on to the game\n' : ""),
    loopTail: (ctx, section) => {
      const branches = [];
      for (const [, m] of MESSAGE_OPTIONS) {
        let body = section("msg:" + m);
        // A new ride or the end of a chase: the police car is gone.
        if (ctx.flags.usesCop && (m === "go" || m === "chase:0")) body = "    police_distance = 9\n" + body;
        if (body) branches.push('if msg == "' + m + '":\n' + indent(body, 0));
      }
      // Words of your own, repeated by the game (see "make the game say").
      for (const key of Object.keys(ctx.sections)) {
        if (!key.startsWith("word:")) continue;
        branches.push('if msg == "' + key.slice(5) + '":\n' + indent(section(key), 0));
      }
      if (section("msg:spd") || ctx.flags.usesSpeed) {
        branches.push('if msg.startswith("spd:"):\n    try:\n        game_speed = int(msg[4:])\n    except ValueError:  # half a line got lost: skip it\n        pass\n' + section("msg:spd"));
      }
      if (section("msg:wanted") || ctx.flags.usesWanted) {
        branches.push('if msg.startswith("wanted:"):\n    try:\n        wanted_level = int(msg[7:])\n    except ValueError:  # half a line got lost: skip it\n        pass\n' + section("msg:wanted"));
      }
      if (ctx.flags.usesCop) branches.push('if msg.startswith("cop:"):\n    try:\n        police_distance = int(msg[4:])\n    except ValueError:  # half a line got lost: skip it\n        pass\n');
      // ---- Race: the game says where you are (pos:DIST:LANE) and your time at the finish (fin:SECONDS)
      if (ctx.flags.raceSend) {
        branches.push('if send_position and msg.startswith("pos:"):\n    radio.send("p:" + race_name + ":" + msg[4:])  # tell the others where you are\n');
        branches.push('if send_position and msg.startswith("fin:"):\n    radio.send("f:" + race_name + ":" + msg[4:])  # you crossed the line: tell them your time\n');
        // ---- Items: tx:<radio message> = the game asks the board to send this by radio (an item you used)
        branches.push('if send_position and msg.startswith("tx:"):\n    radio.send(msg[3:])  # an item you used: the game wrote the radio message, you send it\n');
      }
      // ---- Items (item race): item:<name> = the item you hold now (item:none: used or lost); hit:<name> = it hit you
      if (ctx.flags.usesItem) {
        const got = section("msg:item");
        branches.push('if msg.startswith("item:"):\n    my_item = msg[5:]  # "rocket", "shield", ... or "none" when it is gone\n'
          + (got ? '    if my_item != "none":  # you picked one up\n' + indent(got, 4) : ""));
      }
      if (ctx.flags.usesHit) {
        branches.push('if msg.startswith("hit:"):\n    hit_item = msg[4:]  # "rocket", "bolt", "police" or "oil"\n' + section("msg:hit"));
      }
      let out = "";
      if (branches.length) {
        out += "# messages from the game\nfor msg in messages():\n" + indent(branches.map((b, i) => (i ? "el" : "") + b).join(""), 4);
      } else {
        out += "messages()  # read (and ignore) what the game sends\n";
      }
      out += 'loops += 1\nif loops % 20 == 0:\n    print("hi:" + VERSION)  # tell the game which micro:bit this is\n';
      if (ctx.flags.usesStart) out += '    if start_zone:\n        print("start:" + start_zone)  # again, in case the game missed it\n';
      if (ctx.flags.usesToon) out += '    if toon_mode:\n        print("toon:" + toon_mode)  # again, in case the game missed it\n';
      if (ctx.flags.usesOpening) out += '    for ride_event in every_ride:\n        print("open:" + ride_event)  # again: the game starts every ride with it\n';
      if (ctx.flags.race) out += '    if race_name:\n        print("race:" + race_name + ":" + str(radio_group))  # I want to race, in this group\n';
      return out;
    },
    toolbox: (blk, num) => [{
      kind: "category", name: "CityScooter", categorystyle: "game_category", contents: [
        blk("cs_steer", { inputs: { VALUE: num(0) } }), blk("cs_jump"), blk("cs_brake"), blk("cs_punch"), blk("cs_bell"), blk("cs_boost"), blk("cs_toon"),
        blk("cs_on_message"), blk("cs_on_message", { fields: { MSG: "gift" } }), blk("cs_on_message", { fields: { MSG: "warn:ped" } }),
        blk("cs_on_speed"), blk("cs_speed"),
        blk("cs_on_message", { fields: { MSG: "chase:1" } }), blk("cs_on_message", { fields: { MSG: "inc:C" } }),
        blk("cs_on_wanted"), blk("cs_wanted"), blk("cs_cop"), blk("cs_surrender"),
        blk("cs_do"), blk("cs_stars", { inputs: { N: num(1) } }), blk("cs_say"), blk("cs_on_word"),
        blk("cs_on_message", { fields: { MSG: "air:1" } }), blk("cs_trick"), // ramps and air (v0.3)
        blk("cs_on_zone", { fields: { ZONE: "kingspark" } }), blk("cs_start", { fields: { ZONE: "kingspark" } }), // ---- Districts (v0.3)
        // ---- Race (radio race v1): the slides use these four, in this order
        { kind: "label", text: "Race" },
        blk("cs_race_join"), blk("cs_race_send"), blk("cs_race_relay"), blk("cs_race_start", { fields: { ZONE: "uwa", METRES: 2000 } }),
        // ---- Items (item race): the six blocks of docs/item-race.md, in that order
        { kind: "label", text: "Items" },
        blk("cs_on_item"), blk("cs_item"), blk("cs_item_is", { fields: { ITEM: "rocket" } }), blk("cs_use"), blk("cs_on_hit"), blk("cs_hit_item"),
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
