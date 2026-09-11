using Chiki.Sim;

namespace Sim;

public class Nodes
{
    private const string NormalTank = "enemy-ren";

    [Test]
    public void normal_node_fights_pool_enemy_then_rewards()
    {
        var content = TestContent.LoadRunContent();
        var run = TestContent.RunAtNormalNode(content, NormalTank);
        var node = run.CurrentNode;
        Assume.That(node.Type, Is.EqualTo(NodeType.NormalBattle));
        Assume.That(content.FindEnemy(NormalTank)!.Role, Is.EqualTo(EnemyRole.Tank));

        var battle = TestContent.WinNodeBattle(run);

        Assert.Multiple(() =>
        {
            Assert.That(battle.Tier, Is.EqualTo(EncounterTier.Normal));
            Assert.That(battle.Enemy.Id, Is.EqualTo(NormalTank));
            Assert.That(battle.Outcome, Is.EqualTo(BattleOutcome.Won));
            Assert.That(run.PendingReward, Is.Not.Null);
            Assert.That(run.PendingReward!.NodeId, Is.EqualTo(node.Id));
            Assert.That(run.PendingReward.Tier, Is.EqualTo(EncounterTier.Normal));
        });
    }

    [Test]
    public void move_blocked_until_reward_resolved()
    {
        var content = TestContent.LoadRunContent();
        var picking = TestContent.RunAtNormalNode(content);
        var skipping = TestContent.RunAtNormalNode(content);
        TestContent.WinNodeBattle(picking);
        TestContent.WinNodeBattle(skipping);
        Assume.That(picking.PendingReward, Is.Not.Null);
        Assume.That(skipping.PendingReward, Is.Not.Null);
        var next = picking.ForwardNodes[0].Id;

        var blockedBeforePick = picking.MoveTo(next);
        var blockedBeforeSkip = skipping.MoveTo(next);
        picking.PickReward(picking.PendingReward!.Cards[0]);
        skipping.SkipReward();
        var afterPick = picking.MoveTo(next);
        var afterSkip = skipping.MoveTo(next);

        Assert.Multiple(() =>
        {
            Assert.That(blockedBeforePick, Is.EqualTo(MoveResult.RewardPending));
            Assert.That(blockedBeforeSkip, Is.EqualTo(MoveResult.RewardPending));
            Assert.That(afterPick, Is.EqualTo(MoveResult.Moved));
            Assert.That(afterSkip, Is.EqualTo(MoveResult.Moved));
            Assert.That(picking.Events.OfType<RewardOffered>().Count(), Is.EqualTo(1), "the reward flow runs once per win");
            Assert.That(skipping.Events.OfType<RewardOffered>().Count(), Is.EqualTo(1));
        });
    }
}
