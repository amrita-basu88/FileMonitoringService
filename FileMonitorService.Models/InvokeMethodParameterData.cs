using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace FileMonitorService.Models
{
    [Serializable]
    public class InvokeMethodParameterData
    {
        [Key]
        public long Id { get; set; }
        public String AssemblyName { get; set; }
        public String ClassName { get; set; }
        public String XmlData { get; set; }

        /// <summary>
        /// Foreign Key to force cascade delete
        /// </summary>
        [Required]
        public virtual long InvokeMethodDataId { get; set; }

        public InvokeMethodParameterData()
        {
        }

        public InvokeMethodParameterData(InvokeMethodParameterData invokeMethodParameterData)
        {
            if (invokeMethodParameterData == null)
            {
                return;
            }

            AssemblyName = invokeMethodParameterData.AssemblyName;
            ClassName = invokeMethodParameterData.ClassName;
            SerializeToXmlData(invokeMethodParameterData.XmlData);
        }

        public object DeserializeXmlData( Type type )
        {
            if (XmlData == null)
            {
                return null;
            }

            XmlSerializer mySerializer = new XmlSerializer(type);
            using (StringReader stringreader = new StringReader(XmlData))
            {
                return mySerializer.Deserialize(stringreader);
            }
        }

        public static String SerializeToXmlData<T>(T obj) where T : class
        {
            if (obj == null)
            {
                return null;
            }

            XmlWriterSettings settings = new XmlWriterSettings { OmitXmlDeclaration = false, Indent = true };
            XmlSerializer mySerializer = new XmlSerializer(typeof(T));
            using (StringWriter stringwriter = new Utf8StringWriter())
            using (XmlWriter xmlWriter = XmlWriter.Create(stringwriter, settings))
            {
                XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
                namespaces.Add("", "");
                mySerializer.Serialize(xmlWriter, obj, namespaces);
                return stringwriter.ToString();
            }
        }

        public class Utf8StringWriter : StringWriter
        {
            public override Encoding Encoding
            {
                get { return Encoding.UTF8; }
            }
        }
    }
}
