using System;
using System.Collections.Generic;
using System.Text;

namespace LocalisationTranslator
{
    /// <summary>
    /// Represent an a single error which occured while running the app or notification which would give insight about a key
    /// </summary>
    public class Log
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="occurance"></param>
        public Log(Occurance occurance)
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
        public Log(Occurance occurance, int line)
        {
            this.Occurance = occurance;
            this.Line = line;
            FormatMessage(this);
        }

        public Log(Occurance occurance, int line, string token){
            this.Occurance = occurance;
            this.Line = line;
            this.Token = token;
            FormatMessage(this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="occurance"></param>
        /// <param name="line"></param>
        /// <param name="token"></param>
        /// <param name="tokenIndex"></param>
        public Log(Occurance occurance, int line, string token, int tokenIndex)
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
        public static void FormatMessage(Log log)
        {
            switch (log.Occurance)
            {
                case Occurance.NoData:
                    log.Message = $"The file '{log.Token}' doesn't have any contents";
                    break;

                case Occurance.WhenHeadersAreLess:
                    log.Message = $"It seems that there are less headers in '{log.Token}' than in 'AppSettings.FileStructure.Headers'";
                    break;

                case Occurance.WhenHeadersAreMore:
                    log.Message = $"It seems that there are less headers in '{log.Token}' than in 'AppSettings.FileStructure.Headers'";
                    break;

                case Occurance.WhenHeadersAreNotMatching:
                    log.Message = $"The header: '{log.Token}' at column: {log.TokenIndex} does not match with the specified header in 'AppSettings.FileStructure.Headers'";
                    break;

                case Occurance.WhenReadingMissingData:
                    log.Message = $"At line: {log.Line}, no value was found for '{log.Token}'";
                    break;

                case Occurance.WhenReadingBadData:
                    log.Message = "";
                    break;

                case Occurance.WhenValidating:
                    log.Message = "";
                    break;

                case Occurance.WhenTranslating:
                    log.Message = "";
                    break;

                case Occurance.WhenShipping:
                    log.Message = "";
                    break;

                case Occurance.CSVHelperThrow:
                    log.Message = "";
                    break;

                case Occurance.NotTranslatedSeparateFile:
                    log.Message = $"The localisation string with key '{log.Token}' with original line ({log.Line}) was not processed as requested, but was saved on a separate file as requested.";
                    break;

                case Occurance.NotTranslatedSameFile:
                    log.Message = $"The localisation string with key '{log.Token}' with original line ({log.Line}) was not processed as requested, but was saved on the translated file.";
                    break;

                case Occurance.TranslatedSeparateFile:
                    log.Message = $"The localisation string with key '{log.Token}' with original line ({log.Line}) was processed as requested, and was saved on a separate file as requested.";
                    break;
                case Occurance.TranslatedSameFile:
                    log.Message = $"The localisation string with key '{log.Token}' with original line ({log.Line}) was processed as requested, and was saved on the translated file as requested.";
                    break;

                default:
                    log.Message = "Error message could not be created, error type not matched";
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
        WhenTranslating = 6,
        WhenValidating = 7,
        WhenShipping = 8,
        CSVHelperThrow = 9,
        NotTranslatedSeparateFile = 10,
        NotTranslatedSameFile = 11,
        TranslatedSeparateFile = 12,
        TranslatedSameFile = 13
    }
}
