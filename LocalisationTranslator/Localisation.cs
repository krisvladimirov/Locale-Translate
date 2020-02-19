using System;
using System.Collections.Generic;
using System.Text;

namespace LocalisationTranslator
{
    public class Localisation
    {
        /// <summary>
        /// The localisation key
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// The context of the localisation string
        /// </summary>
        public string? Context {get; set;}

        /// <summary>
        /// The language code
        /// </summary>
        public string Language { get; set; }

        /// <summary>
        /// The actual text value of the key
        /// </summary>
        public string Text { get; set; }
    }
}
