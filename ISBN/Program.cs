using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ISBN
{
    enum DataRetrievalType
    {
        Server = 1,
        Cache = 2
    }

    class BookData
    {
        public int RowNumber { get; set; }
        public DataRetrievalType RetrievalType { get; set; }
        public string ISBN { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public List<string> Authors { get; set; }
        public string NumberOfPages { get; set; }
        public string PublishDate { get; set; }
    }

    class CsvGenerator
    {
        public static string GenerateCsv(List<BookData> bookDataList)
        {
            StringBuilder csvContent = new StringBuilder();


            csvContent.AppendLine("Row Number,Data Retrieval Type,ISBN,Title,Subtitle,Author Name(s),Number of Pages,Publish Date");

            foreach (var bookData in bookDataList)
            {
                string authors = bookData.Authors.Count > 1 ? string.Join(";", bookData.Authors) : string.Join("", bookData.Authors);

                string rowData = $"{bookData.RowNumber},{bookData.RetrievalType},{bookData.ISBN},{bookData.Title},{bookData.Subtitle},{$"\"{authors}\""},{bookData.NumberOfPages},{$"\"{bookData.PublishDate}\""}";
                csvContent.AppendLine(rowData);
            }

            return csvContent.ToString();
        }
    }
    class Program
    {
        private static Dictionary<string, BookData> cache = new Dictionary<string, BookData>();

        static async Task Main(string[] args)
        {
            Console.WriteLine("Enter the file path: ");
            string filePath = Console.ReadLine();

            List<BookData> bookDataList = new List<BookData>();
            List<BookData> isbns = ReadInputFile(filePath);

            using (HttpClient client = new HttpClient())
            {

                foreach (var isbn in isbns)
                {
                    BookData bookData = await RetrieveBookData(client, isbn.ISBN, isbn.RowNumber);
                    bookDataList.Add(bookData);
                }
            }


            Console.WriteLine("CSV file generated successfully!");

            Console.WriteLine("Do you want to download the file? (y/n): ");
            string response = Console.ReadLine();

            if (response.ToLower() == "y")
            {

                string csvContent = CsvGenerator.GenerateCsv(bookDataList);

                string path = @"C:\users\dev\Downloads\books.csv";

                File.WriteAllText(path, csvContent);

                Console.WriteLine("Download complete. Find the file at: " + path);
            }


        }

        public static List<BookData> ReadInputFile(string filePath)
        {
            List<BookData> bookDataList = new List<BookData>();

            string[] lines = File.ReadAllLines(filePath);

            using (StreamReader reader = new StreamReader(filePath))
            {
                int rowNumber = 1;
                foreach (var line in lines)
                {
                    if (line.Contains(','))
                    {
                        var isbnsSameLine = line.Split(',');

                        foreach (var i in isbnsSameLine)
                        {
                            BookData bookData = new BookData
                            {
                                RowNumber = rowNumber,
                                ISBN = i.Trim()
                            };

                            bookDataList.Add(bookData);
                        }
                        rowNumber++;
                    }
                    else
                    {
                        BookData bookData = new BookData
                        {
                            RowNumber = rowNumber,
                            ISBN = line.Trim()
                        };

                        bookDataList.Add(bookData);
                        rowNumber++;
                    }

                }

            }

            return bookDataList;
        }

        static async Task<BookData> RetrieveBookData(HttpClient client, string isbn, int rowNumber)
        {
            BookData bookData = new BookData();
            bookData.ISBN = isbn;

            bool isDataCached = CheckCache(isbn);

            if (!isDataCached)
            {
                HttpResponseMessage response = await client.GetAsync($"https://openlibrary.org/api/books?bibkeys=ISBN:{isbn}&jscmd=data&format=json");
                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadAsStringAsync();
                    dynamic json = Newtonsoft.Json.JsonConvert.DeserializeObject(data);
                    dynamic book = json[$"ISBN:{isbn}"];
                    bookData.RowNumber = rowNumber;
                    bookData.Title = book.title;
                    bookData.Subtitle = book.subtitle ?? "N/A";
                    bookData.Authors = new List<string>();
                    foreach (dynamic author in book.authors)
                    {
                        bookData.Authors.Add($"{author.name}");
                    }
                    bookData.NumberOfPages = book.number_of_pages ?? "N/A";
                    bookData.PublishDate = book.publish_date;

                    bookData.RetrievalType = DataRetrievalType.Server;

                    StoreInCache(isbn, bookData);
                }
                else
                {
                    Console.WriteLine($"Error retrieving book data for ISBN: {isbn}");
                }
            }
            else
            {

                BookData old = cache[isbn];

                var cacheBookData = new BookData
                {
                    RetrievalType = DataRetrievalType.Cache,
                    ISBN = old.ISBN,
                    Title = old.Title,
                    Subtitle = old.Subtitle,
                    Authors = old.Authors,
                    NumberOfPages = old.NumberOfPages,
                    PublishDate = old.PublishDate,
                    RowNumber = rowNumber
                };

                return cacheBookData;
            }

            return bookData;
        }


        static bool CheckCache(string isbn)
        {
            return cache.ContainsKey(isbn);
        }


        static void StoreInCache(string isbn, BookData bookData)
        {
            cache[isbn] = bookData;
        }

    }
}
