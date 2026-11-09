using CityScooter.Protocol;
using NUnit.Framework;

namespace CityScooter.Tests
{
    /// <summary>v0.4 radio race (docs/radio-race.md "实现约定 v1"): race:, rx: and what is inside rx:, pos:, fin:.</summary>
    public class RaceProtocolTests
    {
        [TestCase("race:AMY:12", "AMY", 12)]
        [TestCase("race:amy:12", "AMY", 12)]
        [TestCase("RACE : Amy : 0", "AMY", 0)]
        [TestCase("race:Chloe-B:255", "CHLOE", 255)] // letters and digits only, upper case, at most 5
        [TestCase("race:R2 D2:13", "R2D2", 13)]
        [TestCase("race:a:b:11", "AB", 11)]
        public void Race_Parses_NameAndGroup(string line, string name, int group)
        {
            Command c = CommandParser.Parse(line);
            Assert.AreEqual(CommandKind.Race, c.Kind);
            Assert.AreEqual(name, c.Text);
            Assert.AreEqual(group, (int)c.Value);
            Assert.IsTrue(c.IsProtocol);
        }

        [TestCase("race:AMY")]
        [TestCase("race:AMY:")]
        [TestCase("race::12")]
        [TestCase("race:!!!:12")]
        [TestCase("race:AMY:256")]
        [TestCase("race:AMY:-1")]
        [TestCase("race:AMY:twelve")]
        [TestCase("race:AMY:1.5")]
        public void Race_WithAnEmptyNameOrBadGroup_IsMalformed(string line)
        {
            Assert.AreEqual(CommandKind.Malformed, CommandParser.Parse(line).Kind, line);
        }

        [Test]
        public void Rx_KeepsThePayloadAsItCame()
        {
            Command c = CommandParser.Parse("rx:p:BEN:120:-2");
            Assert.AreEqual(CommandKind.Radio, c.Kind);
            Assert.AreEqual("p:BEN:120:-2", c.Text);
            Command other = CommandParser.Parse("rx:hello there");
            Assert.AreEqual(CommandKind.Radio, other.Kind, "anything heard on the radio is passed on (the game ignores it)");
            Assert.AreEqual("hello there", other.Text);
            Assert.AreEqual(RadioKind.None, RadioMessage.Parse(other.Text).Kind);
        }

        [TestCase("p:BEN:120:-2", "BEN", 120f, -2f)]
        [TestCase("P : ben : 12.5 : 1.7", "BEN", 12.5f, 1.7f)]
        [TestCase("p:AMY:-5:0", "AMY", 0f, 0f)] // no negative distance
        [TestCase("p:AMY:99999:50", "AMY", 99999f, 20f)] // lanes clamped; the distance is checked by the game (plausibility)
        public void Position_Parses(string payload, string name, float distance, float lane)
        {
            RadioMessage m = RadioMessage.Parse(payload);
            Assert.AreEqual(RadioKind.Position, m.Kind);
            Assert.AreEqual(name, m.Name);
            Assert.AreEqual(distance, m.Distance, 1e-4f);
            Assert.AreEqual(lane, m.Lane, 1e-4f);
        }

        [TestCase("p:BEN:120")]
        [TestCase("p:BEN:120:-2:9")]
        [TestCase("p::120:0")]
        [TestCase("p:BEN:far:0")]
        [TestCase("p:BEN:NaN:0")]
        [TestCase("p:BEN:120:left")]
        [TestCase("pos:120:0")]
        [TestCase("")]
        [TestCase("hello")]
        public void BrokenPositions_AreDropped(string payload)
        {
            Assert.AreEqual(RadioKind.None, RadioMessage.Parse(payload).Kind, payload);
        }

        [Test]
        public void Finish_Parses()
        {
            RadioMessage m = RadioMessage.Parse("f:AMY:72.35");
            Assert.AreEqual(RadioKind.Finish, m.Kind);
            Assert.AreEqual("AMY", m.Name);
            Assert.AreEqual(72.35f, m.Seconds, 1e-4f);
            Assert.AreEqual(RadioKind.None, RadioMessage.Parse("f:AMY:0").Kind);
            Assert.AreEqual(RadioKind.None, RadioMessage.Parse("f:AMY:-3").Kind);
            Assert.AreEqual(RadioKind.None, RadioMessage.Parse("f:AMY").Kind);
        }

        [TestCase("go:4821:uwa:2000", 4821, "uwa", 2000)]
        [TestCase("GO : 7 : Kings Park : 800", 7, "kingspark", 800)]
        [TestCase("go:4821:perth:2000", 4821, "uwa")] // not a district id: uwa
        [TestCase("go:0:cbd:100", 1, "cbd", 300)] // clamped to 1..99999 and 300..5000
        [TestCase("go:123456:quay:9000", 99999, "quay", 5000)]
        [TestCase("go:5:warnbro:1234.6", 5, "warnbro", 1235)]
        public void Go_Parses_AndClamps(string payload, int seed, string start, int metres = 2000)
        {
            RadioMessage m = RadioMessage.Parse(payload);
            Assert.AreEqual(RadioKind.Go, m.Kind);
            Assert.AreEqual(seed, m.Seed);
            Assert.AreEqual(start, m.Start);
            Assert.AreEqual(metres, m.Metres);
        }

        [TestCase("go:abc:uwa:2000")]
        [TestCase("go:12:uwa")]
        [TestCase("go:12:uwa:far")]
        [TestCase("go")]
        public void BrokenGo_IsIgnored(string payload)
        {
            Assert.AreEqual(RadioKind.None, RadioMessage.Parse(payload).Kind, payload);
        }

        [Test]
        public void GameToBoard_PosAndFin()
        {
            Assert.AreEqual("pos:1234:-2", Messages.Position(1234, -2));
            Assert.AreEqual("pos:0:0", Messages.Position(0, 0));
            Assert.AreEqual("pos:0:20", Messages.Position(-3, 99));
            Assert.AreEqual("fin:72.35", Messages.Finish(72.349f));
            Assert.AreEqual("fin:5.00", Messages.Finish(5f));
            Assert.LessOrEqual(("p:ABCDE:" + Messages.Position(99999, -20).Substring(4)).Length, 29, "a relayed p: fits in a radio packet");
        }

        [Test]
        public void RaceWords_AreTheGames()
        {
            foreach (string w in new[] { "race", "rx", "pos", "fin", "RACE" })
            {
                Assert.IsTrue(Messages.IsGameMessage(w), w);
                Assert.IsFalse(Messages.IsSayWord(w), w);
            }
            Assert.AreEqual(CommandKind.Malformed, CommandParser.Parse("say:race").Kind);
            Assert.AreEqual(CommandKind.Unknown, CommandParser.Parse("pos:12:0").Kind, "pos: is the game's: a board that echoes it is just printing");
            Assert.AreEqual(CommandKind.Unknown, CommandParser.Parse("race").Kind);
        }

        [Test]
        public void RaceName_Sanitises()
        {
            Assert.AreEqual("AMY", Messages.RaceName("amy"));
            Assert.AreEqual("ABCDE", Messages.RaceName("abcdefgh"));
            Assert.AreEqual("", Messages.RaceName("!!"));
            Assert.AreEqual("", Messages.RaceName(null));
            Assert.AreEqual("E", Messages.RaceName("é-e"));
        }
    }
}
