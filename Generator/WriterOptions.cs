using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneratorLib
{
    public class WriterOptions
    {
        public string Filename { get; }

        public string Content { get; }

        public WriterOptions(string filename, string content)
        {
            Filename = filename;
            Content = content;
        }
    }
}
