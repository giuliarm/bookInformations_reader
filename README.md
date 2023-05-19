# bookInformations_reader
Given an input file of ISBNs, application retrieve full book info via the OpenLibrary API and output a CSV.

The C# program reads the Input File path and the desired destination, numbering its lines and maintaining its order when there is a line break.
Using the OpenLibrary API to fetch information from the ISBN only once, keeping it in a local cache in case we already have information about the next one to look for.
It then returns that CSV file asking the user if they want to download it for analysis.

