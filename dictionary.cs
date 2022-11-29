using System.Xml.Serialization;
using System.IO;

namespace DeckGenerator
{
    public class dictionary 
    {
        [XmlElement(ElementName = "word")]
        public Word[] Words;

        public static dictionary Deserialize(string file) 
        {
            XmlSerializer serializer = new XmlSerializer(typeof(dictionary));
            dictionary dictionary;

            using (Stream reader = new FileStream(file, FileMode.Open)) {
                dictionary = (dictionary)serializer.Deserialize(reader);
            }

            for (int i = 0; i < dictionary.Words.Length; i++) 
            {
                Word[] words = dictionary.Words;

                if (words[i].Value.Contains(" (")) {
                    words[i].Value = words[i].Value.Substring(0, words[i].Value.IndexOf(" ("));
                }

                words[i].Value = words[i].Value.Replace("|", "").ToLower();

                if (words[i].Derivations != null) {
                    for (int j = 0; j < words[i].Derivations.Length; j++) {
                        words[i].Derivations[j].Value = words[i].Derivations[j].Value.Replace("|", "").ToLower();
                    }
                }
            }

            return dictionary;
        }
    }

    public class Word
    {
        [XmlAttribute(AttributeName = "value")]
        public string Value;
        [XmlAttribute(AttributeName = "class")]
        public string Class;

        [XmlElement(ElementName = "translation")]
        public Translation[] Translations;

        [XmlElement(ElementName = "derivation")]
        public Derivation[] Derivations;

        [XmlElement(ElementName = "example")]
        public Example[] Examples;
            
        [XmlElement(ElementName = "paradigm")]
        public Paradigm Paradigm;

        [XmlElement(ElementName = "definition")]
        public Definition Definition;
    }

    public class Example 
    {
        [XmlAttribute(AttributeName = "value")]
        public string Value;

        [XmlElement(ElementName = "translation")]
        public Translation Translation;
    }

    public class Definition 
    {
        [XmlAttribute(AttributeName = "value")]
        public string Value;
        [XmlElement(ElementName = "translation")]
        public Translation Translation;
    }


    public class Paradigm {
        [XmlElement(ElementName = "inflection")]
        public Inflection[] Inflections;
    }

    public class Inflection
    {
        [XmlAttribute(AttributeName = "value")]
        public string Value;
    }
    
    public class Derivation {
        [XmlAttribute(AttributeName = "value")]
        public string Value;

        [XmlElement(ElementName = "translation")]
        public Translation[] Translations;
    }

    public class Translation 
    {
        [XmlAttribute(AttributeName = "comment")]
        public string Comment;

        [XmlAttribute(AttributeName = "value")]
        public string Value;
    }
}