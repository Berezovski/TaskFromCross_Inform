using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace TaskFromCross_Inform
{
    /// <summary>
    /// Работа с текстом по ТЗ
    /// </summary>
    sealed class WorkWithText
    {
        /// <summary>
        /// Свойство, отвечающее за время последнего выполнения функции поиска и взятия 10 триплетов
        /// </summary>
        public Stopwatch WorkTime { get; }

        /// <summary>
        /// Dictionary для многопоточного безопасного использования
        /// </summary>
        private ConcurrentDictionary<string, int> _dictionaryOfTriplets;

        /// <summary>
        /// Разделители, которые учитываются при работе с файлом
        /// </summary>
        private readonly char[] _separators = new char[5] { ' ', '\n', '\t', '\0', '\r' };

        /// <summary>
        /// Инициализация словаря и счётчика
        /// </summary>
        public WorkWithText()
        {
            _dictionaryOfTriplets = new ConcurrentDictionary<string, int>();
            WorkTime = new Stopwatch();
        }

        /// <summary>
        /// Очистить счётчик
        /// </summary>
        public void ClearWorkTime()
        {
            WorkTime.Reset();
        }

        /// <summary>
        /// Взятие десяти часто встречающихся триплетов из строки
        /// </summary>
        /// <param name="userString"> текст </param>
        /// <returns> 10 триплетов (по умолчанию элементы: "-Nothing-") </returns>
        public string[] GetTopTenTriplets(string userString)
        {
            ClearWorkTime();
            WorkTime.Start();

            int processorCount = Environment.ProcessorCount;
            string[] words = SplitStringIntoWords(userString);
            string[] answer;

            if ((words.Length < processorCount*100) || (processorCount < 2))
            {
                answer = GetTenTopTripletsFromWords(words);
                ClearConcurrentDictionary();
                WorkTime.Stop();
                return answer;
            }
            else
            {
                answer = GetTenTopTripletsFromWordsAsync(words, processorCount).Result;
                ClearConcurrentDictionary();
                WorkTime.Stop();
                return answer;
            }

        }

        /// <summary>
        /// Считывание триплетов и указывание количество одинаковых в коллекцию _dictionaryOfTriplets
        /// </summary>
        /// <param name="words"> Массов слов </param>
        private void SetConcurrentDictionaryOfTripletsFromWords(string[] words)
        {
            string[] tmpTripletsFromOneWord;
            int tmpValue;

            for(int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 2)
                {
                    tmpTripletsFromOneWord = GetTripletsFromOneWord(words[i]);
                    for (int j =0; j < tmpTripletsFromOneWord.Length; j++)
                    {
                        if (!_dictionaryOfTriplets.TryAdd(tmpTripletsFromOneWord[j], 1))
                        {
                            tmpValue = _dictionaryOfTriplets[tmpTripletsFromOneWord[j]];
                            while(!_dictionaryOfTriplets.TryUpdate(tmpTripletsFromOneWord[j], tmpValue + 1, tmpValue))
                            {
                                tmpValue = _dictionaryOfTriplets[tmpTripletsFromOneWord[j]];
                            }
                        }

                    }
                }
                else
                {

                }

            }
        }

        /// <summary>
        /// Взятие топ 10 триплетов из слов (не многопоточное)
        /// </summary>
        /// <param name="words"> Массив слов </param>
        /// <returns> Топ 10 триплетов </returns>
        private string[] GetTenTopTripletsFromWords(string[] words)
        {
            SetConcurrentDictionaryOfTripletsFromWords(words);
            Dictionary<string, int> tmpDict = GetOrderedDictionaryFromConcurrentDictionaryByValue();
            tmpDict = SortTenFirstPairWithSameValueInDictionary(tmpDict);
            string[] answer = GetTopTenTripletsFromDictionary(tmpDict);

            return answer;
        }

        /// <summary>
        /// Взятие топ 10 триплетов из слов (многопоточное), с учётом количества процессоров
        /// </summary>
        /// <param name="words"> Массив слов </param>
        /// <param name="processorCount"> Количество процессоров </param>
        /// <returns> Топ 10 триплетов </returns>
        private async Task<string[]> GetTenTopTripletsFromWordsAsync(string[] words, int processorCount)
        {
            string[][] wordsForTaskArray = DividedWordsIntoGroups(words, processorCount);

            Task[] allTasks = new Task[processorCount];
            for (int i = 0; i < processorCount; i++)
            {
                int indexI = i; // т.к. значение i может поменяться при EndInvoke

                allTasks[indexI] = Task.Run(() => SetConcurrentDictionaryOfTripletsFromWords(wordsForTaskArray[indexI]));
            }
            await Task.WhenAll(allTasks);

            Dictionary<string, int> tmpDict = GetOrderedDictionaryFromConcurrentDictionaryByValue();
            tmpDict = SortTenFirstPairWithSameValueInDictionary(tmpDict);
            string[] answer = GetTopTenTripletsFromDictionary(tmpDict);

            return answer;
        }

        /// <summary>
        /// Разделение слов на группы для многопоточной работы (1 группа -> 1 поток)
        /// </summary>
        /// <param name="words"> Массив слов </param>
        /// <param name="groupCount"> Количество групп </param>
        /// <returns> Группы массивов слов </returns>
        private string[][] DividedWordsIntoGroups(string[] words, int groupCount)
        {
            string[][] groupsOfWords = new string[groupCount][];

            for (int i = 0; i < groupCount; i++)
            {
                if (i != groupCount - 1)
                {
                    groupsOfWords[i] = new string[words.Length / groupCount];
                    Array.Copy(words, groupsOfWords[0].Length * i, groupsOfWords[i], 0, groupsOfWords[i].Length);
                }
                else
                {
                    groupsOfWords[i] = new string[words.Length - (words.Length / groupCount) * (groupCount - 1)];
                    Array.Copy(words, groupsOfWords[0].Length * i, groupsOfWords[i], 0, groupsOfWords[i].Length);
                }

            }
            return groupsOfWords;
        }

        /// <summary>
        /// Разделение строки на слова (учитывая сепараторы)
        /// </summary>
        /// <param name="str"> Строка </param>
        /// <returns> Слова </returns>
        private string[] SplitStringIntoWords(string str)
        {
            return str.Split(_separators, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Сортируем _dictionaryOfTriplets по значениям (по убыванию)
        /// </summary>
        /// <returns> Отсортированный Dictionary </returns>
        private Dictionary<string, int> GetOrderedDictionaryFromConcurrentDictionaryByValue()
        {
            return _dictionaryOfTriplets.OrderByDescending(pair => pair.Value).ToDictionary(x => x.Key, y => y.Value);
        }

        /// <summary>
        /// Взятие топ 10 триплетов из Dictionary
        /// </summary>
        /// <param name="dictionaryOfTriplets"></param>
        /// <returns> Топ 10 триплетов из Dictionary </returns>
        private string[] GetTopTenTripletsFromDictionary(Dictionary<string, int> dictionaryOfTriplets)
        {
            string[] answer = new string[10] { "-Nothing-", "-Nothing-", "-Nothing-", "-Nothing-", "-Nothing-",
                "-Nothing-" , "-Nothing-", "-Nothing-" , "-Nothing-", "-Nothing-" };

            for (int i = 0; (i < dictionaryOfTriplets.Count) && (i < 10); i++)
                answer[i] = dictionaryOfTriplets.ElementAt(i).Key;

            return answer;
        }

        /// <summary>
        /// Взятие триплетов из 1 слова
        /// </summary>
        /// <param name="word"> Слово </param>
        /// <returns> Триплеты </returns>
        private string[] GetTripletsFromOneWord(string word)
        {
            List<string> allTriplets= new List<string>();

            for (int i = 0; i < word.Length - 2; i++)
            {
                allTriplets.Add(word.Substring(i, 3));
            }

            return allTriplets.ToArray();
        }

        /// <summary>
        /// Очистка _dictionaryOfTriplets
        /// </summary>
        private void ClearConcurrentDictionary()
        {
            _dictionaryOfTriplets.Clear();
        }

        /// <summary>
        /// Сортирует ключи с одинаковыми значениями (для исправления хаотичности
        /// порядка ключей с одинаковыми значениями при многопоточном добавлении) 
        /// </summary>
        /// <param name="sortedDictionaryOfTripletsByValue"> Отсортированный по значениям Dictionary</param>
        /// <returns> Полностью отсортированный Dictionary </returns>
        private Dictionary<string, int> SortTenFirstPairWithSameValueInDictionary(Dictionary<string, int> sortedDictionaryOfTripletsByValue)
        {            
            int lastValue=0;
            Dictionary<string, int> tmpDictionar;
            Dictionary<string, int> finalDictionary = new Dictionary<string, int>();

            for (int elementIndex =0; (elementIndex < 10) && (sortedDictionaryOfTripletsByValue.Count > elementIndex); elementIndex++)          
            {
                if (lastValue == sortedDictionaryOfTripletsByValue.ElementAt(elementIndex).Value)
                {
                    continue;
                }
                lastValue = sortedDictionaryOfTripletsByValue.ElementAt(elementIndex).Value;

                tmpDictionar = sortedDictionaryOfTripletsByValue.Where(x => x.Value == lastValue).ToDictionary(x => x.Key, y => y.Value);
                tmpDictionar = tmpDictionar.OrderBy(x => x.Key).ToDictionary(x => x.Key, y => y.Value);

                foreach (var element in tmpDictionar)
                {
                    finalDictionary.TryAdd(element.Key, element.Value);
                }

            }

            return finalDictionary;
        }
    }
}
