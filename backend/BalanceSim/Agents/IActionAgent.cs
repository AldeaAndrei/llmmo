using BalanceSim.Core;

namespace BalanceSim.Agents;

public record ActionScore(
    double PaybackTicks,
    double WealthDelta,
    double MilitaryDelta,
    double BottleneckRelief,
    double FutureProductionGain)
{
    public double Weighted(double paybackW, double wealthW, double militaryW, double bottleneckW, double futureW) =>
        paybackW * (PaybackTicks > 0 ? 1.0 / PaybackTicks : 0)
        + wealthW * WealthDelta
        + militaryW * MilitaryDelta
        + bottleneckW * BottleneckRelief
        + futureW * FutureProductionGain;
}

public interface IActionScorer
{
    ActionScore Score(SimCityState state, CandidateAction action, IReadOnlyList<DefenderFixture> fixtures);
}

public interface IActionAgent
{
    string Name { get; }
    CandidateAction? ChooseAction(SimCityState state, IReadOnlyList<DefenderFixture> fixtures);
}
