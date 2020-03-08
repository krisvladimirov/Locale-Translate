using NUnit.Framework;

namespace LocalisationTranslator.Test
{
    [TestFixture]
    public class TestApp
    {
        
        [SetUp]
        public void Setup()
        {
            App.erroredLines.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            App.erroredLines.Clear();
        }

        #region FindOriginalLine
        /*
         * Note errored lines are not zero-based, also the header line is taken into account,
         * therefore line 2 is the first one which could have an error
         * currentLine is the index at which an error has occured while attempting to translate or ICU/HTML has been found,
         * therefore to correct map is always currentLine + 2 (header + non zero-based input)
         */

        /// <summary>
        /// Assume total keys are 10
        /// Assume 1 keys has failed the CsvHelper's validation
        /// </summary>
        [Test]
        public void ErrorAtFirstRecord()
        {
            int currentLine = 0;
            App.erroredLines.Add(2);
            var result = App.FindOriginalLine(currentLine);
            Assert.That(result, Is.EqualTo(3));
        }

        /// <summary>
        /// Assume total keys are 10
        /// Assume 1 key has failed the CsvHelper's validation
        /// </summary>
        [Test]
        public void ErrorAtLastRecord()
        {
            int currentLine = 3;
            App.erroredLines.Add(2);
            var result = App.FindOriginalLine(currentLine);
            Assert.That(result, Is.EqualTo(6));

        }
        /// <summary>
        /// Assume total keys are 10
        /// Assume 2 keys have failed the CsvHelper's validation
        /// First key at line 2 and last key at line 11
        /// </summary>
        /// <param name="currentLine">The current line (index of the record that is being translated</param>
        /// <param name="expected">The expected original line</param>
        [TestCase(2, 5)]
        [TestCase(7, 10)]
        public void ErrorAtFirstAndLastRecord(int currentLine, int expected)
        {
            App.erroredLines.Add(2);
            App.erroredLines.Add(11);
            var result = App.FindOriginalLine(currentLine);
            Assert.That(result, Is.EqualTo(expected));
        }


        /// <summary>
        /// Assume total keys are 10
        /// Assume 5 keys have failed the CsvHelper's validation
        /// 1 Header key
        /// 2 Failed
        /// 3
        /// 4
        /// 5 Failed
        /// 6 Failed
        /// 7 Failed
        /// 8
        /// 9
        /// 10
        /// 11 Failed
        /// </summary>
        /// <param name="currentLine">The current line (index of the record that is being translated</param>
        /// <param name="expected">The expected original line</param>
        [TestCase(1, 4)]
        [TestCase(3, 9)]
        [TestCase(4, 10)]
        public void ConsecutiveRecordErrorsAtTheMiddle(int currentLine, int expected)
        {
            App.erroredLines.Add(2);
            App.erroredLines.Add(5);
            App.erroredLines.Add(6);
            App.erroredLines.Add(7);
            App.erroredLines.Add(11);
            var result = App.FindOriginalLine(currentLine);
            Assert.That(result, Is.EqualTo(expected));
        }

        /// <summary>
        /// Assume total keys are 10
        /// Assume 5 keys have failed the CsvHelper's validation
        /// 1 Header key
        /// 2 Failed
        /// 3 Failed
        /// 4 Failed
        /// 5 Failed
        /// 6
        /// 7
        /// 8
        /// 9
        /// 10
        /// 11 Failed
        [TestCase(0, 6)]
        [TestCase(3, 9)]
        [TestCase(4, 10)]
        public void ConsecutiveRecordErrorsAtTheStart(int currentLine, int expected)
        {
            App.erroredLines.Add(2);
            App.erroredLines.Add(3);
            App.erroredLines.Add(4);
            App.erroredLines.Add(5);
            App.erroredLines.Add(11);
            var result = App.FindOriginalLine(currentLine);
            Assert.That(result, Is.EqualTo(expected));
        }

        #endregion
    }
}
