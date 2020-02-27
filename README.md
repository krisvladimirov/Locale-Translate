:shit: Do me :shit:

## What does the CSV validate?
1. Check if the headers in the input file are matching the provided headers in AppSettings
   1. On the first header that does not match an error is logged
2. If there are less header an error is logged process exits
3. If there are more header an error is logged process exits
4. If there are not errors the log is simply not created.
5. If there is missing data for header an error is logged process continues

## What is not supported yet
1. Comparison records
2. Multi language translation
3. Doesn't check if the if the language field are matching the source language
   1. AWS could potentially throw an error if the source language is 'en' but something else is given as "Какво прайм сега, а? Ще се наказваме ли, ти ми кажи?" (To be tested)
4. Terminologies are not supported.