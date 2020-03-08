using System;
using System.Collections.Generic;
using System.Text;

namespace LocalisationTranslator
{
    /// <summary>
    /// Data model holding application settings
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public AppSettings() { }

        /// <summary>
        /// The code of the source language i.e. en
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// The code of the target language
        /// </summary>
        public string Target{ get; set; }

        /// <summary>
        /// The file structure of the CSV to read.
        /// </summary>
        public FileStructure FileStructure { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <value></value>
        public Options Options {get; set;}

    }

    /// <summary>
    /// Data model holding the CSV file structure for dynamic object creation
    /// </summary>
    public class FileStructure
    {
        /// <summary>
        /// The path to the file
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// The headers of the file
        /// </summary>
        public List<string> Headers { get; set; }

        /// <summary>
        /// Explicitly specifies which header contains the text for translation
        /// </summary>
        public string TextHeader { get; set; }

        /// <summary>
        /// Explicitly specifies which header contains the key
        /// </summary>
        public string KeyHeader { get; set; }
        
        /// <summary>
        /// Explicitly specifies which header contains the language code
        /// </summary>
        public string LanguageHeader { get; set; }
    }

    /// <summary>
    /// Data model holding the setting's validation
    /// </summary>
    public class Validation
    {
        /// <summary>
        /// Whether or not to add keys without any text to the translated output.
        /// `True` would add them, `False` will print them into a separate csv file.
        /// </summary>
        public bool AddKeyWithNoText { get; set; }

        /// <summary>
        /// Instructions on how to handle strings containing ICU format or HTML
        /// </summary>
        public MarkUpData ICUandHTML { get; set; }

    }

    /// <summary>
    /// 
    /// </summary>
    public class Options
    {
        /// <summary>
        /// The application validation settings
        /// </summary>
        public Validation Validation { get; set; }

        /// <summary>
        /// Whether or not to produce a comparison file
        /// False by default
        /// </summary>
        /// <value></value>
        public bool ComparisonFile { get; set; } = false;
    }

    /// <summary>
    /// Such data can be ICU string or string containing HTML
    /// </summary>
    public class MarkUpData
    {
        /// <summary>
        /// Whether or not to add keys containing ICU or HTML strings
        /// </summary>
        public bool SeparateFile { get; set; }

        /// <summary>
        /// Whether translation should be attempted on keys containing ICU or HTML strings
        /// </summary>
        public bool Translate { get; set; }
    }
    
}
