using System;

namespace TaskFromCross_Inform
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                throw new ArgumentNullException("Specify the path to the file in the arguments");
            }

            WorkWithText texWork = new WorkWithText();

            PrintArray(texWork.GetTopTenTriplets(WorkWithFile.GetAllLinesFromFile(args[0])));
            Console.WriteLine(texWork.WorkTime.Elapsed.ToString());
             texWork = new WorkWithText();

            PrintArray(texWork.GetTopTenTriplets(WorkWithFile.GetAllLinesFromFile(args[0])));
            Console.WriteLine(texWork.WorkTime.Elapsed.ToString());
             texWork = new WorkWithText();

            PrintArray(texWork.GetTopTenTriplets(WorkWithFile.GetAllLinesFromFile(args[0])));
            Console.WriteLine(texWork.WorkTime.Elapsed.ToString());
        }

        static void PrintArray(string[] str)
        {
            for (int i = 0; i < str.Length; i++)
            {
                if (i == str.Length-1)
                {
                    Console.Write(str[i]);
                    break;
                }
                Console.Write(str[i]);
                Console.Write(", ");
            }
            Console.WriteLine();
        }
    }
}
