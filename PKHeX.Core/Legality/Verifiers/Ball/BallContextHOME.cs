using System;
using static PKHeX.Core.Ball;
using static PKHeX.Core.BallInheritanceResult;

namespace PKHeX.Core;

/// <summary>
/// Ball Inheritance Permissions for games interconnected to HOME.
/// </summary>
public sealed class BallContextHOME : IBallContext
{
    public static readonly BallContextHOME Instance = new();

    public bool CanBreedWithBall(ushort species, byte form, Ball ball)
    {
        // Eagerly return true for the most common case
        if (ball is Poke)
            return true;

        if (species >= Permit.Length)
            return false;
        var permitBit = GetPermitBit(ball);
        if (permitBit == BallType.None)
            return false;

        var permit = Permit[species];
        if ((permit & (1 << (byte)permitBit)) == 0)
            return false;
        return true;
    }

    /// <summary>
    /// Checks if a past ball inheritance context's restriction for Hidden Ability exclusions can be ignored.
    /// </summary>
    /// <param name="format">Current Entity format</param>
    /// <param name="species">Original encounter species</param>
    /// <returns>True if the restriction can be ignored, false if not.</returns>
    /// <remarks>Ability Patch can be used on any species that has a Hidden Ability distinct from its regular ability.</remarks>
    public static bool IsAbilityPatchPossible(byte format, ushort species)
    {
        if (format <= 7)
            return false;
        return IsPastGenWithUniqueHiddenAbility(species);
    }

    /// <summary>
    /// Species (that can breed, from Gen6/7) that can visit a Gen8+ game with a unique Hidden Ability.
    /// </summary>
    /// <param name="species">Original encounter species</param>
    /// <returns>True if ability patch can bump to Hidden Ability, false if not.</returns>
    /// <remarks>
    /// Gen8+ has encounters with HA, so this is no longer a concern for anything originating from Gen8+.
    /// </remarks>
    private static bool IsPastGenWithUniqueHiddenAbility(ushort species) => species switch
    {
        // Species that have never had a Hidden Ability distinct from their regular ability
        (int)Species.Lunatone => false, // Levitate
        (int)Species.Solrock => false, // Levitate
        (int)Species.Rotom => false, // Levitate
        (int)Species.Archen => false, // Defeatist
        _ => true,
    };

    public BallInheritanceResult CanBreedWithBall(ushort species, byte form, Ball ball, PKM pk) => CanBreedWithBall(species, form, ball) ? Valid : Invalid;

    private static BallType GetPermitBit(Ball ball) => ball switch
    {
        Ultra => BallType.Gen3,
        Great => BallType.Gen3,
        Poke => BallType.Gen3,
        Safari => BallType.Safari,

        Net => BallType.Gen3,
        Dive => BallType.Gen3,
        Nest => BallType.Gen3,
        Repeat => BallType.Gen3,
        Timer => BallType.Gen3,
        Luxury => BallType.Gen3,
        Premier => BallType.Gen3,

        Dusk => BallType.Gen4,
        Heal => BallType.Gen4,
        Quick => BallType.Gen4,

        Fast => BallType.Apricorn,
        Level => BallType.Apricorn,
        Lure => BallType.Apricorn,
        Heavy => BallType.Apricorn,
        Love => BallType.Apricorn,
        Friend => BallType.Apricorn,
        Moon => BallType.Apricorn,

        Sport => BallType.Sport,
        Dream => BallType.Dream,
        Beast => BallType.Beast,
        _ => BallType.None,
    };

    public static ReadOnlySpan<byte> Permit =>
    [
        0x00, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7B, 0x0B, 0x0B, 0x6F, 0x0B, 0x0B, 0x6F, // 000-019
        0x0B, 0x6F, 0x0B, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, // 020-039
        0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x0B, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, // 040-059
        0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, // 060-079
        0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, // 080-099
        0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, // 100-119
        0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x00, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, // 120-139
        0x7F, 0x7F, 0x7F, 0x7F, 0x00, 0x00, 0x00, 0x7F, 0x7F, 0x7F, 0x00, 0x00, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, // 140-159
        0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x6F, 0x0B, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, // 160-179
        0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, // 180-199
        0x7F, 0x00, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, // 200-219
        0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, // 220-239
        0x7F, 0x7F, 0x7F, 0x00, 0x00, 0x00, 0x7F, 0x7F, 0x7F, 0x00, 0x00, 0x00, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, // 240-259
        0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x3B, 0x0B, 0x0B, 0x0B, 0x0B, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x6B, 0x0B, 0x7F, 0x7F, // 260-279
        0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, // 280-299
        0x2B, 0x0B, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, // 300-319
        0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x6F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, // 320-339
        0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x6B, 0x6F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, // 340-359
        0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x6B, 0x0B, 0x0B, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x00, 0x00, 0x00, // 360-379
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x2F, // 380-399
        0x0B, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x2B, 0x0B, 0x0B, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, // 400-419
        0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x2B, 0x0B, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, // 420-439
        0x7F, 0x2B, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x2F, 0x7F, 0x7F, 0x7F, 0x7F, // 440-459
        0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, // 460-479
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, // 480-499
        0x7F, 0x7F, 0x7F, 0x7F, 0x03, 0x03, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x23, 0x03, 0x23, 0x03, 0x23, 0x03, 0x7F, 0x7F, 0x7F, // 500-519
        0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, // 520-539
        0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, // 540-559
        0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, // 560-579
        0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, // 580-599
        0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, // 600-619
        0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x00, 0x00, // 620-639
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, // 640-659
        0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x4B, 0x7F, 0x7F, 0x7F, // 660-679
        0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, // 680-699
        0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x00, 0x00, 0x00, 0x00, // 700-719
        0x00, 0x00, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, // 720-739
        0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, // 740-759
        0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x00, 0x00, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, // 760-779
        0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // 780-799
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, // 800-819
        0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, // 820-839
        0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, // 840-859
        0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, // 860-879
        0x00, 0x00, 0x00, 0x00, 0x7F, 0x7F, 0x7F, 0x7F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x7F, // 880-899
        0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, // 900-919
        0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, // 920-939
        0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, // 940-959
        0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, // 960-979
        0x7F, 0x7F, 0x7F, 0x7F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x7F, 0x7F, 0x7F, 0x00, // 980-999
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x7F, 0x7F, 0x7F, 0x00, 0x00, 0x00, 0x00, 0x7F, 0x7F, // 1000-1019
    ];

    private enum BallType : byte
    {
        Gen3 = 0,
        Gen4 = 1,
        Safari = 2,
        Apricorn = 3,
        Sport = 4,
        Dream = 5,
        Beast = 6,

        None = 9,
    }
}
