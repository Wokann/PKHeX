using System;
using System.Diagnostics.CodeAnalysis;

namespace PKHeX.Core;

/// <summary>
/// Mainline format for Generation 1 &amp; 2 <see cref="PKM"/> objects.
/// </summary>
/// <remarks>This format stores <see cref="PKM.Nickname"/> and <see cref="PKM.OriginalTrainerName"/> in buffers separate from the rest of the details.</remarks>
public abstract class GBPKML : GBPKM
{
    internal const int StringLengthJapanese = 6;
    internal const int StringLengthNotJapan = 11;
    public sealed override int MaxStringLengthTrainer => Japanese ? 5 : 7;
    public sealed override int MaxStringLengthNickname => Japanese ? 5 : 10;
    public sealed override bool Japanese => RawOT.Length == StringLengthJapanese;

    private readonly byte[] RawOT;
    private readonly byte[] RawNickname;

    // Trash Bytes
    public sealed override Span<byte> NicknameTrash => RawNickname;
    public sealed override Span<byte> OriginalTrainerTrash => RawOT;
    public override int TrashCharCountTrainer => RawOT.Length;
    public override int TrashCharCountNickname => RawNickname.Length;

    protected GBPKML([ConstantExpected] int size, bool jp = false) : base(size)
    {
        int strLen = jp ? StringLengthJapanese : StringLengthNotJapan;

        // initialize string buffers
        RawOT = new byte[strLen];
        RawNickname = new byte[strLen];
        OriginalTrainerTrash.Fill(StringConverter1.TerminatorCode);
        NicknameTrash.Fill(StringConverter1.TerminatorCode);
    }

    protected GBPKML(byte[] data, bool jp = false) : base(data)
    {
        int strLen = jp ? StringLengthJapanese : StringLengthNotJapan;

        // initialize string buffers
        RawOT = new byte[strLen];
        RawNickname = new byte[strLen];
        OriginalTrainerTrash.Fill(StringConverter1.TerminatorCode);
        NicknameTrash.Fill(StringConverter1.TerminatorCode);
    }

    public override void SetNotNicknamed(int language)
    {
        GetNonNickname(language, RawNickname);
        _isnicknamed = false;
    }

    protected override int GetNonNickname(int language, Span<byte> data)
    {
        var name = SpeciesName.GetSpeciesNameGeneration(Species, language, Format);
        int length = SetString(data, name, data.Length, StringConverterOption.Clear50);
[GEN1_Y_EN_CKN]
        // 此程序会将0xF2转码为0xE8，由于ckn版皮卡丘美版汉化编码不兼容原编码，故直接跳过该判定。
        // if (!Korean) // Decimal point<->period fix
        //     data.Replace<byte>(0xF2, 0xE8);
[ELSEIF GEN123_EN]
        // 新1~2代美版汉化编码兼容原编码，在保留原转码程序基础上，添加对汉字编码的识别跳过。
        if (!Korean) {
            // Decimal point<->period fix
            for (int i = 0; i < data.Length; i++)
            {
                var c = data[i];
                if(!Japanese && c >= 0x01 && c <= 0x2E && c != 0x14 && c != 0x15 && c != 0x16 && c != 0x17 && c != 0x20 && c != 0x21 && c != 0x22 && c != 0x23 && c != 0x24 && c != 0x25 && c != 0x26 && c != 0x27)
                {
                    i = i + 2;
                    c = data[i];
                }
                if (c == 0xF2)
                    c = 0xE8;
            }
        }
[ELSE]
        if (!Korean) // Decimal point<->period fix
            data.Replace<byte>(0xF2, 0xE8);
[/END]
        return length;
    }

    public sealed override string Nickname
    {
        get => GetString(NicknameTrash);
        set
        {
            if (!IsNicknamed && Nickname == value)
                return;

            SetStringKeepTerminatorStyle(value, NicknameTrash);
        }
    }

    public sealed override string OriginalTrainerName
    {
        get => GetString(OriginalTrainerTrash);
        set
        {
            if (value == OriginalTrainerName)
                return;
            SetStringKeepTerminatorStyle(value, OriginalTrainerTrash);
        }
    }

    private void SetStringKeepTerminatorStyle(ReadOnlySpan<char> value, Span<byte> exist)
    {
        // Reset the destination buffer based on the termination style of the existing string.
        bool zeroed = exist.Contains<byte>(0);
        StringConverterOption converterOption = (zeroed) ? StringConverterOption.ClearZero : StringConverterOption.Clear50;
        SetString(exist, value, value.Length, converterOption);
    }
}
