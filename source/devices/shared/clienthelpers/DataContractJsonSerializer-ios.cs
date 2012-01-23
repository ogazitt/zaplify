using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;

namespace System.Runtime.Serialization.Json
{
	public class DataContractJsonSerializer
	{
		private Type type;
		private JsonSerializer js;
		
		public DataContractJsonSerializer (Type t)
		{
			this.type = t;
			this.js = new JsonSerializer();
		}
		
		public object ReadObject(Stream stream)
		{
			StreamReader reader = new StreamReader(stream);		
			return js.Deserialize(reader, type);
		}
		
		public void WriteObject(Stream stream, object o)
		{
			StreamWriter writer = new StreamWriter(stream);
			js.Serialize(writer, o);	
			writer.Flush ();
		}
	}
}

