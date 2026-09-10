#nullable enable
using Chiki.Sim;

namespace Chiki.Client.Driver
{
    /// <summary>
    /// Something that reads the battle's event stream (PRD 4.8): the Rhythm Line, the slot
    /// rows, feedback, the run log. Presenters read events and battle state; they never change it.
    /// </summary>
    public interface IBattlePresenter
    {
        void OnBattleEvent(Sim.Battle battle, BattleEvent battleEvent);
    }
}
