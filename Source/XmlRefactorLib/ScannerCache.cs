using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;


namespace XmlRefactor
{
    public class ScannerCache
    {
        private Dictionary<string, HashSet<string>> cache = new Dictionary<string, HashSet<string>>();

        public void Add(string Filename, Rule rule)
        {
            HashSet<string> files;
            if (!cache.TryGetValue(rule.InputParameter, out files))
            {
                files = new HashSet<string>();
                cache.Add(rule.InputParameter, files);
            }
            files.Add(Filename);
        }

        public HashSet<string> Files(Rule rule)
        {
            HashSet<string> files;
            if (cache.TryGetValue(rule.InputParameter, out files))
            {
                return files;
            }

            return new HashSet<string>();
        }

        public void ToFile(string filename)
        {
            string jsonString = JsonSerializer.Serialize(cache);
            File.WriteAllText(filename, jsonString);
        }

        public static ScannerCache DeserializeDictionaryFromFile(string filePath)
        {
            string jsonString = File.ReadAllText(filePath);
            ScannerCache scannerCache = new ScannerCache();
            scannerCache.cache = JsonSerializer.Deserialize<Dictionary<string, HashSet<string>>>(jsonString);
            return scannerCache;
        }
    }
}
