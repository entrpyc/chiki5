using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Chiki.Sim.Data
{
    /// <summary>
    /// Writes pretty-printed JSON, two-space indented like the content files, for the save
    /// documents the simulation produces (PRD 4.2). Numbers are integers only: rule data never
    /// holds a float. The output parses back with <see cref="JsonValue.Parse"/>.
    /// </summary>
    public sealed class JsonWriter
    {
        private readonly StringBuilder _text = new StringBuilder();
        private readonly Stack<int> _counts = new Stack<int>();
        private bool _expectingValue;

        public JsonWriter BeginObject()
        {
            BeforeValue();
            _text.Append('{');
            _counts.Push(0);
            return this;
        }

        public JsonWriter EndObject()
        {
            Close('}');
            return this;
        }

        public JsonWriter BeginArray()
        {
            BeforeValue();
            _text.Append('[');
            _counts.Push(0);
            return this;
        }

        public JsonWriter EndArray()
        {
            Close(']');
            return this;
        }

        /// <summary>The name of the next member; its value follows.</summary>
        public JsonWriter Name(string name)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (_counts.Count == 0 || _expectingValue)
            {
                throw new InvalidOperationException("A member name goes inside an object, before its value.");
            }

            Separate();
            WriteString(name);
            _text.Append(": ");
            _expectingValue = true;
            return this;
        }

        public JsonWriter Value(string? value)
        {
            BeforeValue();
            if (value is null)
            {
                _text.Append("null");
            }
            else
            {
                WriteString(value);
            }

            return this;
        }

        public JsonWriter Value(int value)
        {
            BeforeValue();
            _text.Append(value.ToString(CultureInfo.InvariantCulture));
            return this;
        }

        public JsonWriter Value(int? value)
        {
            return value is null ? Null() : Value(value.Value);
        }

        public JsonWriter Value(bool value)
        {
            BeforeValue();
            _text.Append(value ? "true" : "false");
            return this;
        }

        public JsonWriter Null()
        {
            BeforeValue();
            _text.Append("null");
            return this;
        }

        public JsonWriter Member(string name, string? value) => Name(name).Value(value);

        public JsonWriter Member(string name, int value) => Name(name).Value(value);

        public JsonWriter Member(string name, int? value) => Name(name).Value(value);

        public JsonWriter Member(string name, bool value) => Name(name).Value(value);

        /// <summary>A member holding an array of strings (or nulls).</summary>
        public JsonWriter Member(string name, IEnumerable<string?> values)
        {
            Name(name).BeginArray();
            foreach (var value in values)
            {
                Value(value);
            }

            return EndArray();
        }

        public override string ToString()
        {
            if (_counts.Count != 0)
            {
                throw new InvalidOperationException("The document is not closed.");
            }

            return _text.ToString();
        }

        private void BeforeValue()
        {
            if (_counts.Count == 0)
            {
                if (_text.Length > 0)
                {
                    throw new InvalidOperationException("A document holds one value.");
                }

                return;
            }

            if (_expectingValue)
            {
                _expectingValue = false;
                return;
            }

            Separate();
        }

        /// <summary>A comma after the previous item of the open container, then a newline and the indent.</summary>
        private void Separate()
        {
            int count = _counts.Pop();
            if (count > 0)
            {
                _text.Append(',');
            }

            _counts.Push(count + 1);
            NewLine(_counts.Count);
        }

        private void Close(char bracket)
        {
            if (_counts.Count == 0 || _expectingValue)
            {
                throw new InvalidOperationException("No open container to close here.");
            }

            int count = _counts.Pop();
            if (count > 0)
            {
                NewLine(_counts.Count);
            }

            _text.Append(bracket);
        }

        private void NewLine(int depth)
        {
            _text.Append('\n');
            _text.Append(' ', depth * 2);
        }

        private void WriteString(string value)
        {
            _text.Append('"');
            foreach (char c in value)
            {
                switch (c)
                {
                    case '"': _text.Append("\\\""); break;
                    case '\\': _text.Append("\\\\"); break;
                    case '\n': _text.Append("\\n"); break;
                    case '\r': _text.Append("\\r"); break;
                    case '\t': _text.Append("\\t"); break;
                    case '\b': _text.Append("\\b"); break;
                    case '\f': _text.Append("\\f"); break;
                    default:
                        if (c < ' ')
                        {
                            _text.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            _text.Append(c);
                        }

                        break;
                }
            }

            _text.Append('"');
        }
    }
}
