using System;

namespace Chiki.Sim.Data
{
    /// <summary>
    /// Writes a <see cref="RunLog"/> as the run log file's JSON (PRD 3.15.1, 3.15.2): seed,
    /// difficulty modifiers, Assist flag, the ordered route, the outcome and one record per
    /// battle. Nothing about the profile or the machine goes in; the client only chooses the
    /// file's name and folder.
    /// </summary>
    public static class RunLogSerializer
    {
        /// <summary>The log file layout's version, independent of the run save's.</summary>
        public const int SchemaVersion = 1;

        public static string ToJson(RunLog log)
        {
            if (log is null)
            {
                throw new ArgumentNullException(nameof(log));
            }

            var w = new JsonWriter();
            w.BeginObject();
            w.Member("schemaVersion", SchemaVersion);
            w.Member("seed", log.Seed);
            w.Member("difficultyModifiers", log.DifficultyModifiers);
            w.Member("assist", log.Assist);
            w.Member("outcome", RunSerializer.StatusToId(log.Outcome));
            w.Member("route", log.Route);
            w.Name("battles").BeginArray();
            foreach (var battle in log.Battles)
            {
                RunSerializer.WriteBattle(w, battle);
            }

            w.EndArray();
            w.EndObject();
            return w.ToString();
        }
    }
}
