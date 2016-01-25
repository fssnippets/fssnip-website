module FsSnip.Rssfeed

open System.Xml
open System.Text
open System.IO

// -------------------------------------------------------------------------------------------------
// Rss XML writer - based on Phil's post: http://trelford.com/blog/post/F-XML-Comparison-(XElement-vs-XmlDocument-vs-XmlReaderXmlWriter-vs-Discriminated-Unions).aspx
// -------------------------------------------------------------------------------------------------

type XmlAttribute = {Prefix: string; LocalName: string; Value: string}
type RssItem = {Title: string; Link: string; PubDate: string; Author: string; Description: string}

/// F# Element Tree
type Xml = 
    | Element of string * XmlAttribute list * string * Xml seq    
    member this.WriteContentTo(writer:XmlWriter) =
        let rec Write element =
            match element with
            | Element (name, attributes, value, children) -> 
                writer.WriteStartElement(name)
                attributes
                |> List.iter (fun attr -> writer.WriteAttributeString(attr.Prefix,attr.LocalName, null,attr.Value))
                writer.WriteString(value)
                children |> Seq.iter (fun child -> Write child)
                writer.WriteEndElement()
        Write this                
    override this.ToString() =
        let output = StringBuilder()             
        using (new XmlTextWriter(new StringWriter(output), 
                Formatting=Formatting.Indented))
            this.WriteContentTo        
        output.ToString()

// XmlAttribute helper
let attribute prefix localName value =
    {Prefix = prefix; LocalName = localName; Value = value}

// Take an item and return the XML
let getItem (item: RssItem) = 
    Element("item", [],"",
      [ Element("title", [attribute "xmlns" "cf" "http://www.microsoft.com/schemas/rss/core/2005"; attribute "cf" "type" "text"], item.Title,[])
        Element("link", [],item.Link,[])
        Element("pubDate", [],item.PubDate,[])
        Element("author", [],item.Author,[])
        Element("description", [attribute "xmlns" "cf" "http://www.microsoft.com/schemas/rss/core/2005"; attribute "cf" "type" "text"], item.Description,[]) ])

// Take a list of items and return the channel
let getChannel title link description language (items: seq<Xml>) =
    let channelAttributes =
      [ attribute "xmlns" "cfi" "http://www.microsoft.com/schemas/rss/core/2005/internal"
        attribute "xmlns" "lastdownloaderror" "None"]
    let channelElements = 
      [ Element("title", [attribute "xmlns" "cf" "http://www.microsoft.com/schemas/rss/core/2005"; attribute "cf" "type" "text"],title,[])
        Element("link", [],link,[])
        Element("description", [attribute "xmlns" "cf" "http://www.microsoft.com/schemas/rss/core/2005"; attribute "cf" "type" "text"],description,[])
        Element("dc:language", [],language,[]) ]
    Element("channel", channelAttributes, "", (Seq.append channelElements items))

// Take a channel and return the Rss xml
let getRss channel = 
  let rssAttributes = 
    [ attribute null "version" "2.0"
      attribute "xmlns" "atom" "http://www.w3.org/2005/Atom"
      attribute "xmlns" "cf" "http://www.microsoft.com/schemas/rss/core/2005"
      attribute "xmlns" "dc" "http://purl.org/dc/elements/1.1/"
      attribute "xmlns" "slash" "http://purl.org/rss/1.0/modules/slash/"
      attribute "xmlns" "wfw" "http://wellformedweb.org/CommentAPI/" ]
  Element("rss", rssAttributes, "", [channel])

// take the channel config and item list and return the string output in valid XML format
let RssOutput title link description language (list: seq<RssItem>) =
    let rss = 
        list
        |> Seq.map getItem
        |> getChannel title link description language
        |> getRss
    "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" + (rss.ToString())