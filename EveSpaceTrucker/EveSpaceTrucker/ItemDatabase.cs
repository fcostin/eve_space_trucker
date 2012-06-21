using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EveSpaceTrucker
{
    public class ItemDatabase
    {
        public class Entry
        {
            public string typeId, name, group;
            public double volume;

            public Entry(string typeId, string name, string group, double volume)
            {
                this.typeId = typeId;
                this.name = name;
                this.group = group;
                this.volume = volume;
            }

            public override string ToString()
            {
                return "item(id=" + typeId + ", name=" + name + ", group=" + group + ", volume=" + volume + ")";
            }
        }

        public Dictionary<string, Entry> idToEntry;
        public Dictionary<string, Entry> nameToEntry;

        public ItemDatabase()
        {
            idToEntry = new Dictionary<string, Entry>();
            nameToEntry = new Dictionary<string, Entry>();
        }

        public void addEntry(string itemInfo)
        {
            char[] splitChars = { ';' };
            char[] trimChars = { '\"' };
            string[] tokens = itemInfo.Split(splitChars);
            if (tokens.Length != 15)
                return;
            for (int i = 0; i < tokens.Length; ++i)
                tokens[i] = tokens[i].Trim(trimChars);

            const int TOK_TYPE_ID = 0, TOK_GROUP_ID = 1, TOK_NAME = 2, TOK_VOLUME = 7;

            //parse volume, in m^3
            tokens[TOK_VOLUME] = tokens[TOK_VOLUME].Replace(",", "");
            tokens[TOK_VOLUME] = tokens[TOK_VOLUME].Replace(".", "");
            double volume = Double.Parse(tokens[TOK_VOLUME]);

            Entry parsedEntry = new Entry(tokens[TOK_TYPE_ID],
                tokens[TOK_NAME], tokens[TOK_GROUP_ID], volume);

            idToEntry[tokens[TOK_TYPE_ID]] = parsedEntry;
            nameToEntry[tokens[TOK_NAME]] = parsedEntry;
        }

        public void parseInvTypes(string path)
        {
            StreamReader reader = File.OpenText(path);
            string line = reader.ReadLine();
            //ignore header line
            line = reader.ReadLine();
            while (line != null)
            {
                addEntry(line);
                line = reader.ReadLine();
            }
            reader.Close();
        }
    }
}
