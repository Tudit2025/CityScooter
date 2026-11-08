using CityScooter.Protocol;
using NUnit.Framework;

namespace CityScooter.Tests
{
    /// <summary>v0.3 ramps and air: "trick", "do:ramp", and the game's air:/land: messages.</summary>
    public class RampProtocolTests
    {
        [TestCase("trick")]
        [TestCase("TRICK")]
        [TestCase(" trick ")]
        [TestCase("trick:1")]
        public void Trick_Parses(string line)
        {
            Assert.AreEqual(CommandKind.Trick, CommandParser.Parse(line).Kind);
        }

        [Test]
        public void DoRamp_IsATrigger()
        {
            Command c = CommandParser.Parse("do:ramp");
            Assert.AreEqual(CommandKind.Trigger, c.Kind);
            Assert.AreEqual("ramp", c.Text);
            Assert.AreEqual(CommandKind.Trigger, CommandParser.Parse("DO : Ramp").Kind);
            Assert.AreEqual(CommandKind.Malformed, CommandParser.Parse("do:ramp:2").Kind, "no number for ramps");
        }

        [Test]
        public void Mixer_CountsTricks_UntilConsumed()
        {
            var mixer = new ControlMixer();
            mixer.Apply(CommandParser.Parse("trick"), 0);
            mixer.Apply(CommandParser.Parse("trick"), 0.01);
            Assert.AreEqual(2, mixer.PendingTricks);
            Assert.AreEqual(2, mixer.ConsumeTricks(), "every line counts (the game's spam rule sees them all)");
            Assert.AreEqual(0, mixer.ConsumeTricks());
            mixer.Apply(CommandParser.Parse("trick"), 1);
            mixer.Reset();
            Assert.AreEqual(0, mixer.PendingTricks);
        }

        [Test]
        public void AirAndLandMessages()
        {
            Assert.AreEqual("air:1", Messages.Air(true));
            Assert.AreEqual("air:0", Messages.Air(false));
            Assert.AreEqual("land:ok", Messages.LandOk);
            Assert.AreEqual("land:trick", Messages.LandTrick);
        }

        [TestCase("trick")]
        [TestCase("air:1")]
        [TestCase("land:ok")]
        public void TheGamesWords_CannotBeSaid(string word)
        {
            Assert.IsTrue(Messages.IsGameMessage(word));
            Assert.IsFalse(Messages.IsSayWord(word));
        }
    }
}
