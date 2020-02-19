using Amazon.Translate;
using Amazon.Translate.Model;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace LocalisationTranslator
{
    /// <summary>
    /// 
    /// </summary>
    public class TranslateService
    {
        /// <summary>
        /// 
        /// </summary>
        private IAmazonTranslate translate;
        public TranslateService(IAmazonTranslate translate)
        {
            this.translate = translate;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="fileStream"></param>
        /// <returns></returns>
        public async Task<ImportTerminologyResponse> SetTerminolgy(string name, MemoryStream fileStream)
        {
            return await this.translate.ImportTerminologyAsync(new ImportTerminologyRequest
            {
                Name = name,
                MergeStrategy = MergeStrategy.OVERWRITE,
                TerminologyData = new TerminologyData
                {
                    File = fileStream,
                    Format = TerminologyDataFormat.CSV
                }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="sourceLanguage"></param>
        /// <param name="targetLanguage"></param>
        /// <returns></returns>
        public async Task<TranslateTextResponse> TranslateText(string text, string sourceLanguage, string targetLanguage)
        {
            return await this.TranslateText(text, sourceLanguage, targetLanguage, null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text">The text to translate.</param>
        /// <param name="sourceLanguage">The source language of the text.</param>
        /// <param name="targetLanguage">The target language to translate the text to.</param>
        /// <param name="terminologies">Set of terminologies to use.</param>
        /// <returns></returns>
        public async Task<TranslateTextResponse> TranslateText(string text, string sourceLanguage, string targetLanguage, List<string> terminologies)
        {
            var request = new TranslateTextRequest
            {
                SourceLanguageCode = sourceLanguage,
                TargetLanguageCode = targetLanguage,
                TerminologyNames = terminologies,
                Text = text
            };

            return await this.translate.TranslateTextAsync(request);
        }
    }
}
