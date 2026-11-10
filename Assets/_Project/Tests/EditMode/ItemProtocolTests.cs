using CityScooter.Protocol;
using NUnit.Framework;

namespace CityScooter.Tests
{
    /// <summary>v0.5 item race (docs/item-race.md): "use", "item:", "tx:", "hit:", "blocked:" and the radio message "i:".</summary>
    public class ItemProtocolTests
    {
        [TestCase("use")]
        [TestCase("USE")]
        [TestCase("  use  ")]
        [TestCase("use:1")] // forgiven, like jp:1
        public void Use_Parses(string line)
        {
            Command c = CommandParser.Parse(line);
            Assert.AreEqual(CommandKind.Use, c.Kind, line);
            Assert.IsTrue(c.IsProtocol);
        }

        [Test]
        public void Use_IsCountedOnce_ByTheMixer()
        {
            var mixer = new ControlMixer();
            Assert.IsFalse(mixer.ConsumeUse());
            mixer.Apply(CommandParser.Parse("use"), 0d);
            mixer.Apply(CommandParser.Parse("use"), 0d); // a program that says it every loop still uses one item
            Assert.IsTrue(mixer.ConsumeUse());
            Assert.IsFalse(mixer.ConsumeUse());
            mixer.Apply(CommandParser.Parse("use"), 1d);
            mixer.Reset();
            Assert.IsFalse(mixer.ConsumeUse(), "Reset forgets it");
        }

        [Test]
        public void GameToBoardMessages_AreBuiltAsTheSpecSays()
        {
            Assert.AreEqual("item:rocket", Messages.Item("rocket"));
            Assert.AreEqual("item:none", Messages.Item(null));
            Assert.AreEqual("item:none", Messages.Item("banana"), "only the six items have a name");
            Assert.AreEqual("item:none", Messages.ItemNone);
            Assert.AreEqual("hit:rocket", Messages.Hit(Messages.ItemRocket));
            Assert.AreEqual("hit:oil", Messages.Hit(Messages.ItemOil));
            Assert.AreEqual("blocked:rocket", Messages.Blocked(Messages.ItemRocket));
            Assert.AreEqual("blocked:crash", Messages.Blocked(Messages.BlockedCrash));
            Assert.AreEqual("tx:i:AMY:bolt:5", Messages.Transmit("i:AMY:bolt:5"));
            CollectionAssert.AreEqual(new[] { "boost", "shield", "oil", "rocket", "bolt", "police" }, Messages.ItemNames);
            foreach (string name in Messages.ItemNames) Assert.IsTrue(Messages.IsItem(name));
            Assert.IsFalse(Messages.IsItem("none"));
            Assert.IsFalse(Messages.IsItem("Rocket"), "lower case on the wire");
        }

        [Test]
        public void WhatTheGameSends_IsNotABoardCommand()
        {
            // A board that echoes what it hears must not make the game do anything.
            foreach (string line in new[] { "item:rocket", "item:none", "hit:rocket", "blocked:rocket", "tx:i:AMY:bolt:5" })
                Assert.AreEqual(CommandKind.Unknown, CommandParser.Parse(line).Kind, line);
            // And a board may not make the game say them.
            foreach (string word in new[] { "use", "item", "hit", "blocked", "tx", "item:rocket" })
                Assert.IsFalse(Messages.IsSayWord(word), word);
        }

        [TestCase("i:AMY:rocket:BEN:3", "AMY", "rocket", "BEN", 3)]
        [TestCase("i:AMY:police:BEN:4", "AMY", "police", "BEN", 4)]
        [TestCase("I : amy : ROCKET : ben : 0", "AMY", "rocket", "BEN", 0)]
        [TestCase("i:Chloe-B:police:R2 D2:9", "CHLOE", "police", "R2D2", 9)]
        public void ItemOnARider_Parses(string payload, string from, string item, string target, int sequence)
        {
            RadioMessage m = RadioMessage.Parse(payload);
            Assert.AreEqual(RadioKind.Item, m.Kind, payload);
            Assert.AreEqual(from, m.Name);
            Assert.AreEqual(item, m.Item);
            Assert.AreEqual(target, m.Target);
            Assert.AreEqual(sequence, m.Sequence);
            Assert.IsTrue(m.IsRace);
        }

        [Test]
        public void Bolt_Parses()
        {
            RadioMessage m = RadioMessage.Parse("i:AMY:bolt:5");
            Assert.AreEqual(RadioKind.Item, m.Kind);
            Assert.AreEqual("AMY", m.Name);
            Assert.AreEqual(Messages.ItemBolt, m.Item);
            Assert.IsNull(m.Target);
            Assert.AreEqual(5, m.Sequence);
        }

        [TestCase("i:AMY:oil:1234:-2:6", 1234f, -2f, 6)]
        [TestCase("i:AMY:oil:0:0:0", 0f, 0f, 0)]
        [TestCase("i:AMY:oil:12.5:1.5:1", 12.5f, 1.5f, 1)]
        [TestCase("i:AMY:oil:-5:99:2", 0f, 20f, 2)] // clamped like p:
        public void Oil_Parses(string payload, float distance, float lane, int sequence)
        {
            RadioMessage m = RadioMessage.Parse(payload);
            Assert.AreEqual(RadioKind.Item, m.Kind, payload);
            Assert.AreEqual(Messages.ItemOil, m.Item);
            Assert.AreEqual("AMY", m.Name);
            Assert.AreEqual(distance, m.Distance, 1e-4f);
            Assert.AreEqual(lane, m.Lane, 1e-4f);
            Assert.AreEqual(sequence, m.Sequence);
        }

        [TestCase("i:AMY:rocket:BEN")] // no item number
        [TestCase("i:AMY:rocket:BEN:x")]
        [TestCase("i:AMY:rocket:BEN:10")] // one digit only
        [TestCase("i:AMY:rocket:BEN:-1")]
        [TestCase("i:AMY:rocket::3")]
        [TestCase("i::rocket:BEN:3")]
        [TestCase("i:AMY:rocket:3")] // a rocket needs a target
        [TestCase("i:AMY:bolt:BEN:3")] // a bolt has none
        [TestCase("i:AMY:bolt")]
        [TestCase("i:AMY:oil:1234:6")]
        [TestCase("i:AMY:oil:far:-2:6")]
        [TestCase("i:AMY:oil:1234:left:6")]
        [TestCase("i:AMY:boost:3")] // boost and shield never go over the radio
        [TestCase("i:AMY:shield:3")]
        [TestCase("i:AMY:banana:BEN:3")]
        [TestCase("i:")]
        [TestCase("i")]
        [TestCase("item:rocket")]
        public void BrokenItemMessages_AreDropped(string payload)
        {
            Assert.AreEqual(RadioKind.None, RadioMessage.Parse(payload).Kind, payload);
        }

        [Test]
        public void ItemMessages_AreBuilt_AndReadBackTheSame()
        {
            Assert.AreEqual("i:AMY:rocket:BEN:3", RadioMessage.ItemOn("AMY", Messages.ItemRocket, "BEN", 3));
            Assert.AreEqual("i:AMY:police:BEN:4", RadioMessage.ItemOn("AMY", Messages.ItemPolice, "BEN", 4));
            Assert.AreEqual("i:AMY:bolt:5", RadioMessage.ItemBolt("AMY", 5));
            Assert.AreEqual("i:AMY:oil:1234:-2:6", RadioMessage.ItemOil("AMY", 1234, -2, 6));
            Assert.AreEqual("i:AMY:oil:0:20:0", RadioMessage.ItemOil("AMY", -7, 99, 10), "clamped; the item number goes round after 9");
            Assert.AreEqual("i:AMY:bolt:1", RadioMessage.ItemBolt("AMY", 11));
            foreach (string payload in new[] { "i:AMY:rocket:BEN:3", "i:AMY:police:BEN:4", "i:AMY:bolt:5", "i:AMY:oil:1234:-2:6" })
                Assert.AreEqual(payload, RadioMessage.Parse(payload).ToString());
        }

        [Test]
        public void ItemMessages_FitInOneRadioPacket()
        {
            // radio.send carries 29 characters at most (docs/radio-race.md); the spec promises 22 for the usual ones.
            Assert.LessOrEqual(RadioMessage.ItemOn("AMYXX", Messages.ItemPolice, "BENXX", 9).Length, 22);
            Assert.LessOrEqual(RadioMessage.ItemOn("AMYXX", Messages.ItemRocket, "BENXX", 9).Length, 22);
            Assert.LessOrEqual(RadioMessage.ItemBolt("AMYXX", 9).Length, 22);
            Assert.LessOrEqual(RadioMessage.ItemOil("AMYXX", 5000, -8, 9).Length, 22);
            Assert.LessOrEqual(RadioMessage.ItemOil("AMYXX", 99999, -20, 9).Length, 29);
            // "tx:" + the payload must also fit a line the boards read comfortably
            Assert.LessOrEqual(Messages.Transmit(RadioMessage.ItemOil("AMYXX", 99999, -20, 9)).Length, 32);
        }

        [Test]
        public void TheOldRaceMessages_StillParse_NextToTheNewOne()
        {
            Assert.AreEqual(RadioKind.Position, RadioMessage.Parse("p:AMY:1234:-2").Kind);
            Assert.AreEqual(RadioKind.Finish, RadioMessage.Parse("f:AMY:72.35").Kind);
            Assert.AreEqual(RadioKind.Go, RadioMessage.Parse("go:4821:uwa:2000").Kind);
            Command rx = CommandParser.Parse("rx:i:AMY:rocket:BEN:3");
            Assert.AreEqual(CommandKind.Radio, rx.Kind);
            Assert.AreEqual(RadioKind.Item, RadioMessage.Parse(rx.Text).Kind);
        }
    }
}
