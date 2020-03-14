# Localisation Translator
A small application for translating localisation strings supplied in a CSV format. The application makes use of Amazon's Translate service using <a href="https://www.nuget.org/packages/AWSSDK.Translate">AWSSDK.Translate (Nuget package)</a>. For more information regarding Amazon Translate language support and features visit: <a href="https://aws.amazon.com/translate/details/">Features and Documentation</a>. 

NOTE: You would need to have an AWS account with Translate services active on it.

## Why built it?
Because I needed a simple and relatively fast way to translate a large set of localisation strings, where files had different structure.

## What features does it support?

## What about future features?


## Execution steps?

## How to use it
First and foremost the app needs to be configured inside ```./appsettings.json```.</br>
**Example configuration:**
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
**Example CSV input file:**
```
key,context,language,text
Welcome,,en,Welcome to the app
Welcome.FacebookUser,,en,Welcome to the App because you joined from FB you get free credits
Tutorial.CreateEvent,,en,To create <strong>a given<strong> provide the following proterties
Tutorial.JoinEvent,,en,To join an event click here
Tutorial.SearchEvent,,en,To search for an event click here
Tutorial.ShareEvent,,en,To share an event click here
Tutorial.BookEvent,,en,To book an event do this
Notification.Pending.JoinRequest,,en,You have {0} users asking to join
Notification.ReadyCheck.Accept,,en,<strong>Accept<strong>
Notification.ReadyCheck.Decline,,en,<strong>Decline<strong>
```

**Example of expected output files:**


#### Attributes and their functionality
- ```"Source:"``` - Specifies the source language
  - Amazon Translate can determine the source of the language but it advisible to provide the language code
- ```"Target:"``` - Specifies the target language
- ```"Path:"``` - The relative path to the CSV file
- ```"Headers:"``` - The headers of the CSV file
- ```"TextHeader:"``` - The header at which the text that needs translation is
- ```"KeyHeader:"``` - The header at which the localisation string's key is
- ```"LanguageHeader:"``` - The header at which the language code is
- ```"AddKeyWithNoText:"``` - Whether or not to add a key in the translated output (i.e. whole record) if text field is empty
  - i.e. ```Tutorial.ShareEvent,,en,``` has no text field (check table for default values)
- ```"SeparateFile:"``` - Whether or not to add keys containing ICU or HTML strings to the main translated output
- ```"Translate:"``` - Whether translation should be attempted on keys containing ICU or HTML strings
- ```"ComparisonFile:"``` - Whether or not to produce a comparison file, with structure:
  - ```key,en(the source lang),ja(target lang)```
  - **The translated output file will still be created**

| Attribute          |     Necessary      | Default value |
| ---                |        :---:       |      :---:    |
| Source             |                    |      N/A      |
| Target             | :heavy_check_mark: |      N/A      |
| Path               | :heavy_check_mark: |      N/A      |
| Headers            | :heavy_check_mark: |      N/A      |
| Text Header        | :heavy_check_mark: |      N/A      |
| KeyHeader          | :heavy_check_mark: |      N/A      |
| LanguageHeader     | :heavy_check_mark: |      N/A      |
| AddKeyWithNoText   |                    |     true      |
| SeparateFile       |                    |     false     |
| Translate          |                    |     false     |
| Comparison file    |                    |     false     |

## What validation is in place?
### Header validation
The headers of the provided CSV file will be checked if they are matching the provided headers in ```./appsettings.json```. Check (3) is case sensitive. Rules when specifying the headers attribute:
1. Check if the ```"Headers:"``` attribute is **NOT** empty.  
2. Check if the number of headers in the input file matches the number of headers in ```./appsettings.json```.
3. Check if the headers in the input file are matching the provided headers in ```./appsettings.json```. The app is case sensitive.

Whenever a single rule is broken, the app will not proceed to cost estimation step.

### Missing field is found?

### Bad data is found?
As described in <a href="https://joshclose.github.io/CsvHelper/api/CsvHelper.Configuration/Configuration/">CsvHelper.Configuration</a> bad data is considered a field that *"contains a quote and that field is not quoted (escaped)"*.



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