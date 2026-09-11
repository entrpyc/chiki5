using Chiki.Sim;
using SimRunLog = Chiki.Sim.RunLog;

namespace Sim;

public class RunLog
{
    private const string Tank = "enemy-ren";
    private const string Aggressor = "enemy-kess";

    [Test]
    public void same_enemy_flagged()
    {
        var content = TestContent.LoadRunContent();
        var run = new RunSetup(Array.Empty<string>()).Start(content, "chiki-1");
        Assume.That(content.FindEnemy(Tank)!.Role, Is.EqualTo(EnemyRole.Tank));
        Assume.That(content.FindEnemy(Aggressor)!.Role, Is.EqualTo(EnemyRole.Aggressor));

        foreach (var enemyId in new[] { Tank, Tank, Aggressor })
        {
            var battle = run.StartBattle(content.FindEnemy(enemyId)!, enemyHp: 1);
            TestContent.FightToWin(battle);
            Assume.That(battle.Outcome, Is.EqualTo(BattleOutcome.Won));
            run.SettleBattle(battle);
        }

        var log = SimRunLog.From(run);

        Assert.Multiple(() =>
        {
            Assert.That(log.Battles.Select(b => b.EnemyId), Is.EqualTo(new[] { Tank, Tank, Aggressor }));
            Assert.That(log.Battles.Select(b => b.SameEnemyAsPrevious), Is.EqualTo(new[] { false, true, false }));
        });
    }
}
