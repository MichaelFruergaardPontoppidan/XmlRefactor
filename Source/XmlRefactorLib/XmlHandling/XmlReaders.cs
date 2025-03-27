using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;

namespace XmlRefactor
{
	/// <summary>
	/// A class to read the contents of an xml file.
	/// It will remove header information and find dependencies
	/// </summary> 
	
	public class XmlReader
	{
        public Encoding fileEncoding;
		private UTF8Encoding utf8Encoding = new UTF8Encoding(true, true); // turn on BOM and error checking
		private string text;

        public string Text()
        {
            return text;
        }

		/// <summary>
		/// Constructor of the Xml reader class
		/// </summary>
		/// <param name="file">Xml file to read</param>
		public XmlReader(string file)
		{
            Encoding utf8Encoding = Encoding.UTF8;
            Encoding ansi1252fileEncoding = Encoding.GetEncoding(1252); // all existing XML files should be in 1252. Danish ones will be a problem.;

            using (FileStream fsr = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (BufferedStream bs = new BufferedStream(fsr))
            using (BinaryReader breader = new BinaryReader(bs))
            {
                long fileLength = fsr.Length;
                byte[] binaryContent = new byte[fileLength];
                breader.Read(binaryContent, 0, (int)fileLength);

                try
                {
                    if (fileLength > 3 &&
                       binaryContent[0] == 0xEF &&
                       binaryContent[1] == 0xBB &&
                       binaryContent[2] == 0xBF)
                    {
                        fileEncoding = utf8Encoding;
                        text = fileEncoding.GetString(binaryContent, 3, binaryContent.Length - 3);
                    }
                    else
                    {
                        fileEncoding = ansi1252fileEncoding;
                        text = fileEncoding.GetString(binaryContent, 0, binaryContent.Length);
                    }
                }
                catch 
                {
                    text = "";
                }
			}			
		}       
	}
}