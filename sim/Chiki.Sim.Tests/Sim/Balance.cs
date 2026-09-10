using Chiki.Sim;
using SimBalance = Chiki.Sim.Balance;

namespace Sim;

public class Balance
{
    [Test]
    public void worked_check_360()
    {
        int hp = SimBalance.EnemyHp(
            actionsPerMinuteThousandths: 60 * Fixed.One,
            attackRatioThousandths: 600,
            avgCardDmg: 10,
            judgmentMix: JudgmentMix.AllPerfect,
            intendedSeconds: 60);

        Assert.That(hp, Is.EqualTo(360));
    }
}
