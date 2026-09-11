#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using Chiki.Client.Content;
using Chiki.Sim.Data;
using UnityEngine;

namespace Chiki.Client.Text
{
    /// <summary>
    /// The string table (PRD 3.12.7): every player-facing string lives in
    /// <c>data/strings/&lt;language&gt;.json</c>, a flat key-to-text map, and presenters ask for
    /// it by key. English ships; another language is another file, never a code change.
    /// A missing key renders as the key in brackets so it is visible, never silent.
    /// </summary>
    public static class Strings
    {
        public const string DefaultLanguage = "en";

        private static Dictionary<string, string>? _table;
        private static string _language = DefaultLanguage;

        /// <summary>The language file in use; changing it reloads the table on the next lookup.</summary>
        public static string Language
        {
            get => _language;
            set
            {
                if (value != _language)
                {
                    _language = value;
                    _table = null;
                }
            }
        }

        public static string Get(string key)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var table = Table();
            return table.TryGetValue(key, out var text) ? text : "[" + key + "]";
        }

        public static string Format(string key, params object[] args)
        {
            return string.Format(CultureInfo.InvariantCulture, Get(key), args);
        }

        /// <summary>Forgets the loaded table so the next lookup reads the file again.</summary>
        public static void Reset()
        {
            _table = null;
        }

        private static Dictionary<string, string> Table()
        {
            if (_table != null)
            {
                return _table;
            }

            var table = new Dictionary<string, string>();
            try
            {
                var root = JsonValue.Parse(ContentFiles.ReadText("strings/" + _language + ".json"));
                foreach (var name in root.MemberNames)
                {
                    table[name] = root[name].AsString();
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("String table '" + _language + "' could not be read: " + exception.Message);
            }

            _table = table;
            return table;
        }
    }
}
