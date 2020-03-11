# Localisation Translator
A small application for translating localisation strings supplied in a CSV format. The application makes use of Amazon's Translate service using <a href="https://www.nuget.org/packages/AWSSDK.Translate">AWSSDK.Translate (Nuget package)</a>. For more information regarding Amazon Translate language support and features visit: <a href="https://aws.amazon.com/translate/details/">Features and Documentation</a>. 

NOTE: You would need to have an AWS account with Translate services active on it.

## Why built it?
Because I needed a simple and relatively fast way to translate a large set of localisation strings, where files had different structure.

## What features does it support?

## What about future features?


## How to use it
First and foremost the app needs to be configured inside ```./appsettings.json```.
Example configuration:
```
{
  "Settings": {
    "Source": "en",
    "Target": "bg",
    "FileStructure": {
      "Path": "example.csv",
      "Headers": [ "key", "context", "language", "text" ],
      "TextHeader": "text",
      "KeyHeader": "key",
      "LanguageHeader": "language"
    },
    
    "Options": {
      "Validation": {
        "AddKeyWithNoText": true,
        "ICUandHTML": {
          "SeparateFile": false,
          "Translate": false
        }
      },
      "ComparisonFile": true
    }
  },
  "AWS": {
    "Profile": "default",
    "Region": "us-west-2"
  }
}
```
#### Attributes and their functionality
- ```"Source:"``` - Specifies the source language
- ```"Target:"``` - Specifies the target language
- ```"Path:"``` - The relative path to the CSV file
- ```"Headers:"``` - The headers of the CSV file
- ```"TextHeader:"``` - The header at which the text that needs translation is
- ```"KeyHeader:"``` - The header at which the localisation string's key is
- ```"LanguageHeader:"``` - The header at which the language code is
- ```"AddKeyWithNoText:"``` - Whether or not to add a key (i.e. whole record) if text field is empty
- ```"SeparateFile:"``` - Whether or not to add keys containing ICU or HTML strings to the main translated output
- ```"Translate:``` - Whether translation should be attempted on keys containing ICU or HTML strings
- ```"ComparisonFile:"``` - Whether or not to produce a comparison file

| Attribute          |     Necessary      | Default value |
| ---                |        :---:       |      :---:    |
| Source             |                    |      N/A      |
| Target             | :heavy_check_mark: |      N/A      |
| Path               | :heavy_check_mark: |      N/A      |
| Headers            | :heavy_check_mark: |      N/A      |
| Text Header        | :heavy_check_mark: |      N/A      |
| KeyHeader          | :heavy_check_mark: |      N/A      |
| LanguageHeader     | :heavy_check_mark: |      N/A      |
| AddKeyWithNoText   |                    |     false     |
| SeparateFile       |                    |     false     |
| Translate          |                    |     false     |
| Comparison file    |                    |     false     |

## What does the CSV validate?
1. Check if the headers in the input file are matching the provided headers in AppSettings
   1. On the first header that does not match an error is logged
2. If there are less header an error is logged process exits
3. If there are more header an error is logged process exits
4. If there are not errors the log is simply not created.
5. If there is missing data for header an error is logged process continues

## What is supported?
1. Finding the original line now working :heavy_check_mark:
2. Handle creation of new output files
   - Saves everything under the **..\\\\output\\\\run-{number}** directory. Creates **..\\\\output** if it is missing and stores the outputs of each run in a separate directory
   - The numbering is based on how many **..\\\\output\\\\run-{number}** there are ("\\\\run-[0-9]\Z")
   - Test it on Unix?
3. Comparison file ouput **false** by default

## What is not supported yet
1. Comparison records
2. Multi language translation
3. Cost estimates
4. Validating AWS credentials, so no '**Who Dis**' situations happen
5. Doesn't check if the if the language field are matching the source language
   1. AWS could potentially throw an error if the source language is 'en' but something else is given as "Какво прайм сега, а? Ще се наказваме ли, ти ми кажи?" (To be tested)
6. Terminologies are not supported.