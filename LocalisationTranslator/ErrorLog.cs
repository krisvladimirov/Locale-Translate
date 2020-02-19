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
        // Error list
        private static string 


        /// <summary>
        /// The error message spit out
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// When did the error happen
        /// </summary>
        public Occurance Occurance { get; set; }

        /// <summary>
        /// The line at which the error occured
        /// </summary>
        public int Line { get; set; }

        /// <summary>
        /// The field at which the error occured
        /// </summary>
        public int Field { get; set; }

        /// <summary>
        /// The field index (i.e. column) at which the error occured
        /// </summary>
        public int FieldIndex { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="occurance"></param>
        /// <returns></returns>
        public string FormatMessage(Occurance occurance)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// When did the error happen
    /// </summary>
    public enum Occurance
    {
        WhenAttempingToRead = 0,
        WhenValidating = 1,
        WhenShipping = 2
    }
}
