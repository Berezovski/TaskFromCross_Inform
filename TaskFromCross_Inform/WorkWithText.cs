using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskFromCross_Inform
{
    sealed class WorkWithText
    {
        public Stopwatch WorkTime { get; }

        private ConcurrentDictionary<string, int> _dictionaryOfTriplets;

        public WorkWithText()
        {
            _dictionaryOfTriplets = new ConcurrentDictionary<string, int>();
            WorkTime = new Stopwatch();
        }

        public void ClearWorkTime()
        {
            WorkTime.Reset();
        }

        public string[] GetTopTenTriplets(string userString)
        {
            ClearWorkTime();
            WorkTime.Start();

            int processorCount = Environment.ProcessorCount;
            string[] words = SplitStringIntoWords(userString);
            string[] answer;

            if (words.Length < processorCount*100 )
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

        private void GetDictionaryOfTripletsFromWords(string[] words)
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

        private string[] GetTenTopTripletsFromWords(string[] words)
        {
            GetDictionaryOfTripletsFromWords(words);
            Dictionary<string, int> tmpDict = GetOrderedDictionaryFromConcurrentDictionaryByValue();
            tmpDict = SortTenFirstPairWithSameValueInDictionary(tmpDict);
            string[] answer = GetTopTenTripletsFromDictionary(tmpDict);

            return answer;
        }

        private async Task<string[]> GetTenTopTripletsFromWordsAsync(string[] words, int processorCount)
        {
            string[][] wordsForTaskArray = DividedWordsIntoGroups(words, processorCount);

            Task[] allTasks = new Task[processorCount];
            for (int i = 0; i < processorCount; i++)
            {
                int indexI = i; // т.к. значение i может поменяться при EndInvoke

                allTasks[indexI] = Task.Run(() => GetDictionaryOfTripletsFromWords(wordsForTaskArray[indexI]));
            }
            await Task.WhenAll(allTasks);

            Dictionary<string, int> tmpDict = GetOrderedDictionaryFromConcurrentDictionaryByValue();
            tmpDict = SortTenFirstPairWithSameValueInDictionary(tmpDict);
            string[] answer = GetTopTenTripletsFromDictionary(tmpDict);

            return answer;
        }

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

        private string[] SplitStringIntoWords(string str)
        {
            return str.Split(new char[] { ' ', '\n', '\t', '\0', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private Dictionary<string, int> GetOrderedDictionaryFromConcurrentDictionaryByValue()
        {
            return _dictionaryOfTriplets.OrderByDescending(pair => pair.Value).ToDictionary(x => x.Key, y => y.Value);
        }

        private string[] GetTopTenTripletsFromDictionary(Dictionary<string, int> dictionaryOfTriplets)
        {
            string[] answer = new string[10] { "-Nothing-", "-Nothing-", "-Nothing-", "-Nothing-", "-Nothing-",
                "-Nothing-" , "-Nothing-", "-Nothing-" , "-Nothing-", "-Nothing-" };

            for (int i = 0; (i < dictionaryOfTriplets.Count) && (i < 10); i++)
                answer[i] = dictionaryOfTriplets.ElementAt(i).Key;

            return answer;
        }

        private string[] GetTripletsFromOneWord(string word)
        {
            List<string> allTriplets= new List<string>();

            for (int i = 0; i < word.Length - 2; i++)
            {
                allTriplets.Add(word.Substring(i, 3));
            }

            return allTriplets.ToArray();
        }

        private void ClearConcurrentDictionary()
        {
            _dictionaryOfTriplets.Clear();
        }

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
