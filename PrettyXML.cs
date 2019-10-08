using System;
using System.Text;
using System.Xml;
using System.Xml.Linq;

public static class PrettyXml
{
    public static string Fire(string xml)
    {
        try
        {

            var stringBuilder = new StringBuilder();

            var element = XElement.Parse(xml);

            var settings = new XmlWriterSettings();
            settings.OmitXmlDeclaration = true;
            settings.Indent = true;
            settings.NewLineOnAttributes = true;

            using (var xmlWriter = XmlWriter.Create(stringBuilder, settings))
            {
                element.Save(xmlWriter);
            }

            return stringBuilder.ToString();
        }
        catch (Exception ex) {
            return xml;
        }


    }
}