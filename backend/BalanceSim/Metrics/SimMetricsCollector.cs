using BalanceSim.Core;
using llmmo.Api.GameRules;

namespace BalanceSim.Metrics;

public record TimeseriesPoint(
    int Tick,
    double Day,
    WealthSnapshot Wealth,
    SimResources Resources,
    SimResourceCaps ResourceCaps,
    ResourceProduction Production,
    Dictionary<string, bool> AtCap,
    Dictionary<string, int> BuildingLevels,
    Dictionary<string, int> Troops,
    bool UpgradeBusy,
    bool TrainBusy,
    List<SimEvent> Events);

public record RunSummary(
    int? TicksToWealth100Pct,
    int StarvationEvents,
    int TroopsLostToStarvation,
    double UpgradeSlotUtilizationPct,
    double TrainSlotUtilizationPct,
    int TicksWaitingForResources,
    int AttacksLaunched,
    int AttacksWon,
    SimResources TotalLoot,
    string DominantBottleneck);

public sealed class SimMetricsCollector
{
    private readonly BalanceProfile _profile;
    private readonly CompositeWealthCalculator _wealth = new();
    private readonly List<TimeseriesPoint> _points = [];
    private readonly ProgressionConfig _progression;

    private int _upgradeBusyTicks;
    private int _trainBusyTicks;
    private int _waitingTicks;
    private int _starvationEvents;
    private int _attacksLaunched;
    private int _attacksWon;
    private int _totalLootGold;
    private int _totalLootStone;
    private int _totalLootWood;
    private int _totalLootFood;
    private int _foodBottleneckTicks;
    private int _goldBottleneckTicks;
    private int _woodBottleneckTicks;
    private int _stoneBottleneckTicks;
    private int _totalTicks;
    private int? _ticksTo100;
    private string _agentName = "";

    public SimMetricsCollector(BalanceProfile profile, ProgressionConfig? progression = null)
    {
        _profile = profile;
        _progression = progression ?? ProgressionConfig.LoadDefault();
    }

    public IReadOnlyList<TimeseriesPoint> Points => _points;
    public string AgentName => _agentName;

    public void Reset(SimCityState state, string agentName)
    {
        _points.Clear();
        _upgradeBusyTicks = 0;
        _trainBusyTicks = 0;
        _waitingTicks = 0;
        _starvationEvents = 0;
        _attacksLaunched = 0;
        _attacksWon = 0;
        _totalLootGold = 0;
        _totalLootStone = 0;
        _totalLootWood = 0;
        _totalLootFood = 0;
        _foodBottleneckTicks = 0;
        _goldBottleneckTicks = 0;
        _woodBottleneckTicks = 0;
        _stoneBottleneckTicks = 0;
        _totalTicks = 0;
        _ticksTo100 = null;
        _agentName = agentName;
    }

    public void RecordTick(SimCityState state, int sampleEvery = 36)
    {
        _totalTicks++;

        if (state.PendingUpgrade is not null) _upgradeBusyTicks++;
        if (state.PendingTrain is not null) _trainBusyTicks++;

        foreach (var evt in state.TickEvents)
        {
            if (evt.Type == "starvation") _starvationEvents++;
        }

        UpdateBottlenecks(state);

        var wealth = _wealth.Compute(state);
        if (_ticksTo100 is null && wealth.TotalPct >= 100)
        {
            _ticksTo100 = state.Tick;
        }

        if (state.Tick % sampleEvery != 0 && state.Tick != 0)
        {
            return;
        }

        var production = state.CalculateProduction();
        var atCap = new Dictionary<string, bool>
        {
            ["gold"] = state.Gold >= state.MaxGold,
            ["stone"] = state.Stone >= state.MaxStone,
            ["wood"] = state.Wood >= state.MaxWood,
            ["food"] = state.Food >= state.MaxFood,
        };

        _points.Add(new TimeseriesPoint(
            state.Tick,
            TicksToDay(state.Tick),
            wealth,
            new SimResources(state.Gold, state.Stone, state.Wood, state.Food),
            new SimResourceCaps(state.MaxGold, state.MaxStone, state.MaxWood, state.MaxFood),
            production,
            atCap,
            new Dictionary<string, int>(state.BuildingLevels),
            new Dictionary<string, int>(state.TroopCounts),
            state.PendingUpgrade is not null,
            state.PendingTrain is not null,
            state.TickEvents.ToList()));
    }

    public void RecordWaitingTick() => _waitingTicks++;

    public void RecordAttackLaunch() => _attacksLaunched++;

    public void RecordAttack(bool won, DefenderResources loot)
    {
        if (won) _attacksWon++;
        _totalLootGold += loot.Gold;
        _totalLootStone += loot.Stone;
        _totalLootWood += loot.Wood;
        _totalLootFood += loot.Food;
    }

    public void RecordScout(bool survived) { }

    public RunSummary BuildSummary()
    {
        var bottleneck = GetDominantBottleneck();
        return new RunSummary(
            _ticksTo100,
            _starvationEvents,
            _starvationEvents * 2,
            _totalTicks > 0 ? 100.0 * _upgradeBusyTicks / _totalTicks : 0,
            _totalTicks > 0 ? 100.0 * _trainBusyTicks / _totalTicks : 0,
            _waitingTicks,
            _attacksLaunched,
            _attacksWon,
            new SimResources(_totalLootGold, _totalLootStone, _totalLootWood, _totalLootFood),
            bottleneck);
    }

    public double TicksToDay(int tick) =>
        tick * _progression.SecondsPerTick / (3600.0 * _progression.ActiveHoursPerDay);

    private void UpdateBottlenecks(SimCityState state)
    {
        var production = state.CalculateProduction();
        if (production.Food > 0 && state.Food >= state.MaxFood) _foodBottleneckTicks++;
        if (production.Gold > 0 && state.Gold >= state.MaxGold) _goldBottleneckTicks++;
        if (production.Wood > 0 && state.Wood >= state.MaxWood) _woodBottleneckTicks++;
        if (production.Stone > 0 && state.Stone >= state.MaxStone) _stoneBottleneckTicks++;
    }

    private string GetDominantBottleneck()
    {
        var max = Math.Max(Math.Max(_foodBottleneckTicks, _goldBottleneckTicks),
            Math.Max(_woodBottleneckTicks, _stoneBottleneckTicks));
        if (max == _foodBottleneckTicks) return "food";
        if (max == _goldBottleneckTicks) return "gold";
        if (max == _woodBottleneckTicks) return "wood";
        return "stone";
    }

    public Dictionary<string, double> BottleneckPct()
    {
        if (_totalTicks == 0) return new Dictionary<string, double>();
        return new Dictionary<string, double>
        {
            ["food"] = 100.0 * _foodBottleneckTicks / _totalTicks,
            ["gold"] = 100.0 * _goldBottleneckTicks / _totalTicks,
            ["wood"] = 100.0 * _woodBottleneckTicks / _totalTicks,
            ["stone"] = 100.0 * _stoneBottleneckTicks / _totalTicks,
        };
    }
}
