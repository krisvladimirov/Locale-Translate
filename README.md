# Localisation Translator
A small application for translating localisation strings supplied in a CSV format. The application makes use of Amazon's Translate service using <a href="https://www.nuget.org/packages/AWSSDK.Translate">AWSSDK.Translate (Nuget package)</a>. For more information regarding Amazon Translate language support and features visit: <a href="https://aws.amazon.com/translate/details/">Features and Documentation</a>. 

**NOTE**: You would need to have an AWS account with Translate services active on it.

## Why built it?
Because I needed a simple and relatively fast way to translate a large set of localisation strings, where files had a different structure.

## What features does it have?
The app can produce comparison files for manual checking of the correctness of the translated output (in **MS Excel**, etc.) or in other words a ```.csv``` file that within it has the following headers:**```[key,source,target]```**. For example:</br>
```
key,en,bg
Welcome,Welcome,Добре дошъл
```

You can also specify whether or not you would like keys which include ICU or HTML data to be included in the output file. Additionally, whether or not to try and translate such strings. The app will always produce a text log, with the name **icu_html.txt** of every read key that includes an ICU or HTML string. That is done for convenience and easy tracking as such strings generally need to be handled manually. It must be noted that the original line refers to the line in the input file where the string was encountered. In this example **'was not processed as requested'** refers to the ```"Translate:"``` option, while **'but was saved on the translated file'** refers to the ```"SeparateFile:"``` option both specified in ```./appsettings.json```. For example:</br>
```
The localisation string with key 'Tutorial.CreateEvent' with original line (4) was not processed as requested, but was saved on the translated file.
The localisation string with key 'Notification.Pending.JoinRequest' with original line (9) was not processed as requested, but was saved on the translated file.
The localisation string with key 'Notification.ReadyCheck.Accept' with original line (10) was not processed as requested, but was saved on the translated file.
The localisation string with key 'Notification.ReadyCheck.Decline' with original line (11) was not processed as requested, but was saved on the translated file.

```
The app has the option to skip records that have empty strings for the field that needs to be translated. By skipping it, the record will be saved in a separate file with the name **records_without_text.csv**. For example:</br>
```
key,context,language,text
Welcome,,en,                            <- Has no text, therefore it is empty
Welcome.FacebookUser,,en,Welcome to the App because you joined from FB you get free credits
```

#### **NOTE**
There is a difference between a text field being empty and a text field not being provided properly. Missing fields will be treated as errors at a certain line of a file. For example:</br>
```
key,context,language,text
Welcome,,en,                            <- Has no text is empty
Welcome.FacebookUser,,enWelcome to the App because you joined from FB you get free credits
                       ^
                       |
                       An error as there are 3 fields instead of 4. The text field is missing. Error at original line (3)
```

## What about future features?
Add support for AWS credential checking and determining whether a user has Amazon Translate's service enabled.

Add support for adding custom terminologies.

Add support for multi-language translation at the same time.

Currently, the application is sending 1 request at a time and awaiting a response. This isn't the most efficient way of handling large files. A potential solution would be to multi-thread (send multiple requests at the same time and await them) the operations based on the hardware capabilities/size of the file. Amazon Translate supports batch translate operation but this will not be considered as it out of the scope of this application as the data will need to in **Amazon S3** and referenced from there. 

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
Tutorial.CreateEvent,,enTo create <strong>a given<strong> provide the following proterties        <- Error here on purpose
Tutorial.JoinEvent,,en,         <- No text on purpose
Tutorial.SearchEvent,,en,<strong>To search for an event click here</strong>
Tutorial.ShareEvent,,en,        <- No text on purpose
Tutorial.BookEvent,,enTo book an event do this        <- Error here on purpose
Notification.Pending.JoinRequest,,en,You have {0} users asking to join
Notification.ReadyCheck.Accept,,en,<strong>Accept<strong>
Notification.ReadyCheck.Decline,,en,<strong>Decline<strong>
```

**Example of expected output files:**
<img src="Assets\example_output.png" alt="missing" />

**Example of ```translated.csv```**
```
key,context,language,text
Welcome,,bg,Добре дошъл в приложението
Welcome.FacebookUser,,bg,Добре дошъл в проложение понеже ти се присъедини чрез ФБ ще получаваш безплатни кредити
```

**Example of ```error_log.txt```**
```
At line: 4, no value was found for 'text'.
At line: 8, no value was found for 'text'.
```

**Example of ```icu_html.txt```**
```
The localisation string with key 'Tutorial.SearchEvent' with original line (6) was not processed as requested, but was saved on a separate file as requested.
The localisation string with key 'Notification.Pending.JoinRequest' with original line (9) was not processed as requested, but was saved on a separate file as requested.
The localisation string with key 'Notification.ReadyCheck.Accept' with original line (8) was not processed as requested, but was saved on a separate file as requested.
The localisation string with key 'Notification.ReadyCheck.Decline' with original line (11) was not processed as requested, but was saved on a separate file as requested.
```

**Example of ```separated_records.csv```**
```
key,context,language,text
Tutorial.SearchEvent,,en,<strong>To search for an event click here</strong>
Notification.Pending.JoinRequest,,en,You have {0} users asking to join
Notification.ReadyCheck.Accept,,en,<strong>Accept<strong>
Notification.ReadyCheck.Decline,,en,<strong>Decline<strong>
```

**Example of ```comparison.csv```**
```
key,en,bg
Welcome,Welcome to the app,Добре дошъл в приложението
Welcome.FacebookUser,Welcome to the App because you joined from FB you get free credits,Добре дошъл в проложение понеже ти се присъедини чрез ФБ ще получиш безплатни кредити
Tutorial.SearchEvent,<strong>To search for an event click here</strong>,NOT_TRANSLATED_AS_REQUESTED
Notification.Pending.JoinRequest,You have {0} users asking to join,NOT_TRANSLATED_AS_REQUESTED
Notification.ReadyCheck.Accept,<strong>Accept<strong>,NOT_TRANSLATED_AS_REQUESTED
Notification.ReadyCheck.Decline,<strong>Decline<strong>,NOT_TRANSLATED_AS_REQUESTED
```

**Example of ```records_without_text.csv```**
```
key,context,language,text
Tutorial.JoinEvent,,en,
Tutorial.ShareEvent,,en,
```

#### Attributes and their functionality
- ```"Source:"``` - Specifies the source language
  - Amazon Translate can determine the source of the language but it advisable to provide the language code
- ```"Target:"``` - Specifies the target language
- ```"Path:"``` - The relative path to the CSV file
- ```"Headers:"``` - The headers of the CSV file
- ```"TextHeader:"``` - The header at which the text that needs translation is
- ```"KeyHeader:"``` - The header at which the localisation string's key is
- ```"LanguageHeader:"``` - The header at which the language code is
- ```"AddKeyWithNoText:"``` - Whether or not to add a key in the translated output (i.e. whole record) if the text field is empty
  - i.e. ```Tutorial.ShareEvent,,en,``` has no text field (check table for default values)
- ```"SeparateFile:"``` - Whether or not to add keys containing ICU or HTML strings to the main translated output
- ```"Translate:"``` - Whether translation should be attempted on keys containing ICU or HTML strings
- ```"ComparisonFile:"``` - Whether or not to produce a comparison file, with structure:
  - ```key,en(the source lang),bg(target lang)```
  - **The translated output file will still be created**

| Attribute          |     Necessary      | Default value |
| ---                |        :---:       |      :---:    |
| Source             |         N/A        |      N/A      |
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

Whenever a single rule is broken, the app will not proceed to the cost estimation step.

### Missing field is found?
The CSV parser would ensure that the correct number of fields, corresponding to the specified header values in the first row, are supplied. In the case of missing data, the parser will save the line at which an exception was raised and append the information to the output log. The line will then be excluded from further processing and the parser will continue reading the rest of the rows if any.

### Bad data is found?
As described in <a href="https://joshclose.github.io/CsvHelper/api/CsvHelper.Configuration/Configuration/">CsvHelper.Configuration</a> bad data is considered a field that *"contains a quote and that field is not quoted (escaped)"*. In the case of bad data, the parser will save the line at which an exception was raised and append the information to the output log. The line will then be excluded from further processing and the parser will continue reading the rest of the rows if any.

### Missing file or wrong file extension?
The app will terminate and produce an output log whenever a provided input file doesn't exist or has the wrong extension.

### AWS Credential checking? (TODO)
**TODO**
