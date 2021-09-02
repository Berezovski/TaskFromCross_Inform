using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskFromCross_Inform
{
    static class WorkWithFile
    {
        public static string GetAllLinesFromFile(string filePath)
        {
            string answer = null;
            try
            {
                answer = File.ReadAllText(filePath);
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine(ex);
            }

            return answer;

        }
    }
}
