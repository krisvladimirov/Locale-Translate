using System;
using System.Collections.Generic;
using System.Text;

namespace LocalisationTranslator
{
    /// <summary>
    /// Represent an error which occured while running the app
    /// </summary>
    public class ErrorLog
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="occurance"></param>
        public ErrorLog(Occurance occurance)
        {
            this.Occurance = occurance;
            this.Line = 0;
            FormatMessage(this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="occurance"></param>
        /// <param name="line"></param>
        public ErrorLog(Occurance occurance, int line)
        {
            this.Occurance = occurance;
            this.Line = line;
            FormatMessage(this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="occurance"></param>
        /// <param name="line"></param>
        /// <param name="token"></param>
        /// <param name="tokenIndex"></param>
        public ErrorLog(Occurance occurance, int line, string token, int tokenIndex)
        {
            this.Occurance = occurance;
            this.Line = line;
            this.Token = token;
            this.TokenIndex = tokenIndex;
            FormatMessage(this);
        }
        /// <summary>
        /// The error message to spit out
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// When did the error happen
        /// </summary>
        public Occurance Occurance { get; set; }

        /// <summary>
        /// The line at which the error occured
        /// </summary>
        public int Line { get; set; }

        /// <summary>
        /// The token at which the error occured
        /// </summary>
        public string? Token { get; set; }

        /// <summary>
        /// The token index (i.e. column) at which the error occured
        /// </summary>
        public int TokenIndex { get; set; }

        /// <summary>
        /// Formats the error for dumping.
        /// </summary>
        /// <param name="error">The error to format</param>
        /// <returns></returns>
        public static void FormatMessage(ErrorLog error)
        {
            switch (error.Occurance)
            {
                case Occurance.NoData:
                    error.Message = $"The file '{error.Token}' doesn't have any contents";
                    break;

                case Occurance.WhenHeadersAreLess:
                    error.Message = $"It seems that there are less headers in '{error.Token}' than in 'AppSettings.FileStructure.Headers'";
                    break;

                case Occurance.WhenHeadersAreMore:
                    error.Message = $"It seems that there are less headers in '{error.Token}' than in 'AppSettings.FileStructure.Headers'";
                    break;

                case Occurance.WhenHeadersAreNotMatching:
                    error.Message = $"The header: '{error.Token}' at column: {error.TokenIndex} does not match with the specified header in 'AppSettings.FileStructure.Headers'";
                    break;

                case Occurance.WhenReadingMissingData:
                    error.Message = $"At line: {error.Line}, no value was found for '{error.Token}'";
                    break;

                case Occurance.WhenReadingBadData:
                    error.Message = "";
                    break;

                case Occurance.WhenValidating:
                    error.Message = "";
                    break;

                case Occurance.WhenShipping:
                    error.Message = "";
                    break;

                default:
                    error.Message = "Error message could not be created, error type not matched";
                    break;

            }
        }
    }

    /// <summary>
    /// When did the error happen
    /// </summary>
    public enum Occurance
    {
        NoData = 0,
        WhenHeadersAreLess = 1,
        WhenHeadersAreMore = 2,
        WhenHeadersAreNotMatching = 3,
        WhenReadingMissingData = 4,
        WhenReadingBadData = 5,
        WhenValidating = 6,
        WhenShipping = 7,
        CSVHelperThrow = 8
    }
}
