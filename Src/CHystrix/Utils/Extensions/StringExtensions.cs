//
// ServiceStack.Text: .NET C# POCO JSON, JSV and CSV Text Serializers.
//
// Authors:
//   William Yang (b.yang@ctrip.com)
//
// Copyright 2012 CTrip Ltd.
//
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace CHystrix.Utils.Extensions
{
    internal static class StringExtensions
    {
        public static string ToJson(this object obj)
        {
            if (obj == null)
                return null;

            DataContractJsonSerializer serializer = new DataContractJsonSerializer(obj.GetType());
            string result = null;
            using (MemoryStream stream = new MemoryStream())
            {
                serializer.WriteObject(stream, obj);
                stream.Seek(0, SeekOrigin.Begin);
                using (StreamReader reader = new StreamReader(stream))
                {
                    result = reader.ReadToEnd();
                }
            }

            return result;
        }

        public static T FromJson<T>(this string json)
        {
            return (T)json.DeserializeFromString(typeof(T));
        }

        private static object DeserializeFromString(this string json, Type type)
        {
            try
            {
                using (var ms = new MemoryStream())
                {
                    var bytes = Encoding.UTF8.GetBytes(json);
                    ms.Write(bytes, 0, bytes.Length);
                    ms.Position = 0;
                    var serializer = new DataContractJsonSerializer(type);
                    return serializer.ReadObject(ms);
                }
            }
            catch (Exception ex)
            {
                throw new SerializationException("JsonDeserializer: Error converting string to type: " + ex.Message, ex);
            }
        }

        public static string EncodeXml(this string value)
        {
            return value.Replace("<", "&lt;").Replace(">", "&gt;").Replace("&", "&amp;");
        }

        public static string EncodeJson(this string value)
        {
            return String.Concat
            ("\"",
                value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", "\\n"),
                "\""
            );
        }

        public static string UrlEncode(this string text)
        {
            if (String.IsNullOrEmpty(text)) return text;

            var sb = new StringBuilder();

            foreach (var charCode in Encoding.UTF8.GetBytes(text))
            {

                if (
                    charCode >= 65 && charCode <= 90        // A-Z
                    || charCode >= 97 && charCode <= 122    // a-z
                    || charCode >= 48 && charCode <= 57     // 0-9
                    || charCode >= 44 && charCode <= 46     // ,-.
                    )
                {
                    sb.Append((char)charCode);
                }
                else
                {
                    sb.Append('%' + charCode.ToString("x2"));
                }
            }

            return sb.ToString();
        }

        public static string UrlDecode(this string text)
        {
            if (String.IsNullOrEmpty(text)) return null;

            var bytes = new List<byte>();

            var textLength = text.Length;
            for (var i = 0; i < textLength; i++)
            {
                var c = text[i];
                if (c == '+')
                {
                    bytes.Add(32);
                }
                else if (c == '%')
                {
                    var hexNo = Convert.ToByte(text.Substring(i + 1, 2), 16);
                    bytes.Add(hexNo);
                    i += 2;
                }
                else
                {
                    bytes.Add((byte)c);
                }
            }

            return Encoding.UTF8.GetString(bytes.ToArray());
        }

        public static string WithTrailingSlash(this string path)
        {
            if (String.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");

            if (path[path.Length - 1] != '/')
            {
                return path + "/";
            }
            return path;
        }
    }
}