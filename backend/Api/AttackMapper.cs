using llmmo.Api.Dtos;
using llmmo.Api.Troops;
using llmmo.Entities;

namespace llmmo.Api;

public static class AttackMapper
{
    public static AttackCreatedDto ToCreatedDto(MilitaryAttack attack)
    {
        var troops = TroopStackHelper.Parse(attack.Troops)
            .Select(TroopMapper.ToStackDto)
            .ToList();

        return new AttackCreatedDto(
            attack.Id,
            attack.Type,
            attack.Status,
            attack.OutboundDurationTicks,
            attack.ReturnDurationTicks,
            attack.DepartedAtTick,
            attack.ArrivesAtTick,
            troops);
    }

    public static AttackMapDto ToMapDto(MilitaryAttack attack, City sourceCity, int currentTick)
    {
        var (phase, progress, currentX, currentY) = ComputePosition(attack, sourceCity, currentTick);
        var troops = TroopStackHelper.Parse(
            attack.Status == "returning" && !string.IsNullOrEmpty(attack.Survivors)
                ? attack.Survivors
                : attack.Troops)
            .Select(TroopMapper.ToStackDto)
            .ToList();

        return new AttackMapDto(
            attack.Id,
            attack.Type,
            attack.Status,
            phase,
            progress,
            currentX,
            currentY,
            new AttackLocationDto(sourceCity.X, sourceCity.Y, sourceCity.Id),
            new AttackLocationDto(attack.TargetX, attack.TargetY, attack.TargetCityId),
            troops);
    }

    public static (string Phase, double Progress, int CurrentX, int CurrentY) ComputePosition(
        MilitaryAttack attack,
        City sourceCity,
        int currentTick)
    {
        var fromX = sourceCity.X;
        var fromY = sourceCity.Y;
        var toX = attack.TargetX;
        var toY = attack.TargetY;

        if (attack.Status == "returning")
        {
            (fromX, fromY, toX, toY) = (toX, toY, fromX, fromY);
            var legStart = attack.ArrivesAtTick;
            var legDuration = attack.ReturnDurationTicks;
            var progress = legDuration <= 0
                ? 1.0
                : Math.Clamp((currentTick - legStart) / (double)legDuration, 0, 1);

            return ("returning", progress, LerpTile(fromX, toX, progress), LerpTile(fromY, toY, progress));
        }

        if (attack.Status is "completed" or "failed")
        {
            return ("completed", 1.0, toX, toY);
        }

        var outboundStart = attack.DepartedAtTick;
        var outboundDuration = attack.OutboundDurationTicks;
        var outboundProgress = outboundDuration <= 0
            ? 1.0
            : Math.Clamp((currentTick - outboundStart) / (double)outboundDuration, 0, 1);

        return (
            "outbound",
            outboundProgress,
            LerpTile(fromX, toX, outboundProgress),
            LerpTile(fromY, toY, outboundProgress));
    }

    private static int LerpTile(int from, int to, double progress) =>
        (int)Math.Round(from + (to - from) * progress);
}
