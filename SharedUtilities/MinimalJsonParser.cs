using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SunhavenMods.Shared
{
    internal static class MinimalJsonParser
    {
        internal static void WriteJsonString(StringBuilder sb, string value)
        {
            sb.Append('"');
            if (value != null)
            {
                foreach (char c in value)
                {
                    switch (c)
                    {
                        case '"': sb.Append("\\\""); break;
                        case '\\': sb.Append("\\\\"); break;
                        case '\n': sb.Append("\\n"); break;
                        case '\r': sb.Append("\\r"); break;
                        case '\t': sb.Append("\\t"); break;
                        case '\b': sb.Append("\\b"); break;
                        case '\f': sb.Append("\\f"); break;
                        default: sb.Append(c); break;
                    }
                }
            }
            sb.Append('"');
        }

        internal static void SkipWhitespace(string json, ref int pos)
        {
            while (pos < json.Length && char.IsWhiteSpace(json[pos]))
                pos++;
        }

        internal static object ParseValue(string json, ref int pos)
        {
            SkipWhitespace(json, ref pos);
            if (pos >= json.Length) return null;
            char c = json[pos];
            if (c == '"') return ParseString(json, ref pos);
            if (c == '{') return ParseObject(json, ref pos);
            if (c == '[') return ParseArray(json, ref pos);
            if (c == 't') return ParseLiteral(json, ref pos, "true", true);
            if (c == 'f') return ParseLiteral(json, ref pos, "false", false);
            if (c == 'n') return ParseLiteral(json, ref pos, "null", null);
            if (c == '-' || char.IsDigit(c)) return ParseNumber(json, ref pos);
            return null;
        }

        internal static Dictionary<string, object> ParseObject(string json, ref int pos)
        {
            SkipWhitespace(json, ref pos);
            if (pos >= json.Length || json[pos] != '{') return null;
            pos++;
            var dict = new Dictionary<string, object>();
            SkipWhitespace(json, ref pos);
            if (pos < json.Length && json[pos] == '}') { pos++; return dict; }
            while (pos < json.Length)
            {
                SkipWhitespace(json, ref pos);
                var key = ParseString(json, ref pos);
                if (key == null) break;
                SkipWhitespace(json, ref pos);
                if (pos >= json.Length || json[pos] != ':') break;
                pos++;
                SkipWhitespace(json, ref pos);
                dict[key] = ParseValue(json, ref pos);
                SkipWhitespace(json, ref pos);
                if (pos < json.Length && json[pos] == ',') { pos++; continue; }
                break;
            }
            SkipWhitespace(json, ref pos);
            if (pos < json.Length && json[pos] == '}') pos++;
            return dict;
        }

        internal static List<object> ParseArray(string json, ref int pos)
        {
            SkipWhitespace(json, ref pos);
            if (pos >= json.Length || json[pos] != '[') return null;
            pos++;
            var list = new List<object>();
            SkipWhitespace(json, ref pos);
            if (pos < json.Length && json[pos] == ']') { pos++; return list; }
            while (pos < json.Length)
            {
                SkipWhitespace(json, ref pos);
                list.Add(ParseValue(json, ref pos));
                SkipWhitespace(json, ref pos);
                if (pos < json.Length && json[pos] == ',') { pos++; continue; }
                break;
            }
            SkipWhitespace(json, ref pos);
            if (pos < json.Length && json[pos] == ']') pos++;
            return list;
        }

        internal static string ParseString(string json, ref int pos)
        {
            SkipWhitespace(json, ref pos);
            if (pos >= json.Length || json[pos] != '"') return null;
            pos++;
            var sb = new StringBuilder();
            while (pos < json.Length)
            {
                char c = json[pos];
                if (c == '\\' && pos + 1 < json.Length)
                {
                    pos++;
                    switch (json[pos])
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        default: sb.Append(json[pos]); break;
                    }
                    pos++;
                }
                else if (c == '"') { pos++; return sb.ToString(); }
                else { sb.Append(c); pos++; }
            }
            return sb.ToString();
        }

        internal static object ParseNumber(string json, ref int pos)
        {
            int start = pos;
            bool isFloat = false;
            if (pos < json.Length && json[pos] == '-') pos++;
            while (pos < json.Length && char.IsDigit(json[pos])) pos++;
            if (pos < json.Length && json[pos] == '.') { isFloat = true; pos++; while (pos < json.Length && char.IsDigit(json[pos])) pos++; }
            if (pos < json.Length && (json[pos] == 'e' || json[pos] == 'E'))
            {
                isFloat = true; pos++;
                if (pos < json.Length && (json[pos] == '+' || json[pos] == '-')) pos++;
                while (pos < json.Length && char.IsDigit(json[pos])) pos++;
            }
            string numStr = json.Substring(start, pos - start);
            if (isFloat && double.TryParse(numStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double d)) return d;
            if (!isFloat && long.TryParse(numStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out long l)) return l;
            return 0L;
        }

        internal static object ParseLiteral(string json, ref int pos, string literal, object result)
        {
            if (pos + literal.Length <= json.Length && json.Substring(pos, literal.Length) == literal)
            {
                pos += literal.Length;
                return result;
            }
            pos++;
            return null;
        }

        internal static int ToInt(object val)
        {
            if (val is long l) return (int)l;
            if (val is double d) return (int)d;
            if (val is int i) return i;
            return 0;
        }
    }
}
