using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Merit.BarCodeScanner.Helpers;
using Merit.BarCodeScanner.Models;
using System.IO;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {

            DateTimeFormatInfo usDtfi = new CultureInfo("en-US", false).DateTimeFormat;
            DateTimeFormatInfo ukDtfi = new CultureInfo("en-GB", false).DateTimeFormat;
            string sDtime = "6/22/2017 7:03:07";
            string result = Convert.ToDateTime(sDtime, usDtfi).ToString(ukDtfi.LongTimePattern);



            List<CsvValues> dailyScanValues = new List<CsvValues>();

            List<CsvValues> newScanValues = new List<CsvValues>();

            var path = @"D:\BarcodeRealRaw - warning_20170622,24,25_mix_import file.csv";

            dailyScanValues = File.ReadAllLines(path)
                               .Skip(0)
                               .Select(line => FileHelper.FromCsv(line, dailyScanValues.FindIndex(x => x.Equals(line))))
                               .ToList();
            var row = 1;
            foreach (var item in dailyScanValues)
            {

                var newItem = new CsvValues
                {
                    BarCode = item?.BarCode,
                    Date = item?.BarCode,
                    Id = item.Id,
                    IsBarCode = item.IsBarCode,
                    RowNumber = row,
                    RowType = item?.RowType,
                    Time = item?.Time,                    
                };

                newScanValues.Add(newItem);
                row++;
            }
            //FileHelper.CreateCSVFromGenericList(dailyScanValues, @"E:\DucTest\ducpm85.csv");
            var test = newScanValues;
            Console.WriteLine("My: " + Convert.ToDateTime(result).ToString("dd/MM/yyyy HH:mm:ss"));
            Console.Read();
        }
    }
}
