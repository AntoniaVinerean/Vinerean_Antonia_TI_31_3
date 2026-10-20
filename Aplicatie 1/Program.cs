using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using WordFrequency;

namespace Aplicatie_1
{
    class Document
    {
        public string Id { get; set; } = "";
        public string Folder { get; set; } = "";
        public List<string> Topics { get; set; } = new List<string>();
        public Dictionary<string, int> WordFrequencies { get; set; } = new Dictionary<string, int>();
    }
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("INCARCARE STOPWORDS");

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string stopwordsPath = Path.Combine(baseDir, "stopwords.txt");

            if (!File.Exists(stopwordsPath))
            {
                string projectDir = Directory.GetParent(baseDir)!.Parent!.Parent!.FullName;
                stopwordsPath = Path.Combine(projectDir, "stopwords.txt");
            }

            string projectFolder = AppDomain.CurrentDomain.BaseDirectory;
            while (!Directory.Exists(Path.Combine(projectFolder, "Training")) && Directory.GetParent(projectFolder) != null)
            {
                projectFolder = Directory.GetParent(projectFolder)!.FullName;
            }
            string trainingPath = Path.Combine(projectFolder, "Training");
            string testingPath = Path.Combine(projectFolder, "Testing");
            string outputPath = Path.Combine(projectFolder, "rezultat.txt");


            HashSet<string> stopwords = LoadStopwords(stopwordsPath);
            Console.WriteLine($"Numar total de stopwords incarcate: {stopwords.Count}");

            PorterStemmer stemmer = new PorterStemmer();

            List<Document> documents = new List<Document>();
            HashSet<string> dictionarGlobal = new HashSet<string>();

            ProceseazaFolder(trainingPath, "Training", stopwords, stemmer, documents, dictionarGlobal);
            ProceseazaFolder(testingPath, "Testing", stopwords, stemmer, documents, dictionarGlobal);

            Console.WriteLine($"\nDocumente procesate in total: {documents.Count}");
            Console.WriteLine($"Numar total de cuvinte unice in dictionarul global: {dictionarGlobal.Count}");

            List<string> cuvinteSortate = dictionarGlobal.OrderBy(c => c).ToList();
            Dictionary<string, int> vocabularIndex = new Dictionary<string, int>();

            for (int i = 0; i < cuvinteSortate.Count; i++)
            {
                vocabularIndex[cuvinteSortate[i]] = i;
            }

            Console.WriteLine("Se scrie fisierul rezultat.txt...");
            SalveazaFisier(outputPath, cuvinteSortate, vocabularIndex, documents);

            Console.WriteLine($"Gata! Fisierul a fost creat la:\n{outputPath}");
            Console.WriteLine("\nApasa orice tasta pentru a inchide...");
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

        static void ProceseazaFolder(string folderPath, string tipFolder, HashSet<string> stopwords, PorterStemmer stemmer, List<Document> listaDocumente, HashSet<string> dictionarGlobal)
        {
            if (!Directory.Exists(folderPath))
            {
                Console.WriteLine($"[EROARE] Folderul nu a fost gasit la calea:\n{folderPath}");
                return;
            }

            string[] fisiere = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
                            .Where(f => f.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                            .ToArray();

            Console.WriteLine($"Se proceseaza {fisiere.Length} fisiere din {tipFolder}...");

            foreach (string fisier in fisiere)
            {
                Document doc = ProceseazaFisierXml(fisier, tipFolder, stopwords, stemmer, dictionarGlobal);

                if (doc != null)
                {
                    listaDocumente.Add(doc);
                }
            }
        }

        static Document ProceseazaFisierXml(string fisierPath, string tipFolder, HashSet<string> stopwords,
                                            PorterStemmer stemmer, HashSet<string> dictionarGlobal)
        {
            try
            {
                XDocument xml = XDocument.Load(fisierPath);
                var root = xml.Root;
                if (root == null) return null;

                Document doc = new Document
                {
                    Id = Path.GetFileNameWithoutExtension(fisierPath),
                    Folder = tipFolder
                };

                string titlu = root.Element("title")?.Value ?? "";
                string continut = "";

                var textNode = root.Element("text");

                if (textNode != null)
                {
                    continut = string.Join(" ", textNode.Elements("p").Select(p => p.Value));
                }

                string textComplet = titlu + " " + continut;

                var topicsNode = root.Descendants()
                     .FirstOrDefault(x => x.Name.LocalName == "codes" &&
                                         (string?)x.Attribute("class") == "bip:topics:1.0");

                if (topicsNode != null)
                {
                    doc.Topics = topicsNode.Elements()
                                           .Where(e => e.Name.LocalName == "code")
                                           .Select(e => (string?)e.Attribute("code") ?? (string?)e.Attribute("value") ?? "")
                                           .Where(v => !string.IsNullOrEmpty(v))
                                           .ToList();
                }

                string doarLitere = Regex.Replace(textComplet.ToLower(), @"[^a-z]+", " ");
                string[] cuvinteBrute = doarLitere.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string cuvant in cuvinteBrute)
                {
                    if (cuvant.Length <= 2) continue;
                    if (stopwords.Contains(cuvant)) continue;

                    string radacina = stemmer.StemWord(cuvant);
                    if (string.IsNullOrEmpty(radacina)) continue;

                    if (doc.WordFrequencies.ContainsKey(radacina))
                    {
                        doc.WordFrequencies[radacina]++;
                    }
                    else
                    {
                        doc.WordFrequencies[radacina] = 1;
                    }

                    dictionarGlobal.Add(radacina);
                }

                return doc;
            }
            catch
            {
                return null;
            }
        }

        static void SalveazaFisier(string outputPath, List<string> cuvinteSortate,
                                   Dictionary<string, int> vocabularIndex, List<Document> documente)
        {
            using (StreamWriter writer = new StreamWriter(outputPath))
            {
                foreach (string cuvant in cuvinteSortate)
                {
                    writer.WriteLine($"@attribute {cuvant}");
                }

                writer.WriteLine("@data");

                foreach (var doc in documente)
                {
                    var elementeVector = doc.WordFrequencies
                        .Select(kv => new { Index = vocabularIndex[kv.Key], Frecventa = kv.Value })
                        .OrderBy(x => x.Index)
                        .Select(x => $"{x.Index}:{x.Frecventa}");

                    string vectorRar = string.Join(" ", elementeVector);
                    string claseTopics = doc.Topics.Count > 0 ? string.Join(" ", doc.Topics) : "none";

                    writer.WriteLine($"{doc.Id} # {vectorRar} # {claseTopics} # {doc.Folder}");

                }
            }
        }

    }
}