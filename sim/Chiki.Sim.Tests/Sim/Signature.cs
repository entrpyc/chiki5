using Chiki.Sim;
using SimBattle = Chiki.Sim.Battle;
using Stats = Chiki.Sim.RunStats;

namespace Sim;

public class Signature
{
    // BPM 120: beat n sits at n * 500 ms. Offsets: +0 Perfect, +60 Good, +120 Miss (P2.6).
    private const int Perfect = 0;
    private const int Good = 60;
    private const int Miss = 120;

    [Test]
    public void send_banks_not_plays()
    {
        var stats = new Stats();
        var battle = SendAgainstLeft20(stats, Perfect);

        Assert.Multiple(() =>
        {
            Assert.That(battle.EnemyHp, Is.EqualTo(battle.EnemyMaxHp));
            Assert.That(battle.SignatureChain, Has.Count.EqualTo(1));
            Assert.That(300 - stats.Ard, Is.EqualTo(0));
        });
    }

    [Test]
    public void good_send_takes_half()
    {
        var stats = new Stats();
        SendAgainstLeft20(stats, Good);

        Assert.That(300 - stats.Ard, Is.EqualTo(10));
    }

    [Test]
    public void third_bank_fires_30()
    {
        // Left attacks on beats 1, 3 and 5, each answered by a send from a different slot.
        var battle = TestContent.Battle(new Stats(), TestContent.Chart(TestContent.Track(), 4, 12, 20), enemyHp: 100);
        battle.Send(TestContent.SlotE, TestContent.LeftAttack10, 500 + Perfect);
        battle.Send(TestContent.SlotR, TestContent.LeftAttack10, 1500 + Perfect);
        battle.AdvanceToBeat(4);
        Assume.That(battle.SignatureChain, Has.Count.EqualTo(2));

        battle.Send(TestContent.SlotELine2, TestContent.LeftAttack10, 2500 + Perfect);
        battle.AdvanceToBeat(6);

        Assert.Multiple(() =>
        {
            Assert.That(battle.EnemyHp, Is.EqualTo(70));
            Assert.That(battle.SignatureChain, Is.Empty);
            Assert.That(battle.Events.OfType<SignatureFired>().Single().Damage, Is.EqualTo(30));
        });
    }

    [Test]
    public void missed_send_banks_and_cools()
    {
        var battle = TestContent.Battle(4);

        battle.Send(TestContent.SlotE, TestContent.LeftAttack(10, cooldownBeats: 3), 500 + Miss);
        battle.Advance(700); // the window has closed; beat 2 has not started

        Assert.Multiple(() =>
        {
            Assert.That(battle.SignatureChain, Has.Count.EqualTo(1));
            Assert.That(battle.CooldownOf(TestContent.SlotE), Is.EqualTo(3));
        });
    }

    /// <summary>The enemy attacks Left for 20 on beat 1; a Left Attack is sent with the given offset.</summary>
    private static SimBattle SendAgainstLeft20(Stats stats, int offsetMs)
    {
        var battle = TestContent.Battle(stats, TestContent.Chart(TestContent.Track(), TestContent.Left(4)), enemyDmg: 20);
        battle.Send(TestContent.SlotE, TestContent.LeftAttack10, 500 + offsetMs);
        battle.AdvanceToBeat(2);
        return battle;
    }
}
