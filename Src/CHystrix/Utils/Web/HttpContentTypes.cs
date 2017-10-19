using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CHystrix.Utils.Web
{
    internal static class HttpContentTypes
    {
        public const string Utf8Suffix = "; charset=utf-8";

        public const string FormUrlEncoded = "application/x-www-form-urlencoded";

        public const string MultiPartFormData = "multipart/form-data";

        public const string Html = "text/html";

        public const string JsonReport = "text/jsonreport";

        public const string Xml = "application/xml";

        public const string XmlText = "text/xml";

        public const string Soap11 = " text/xml; charset=utf-8";

        public const string Soap12 = " application/soap+xml";

        public const string Json = "application/json";

        public const string JsonText = "text/json";

        public const string JavaScript = "application/javascript";

        public const string Jsv = "application/jsv";

        public const string JsvText = "text/jsv";

        public const string Csv = "text/csv";

        public const string Yaml = "application/yaml";

        public const string YamlText = "text/yaml";

        public const string PlainText = "text/plain";

        public const string MarkdownText = "text/markdown";

        public const string ProtoBuf = "application/x-protobuf";

        public const string MsgPack = "application/x-msgpack";

        public const string Bson = "application/bson";

        public const string Binary = "application/octet-stream";

        public const string FastInfoset = "application/fastinfoset";

        public const string EventStream = "text/event-stream";

    }
}
