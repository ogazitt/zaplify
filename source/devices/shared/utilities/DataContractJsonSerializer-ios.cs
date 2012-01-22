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
			// close the generic Serialize<T> method with the type 
			MethodInfo method = typeof(JsonSerializer).GetMethod("Deserialize"); 
			MethodInfo generic = method.MakeGenericMethod(type);
			
			// get a stream reader and pass it to the generic method
			StreamReader reader = new StreamReader(stream);			
			return generic.Invoke(js, new object[] { reader });
		}
		
		public void WriteObject(Stream stream, object o)
		{
			StreamWriter writer = new StreamWriter(stream);
			js.Serialize(writer, o);	
		}
	}
}

