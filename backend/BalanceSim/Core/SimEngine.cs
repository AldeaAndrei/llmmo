using BalanceSim.Agents;
using BalanceSim.Metrics;
using llmmo.Api.Buildings;
using llmmo.Api.GameRules;

namespace BalanceSim.Core;

public sealed class SimEngine
{
    private readonly BalanceProfile _profile;
    private readonly IActionAgent _agent;
    private readonly IReadOnlyList<DefenderFixture> _fixtures;
    private readonly SimMetricsCollector _metrics;
    private readonly Random _random;

    public SimEngine(
        BalanceProfile profile,
        IActionAgent agent,
        IReadOnlyList<DefenderFixture>? fixtures = null,
        int seed = 42,
        SimMetricsCollector? metrics = null)
    {
        _profile = profile;
        _agent = agent;
        _fixtures = fixtures ?? DefenderFixtureLoader.LoadDefaults();
        _metrics = metrics ?? new SimMetricsCollector(profile);
        _random = new Random(seed);
    }

    public SimCityState State { get; private set; } = null!;
    public SimMetricsCollector Metrics => _metrics;

    public void Run(int ticks)
    {
        State = new SimCityState(_profile);
        _metrics.Reset(State, _agent.Name);

        for (var i = 0; i < ticks; i++)
        {
            State.TickEvents.Clear();
            CompleteReadyActions();
            ProcessMilitary();
            State.ApplyProduction();
            State.ApplyUpkeep();
            AgentDecision();
            _metrics.RecordTick(State);
            State.Tick++;
        }
    }

    private void CompleteReadyActions()
    {
        if (State.PendingUpgrade is { } upgrade && State.Tick >= upgrade.ReadyAtTick)
        {
            if (upgrade.BuildingType is not null)
            {
                State.BuildingLevels[upgrade.BuildingType]++;
                State.ApplyBuildingEffects();
                State.UpgradeHistory.Add(upgrade.BuildingType);
                State.TickEvents.Add(new SimEvent("upgrade_complete", upgrade.BuildingType));
            }

            State.PendingUpgrade = null;
        }

        if (State.PendingTrain is { } train && State.Tick >= train.ReadyAtTick)
        {
            if (train.TroopType is not null)
            {
                State.TroopCounts[train.TroopType] = State.GetTroopCount(train.TroopType) + train.Count;
                State.TickEvents.Add(new SimEvent("train_complete", $"{train.TroopType}×{train.Count}"));
            }

            State.PendingTrain = null;
        }
    }

    private void ProcessMilitary()
    {
        if (State.ActiveMission is not { } mission)
        {
            return;
        }

        mission = mission with { TicksRemaining = mission.TicksRemaining - 1 };

        if (mission.TicksRemaining > 0)
        {
            State.ActiveMission = mission;
            return;
        }

        if (mission.Phase == MilitaryPhase.Outbound)
        {
            var fixture = GetFixture(mission.FixtureName);
            var defenderPower = mission.SoldiersSent > 0
                ? 0
                : 0;

            var defenderSoldierPower = fixture.Soldiers * 3;
            var wallBonus = _profile.WallDefencePerLevel * fixture.WallLevel;
            defenderPower = defenderSoldierPower + wallBonus;

            var attackerPower = mission.SoldiersSent * 3;
            var won = attackerPower > defenderPower;

            var loot = new DefenderResources(0, 0, 0, 0);
            if (won)
            {
                var capacity = mission.SoldiersSent * 4;
                var perCap = capacity / 4;
                loot = new DefenderResources(
                    Math.Min(fixture.Resources.Gold, perCap),
                    Math.Min(fixture.Resources.Stone, perCap),
                    Math.Min(fixture.Resources.Wood, perCap),
                    Math.Min(fixture.Resources.Food, perCap));
            }

            var survivors = won
                ? Math.Max(1, mission.SoldiersSent - (int)Math.Floor(mission.SoldiersSent * 0.1))
                : Math.Max(0, mission.SoldiersSent - (int)Math.Floor(mission.SoldiersSent * 0.9));

            var returnTicks = Math.Max(1, (int)Math.Ceiling(fixture.Distance / 1.0));

            State.ActiveMission = new MilitaryMission(
                mission.FixtureName,
                MilitaryPhase.Returning,
                survivors,
                returnTicks,
                loot.Gold,
                loot.Stone,
                loot.Wood,
                loot.Food,
                won);

            State.TickEvents.Add(new SimEvent("attack_result", won ? "won" : "lost"));
            _metrics.RecordAttack(won, loot);
            return;
        }

        if (mission.Won)
        {
            State.AddResources(mission.LootWood, mission.LootStone, mission.LootGold, mission.LootFood);
        }

        State.TroopCounts["soldier"] = State.GetTroopCount("soldier") + mission.SoldiersSent;
        State.ActiveMission = null;
        State.TickEvents.Add(new SimEvent("attack_return", mission.Won ? "with_loot" : "retreat"));
    }

    private void AgentDecision()
    {
        // A mission only ties up soldiers and the train slot; the build queue is
        // independent, so the agent must still be allowed to keep upgrading while
        // troops are away. (Action generation already excludes train/attack/scout
        // when a mission is active.)
        var action = _agent.ChooseAction(State, _fixtures);
        if (action is null)
        {
            _metrics.RecordWaitingTick();
            return;
        }

        EnqueueAction(action);
    }

    public void EnqueueAction(CandidateAction action)
    {
        switch (action.Kind)
        {
            case SimActionKind.Upgrade:
                EnqueueUpgrade(action);
                break;
            case SimActionKind.Train:
                EnqueueTrain(action);
                break;
            case SimActionKind.Scout:
                ResolveScout(action);
                break;
            case SimActionKind.Attack:
                LaunchAttack(action);
                break;
        }
    }

    private void EnqueueUpgrade(CandidateAction action)
    {
        if (!State.UpgradeSlotFree || action.BuildingType is null)
        {
            return;
        }

        var currentLevel = State.GetBuildingLevel(action.BuildingType);
        if (currentLevel >= _profile.MaxBuildingLevel)
        {
            return;
        }

        var cost = State.BuildingsEngine.UpgradeCostForLevel(action.BuildingType, currentLevel + 1);
        if (!State.CanAfford(cost))
        {
            return;
        }

        State.Deduct(cost);
        State.RecordUpgradeSpend(cost);

        var duration = State.BuildingsEngine.UpgradeDurationTicks(currentLevel);
        State.PendingUpgrade = new PendingAction(
            SimActionKind.Upgrade,
            State.Tick + duration,
            action.BuildingType);
    }

    private void EnqueueTrain(CandidateAction action)
    {
        if (!State.TrainSlotFree || action.TroopType is null || action.Count <= 0)
        {
            return;
        }

        var capacity = State.TrainCapacity;
        var count = Math.Min(action.Count, capacity);
        var cost = State.TroopsEngine.TrainCostForCount(action.TroopType, count, capacity);
        if (!State.CanAfford(cost))
        {
            return;
        }

        State.Deduct(cost);
        State.RecordTrainSpend(cost);

        var duration = State.BuildingsEngine.TrainDurationTicks(count, capacity);
        State.PendingTrain = new PendingAction(
            SimActionKind.Train,
            State.Tick + duration,
            TroopType: action.TroopType,
            Count: count);
    }

    private void ResolveScout(CandidateAction action)
    {
        if (action.FixtureName is null || State.GetTroopCount("spy") <= 0)
        {
            return;
        }

        var survived = _random.NextDouble() >= State.SpyDieChance;
        if (!survived)
        {
            State.TroopCounts["spy"] = Math.Max(0, State.GetTroopCount("spy") - 1);
        }

        State.TickEvents.Add(new SimEvent("scout_result", survived ? "survived" : "died"));
        _metrics.RecordScout(survived);
    }

    private void LaunchAttack(CandidateAction action)
    {
        if (action.FixtureName is null || State.GetTroopCount("soldier") <= 0 || State.ActiveMission is not null)
        {
            return;
        }

        var fixture = GetFixture(action.FixtureName);
        var soldiers = action.Count > 0 ? Math.Min(action.Count, State.GetTroopCount("soldier")) : State.GetTroopCount("soldier");
        if (soldiers <= 0)
        {
            return;
        }

        State.TroopCounts["soldier"] = State.GetTroopCount("soldier") - soldiers;
        var outboundTicks = Math.Max(1, (int)Math.Ceiling(fixture.Distance / 1.0));

        State.ActiveMission = new MilitaryMission(
            action.FixtureName,
            MilitaryPhase.Outbound,
            soldiers,
            outboundTicks,
            0, 0, 0, 0,
            false);

        _metrics.RecordAttackLaunch();
    }

    private DefenderFixture GetFixture(string name) =>
        _fixtures.First(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
}
