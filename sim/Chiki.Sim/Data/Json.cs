using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Chiki.Sim.Data
{
    /// <summary>A malformed document or a field of the wrong shape.</summary>
    public sealed class JsonException : Exception
    {
        public JsonException(string message) : base(message)
        {
        }
    }

    public enum JsonKind
    {
        Null,
        Boolean,
        Number,
        String,
        Array,
        Object,
    }

    /// <summary>
    /// A parsed JSON document. The simulation references nothing but the base class library,
    /// which on netstandard2.1 has no JSON reader, so content files are read with this one.
    /// Numbers are kept as text and exposed as integers only: rule data never holds a float.
    /// </summary>
    public sealed class JsonValue
    {
        private readonly string? _text;
        private readonly bool _boolean;
        private readonly List<JsonValue>? _items;
        private readonly Dictionary<string, JsonValue>? _members;
        private readonly List<string>? _memberNames;

        public JsonKind Kind { get; }

        private JsonValue(JsonKind kind, string? text = null, bool boolean = false,
            List<JsonValue>? items = null, Dictionary<string, JsonValue>? members = null, List<string>? memberNames = null)
        {
            Kind = kind;
            _text = text;
            _boolean = boolean;
            _items = items;
            _members = members;
            _memberNames = memberNames;
        }

        public static JsonValue Parse(string text)
        {
            if (text is null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            var parser = new Parser(text);
            var value = parser.ParseValue();
            parser.ExpectEnd();
            return value;
        }

        public bool IsNull => Kind == JsonKind.Null;

        /// <summary>Elements of an array.</summary>
        public IReadOnlyList<JsonValue> Items => _items ?? throw Wrong(JsonKind.Array);

        /// <summary>Member names of an object in document order.</summary>
        public IReadOnlyList<string> MemberNames => _memberNames ?? throw Wrong(JsonKind.Object);

        public bool Has(string name)
        {
            return (_members ?? throw Wrong(JsonKind.Object)).ContainsKey(name);
        }

        /// <summary>A required member; throws when it is missing.</summary>
        public JsonValue this[string name]
        {
            get
            {
                var members = _members ?? throw Wrong(JsonKind.Object);
                if (!members.TryGetValue(name, out var value))
                {
                    throw new JsonException($"Missing required field '{name}'.");
                }

                return value;
            }
        }

        /// <summary>An optional member; null when missing or JSON null.</summary>
        public JsonValue? Optional(string name)
        {
            var members = _members ?? throw Wrong(JsonKind.Object);
            return members.TryGetValue(name, out var value) && !value.IsNull ? value : null;
        }

        public string AsString()
        {
            return Kind == JsonKind.String ? _text! : throw Wrong(JsonKind.String);
        }

        public bool AsBool()
        {
            return Kind == JsonKind.Boolean ? _boolean : throw Wrong(JsonKind.Boolean);
        }

        public long AsLong()
        {
            if (Kind != JsonKind.Number)
            {
                throw Wrong(JsonKind.Number);
            }

            if (_text!.IndexOfAny(new[] { '.', 'e', 'E' }) >= 0
                || !long.TryParse(_text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long value))
            {
                throw new JsonException($"Expected an integer but found '{_text}'.");
            }

            return value;
        }

        public int AsInt()
        {
            long value = AsLong();
            if (value < int.MinValue || value > int.MaxValue)
            {
                throw new JsonException($"Integer '{_text}' is out of range.");
            }

            return (int)value;
        }

        private JsonException Wrong(JsonKind expected)
        {
            return new JsonException($"Expected {expected} but found {Kind}.");
        }

        private sealed class Parser
        {
            private readonly string _s;
            private int _i;

            public Parser(string s)
            {
                _s = s;
            }

            public JsonValue ParseValue()
            {
                SkipWhitespace();
                if (_i >= _s.Length)
                {
                    throw Error("Unexpected end of document.");
                }

                char c = _s[_i];
                switch (c)
                {
                    case '{':
                        return ParseObject();
                    case '[':
                        return ParseArray();
                    case '"':
                        return new JsonValue(JsonKind.String, text: ParseString());
                    case 't':
                        ExpectLiteral("true");
                        return new JsonValue(JsonKind.Boolean, boolean: true);
                    case 'f':
                        ExpectLiteral("false");
                        return new JsonValue(JsonKind.Boolean, boolean: false);
                    case 'n':
                        ExpectLiteral("null");
                        return new JsonValue(JsonKind.Null);
                    default:
                        if (c == '-' || (c >= '0' && c <= '9'))
                        {
                            return new JsonValue(JsonKind.Number, text: ParseNumber());
                        }

                        throw Error($"Unexpected character '{c}'.");
                }
            }

            public void ExpectEnd()
            {
                SkipWhitespace();
                if (_i < _s.Length)
                {
                    throw Error("Unexpected content after the document.");
                }
            }

            private JsonValue ParseObject()
            {
                _i++; // {
                var members = new Dictionary<string, JsonValue>(StringComparer.Ordinal);
                var names = new List<string>();
                SkipWhitespace();
                if (Peek() == '}')
                {
                    _i++;
                    return new JsonValue(JsonKind.Object, members: members, memberNames: names);
                }

                while (true)
                {
                    SkipWhitespace();
                    if (Peek() != '"')
                    {
                        throw Error("Expected a member name.");
                    }

                    string name = ParseString();
                    SkipWhitespace();
                    if (Peek() != ':')
                    {
                        throw Error("Expected ':' after a member name.");
                    }

                    _i++;
                    var value = ParseValue();
                    if (members.ContainsKey(name))
                    {
                        throw Error($"Duplicate member '{name}'.");
                    }

                    members[name] = value;
                    names.Add(name);
                    SkipWhitespace();
                    char c = Peek();
                    _i++;
                    if (c == '}')
                    {
                        return new JsonValue(JsonKind.Object, members: members, memberNames: names);
                    }

                    if (c != ',')
                    {
                        throw Error("Expected ',' or '}' in an object.");
                    }
                }
            }

            private JsonValue ParseArray()
            {
                _i++; // [
                var items = new List<JsonValue>();
                SkipWhitespace();
                if (Peek() == ']')
                {
                    _i++;
                    return new JsonValue(JsonKind.Array, items: items);
                }

                while (true)
                {
                    items.Add(ParseValue());
                    SkipWhitespace();
                    char c = Peek();
                    _i++;
                    if (c == ']')
                    {
                        return new JsonValue(JsonKind.Array, items: items);
                    }

                    if (c != ',')
                    {
                        throw Error("Expected ',' or ']' in an array.");
                    }
                }
            }

            private string ParseString()
            {
                _i++; // opening quote
                var sb = new StringBuilder();
                while (true)
                {
                    if (_i >= _s.Length)
                    {
                        throw Error("Unterminated string.");
                    }

                    char c = _s[_i++];
                    if (c == '"')
                    {
                        return sb.ToString();
                    }

                    if (c != '\\')
                    {
                        sb.Append(c);
                        continue;
                    }

                    if (_i >= _s.Length)
                    {
                        throw Error("Unterminated escape sequence.");
                    }

                    char e = _s[_i++];
                    switch (e)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (_i + 4 > _s.Length)
                            {
                                throw Error("Truncated unicode escape.");
                            }

                            sb.Append((char)int.Parse(_s.Substring(_i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                            _i += 4;
                            break;
                        default:
                            throw Error($"Unknown escape '\\{e}'.");
                    }
                }
            }

            private string ParseNumber()
            {
                int start = _i;
                if (Peek() == '-')
                {
                    _i++;
                }

                if (!ReadDigits())
                {
                    throw Error("Expected digits in a number.");
                }

                if (_i < _s.Length && _s[_i] == '.')
                {
                    _i++;
                    if (!ReadDigits())
                    {
                        throw Error("Expected digits after the decimal point.");
                    }
                }

                if (_i < _s.Length && (_s[_i] == 'e' || _s[_i] == 'E'))
                {
                    _i++;
                    if (_i < _s.Length && (_s[_i] == '+' || _s[_i] == '-'))
                    {
                        _i++;
                    }

                    if (!ReadDigits())
                    {
                        throw Error("Expected digits in an exponent.");
                    }
                }

                return _s.Substring(start, _i - start);
            }

            private bool ReadDigits()
            {
                int start = _i;
                while (_i < _s.Length && _s[_i] >= '0' && _s[_i] <= '9')
                {
                    _i++;
                }

                return _i > start;
            }

            private void ExpectLiteral(string literal)
            {
                if (string.CompareOrdinal(_s, _i, literal, 0, literal.Length) != 0)
                {
                    throw Error($"Expected '{literal}'.");
                }

                _i += literal.Length;
            }

            private char Peek()
            {
                if (_i >= _s.Length)
                {
                    throw Error("Unexpected end of document.");
                }

                return _s[_i];
            }

            private void SkipWhitespace()
            {
                while (_i < _s.Length)
                {
                    char c = _s[_i];
                    if (c == ' ' || c == '\t' || c == '\n' || c == '\r')
                    {
                        _i++;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            private JsonException Error(string message)
            {
                return new JsonException($"{message} (at offset {_i})");
            }
        }
    }
}
