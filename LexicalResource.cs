using System.Xml.Serialization;
using System.IO;

namespace DeckGenerator
{
    [XmlRoot(ElementName = "LexicalResource")]
    public struct XmlLexicalResource {
        [XmlElement(ElementName = "Lexicon")]
        public XmlLexicon Lexicon;

        public static XmlLexicalResource Deserialize(string file) 
        {
            XmlSerializer serializer = new XmlSerializer(typeof(XmlLexicalResource));
            XmlLexicalResource lexicon;

            using (Stream reader = new FileStream(file, FileMode.Open)) {
                lexicon = (XmlLexicalResource)serializer.Deserialize(reader);
            }

            return lexicon;
        }
    }

    public struct XmlLexicon
    {
        [XmlElement(ElementName = "LexicalEntry")]
        public XmlLexicalEntry[] LexicalEntries;
    }

    public struct XmlLexicalEntry
    {
        [XmlElement(ElementName = "Lemma")]
        public XmlLemma Lemma;
    }


    public struct XmlLemma
    {
        [XmlElement(ElementName = "FormRepresentation")]    
        public XmlFormRepresentation FormRepresentation;
    }


    public struct XmlFormRepresentation
    {
        [XmlElement(ElementName = "feat")]
        public XmlFeat[] Feats;
    }

    public struct XmlFeat
    {
        [XmlAttribute(AttributeName = "att")]
        public string Attribute;

        [XmlAttribute(AttributeName = "val")]
        public string Value;
    }

}

