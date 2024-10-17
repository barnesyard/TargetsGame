// See https://aka.ms/new-console-template for more information

/// <summary>
/// This program will generate a JSON file that can be used to create a grid in the Targets Game.
/// It will read in an easy to create CSV 
/// </summary>
/// <remarks>
/// This program will generate a JSON file that can be used to create a grid in the Targets Game.
/// The JSON file will contain the following information:
/// - A list of cells with their region id
/// </remarks>

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Reading CSV and converting to JSON...");

        string csvPath = "c.csv";
        string jsonOutput = ConvertCsvToJson(csvPath);

        // Write JSON output to grid.json file
        string jsonFilePath = "grid.json";
        File.WriteAllText(jsonFilePath, jsonOutput);

        Console.WriteLine($"Conversion complete. JSON output written to {jsonFilePath}");
    }

    static string ConvertCsvToJson(string csvPath)
    {
        // Read all lines from the CSV file
        string[] lines = File.ReadAllLines(csvPath);

        // Create a list to hold all rows
        var rows = new List<List<int>>();

        // Process each line
        foreach (string line in lines)
        {
            var row = new List<int>();
            string[] values = line.Split(',');

            foreach (string value in values)
            {
                if (int.TryParse(value, out int intValue))
                {
                    row.Add(intValue);
                }
                else
                {
                    Console.WriteLine($"Warning: Unable to parse '{value}' as an integer. Skipping this value.");
                }
            }

            rows.Add(row);
        }

        // Convert the list of lists to JSON
        return JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true });
    }
}
