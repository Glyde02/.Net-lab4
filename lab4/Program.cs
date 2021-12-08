using GeneratorLib;
using System;

namespace lab4
{
    class Program
    {
        static void Main(string[] args)
        {
            var generatingOptions = new GeneratingOptions();
            
            generatingOptions.SourceDirectory = @"";
            generatingOptions.DestinationDirectory = @"";
            Generator.GenerateAsync(generatingOptions).Wait();
            
            
        }
    }
}
