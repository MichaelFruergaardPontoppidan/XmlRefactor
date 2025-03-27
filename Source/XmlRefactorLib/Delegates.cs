namespace XmlRefactor
{
    public delegate void ResultDelegate(ResultItem item);
    public delegate void ProgressDelegate(string Filename);
    public delegate void FileMatchedDelegate(string Filename, Rule rule);
    public delegate void SignalEndDelegate();

    public class ResultItem
    {
        public string filename;
        public string before;
        public string after;
        public int hits;
    }
}