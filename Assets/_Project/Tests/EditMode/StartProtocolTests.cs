using CityScooter.Protocol;
using NUnit.Framework;

namespace CityScooter.Tests
{
    /// <summary>v0.3 districts: a board's "start:kingspark" picks where rides start (block "start rides in [Kings Park]").</summary>
    public class StartProtocolTests
    {
        [TestCase("start:kingspark", "kingspark")]
        [TestCase("START:KingsPark", "kingspark")]
        [TestCase("start : Kings Park", "kingspark")]
        [TestCase("start:kings_park", "kingspark")]
        [TestCase("start:quay", "quay")]
        [TestCase("start:perth", "perth")] // not on the route: the game ignores it (the web monitor gives the list)
        public void Start_Parses_ToADistrictId(string line, string id)
        {
            Command c = CommandParser.Parse(line);
            Assert.AreEqual(CommandKind.Start, c.Kind);
            Assert.AreEqual(id, c.Text);
            Assert.IsTrue(c.IsProtocol);
        }

        [TestCase("start:")]
        [TestCase("start:123")]
        [TestCase("start: !")]
        public void Start_WithoutLetters_IsMalformed(string line)
        {
            Assert.AreEqual(CommandKind.Malformed, CommandParser.Parse(line).Kind);
        }

        [Test]
        public void Start_Alone_IsAStudentPrint()
        {
            Assert.AreEqual(CommandKind.Unknown, CommandParser.Parse("start").Kind);
        }

        [Test]
        public void EveryRouteId_SurvivesTheIdFilter()
        {
            foreach (string id in Messages.ZoneIds) Assert.AreEqual(id, Messages.DistrictId(id));
            Assert.AreEqual(16, Messages.DistrictId("abcdefghijklmnopqrstuvwxyz").Length, "at most 16 letters");
        }

        [Test]
        public void Start_CannotBeSaid()
        {
            Assert.IsTrue(Messages.IsGameMessage("start"));
            Assert.IsFalse(Messages.IsSayWord("start"));
        }
    }
}
