using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskFromCross_Inform
{
    /// <summary>
    /// Класс для работы с файлом
    /// </summary>
    static class WorkWithFile
    {
        /// <summary>
        /// Взять все данные из файла в виде строки
        /// </summary>
        /// <param name="filePath"> Путь к файлу </param>
        /// <returns> Строка всех символов считанных из файла </returns>
        public static string GetStringOfAllLinesFromFile(string filePath)
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
