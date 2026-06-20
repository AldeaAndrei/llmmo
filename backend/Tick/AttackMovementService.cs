using llmmo.Api;
using llmmo.Api.Buildings;
using llmmo.Api.Troops;
using llmmo.Data;
using llmmo.Entities;
using Microsoft.EntityFrameworkCore;

namespace llmmo.Tick;

public class AttackMovementService
{
    private readonly AppDbContext _db;
    private readonly CombatResolver _combatResolver;
    private readonly Random _random;

    public AttackMovementService(AppDbContext db, CombatResolver combatResolver)
    {
        _db = db;
        _combatResolver = combatResolver;
        _random = Random.Shared;
    }

    public async Task ProcessAsync(int currentTick, CancellationToken cancellationToken = default)
    {
        var attacks = await _db.MilitaryAttacks
            .Include(attack => attack.SourceCity)
            .ThenInclude(city => city.Troops)
            .Where(attack => attack.Status == "outbound" || attack.Status == "returning")
            .ToListAsync(cancellationToken);

        foreach (var attack in attacks)
        {
            if (attack.Status == "outbound" && currentTick >= attack.ArrivesAtTick)
            {
                if (attack.Type.Equals("scout", StringComparison.OrdinalIgnoreCase))
                {
                    await ResolveScoutAsync(attack, currentTick, cancellationToken);
                }
                else
                {
                    await ResolveAttackArrivalAsync(attack, currentTick, cancellationToken);
                }
            }
            else if (attack.Status == "returning"
                     && attack.ReturnsAtTick is not null
                     && currentTick >= attack.ReturnsAtTick.Value)
            {
                await CompleteReturnAsync(attack, cancellationToken);
            }
        }
    }

    private async Task ResolveScoutAsync(
        MilitaryAttack attack,
        int currentTick,
        CancellationToken cancellationToken)
    {
        var targetCity = attack.TargetCityId is null
            ? null
            : await _db.Cities
                .Include(c => c.Troops)
                .FirstOrDefaultAsync(c => c.Id == attack.TargetCityId.Value, cancellationToken);

        var spySurvives = _random.NextDouble() >= attack.SourceCity.SpyDieChance;
        object payload;

        if (targetCity is null)
        {
            payload = new { result = "nothing_found", spySurvived = spySurvives };
        }
        else
        {
            payload = new
            {
                result = "intel",
                spySurvived = spySurvives,
                resources = ReportPayloadHelper.ToResources(
                    targetCity.Wood,
                    targetCity.Stone,
                    targetCity.Gold,
                    targetCity.Food),
                troops = targetCity.Troops
                    .Where(t => t.Quantity > 0)
                    .Select(t => new { type = t.Type, quantity = t.Quantity })
                    .ToList(),
            };
        }

        if (spySurvives)
        {
            RestoreTroop(attack.SourceCity, "spy", 1);
        }

        attack.Status = "completed";
        attack.ReturnsAtTick = currentTick;

        _db.Reports.Add(new Report
        {
            Id = Guid.NewGuid(),
            PlayerId = attack.PlayerId,
            Type = "scout",
            AttackId = attack.Id,
            SourceCityId = attack.SourceCityId,
            TargetCityId = attack.TargetCityId,
            TargetX = attack.TargetX,
            TargetY = attack.TargetY,
            Payload = ReportPayloadHelper.Serialize(payload),
        });
    }

    private async Task ResolveAttackArrivalAsync(
        MilitaryAttack attack,
        int currentTick,
        CancellationToken cancellationToken)
    {
        var attackers = TroopStackHelper.Parse(attack.Troops);

        if (attack.TargetCityId is null)
        {
            attack.Status = "failed";
            WriteAttackReports(attack, attackers, null, null, false, new LootResult(0, 0, 0, 0));
            return;
        }

        var defenderCity = await _db.Cities
            .Include(c => c.Troops)
            .Include(c => c.Buildings)
            .FirstOrDefaultAsync(c => c.Id == attack.TargetCityId.Value, cancellationToken);

        if (defenderCity is null)
        {
            attack.Status = "failed";
            WriteAttackReports(attack, attackers, null, null, false, new LootResult(0, 0, 0, 0));
            return;
        }

        var combat = _combatResolver.Resolve(attackers, defenderCity);

        foreach (var (type, loss) in combat.DefenderCasualties)
        {
            var troop = defenderCity.Troops.First(t =>
                t.Type.Equals(type, StringComparison.OrdinalIgnoreCase));
            troop.Quantity = Math.Max(0, troop.Quantity - loss);
        }

        if (!combat.AttackerWins || combat.Survivors.Count == 0)
        {
            attack.Status = "failed";
            WriteAttackReports(attack, attackers, combat, defenderCity, false, new LootResult(0, 0, 0, 0));
            return;
        }

        var loot = CombatResolver.ComputeLoot(defenderCity, combat.Survivors);
        defenderCity.Wood = Math.Max(0, defenderCity.Wood - loot.Wood);
        defenderCity.Stone = Math.Max(0, defenderCity.Stone - loot.Stone);
        defenderCity.Gold = Math.Max(0, defenderCity.Gold - loot.Gold);
        defenderCity.Food = Math.Max(0, defenderCity.Food - loot.Food);

        attack.Survivors = TroopStackHelper.Serialize(combat.Survivors);
        attack.LootWood = loot.Wood;
        attack.LootStone = loot.Stone;
        attack.LootGold = loot.Gold;
        attack.LootFood = loot.Food;
        attack.Status = "returning";
        attack.ReturnsAtTick = currentTick + attack.ReturnDurationTicks;

        WriteAttackReports(attack, attackers, combat, defenderCity, true, loot);
    }

    private async Task CompleteReturnAsync(
        MilitaryAttack attack,
        CancellationToken cancellationToken)
    {
        var sourceCity = await _db.Cities
            .Include(c => c.Troops)
            .FirstOrDefaultAsync(c => c.Id == attack.SourceCityId, cancellationToken);

        if (sourceCity is null)
        {
            attack.Status = "completed";
            return;
        }

        sourceCity.Wood += attack.LootWood;
        sourceCity.Stone += attack.LootStone;
        sourceCity.Gold += attack.LootGold;
        sourceCity.Food += attack.LootFood;
        CityResources.ClampToMax(sourceCity);

        var survivors = TroopStackHelper.Parse(attack.Survivors ?? "[]");
        foreach (var entry in survivors)
        {
            RestoreTroop(sourceCity, entry.Type, entry.Count);
        }

        attack.Status = "completed";
    }

    private static void RestoreTroop(City city, string type, int count)
    {
        var troop = city.Troops.FirstOrDefault(t =>
            t.Type.Equals(type, StringComparison.OrdinalIgnoreCase));

        if (troop is null)
        {
            city.Troops.Add(new CityTroop
            {
                Id = Guid.NewGuid(),
                CityId = city.Id,
                Type = type,
                Quantity = count,
            });
            return;
        }

        troop.Quantity += count;
    }

    private void WriteAttackReports(
        MilitaryAttack attack,
        IReadOnlyList<TroopStackEntry> committed,
        CombatResult? combat,
        City? defenderCity,
        bool attackerWins,
        LootResult loot)
    {
        var attackerPayload = BuildAttackPayload(
            perspective: "attacker",
            viewerWins: attackerWins,
            combat,
            committed,
            loot);

        _db.Reports.Add(new Report
        {
            Id = Guid.NewGuid(),
            PlayerId = attack.PlayerId,
            Type = "attack",
            AttackId = attack.Id,
            SourceCityId = attack.SourceCityId,
            TargetCityId = attack.TargetCityId,
            TargetX = attack.TargetX,
            TargetY = attack.TargetY,
            Payload = ReportPayloadHelper.Serialize(attackerPayload),
        });

        if (defenderCity is null || defenderCity.PlayerId == attack.PlayerId)
        {
            return;
        }

        var defenderPayload = BuildAttackPayload(
            perspective: "defender",
            viewerWins: !attackerWins,
            combat,
            committed,
            loot);

        // Point the defender's list entry at the attacker's origin so it reads
        // "Defense ← (sourceX, sourceY)" rather than their own city coords.
        var sourceCity = attack.SourceCity;
        _db.Reports.Add(new Report
        {
            Id = Guid.NewGuid(),
            PlayerId = defenderCity.PlayerId,
            Type = "attack",
            AttackId = attack.Id,
            SourceCityId = attack.SourceCityId,
            TargetCityId = defenderCity.Id,
            TargetX = sourceCity.X,
            TargetY = sourceCity.Y,
            Payload = ReportPayloadHelper.Serialize(defenderPayload),
        });
    }

    private static object BuildAttackPayload(
        string perspective,
        bool viewerWins,
        CombatResult? combat,
        IReadOnlyList<TroopStackEntry> committed,
        LootResult loot)
    {
        object? attacker = null;
        object? defender = null;

        if (combat is not null)
        {
            attacker = ReportPayloadHelper.ToAttackerSide(
                combat.AttackerPower,
                committed,
                combat.AttackerCasualties);

            defender = ReportPayloadHelper.ToDefenderSide(
                combat.DefenderPower,
                combat.DefenderTroopPower,
                combat.WallDefenceBonus,
                combat.DefenderTroopsBefore,
                combat.DefenderCasualties);
        }

        return new
        {
            perspective,
            outcome = viewerWins ? "victory" : "defeat",
            attacker,
            defender,
            loot = ReportPayloadHelper.ToLoot(loot.Wood, loot.Stone, loot.Gold, loot.Food),
        };
    }
}
