using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Tsukuyomi.Infrastructure.Config
{
    internal static class MiniJson
    {
        public static object Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            var parser = new Parser(json);
            return parser.ParseValue();
        }

        private sealed class Parser
        {
            private readonly string _json;
            private int _index;

            public Parser(string json)
            {
                _json = json;
            }

            public object ParseValue()
            {
                SkipWhitespace();
                if (_index >= _json.Length)
                {
                    return null;
                }

                return PeekChar() switch
                {
                    '{' => ParseObject(),
                    '[' => ParseArray(),
                    '"' => ParseString(),
                    't' => ParseTrue(),
                    'f' => ParseFalse(),
                    'n' => ParseNull(),
                    _ => ParseNumber()
                };
            }

            private Dictionary<string, object> ParseObject()
            {
                ConsumeChar('{');
                var map = new Dictionary<string, object>(StringComparer.Ordinal);
                SkipWhitespace();

                if (TryConsumeChar('}'))
                {
                    return map;
                }

                while (_index < _json.Length)
                {
                    SkipWhitespace();
                    var key = ParseString();
                    SkipWhitespace();
                    ConsumeChar(':');
                    var value = ParseValue();
                    map[key] = value;
                    SkipWhitespace();

                    if (TryConsumeChar('}'))
                    {
                        break;
                    }

                    ConsumeChar(',');
                }

                return map;
            }

            private List<object> ParseArray()
            {
                ConsumeChar('[');
                var list = new List<object>();
                SkipWhitespace();

                if (TryConsumeChar(']'))
                {
                    return list;
                }

                while (_index < _json.Length)
                {
                    list.Add(ParseValue());
                    SkipWhitespace();

                    if (TryConsumeChar(']'))
                    {
                        break;
                    }

                    ConsumeChar(',');
                }

                return list;
            }

            private string ParseString()
            {
                ConsumeChar('"');
                var builder = new StringBuilder();

                while (_index < _json.Length)
                {
                    var c = NextChar();
                    if (c == '"')
                    {
                        return builder.ToString();
                    }

                    if (c != '\\')
                    {
                        builder.Append(c);
                        continue;
                    }

                    if (_index >= _json.Length)
                    {
                        break;
                    }

                    var escaped = NextChar();
                    builder.Append(escaped switch
                    {
                        '"' => '"',
                        '\\' => '\\',
                        '/' => '/',
                        'b' => '\b',
                        'f' => '\f',
                        'n' => '\n',
                        'r' => '\r',
                        't' => '\t',
                        'u' => ParseUnicode(),
                        _ => escaped
                    });
                }

                throw new FormatException("Unterminated JSON string.");
            }

            private char ParseUnicode()
            {
                if (_index + 4 > _json.Length)
                {
                    throw new FormatException("Invalid unicode escape sequence.");
                }

                var hex = _json.Substring(_index, 4);
                _index += 4;
                return (char)Convert.ToInt32(hex, 16);
            }

            private object ParseNumber()
            {
                var start = _index;
                while (_index < _json.Length)
                {
                    var c = PeekChar();
                    if (!(char.IsDigit(c) || c is '-' or '+' or '.' or 'e' or 'E'))
                    {
                        break;
                    }

                    _index++;
                }

                var token = _json.Substring(start, _index - start);
                if (token.IndexOf('.') >= 0 || token.IndexOf('e') >= 0 || token.IndexOf('E') >= 0)
                {
                    if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                    {
                        return number;
                    }
                }
                else if (long.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer))
                {
                    return integer;
                }

                throw new FormatException($"Invalid JSON number: {token}");
            }

            private bool ParseTrue()
            {
                ConsumeLiteral("true");
                return true;
            }

            private bool ParseFalse()
            {
                ConsumeLiteral("false");
                return false;
            }

            private object ParseNull()
            {
                ConsumeLiteral("null");
                return null;
            }

            private void ConsumeLiteral(string literal)
            {
                if (_index + literal.Length > _json.Length ||
                    !string.Equals(_json.Substring(_index, literal.Length), literal, StringComparison.Ordinal))
                {
                    throw new FormatException($"Expected '{literal}'.");
                }

                _index += literal.Length;
            }

            private void SkipWhitespace()
            {
                while (_index < _json.Length && char.IsWhiteSpace(_json[_index]))
                {
                    _index++;
                }
            }

            private bool TryConsumeChar(char c)
            {
                SkipWhitespace();
                if (_index < _json.Length && _json[_index] == c)
                {
                    _index++;
                    return true;
                }

                return false;
            }

            private void ConsumeChar(char c)
            {
                SkipWhitespace();
                if (_index >= _json.Length || _json[_index] != c)
                {
                    throw new FormatException($"Expected '{c}' at index {_index}.");
                }

                _index++;
            }

            private char PeekChar()
            {
                return _json[_index];
            }

            private char NextChar()
            {
                return _json[_index++];
            }
        }
    }
}
