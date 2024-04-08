using Microsoft.VisualStudio.TestPlatform.TestHost;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using test_task_EF;
using static System.Net.WebRequestMethods;

namespace test_task_EF.Tests
{
    [TestClass]
    public class ParserTests
    {

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ParseArguments_MissingRequiredArgument_ThrowsArgumentException()
        {
            var arguments = new Dictionary<string, Argument>
            {
                { "path", new Argument("", true, "path") },
                { "ip", new Argument("", true, "ip") }
            };

            Parser.ParseArguments(arguments);
        }

        [TestMethod]
        public void ParseJournal_ValidJournalEntries_ReturnsListWithEntries()
        {
            var lines = new List<string>
            {
                "192.168.0.1:2022-04-08 12:30:00",
                "10.0.0.1:2022-04-08 13:45:00"
            };

            var result = Parser.ParseJournal(lines);

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("192.168.0.1", result[0].Address);
            Assert.AreEqual(new DateTime(2022, 4, 8, 12, 30, 0), result[0].DateTime);
            Assert.AreEqual("10.0.0.1", result[1].Address);
            Assert.AreEqual(new DateTime(2022, 4, 8, 13, 45, 0), result[1].DateTime);
        }

        [TestMethod]
        public void ParseJournal_InvalidJournalEntries_ReturnsEmptyList()
        {
            var lines = new List<string>
            {
                "192.168.0.1:2022-04-08 12:30:00",
                "invalid_address:invalid_date_time"
            };

            var result = Parser.ParseJournal(lines);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("192.168.0.1", result[0].Address);
            Assert.AreEqual(new DateTime(2022, 4, 8, 12, 30, 0), result[0].DateTime);
        }
    }

    [TestClass]
    public class FilterTests
    {
        private List<JournalEntry> GenerateTestJournalEntries()
        {
            var entries = new List<JournalEntry>
            {
                new JournalEntry("192.168.0.1", new DateTime(2022, 4, 8, 12, 30, 0)),
                new JournalEntry("192.168.0.2", new DateTime(2022, 4, 8, 13, 45, 0)),
                new JournalEntry("10.0.0.1", new DateTime(2022, 4, 9, 14, 0, 0)),
                new JournalEntry("10.0.0.2", new DateTime(2022, 4, 9, 15, 15, 0)),
                new JournalEntry("0.0.0.1", new DateTime(2022, 4, 9, 15, 15, 0))
            };
            return entries;
        }

        [TestMethod]
        public void FilterByDate_EntriesWithinRange_ReturnsFilteredEntries()
        {
            var entries = GenerateTestJournalEntries();
            var startDate = "2022-04-08";
            var endDate = "2022-04-09";

            var result = Filter.FilterByDate(startDate, endDate, entries);

            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public void FilterByDate_NoEntriesWithinRange_ReturnsEmptyList()
        {
            var entries = GenerateTestJournalEntries();
            var startDate = "2022-04-10";
            var endDate = "2022-04-11";

            var result = Filter.FilterByDate(startDate, endDate, entries);

            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void FilterByIpRange_EntriesWithinRange_ReturnsFilteredEntries()
        {
            var entries = GenerateTestJournalEntries();
            var lowerRange = "192.168.0.0";

            var result = Filter.FilterByIpRange(lowerRange, entries);

            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public void FilterByIpMask_ValidMask_ReturnsFilteredEntries()
        {
            var entries = GenerateTestJournalEntries();
            var mask = "24";

            var result = Filter.FilterByIpMask(mask, entries);

            Assert.AreEqual(1, result.Count);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void FilterByIpMask_InvalidMask_ThrowsArgumentException()
        {
            var entries = GenerateTestJournalEntries();
            var invalidMask = "40"; // Invalid mask value

            Filter.FilterByIpMask(invalidMask, entries);
        }
    }

    
}