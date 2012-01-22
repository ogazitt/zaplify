using System;
using System.IO;

namespace BuiltSteady.Zaplify.Devices.Utilities
{
    public static class SerializationHelper<T>
    {
        /// <summary>
        /// Deserialize a stream into an object graph
        /// </summary>
        /// <param name="stream">Stream to deserialize</param>
        /// <returns>Object graph of type T</returns>
        public static T Deserialize(Stream stream)
        {
#if IOS
			var js = new Newtonsoft.Json.JsonSerializer();
			StreamReader reader = new StreamReader(stream);
            var type = js.Deserialize<T>(reader);
			return type;
#else
            var dc = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(T));
            return (T) dc.ReadObject(stream);
#endif
		}
		
        /// <summary>
        /// Serialize an object graph to a stream
        /// </summary>
        /// <param name="stream">Stream to serialize into</param>
        /// <param name="o">Object graph of type T</param>
		public static void Serialize(Stream stream, object o)
		{
#if IOS
			var js = new Newtonsoft.Json.JsonSerializer();
            StreamWriter writer = new StreamWriter(stream);
			js.Serialize<T>(writer, o);			
#else
            var dc = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(T));
            dc.WriteObject(stream, o);
#endif
        }
    }
}
