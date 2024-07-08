﻿using Haruka.Arcade.SEGA835Lib.Debugging;
using Haruka.Arcade.SEGA835Lib.Devices;
using Haruka.Arcade.SEGA835Lib.Devices.Card;
using Haruka.Arcade.SEGA835Lib.Devices.Card._837_15396;
using Haruka.Arcade.SEGA835Lib.Serial;
using NUnit.Framework.Legacy;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _835TestsMaybeLess {

    internal class Aime837_15396Test {

        private AimeCardReader_837_15396 reader;

        [SetUp]
        public void Setup() {
            reader = new AimeCardReader_837_15396(3, true);
            reader.serial.DumpRWCommandsToLog = true;
            reader.serial.DumpBytesToLog = true;
        }

        [TearDown]
        public void Cleanup() {
            reader?.Disconnect();
        }

        [Test]
        public void T01_TestGetInfo() {
            Assert.That(reader.Connect(), Is.EqualTo(DeviceStatus.OK));
            Assert.That(reader.GetHWVersion(out string version), Is.EqualTo(DeviceStatus.OK));
            Assert.That(version, Is.Not.Null);
            Assert.That(reader.GetFWVersion(out string version2), Is.EqualTo(DeviceStatus.OK));
            Assert.That(version2, Is.Not.Null);
        }

        [Test]
        public void T02_TestOfflineRead() {
            Assert.That(reader.Connect(), Is.EqualTo(DeviceStatus.OK));
            Assert.That(reader.RadioOn(RadioOnType.Both), Is.EqualTo(DeviceStatus.OK));
            Assert.That(reader.StartPolling(), Is.EqualTo(DeviceStatus.OK));
            Thread.Sleep(100);
            Assert.That(reader.StopPolling(), Is.EqualTo(DeviceStatus.OK));
            Assert.That(reader.RadioOff(), Is.EqualTo(DeviceStatus.OK));
        }

        [Test]
        public void T03_TestDisco() {
            Assert.That(reader.Connect(), Is.EqualTo(DeviceStatus.OK));
            Assert.That(reader.LEDReset(), Is.EqualTo(DeviceStatus.OK));
            Assert.That(reader.LEDGetInfo(out string info), Is.EqualTo(DeviceStatus.OK));
            Log.Write(info);
            Assert.That(info, Is.Not.Null);
            Assert.That(reader.LEDGetHWVersion(out string info2), Is.EqualTo(DeviceStatus.OK));
            Log.Write(info2);
            Assert.That(info2, Is.Not.Null);
            Assert.That(reader.LEDSetColor(Color.Black), Is.EqualTo(DeviceStatus.OK));
            Thread.Sleep(200);
            for (int i = 0; i < 3; i++) {
                Assert.That(reader.LEDSetColor(Color.Red), Is.EqualTo(DeviceStatus.OK));
                Thread.Sleep(200);
                Assert.That(reader.LEDSetColor(Color.Green), Is.EqualTo(DeviceStatus.OK));
                Thread.Sleep(200);
                Assert.That(reader.LEDSetColor(Color.Blue), Is.EqualTo(DeviceStatus.OK));
                Thread.Sleep(200);
                Assert.That(reader.LEDSetColor(Color.White), Is.EqualTo(DeviceStatus.OK));
                Thread.Sleep(200);
            }
            Assert.That(reader.LEDSetColor(Color.Black), Is.EqualTo(DeviceStatus.OK));
        }

        [Test]
        public void T04_TestRead() {
            Assert.That(reader.Connect(), Is.EqualTo(DeviceStatus.OK));
            Assert.That(reader.RadioOn(RadioOnType.Both), Is.EqualTo(DeviceStatus.OK));
            Assert.That(reader.StartPolling(), Is.EqualTo(DeviceStatus.OK));
            Thread.Sleep(100);
            int timeout = 10000;
            while (reader.IsPolling()) {
                if (reader.HasDetectedCard()) {
                    break;
                }
                Thread.Sleep(100);
                timeout -= 100;
                if (timeout <= 0) {
                    Assert.Fail("Card Read Timeout");
                }
            }
            Assert.That(reader.HasDetectedCard(), Is.True);
            Assert.That(reader.GetCardType(), Is.Not.Null);
            Assert.That(reader.GetCardUID(), Is.Not.Null);
            Assert.That(reader.StopPolling(), Is.EqualTo(DeviceStatus.OK));
            Assert.That(reader.RadioOff(), Is.EqualTo(DeviceStatus.OK));
        }
    }
}
