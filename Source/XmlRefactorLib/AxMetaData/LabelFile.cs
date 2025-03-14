using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace XmlRefactor
{
    public class Label
    {
        public string ID;
        public string Text;
        public string Description = String.Empty;
    }

    public class LabelFile
    {
        private List<Label> Labels = new List<Label>();
        private HashSet<string> LabelIDs = new HashSet<string>();

        public LabelFile(string _contents)
        {
            string[] lines = _contents.Split('\n');
            Label entry = null;
            foreach (string line in lines)
            {                
                if (line.StartsWith(" ;"))
                {
                    entry.Description = line.Substring(2).Replace("\r", "");
                    entry = null;
                }
                else
                {
                    int pos = line.IndexOf("=");
                    if (pos > 0)
                    {
                        entry = new Label()
                        {
                            ID = line.Substring(0, pos),
                            Text = line.Substring(pos + 1).Replace("\r", ""),
                        };
                        this.addLabel(entry);
                    }
                }
            }
        }

        public void addLabel(Label _entry)
        {
            if (LabelIDs.Contains(_entry.ID))
            {
                throw new Exception("Label with ID already exists");
            }
            Labels.Add(_entry);
            LabelIDs.Add(_entry.ID);
        }

        public string export()
        {
            string result = String.Empty;
            List<Label> orderedLabels = Labels.OrderBy(lbl => lbl.ID, StringComparer.OrdinalIgnoreCase).ToList();
            foreach (var label in orderedLabels)
            {
                result += String.Format("{0}={1}\r\n", label.ID, label.Text);
                if (label.Description != String.Empty)
                {
                    result += String.Format(" ;{0}\r\n", label.Description);
                }

            }
            return result;
        }

    }
}
