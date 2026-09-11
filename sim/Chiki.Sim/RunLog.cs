using System;
using System.Collections.Generic;

namespace Chiki.Sim
{
    /// <summary>
    /// One battle of a run as the run log records it (PRD 3.15.1, 3.15.2): the enemy, the
    /// duration in beats and milliseconds, the judgment counts per grade with the actions that
    /// got no input, the damage taken, the Signatures fired, the cards played per slot, the
    /// outcome, and whether the enemy is the same as the previous battle's. Built from the
    /// battle's event stream when the run settles it, and kept on the run so a resumed run
    /// still logs every battle.
    /// </summary>
    public sealed record BattleRecord(
        string EnemyId,
        int DurationBeats,
        int DurationMs,
        int Perfects,
        int Goods,
        int Misses,
        int NoInputs,
        int DamageTaken,
        int SignaturesFired,
        IReadOnlyDictionary<string, int> CardsPerSlot,
        BattleOutcome Outcome,
        bool SameEnemyAsPrevious)
    {
        /// <summary>The duration in whole seconds, rounded once (PRD 3.3.4.5).</summary>
        public int DurationSeconds => Fixed.Round(DurationMs, 1000);

        /// <summary>The id a slot is logged under: line and key, as "0-e".</summary>
        public static string SlotId(Slot slot)
        {
            return slot.Line + "-" + slot.Key.ToString().ToLowerInvariant();
        }

        /// <summary>The record of an ended battle, read from its event stream (PRD 4.8).</summary>
        public static BattleRecord From(Battle battle, string? previousEnemyId)
        {
            if (battle is null)
            {
                throw new ArgumentNullException(nameof(battle));
            }

            if (battle.Outcome is null)
            {
                throw new InvalidOperationException("The battle has not ended.");
            }

            int perfects = 0, goods = 0, misses = 0, noInputs = 0, signatures = 0, endQb = 0;
            var perSlot = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var battleEvent in battle.Events)
            {
                switch (battleEvent)
                {
                    case InputJudged judged:
                        switch (judged.Grade)
                        {
                            case Judgment.Perfect: perfects++; break;
                            case Judgment.Good: goods++; break;
                            default: misses++; break;
                        }

                        string slot = SlotId(judged.Slot);
                        perSlot[slot] = perSlot.TryGetValue(slot, out int played) ? played + 1 : 1;
                        break;
                    case ActionResolved resolved when resolved.Grade is null && !resolved.EnemySkipped:
                        noInputs++;
                        break;
                    case SignatureFired _:
                        signatures++;
                        break;
                    case BattleEnded ended:
                        endQb = ended.PositionQb;
                        break;
                }
            }

            return new BattleRecord(
                battle.Enemy.Id,
                Fixed.Round(endQb, 4),
                battle.CurrentTimeMs,
                perfects,
                goods,
                misses,
                noInputs,
                battle.DamageTaken,
                signatures,
                perSlot,
                battle.Outcome.Value,
                previousEnemyId != null && previousEnemyId == battle.Enemy.Id);
        }
    }

    /// <summary>
    /// The run log (PRD 3.15.1): seed, difficulty modifiers and Assist flag, the ordered route
    /// and one record per battle. It carries no profile name and nothing about the machine or
    /// its user; the client writes it under the profile's run-log folder at run end.
    /// </summary>
    public sealed record RunLog(
        string Seed,
        IReadOnlyList<string> DifficultyModifiers,
        bool Assist,
        IReadOnlyList<string> Route,
        IReadOnlyList<BattleRecord> Battles,
        RunStatus Outcome)
    {
        public static RunLog From(Run run)
        {
            if (run is null)
            {
                throw new ArgumentNullException(nameof(run));
            }

            return new RunLog(
                run.Seed,
                new List<string>(run.DifficultyModifiers),
                run.Assist,
                new List<string>(run.Route),
                new List<BattleRecord>(run.Battles),
                run.Status);
        }
    }
}
