using System;
using System.Collections.Generic;
using System.IO;

namespace Aplicatie_1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("INCARCARE STOPWORDS");

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string stopwordsPath = Path.Combine(baseDir, "stopwords.txt");

            if(!File.Exists(stopwordsPath)) {
                string projectDir = Directory.GetParent(baseDir)!.Parent!.Parent!.FullName;
                stopwordsPath = Path.Combine(projectDir, "stopwords.txt");
            }
            HashSet<string> stopwords = LoadStopwords(stopwordsPath);

            Console.WriteLine($"Numar total de stopwords incarcate: {stopwords.Count}");

            string[] cuvinteTest = { "the", "and", "is", "apple", "computer" };
            Console.WriteLine("Testare cuvinte");

            foreach(var cuvant in cuvinteTest)
            {
                bool esteStopword = stopwords.Contains(cuvant);
                Console.WriteLine($"- '{cuvant}' este stopword? -> {esteStopword}");
            }

            Console.WriteLine("Apasa orice tasta pentru a continua");
            Console.ReadKey();
        }

        static HashSet<string> LoadStopwords(string path)
        {
            HashSet<string> words = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!File.Exists(path))
            {
                Console.WriteLine($"[EROARE] Fisierul nu a fost gasit la calea:\n{path}");
                return words;
            }

            // Citim fiecare linie din fisier
            foreach (string line in File.ReadAllLines(path))
            {
                string trimmed = line.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    words.Add(trimmed);
                }
            }

            return words;
        }
    }
}