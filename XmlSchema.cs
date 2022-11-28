using System.Xml.Serialization;

namespace DeckGenerator
{
    public struct LexicalResource {
        [XmlElement(ElementName = "Lexicon")]
        public Lexicon Lexicon;
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

    public class dictionary {
        [XmlElement(ElementName = "word")]
        public Word[] Words;
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
    }

    public class Example {
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

    public class Translation : IEquatable<Translation>
    {
        [XmlAttribute(AttributeName = "comment")]
        public string Comment;

        [XmlAttribute(AttributeName = "value")]
        public string Value;

        public bool Equals(Translation translation)
        {
            return translation == null ? false : translation.Value == Value && translation.Comment == Comment;
        }
    }
    public class Derivation {
        [XmlAttribute(AttributeName = "value")]
        public string Value;

        [XmlElement(ElementName = "translation")]
        public Translation[] Translations;
    }
}

