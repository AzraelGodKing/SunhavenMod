using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SunhavenMods.Shared
{
    /// <summary>
    /// Thrown when <see cref="MinimalJsonParser"/> rejects malformed JSON.
    /// Callers that previously treated partial parses as success should catch this and fall back to backup.
    /// </summary>
    public sealed class JsonParseException : Exception
    {
        public int Position { get; }

        public JsonParseException(string message, int position)
            : base(message)
        {
            Position = position;
        }
    }

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
                        default:
                            if (c < ' ' || c == '\u007f')
                            {
                                sb.Append("\\u");
                                sb.Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                            }
                            else
                            {
                                sb.Append(c);
                            }
                            break;
                    }
                }
            }
            sb.Append('"');
        }

        /// <summary>
        /// Parse a complete JSON document. Rejects trailing content after the root value.
        /// </summary>
        internal static object Parse(string json)
        {
            if (json == null)
                throw new JsonParseException("JSON input is null.", 0);

            int pos = 0;
            object value = ParseValue(json, ref pos);
            SkipWhitespace(json, ref pos);
            if (pos < json.Length)
                throw new JsonParseException($"Unexpected trailing content at position {pos}.", pos);
            return value;
        }

        internal static void SkipWhitespace(string json, ref int pos)
        {
            while (pos < json.Length && char.IsWhiteSpace(json[pos]))
                pos++;
        }

        internal static object ParseValue(string json, ref int pos)
        {
            SkipWhitespace(json, ref pos);
            if (pos >= json.Length)
                throw new JsonParseException("Unexpected end of input.", pos);
            char c = json[pos];
            if (c == '"') return ParseString(json, ref pos);
            if (c == '{') return ParseObject(json, ref pos);
            if (c == '[') return ParseArray(json, ref pos);
            if (c == 't') return ParseLiteral(json, ref pos, "true", true);
            if (c == 'f') return ParseLiteral(json, ref pos, "false", false);
            if (c == 'n') return ParseLiteral(json, ref pos, "null", null);
            if (c == '-' || char.IsDigit(c)) return ParseNumber(json, ref pos);
            throw new JsonParseException($"Unexpected character '{c}' at position {pos}.", pos);
        }

        internal static Dictionary<string, object> ParseObject(string json, ref int pos)
        {
            SkipWhitespace(json, ref pos);
            if (pos >= json.Length || json[pos] != '{')
                throw new JsonParseException("Expected '{' to start object.", pos);
            pos++;
            var dict = new Dictionary<string, object>();
            SkipWhitespace(json, ref pos);
            if (pos < json.Length && json[pos] == '}') { pos++; return dict; }
            while (true)
            {
                SkipWhitespace(json, ref pos);
                var key = ParseString(json, ref pos);
                SkipWhitespace(json, ref pos);
                if (pos >= json.Length || json[pos] != ':')
                    throw new JsonParseException("Expected ':' after object key.", pos);
                pos++;
                SkipWhitespace(json, ref pos);
                dict[key] = ParseValue(json, ref pos);
                SkipWhitespace(json, ref pos);
                if (pos < json.Length && json[pos] == ',')
                {
                    pos++;
                    continue;
                }
                break;
            }
            SkipWhitespace(json, ref pos);
            if (pos >= json.Length || json[pos] != '}')
                throw new JsonParseException("Expected '}' to close object.", pos);
            pos++;
            return dict;
        }

        internal static List<object> ParseArray(string json, ref int pos)
        {
            SkipWhitespace(json, ref pos);
            if (pos >= json.Length || json[pos] != '[')
                throw new JsonParseException("Expected '[' to start array.", pos);
            pos++;
            var list = new List<object>();
            SkipWhitespace(json, ref pos);
            if (pos < json.Length && json[pos] == ']') { pos++; return list; }
            while (true)
            {
                SkipWhitespace(json, ref pos);
                list.Add(ParseValue(json, ref pos));
                SkipWhitespace(json, ref pos);
                if (pos < json.Length && json[pos] == ',')
                {
                    pos++;
                    continue;
                }
                break;
            }
            SkipWhitespace(json, ref pos);
            if (pos >= json.Length || json[pos] != ']')
                throw new JsonParseException("Expected ']' to close array.", pos);
            pos++;
            return list;
        }

        internal static string ParseString(string json, ref int pos)
        {
            SkipWhitespace(json, ref pos);
            if (pos >= json.Length || json[pos] != '"')
                throw new JsonParseException("Expected '\"' to start string.", pos);
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
                        case 'u':
                            if (pos + 4 >= json.Length)
                                throw new JsonParseException("Incomplete Unicode escape in string.", pos);
                            {
                                string hex = json.Substring(pos + 1, 4);
                                if (!ushort.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort codeUnit))
                                    throw new JsonParseException($"Invalid Unicode escape \\u{hex}.", pos);
                                pos += 4;
                                if (codeUnit >= 0xD800 && codeUnit <= 0xDBFF)
                                {
                                    if (pos + 5 >= json.Length || json[pos + 1] != '\\' || json[pos + 2] != 'u')
                                        throw new JsonParseException("Unpaired high surrogate in string.", pos);
                                    string hex2 = json.Substring(pos + 3, 4);
                                    if (!ushort.TryParse(hex2, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort codeUnit2) ||
                                        codeUnit2 < 0xDC00 || codeUnit2 > 0xDFFF)
                                        throw new JsonParseException("Invalid low surrogate in string.", pos);
                                    sb.Append(char.ConvertFromUtf32(char.ConvertToUtf32((char)codeUnit, (char)codeUnit2)));
                                    pos += 6;
                                    break;
                                }
                                sb.Append((char)codeUnit);
                                break;
                            }
                        default:
                            throw new JsonParseException($"Invalid escape '\\{json[pos]}' in string.", pos);
                    }
                    pos++;
                }
                else if (c == '"')
                {
                    pos++;
                    return sb.ToString();
                }
                else if (c < ' ')
                {
                    throw new JsonParseException($"Unescaped control character U+{((int)c):X4} in string.", pos);
                }
                else
                {
                    sb.Append(c);
                    pos++;
                }
            }
            throw new JsonParseException("Unterminated string.", pos);
        }

        internal static object ParseNumber(string json, ref int pos)
        {
            int start = pos;
            bool isFloat = false;
            if (pos < json.Length && json[pos] == '-') pos++;
            if (pos >= json.Length || !char.IsDigit(json[pos]))
                throw new JsonParseException("Invalid number.", start);
            while (pos < json.Length && char.IsDigit(json[pos])) pos++;
            if (pos < json.Length && json[pos] == '.')
            {
                isFloat = true;
                pos++;
                if (pos >= json.Length || !char.IsDigit(json[pos]))
                    throw new JsonParseException("Invalid number fraction.", start);
                while (pos < json.Length && char.IsDigit(json[pos])) pos++;
            }
            if (pos < json.Length && (json[pos] == 'e' || json[pos] == 'E'))
            {
                isFloat = true;
                pos++;
                if (pos < json.Length && (json[pos] == '+' || json[pos] == '-')) pos++;
                if (pos >= json.Length || !char.IsDigit(json[pos]))
                    throw new JsonParseException("Invalid number exponent.", start);
                while (pos < json.Length && char.IsDigit(json[pos])) pos++;
            }
            string numStr = json.Substring(start, pos - start);
            if (isFloat)
            {
                if (double.TryParse(numStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                    return d;
                throw new JsonParseException($"Invalid floating-point number '{numStr}'.", start);
            }
            if (long.TryParse(numStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out long l))
                return l;
            throw new JsonParseException($"Invalid integer '{numStr}'.", start);
        }

        internal static object ParseLiteral(string json, ref int pos, string literal, object result)
        {
            if (pos + literal.Length <= json.Length &&
                string.CompareOrdinal(json, pos, literal, 0, literal.Length) == 0)
            {
                pos += literal.Length;
                return result;
            }
            throw new JsonParseException($"Expected literal '{literal}'.", pos);
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
