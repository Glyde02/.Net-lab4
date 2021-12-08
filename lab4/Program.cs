using GeneratorLib;
using System;

namespace lab4
{
    class Program
    {
        static void Main(string[] args)
        {
            var generatingOptions = new GeneratingOptions();
            Console.WriteLine("Enter source path");
            generatingOptions.SourceDirectory = @"C:\Users\anton\Documents\study\OOP\lab4\Source\Src";
            Console.WriteLine("Enter destination path");
            generatingOptions.DestinationDirectory = @"C:\Users\anton\Documents\study\OOP\lab4\Test";
            Generator.GenerateAsync(generatingOptions).Wait();
            
            
        }
    }
}
