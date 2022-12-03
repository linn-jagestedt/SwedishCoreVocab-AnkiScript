using System.Xml.Serialization;
using System.IO;

namespace DeckGenerator
{
    [XmlRoot(ElementName = "dictionary")]
    public class XmlDictionary 
    {
        [XmlElement(ElementName = "word")]
        public XmlWord[] Words;

        public static XmlDictionary Deserialize(string file) 
        {
            XmlSerializer serializer = new XmlSerializer(typeof(XmlDictionary));
            XmlDictionary dictionary;

            using (Stream reader = new FileStream(file, FileMode.Open)) {
                dictionary = (XmlDictionary)serializer.Deserialize(reader);
            }

            for (int i = 0; i < dictionary.Words.Length; i++) 
            {
                XmlWord[] words = dictionary.Words;

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

    public class XmlWord
    {
        [XmlAttribute(AttributeName = "value")]
        public string Value;
        [XmlAttribute(AttributeName = "class")]
        public string Class;

        [XmlElement(ElementName = "translation")]
        public XmlTranslation[] Translations;

        [XmlElement(ElementName = "derivation")]
        public XmlDerivation[] Derivations;

        [XmlElement(ElementName = "example")]
        public XmlExample[] Examples;
            
        [XmlElement(ElementName = "paradigm")]
        public XmlParadigm Paradigm;

        [XmlElement(ElementName = "definition")]
        public XmlDefinition Definition;

        [XmlElement(ElementName = "phonetic")]
        public XmlPhonetic Phonetic;

        [XmlElement(ElementName = "idiom")]
        public XmlIdiom[] Idioms;
    }

    public class XmlIdiom 
    {
        [XmlAttribute(AttributeName = "value")]
        public string Value;

        [XmlElement(ElementName = "translation")]
        public XmlTranslation Translation;
    }

    public class XmlPhonetic {
        [XmlAttribute(AttributeName = "value")]
        public string Value;
        
        [XmlAttribute(AttributeName = "soundFile")]
        public string SoundFile;
    }

    public class XmlExample 
    {
        [XmlAttribute(AttributeName = "value")]
        public string Value;

        [XmlElement(ElementName = "translation")]
        public XmlTranslation Translation;
    }

    public class XmlDefinition 
    {
        [XmlAttribute(AttributeName = "value")]
        public string Value;
        [XmlElement(ElementName = "translation")]
        public XmlTranslation Translation;
    }


    public class XmlParadigm {
        [XmlElement(ElementName = "inflection")]
        public XmlInflection[] Inflections;
    }

    public class XmlInflection
    {
        [XmlAttribute(AttributeName = "value")]
        public string Value;
    }
    
    public class XmlDerivation {
        [XmlAttribute(AttributeName = "value")]
        public string Value;

        [XmlElement(ElementName = "translation")]
        public XmlTranslation[] Translations;
    }

    public class XmlTranslation 
    {
        [XmlAttribute(AttributeName = "comment")]
        public string Comment;

        [XmlAttribute(AttributeName = "value")]
        public string Value;
    }
}