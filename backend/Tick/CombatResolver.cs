using System.Text.Json;
using llmmo.Api.Troops;
using llmmo.Data;
using llmmo.Entities;
using Microsoft.EntityFrameworkCore;

namespace llmmo.Tick;

public class CombatResolver
{
    private const double WinnerLossFactor = 0.5;
    private const double LoserWipeFactor = 0.9;

    public CombatResult Resolve(
        IReadOnlyList<TroopStackEntry> attackers,
        City defenderCity)
    {
        var attackerPower = attackers.Sum(entry => TroopCatalog.CombatPower(entry.Type, entry.Count));
        var defenderPowerRaw = defenderCity.Troops.Sum(entry =>
            TroopCatalog.CombatPower(entry.Type, entry.Quantity));
        var defenderPower = (int)Math.Floor(defenderPowerRaw * defenderCity.DefenceFactor);

        var attackerWins = attackerPower > defenderPower;

        var attackerCasualties = attackerWins
            ? ApplyLosses(attackers, ComputeWinnerLoss(attackers, defenderPower, attackerPower))
            : ApplyLosses(attackers, ComputeLoserLoss(attackers));

        var defenderStacks = defenderCity.Troops
            .Where(t => t.Quantity > 0)
            .Select(t => new TroopStackEntry(t.Type, t.Quantity))
            .ToList();

        var defenderCasualties = attackerWins
            ? ApplyDefenderLosses(defenderCity.Troops, ComputeLoserLoss(defenderStacks))
            : ApplyDefenderLosses(defenderCity.Troops, ComputeWinnerLoss(
                defenderStacks,
                attackerPower,
                defenderPower));

        var survivors = SubtractStacks(attackers, attackerCasualties);

        return new CombatResult(
            attackerWins,
            attackerPower,
            defenderPower,
            survivors,
            attackerCasualties,
            defenderCasualties);
    }

    public static LootResult ComputeLoot(City defenderCity, IReadOnlyList<TroopStackEntry> survivors)
    {
        var capacity = survivors.Sum(entry => TroopCatalog.CarryCapacity(entry.Type, entry.Count));
        if (capacity <= 0)
        {
            return new LootResult(0, 0, 0, 0);
        }

        var available = new[] { defenderCity.Wood, defenderCity.Stone, defenderCity.Gold, defenderCity.Food };
        var totalAvailable = available.Sum();
        if (totalAvailable <= 0)
        {
            return new LootResult(0, 0, 0, 0);
        }

        var perResourceCap = capacity / 4;
        var loot = new int[4];

        for (var i = 0; i < 4; i++)
        {
            loot[i] = Math.Min(available[i], perResourceCap);
        }

        var used = loot.Sum();
        var remaining = capacity - used;
        if (remaining <= 0)
        {
            return new LootResult(loot[0], loot[1], loot[2], loot[3]);
        }

        var indices = Enumerable.Range(0, 4).Where(i => available[i] > loot[i]).ToList();
        while (remaining > 0 && indices.Count > 0)
        {
            var progressed = false;
            foreach (var index in indices.ToList())
            {
                if (remaining <= 0)
                {
                    break;
                }

                if (loot[index] < available[index])
                {
                    loot[index]++;
                    remaining--;
                    progressed = true;
                }

                if (loot[index] >= available[index])
                {
                    indices.Remove(index);
                }
            }

            if (!progressed)
            {
                break;
            }
        }

        return new LootResult(loot[0], loot[1], loot[2], loot[3]);
    }

    private static int ComputeWinnerLoss(
        IReadOnlyList<TroopStackEntry> troops,
        int loserPower,
        int winnerPower)
    {
        if (winnerPower <= 0)
        {
            return 0;
        }

        var total = TroopStackHelper.TotalCount(troops);
        return (int)Math.Floor(loserPower / (double)winnerPower * total * WinnerLossFactor);
    }

    private static int ComputeLoserLoss(IReadOnlyList<TroopStackEntry> troops)
    {
        var total = TroopStackHelper.TotalCount(troops);
        return (int)Math.Floor(total * LoserWipeFactor);
    }

    private static List<TroopStackEntry> ApplyLosses(
        IReadOnlyList<TroopStackEntry> troops,
        int totalLoss)
    {
        return DistributeLosses(troops, totalLoss);
    }

    private static Dictionary<string, int> ApplyDefenderLosses(
        IEnumerable<CityTroop> troops,
        int totalLoss)
    {
        var stacks = troops
            .Where(t => t.Quantity > 0)
            .Select(t => new TroopStackEntry(t.Type, t.Quantity))
            .ToList();

        var losses = DistributeLosses(stacks, totalLoss);
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var loss in losses)
        {
            result[loss.Type] = loss.Count;
        }

        return result;
    }

    private static List<TroopStackEntry> DistributeLosses(
        IReadOnlyList<TroopStackEntry> troops,
        int totalLoss)
    {
        var total = TroopStackHelper.TotalCount(troops);
        if (totalLoss <= 0 || total <= 0)
        {
            return [];
        }

        totalLoss = Math.Min(totalLoss, total);
        var losses = new List<TroopStackEntry>();
        var remaining = totalLoss;

        foreach (var entry in troops)
        {
            if (entry.Count <= 0)
            {
                continue;
            }

            var share = (int)Math.Floor(totalLoss * (entry.Count / (double)total));
            share = Math.Min(share, entry.Count);
            share = Math.Min(share, remaining);

            if (share > 0)
            {
                losses.Add(new TroopStackEntry(entry.Type, share));
                remaining -= share;
            }
        }

        foreach (var entry in troops)
        {
            if (remaining <= 0)
            {
                break;
            }

            var already = losses.FirstOrDefault(l => l.Type.Equals(entry.Type, StringComparison.OrdinalIgnoreCase))?.Count ?? 0;
            var canLose = entry.Count - already;
            if (canLose <= 0)
            {
                continue;
            }

            var take = Math.Min(canLose, remaining);
            if (already > 0)
            {
                var index = losses.FindIndex(l => l.Type.Equals(entry.Type, StringComparison.OrdinalIgnoreCase));
                losses[index] = new TroopStackEntry(entry.Type, already + take);
            }
            else
            {
                losses.Add(new TroopStackEntry(entry.Type, take));
            }

            remaining -= take;
        }

        return losses;
    }

    private static List<TroopStackEntry> SubtractStacks(
        IReadOnlyList<TroopStackEntry> troops,
        IReadOnlyList<TroopStackEntry> losses)
    {
        var survivors = new List<TroopStackEntry>();

        foreach (var entry in troops)
        {
            var lost = losses
                .FirstOrDefault(l => l.Type.Equals(entry.Type, StringComparison.OrdinalIgnoreCase))
                ?.Count ?? 0;
            var remaining = entry.Count - lost;

            if (remaining > 0)
            {
                survivors.Add(new TroopStackEntry(entry.Type, remaining));
            }
        }

        return survivors;
    }
}

public record CombatResult(
    bool AttackerWins,
    int AttackerPower,
    int DefenderPower,
    IReadOnlyList<TroopStackEntry> Survivors,
    IReadOnlyList<TroopStackEntry> AttackerCasualties,
    IReadOnlyDictionary<string, int> DefenderCasualties);

public record LootResult(int Wood, int Stone, int Gold, int Food);
