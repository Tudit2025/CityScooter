using System.Collections.Generic;
using CityScooter.Protocol;
using NUnit.Framework;

namespace CityScooter.Tests
{
    public class CommandParserTests
    {
        [TestCase("st:-35", -35f)]
        [TestCase("st:0", 0f)]
        [TestCase("st:42.5", 42.5f)]
        [TestCase("ST: 20", 20f)]
        [TestCase("  st : -7  ", -7f)]
        [TestCase("st:250", 100f)]
        [TestCase("st:-999", -100f)]
        public void Steer_ParsesAndClamps(string line, float expected)
        {
            Command c = CommandParser.Parse(line);
            Assert.AreEqual(CommandKind.Steer, c.Kind);
            Assert.AreEqual(expected, c.Value, 1e-4f);
        }

        [TestCase("st:abc")]
        [TestCase("st:")]
        [TestCase("st:NaN")]
        public void Steer_BadValue_IsMalformedNotSteer(string line)
        {
            Assert.AreEqual(CommandKind.Malformed, CommandParser.Parse(line).Kind);
        }

        [TestCase("jp", CommandKind.Jump)]
        [TestCase("JP", CommandKind.Jump)]
        [TestCase("jp:1", CommandKind.Jump)]
        [TestCase("br", CommandKind.Brake)]
        [TestCase(" br ", CommandKind.Brake)]
        public void JumpAndBrake(string line, CommandKind kind)
        {
            Assert.AreEqual(kind, CommandParser.Parse(line).Kind);
        }

        [Test]
        public void Hello_CarriesLowercaseVersion()
        {
            Command c = CommandParser.Parse("hi:V2");
            Assert.AreEqual(CommandKind.Hello, c.Kind);
            Assert.AreEqual("v2", c.Text);
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase((string)null)]
        [TestCase("Traceback (most recent call last):")]
        [TestCase("hello world")]
        [TestCase("MicroPython v1.25.0 on 2025-01-01; micro:bit v2 with nRF52833")]
        [TestCase("NameError: name 'stear' isn't defined")]
        public void NonProtocolLines_AreUnknown_NeverThrow(string line)
        {
            Assert.AreEqual(CommandKind.Unknown, CommandParser.Parse(line).Kind);
        }
    }

    public class LineFramerTests
    {
        [Test]
        public void SplitLine_IsJoinedOnlyWhenNewlineArrives()
        {
            var f = new LineFramer();
            var lines = new List<string>();
            Assert.AreEqual(0, f.Push("cr", lines));
            Assert.IsTrue(f.HasPartialLine);
            Assert.AreEqual(1, f.Push("ash\n", lines));
            CollectionAssert.AreEqual(new[] { "crash" }, lines);
        }

        [Test]
        public void CrLf_And_SeveralLinesPerChunk()
        {
            var f = new LineFramer();
            var lines = new List<string>();
            f.Push("st:10\r\njp\r\nst:", lines);
            f.Push("-20\n", lines);
            CollectionAssert.AreEqual(new[] { "st:10", "jp", "st:-20" }, lines);
        }

        [Test]
        public void OverLongLine_IsTruncated()
        {
            var f = new LineFramer(maxLineLength: 8);
            var lines = new List<string>();
            f.Push("0123456789abcdef\n", lines);
            CollectionAssert.AreEqual(new[] { "01234567" }, lines);
        }
    }

    public class ControlMixerTests
    {
        [Test]
        public void Steer_IsNormalised_AndTimesOut()
        {
            var m = new ControlMixer { SteerTimeout = 0.5 };
            m.Apply(new Command(CommandKind.Steer, -50f), now: 10.0);
            Assert.AreEqual(-0.5f, m.Steer(10.2), 1e-5f);
            Assert.IsTrue(m.HasRecentSteer(10.4));
            Assert.AreEqual(0f, m.Steer(10.6));
        }

        [Test]
        public void LatestSteerWins_AcrossDevices()
        {
            var m = new ControlMixer();
            m.Apply(new Command(CommandKind.Steer, 80f), 1.0);
            m.Apply(new Command(CommandKind.Steer, -20f), 1.05);
            Assert.AreEqual(-0.2f, m.Steer(1.1), 1e-5f);
        }

        [Test]
        public void ManyJumpsInOneFrame_CountAsOne()
        {
            var m = new ControlMixer();
            for (int i = 0; i < 5; i++) m.Apply(new Command(CommandKind.Jump), 1.0);
            Assert.AreEqual(5, m.PendingJumps);
            Assert.IsTrue(m.ConsumeJump());
            Assert.IsFalse(m.ConsumeJump());
        }

        [Test]
        public void Brake_HoldsBrieflyAfterEachMessage()
        {
            var m = new ControlMixer { BrakeHold = 0.3 };
            m.Apply(new Command(CommandKind.Brake), 2.0);
            Assert.IsTrue(m.Braking(2.2));
            Assert.IsFalse(m.Braking(2.31));
        }

        [Test]
        public void UnknownLines_ChangeNothing()
        {
            var m = new ControlMixer();
            m.Apply(CommandParser.Parse("hello"), 1.0);
            Assert.IsFalse(m.HasRecentSteer(1.0));
            Assert.IsFalse(m.ConsumeJump());
            Assert.IsFalse(m.Braking(1.0));
        }
    }

    public class MessagesTests
    {
        [TestCase(-3, "spd:0")]
        [TestCase(4, "spd:4")]
        [TestCase(12, "spd:9")]
        public void Speed_IsClampedToSingleDigit(int level, string expected)
        {
            Assert.AreEqual(expected, Messages.Speed(level));
        }

        [TestCase("pu", CommandKind.Punch)]
        [TestCase("PU", CommandKind.Punch)]
        [TestCase(" pu:1 ", CommandKind.Punch)]
        [TestCase("bo", CommandKind.Boost)]
        [TestCase("BO:1", CommandKind.Boost)]
        [TestCase("bl", CommandKind.Bell)]
        [TestCase("BL:1", CommandKind.Bell)]
        public void V1_PunchAndBell(string line, CommandKind kind)
        {
            Assert.AreEqual(kind, CommandParser.Parse(line).Kind);
        }

        [Test]
        public void V1_GameMessages()
        {
            Assert.AreEqual("1", Messages.ProtocolVersion);
            Assert.AreEqual("ko", Messages.Ko);
            Assert.AreEqual("warn:car", Messages.WarnCar);
            Assert.AreEqual("warn:ped", Messages.WarnPerson);
            Assert.AreEqual("warn:hole", Messages.WarnHole);
            Assert.AreEqual("turn:-1", Messages.Turn(-3));
            Assert.AreEqual("turn:0", Messages.Turn(0));
            Assert.AreEqual("turn:1", Messages.Turn(1));
        }

        [TestCase("say:party", "party")]
        [TestCase(" SAY : Jump_2! ", "Jump_2!")]
        public void V1_SayCarriesTheWord(string line, string word)
        {
            Command c = CommandParser.Parse(line);
            Assert.AreEqual(CommandKind.Say, c.Kind);
            Assert.AreEqual(word, c.Text);
        }

        [TestCase("say:")]
        [TestCase("say:crash")]
        [TestCase("say:GIFT")]
        [TestCase("say:warn:car")]
        [TestCase("say:sur")]
        [TestCase("say:jp")]
        [TestCase("say:two words")]
        [TestCase("say:abcdefghijklmnopq")]
        public void V1_SayRefusesGameMessagesAndOddWords(string line)
        {
            Assert.AreEqual(CommandKind.Malformed, CommandParser.Parse(line).Kind);
        }

        [TestCase("do:chase", "chase", 0)]
        [TestCase("DO : Stars : 3", "stars", 3)]
        [TestCase("do:turn:L", "turn", -1)]
        [TestCase("do:turn:right", "turn", 1)]
        [TestCase("do:car", "car", 0)]
        [TestCase("do:roadworks", "roadworks", 0)]
        public void V1_DoTriggersAGameEvent(string line, string name, int value)
        {
            Command c = CommandParser.Parse(line);
            Assert.AreEqual(CommandKind.Trigger, c.Kind);
            Assert.AreEqual(name, c.Text);
            Assert.AreEqual(value, (int)c.Value);
        }

        [TestCase("do:")]
        [TestCase("do:fireworks")]
        [TestCase("do:stars:9")]
        [TestCase("do:stars")]
        [TestCase("do:turn:up")]
        [TestCase("do:chase:now")]
        public void V1_DoRefusesUnknownEvents(string line)
        {
            Assert.AreEqual(CommandKind.Malformed, CommandParser.Parse(line).Kind);
        }

        // 11/9: "open:" asks for the same events at the start of every ride.
        [TestCase("open:stars:5", "stars", 5)]
        [TestCase("OPEN : chase", "chase", 0)]
        [TestCase("open:turn:L", "turn", -1)]
        [TestCase("open:ramp", "ramp", 0)]
        public void Open_IsAnEveryRideEvent(string line, string name, int value)
        {
            Command c = CommandParser.Parse(line);
            Assert.AreEqual(CommandKind.Opening, c.Kind);
            Assert.AreEqual(name, c.Text);
            Assert.AreEqual(value, (int)c.Value);
        }

        [TestCase("open:")]
        [TestCase("open:fireworks")]
        [TestCase("open:stars:6")]
        public void Open_RefusesUnknownEvents(string line)
        {
            Assert.AreEqual(CommandKind.Malformed, CommandParser.Parse(line).Kind);
        }

        [Test]
        public void Open_IsAGameWordABoardCannotSay()
        {
            Assert.IsTrue(Messages.IsGameMessage("open"));
            Assert.AreEqual(CommandKind.Malformed, CommandParser.Parse("say:open").Kind);
        }

        [Test]
        public void V1_ToonModeIsLatchedUntilChanged()
        {
            var mixer = new ControlMixer();
            Assert.IsFalse(mixer.Toon, "cartoon mode starts off");
            mixer.Apply(CommandParser.Parse("toon:1"), 0);
            Assert.IsTrue(mixer.Toon);
            mixer.Apply(CommandParser.Parse("st:20"), 1);
            Assert.IsTrue(mixer.Toon, "stays on until a program turns it off");
            mixer.Apply(CommandParser.Parse("TOON : 0"), 2);
            Assert.IsFalse(mixer.Toon);
            mixer.Apply(CommandParser.Parse("toon"), 3);
            Assert.IsTrue(mixer.Toon);
            Assert.AreEqual(CommandKind.Malformed, CommandParser.Parse("toon:maybe").Kind);
            mixer.Reset();
            Assert.IsFalse(mixer.Toon);
            Assert.AreEqual("boing", Messages.Boing);
        }

        [TestCase("sur", CommandKind.Surrender)]
        [TestCase(" SUR ", CommandKind.Surrender)]
        [TestCase("sur:1", CommandKind.Surrender)]
        public void Police_Surrender(string line, CommandKind kind)
        {
            Assert.AreEqual(kind, CommandParser.Parse(line).Kind);
        }

        [Test]
        public void Police_GameMessages()
        {
            Assert.AreEqual("wanted:0", Messages.Wanted(-2));
            Assert.AreEqual("wanted:3", Messages.Wanted(3));
            Assert.AreEqual("wanted:5", Messages.Wanted(9));
            Assert.AreEqual("chase:1", Messages.Chase(true));
            Assert.AreEqual("chase:0", Messages.Chase(false));
            Assert.AreEqual("cop:0", Messages.Cop(-1));
            Assert.AreEqual("cop:4", Messages.Cop(4));
            Assert.AreEqual("cop:9", Messages.Cop(30));
            Assert.AreEqual("heli:1", Messages.Heli(true));
            Assert.AreEqual("heli:0", Messages.Heli(false));
            Assert.AreEqual("inc:L", Messages.Incoming(-1));
            Assert.AreEqual("inc:C", Messages.Incoming(0));
            Assert.AreEqual("inc:R", Messages.Incoming(2));
            Assert.AreEqual("arrested", Messages.Arrested);
            Assert.AreEqual("busted", Messages.Busted);
            Assert.AreEqual("lost", Messages.Lost);
            // A board echoing what the game said must never steer, brake or surrender.
            foreach (string m in new[] { "wanted:3", "chase:1", "cop:2", "heli:1", "inc:C", "arrested", "busted", "lost" })
                Assert.AreEqual(CommandKind.Unknown, CommandParser.Parse(m).Kind, m);
        }

        [Test]
        public void Police_MixerCountsSurrenderOnce_AndResetClearsIt()
        {
            var mixer = new ControlMixer();
            mixer.Apply(CommandParser.Parse("sur"), 0);
            mixer.Apply(CommandParser.Parse("sur"), 0);
            Assert.IsTrue(mixer.ConsumeSurrender());
            Assert.IsFalse(mixer.ConsumeSurrender());
            mixer.Apply(CommandParser.Parse("sur"), 1);
            mixer.Reset();
            Assert.IsFalse(mixer.ConsumeSurrender());
        }

        [Test]
        public void V1_MixerCountsPunchAndBellOnce()
        {
            var mixer = new ControlMixer();
            mixer.Apply(CommandParser.Parse("pu"), 0);
            mixer.Apply(CommandParser.Parse("pu"), 0);
            mixer.Apply(CommandParser.Parse("bl"), 0);
            Assert.AreEqual(2, mixer.PendingPunches);
            Assert.IsTrue(mixer.ConsumePunch());
            Assert.IsFalse(mixer.ConsumePunch());
            Assert.IsTrue(mixer.ConsumeBell());
            Assert.IsFalse(mixer.ConsumeBell());
        }
    }
}
