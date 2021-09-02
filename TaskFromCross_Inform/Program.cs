using System;
using System.IO;

namespace TaskFromCross_Inform
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Командная строка пуста, введите туда путь к файлу");
            }

            Console.WriteLine(@"Используемые разделительные символы: ' ', '\r', '\n', '\0', '\t'");
            Console.WriteLine("Полный путь к файлу: " + args[0]);

            string allText = "";

            try
            {
                allText = WorkWithFile.GetStringOfAllLinesFromFile(args[0]);
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine("Не смог найти файл по полному пути: " + args[0]);
                Console.WriteLine(ex);
            }
            WorkWithText texWork = new WorkWithText();

            PrintArray(texWork.GetTopTenTriplets(allText));
            Console.WriteLine(texWork.WorkTime.Elapsed.ToString());


        }

        /// <summary>
        /// Вывод массива через запятую
        /// </summary>
        /// <param name="array"> Массив </param>
        static void PrintArray(string[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (i == array.Length-1)
                {
                    Console.Write(array[i]);
                    break;
                }
                Console.Write(array[i]);
                Console.Write(", ");
            }
            Console.WriteLine();
        }
    }
}
