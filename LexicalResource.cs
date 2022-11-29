using System.Xml.Serialization;
using System.IO;

namespace DeckGenerator
{
    public struct LexicalResource {
        [XmlElement(ElementName = "Lexicon")]
        public Lexicon Lexicon;

        public static LexicalResource Deserialize(string file) 
        {
            XmlSerializer serializer = new XmlSerializer(typeof(LexicalResource));
            LexicalResource lexicon;

            using (Stream reader = new FileStream(file, FileMode.Open)) {
                lexicon = (LexicalResource)serializer.Deserialize(reader);
            }

            return lexicon;
        }
    }

    public struct Lexicon
    {
        [XmlElement(ElementName = "LexicalEntry")]
        public LexicalEntry[] LexicalEntries;
    }

    public struct LexicalEntry
    {
        [XmlElement(ElementName = "Lemma")]
        public Lemma Lemma;
    }


    public struct Lemma
    {
        [XmlElement(ElementName = "FormRepresentation")]    
        public FormRepresentation FormRepresentation;
    }


    public struct FormRepresentation
    {
        [XmlElement(ElementName = "feat")]
        public Feat[] Feats;
    }

    public struct Feat
    {
        [XmlAttribute(AttributeName = "att")]
        public string Attribute;

        [XmlAttribute(AttributeName = "val")]
        public string Value;
    }

}

