﻿using System;

namespace PKHeX.Core;

/// <summary>
/// Logic for converting a <see cref="string"/> for Generation 3.
/// </summary>
public static class StringConverter3
{
    private const byte TerminatorByte = 0xFF;
    private const char Terminator = (char)TerminatorByte;

    /// <summary>
    /// Converts a Generation 3 encoded value array to string.
    /// </summary>
    /// <param name="data">Byte array containing string data.</param>
    /// <param name="jp">Value source is Japanese font.</param>
    /// <returns>Decoded string.</returns>
    public static string GetString(ReadOnlySpan<byte> data, bool jp)
    {
        Span<char> result = stackalloc char[data.Length];
        int i = 0;
        int CharCounts = 0;
        int ChsCounts = 0;
        int EngCounts = 0;
        for (; i < data.Length; i++)
        {
            var value = data[i];
            CharCounts = ChsCounts + EngCounts;

            if (!jp && value >= 0x01 && value <= 0x1E && value != 0x06 && value != 0x1B)
            {
                var value2 = data[i + 1];
                var d = value << 8 | value2;
                var c = GetG3Char2( d );
                result[CharCounts] = c;
                ChsCounts++;
                i++;
            }
            else
            {
                var c = GetG3Char(value, jp); // Convert to Unicode
                if (c == Terminator) // Stop if Terminator/Invalid
                    break;
                result[CharCounts] = c;
                EngCounts++;
            }
        }
        if (i < data.Length)
            i = CharCounts;
        else
            i = CharCounts + 1;
        return new string(result[..i].ToArray());
    }

    /// <summary>
    /// Decodes a character from a Generation 3 encoded value.
    /// </summary>
    /// <param name="chr">Generation 4 decoded character.</param>
    /// <param name="jp">Character destination is Japanese font.</param>
    /// <returns>Generation 3 encoded value.</returns>
    private static char GetG3Char(byte chr, bool jp)
    {
        var table = jp ? G3_JP : G3_EN;
        return table[chr];
    }

    private static char GetG3Char2(int d )
    {
        var table = G3_CH;
        return table[d];
    }

    /// <summary>
    /// Converts a string to a Generation 3 encoded value array.
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="value">Decoded string.</param>
    /// <param name="maxLength">Maximum length of the input <see cref="value"/></param>
    /// <param name="jp">String destination is Japanese font.</param>
    /// <param name="option">Buffer pre-formatting option</param>
    /// <returns>Encoded data.</returns>
    public static int SetString(Span<byte> buffer, ReadOnlySpan<char> value, int maxLength, bool jp,
        StringConverterOption option = StringConverterOption.ClearFF)
    {
        if (value.Length > maxLength)
            value = value[..maxLength]; // Hard cap

        if (option is StringConverterOption.ClearFF)
            buffer.Fill(0xFF);
        else if (option is StringConverterOption.ClearZero)
            buffer.Clear();

        int i = 0;
        int ValueCounts = 0;
        int ChsChars = 0;
        int EngChars = 0;
        int JapChars = 0;
        for (; i < value.Length; i++)
        {
            var chr = value[i];
            ValueCounts = (ChsChars * 2) + EngChars + JapChars;

            if(!jp)
            {
                var b0 = SetG3Char2(chr);
                if (b0 == TerminatorByte)
                    break;
                if (b0 >= 0 && b0 < 256)
                {
                    buffer[ValueCounts] = (byte)b0;
                    EngChars++;
                }
                else
                {
                    var b1 = b0 >> 8;
                    var b2 = b0 & 0xff;
                    buffer[ValueCounts] = (byte) b1;
                    ValueCounts++;
                    buffer[ValueCounts] = (byte) b2;
                    ChsChars++;
                }
            }
            else
            {
                var b = SetG3Char(chr, jp);
                if (b == TerminatorByte)
                    break;
                buffer[ValueCounts] = b;
                JapChars++;
            }
        }

        int count = ValueCounts + 1;
        if (count < buffer.Length)
            buffer[count++] = TerminatorByte;
        return count;

    }

    /// <summary>
    /// Encodes a character to a Generation 3 encoded value.
    /// </summary>
    /// <param name="chr">Generation 4 decoded character.</param>
    /// <param name="jp">Character destination is Japanese font.</param>
    /// <returns>Generation 3 encoded value.</returns>
    private static byte SetG3Char(char chr, bool jp)
    {
        if (chr == '\'') // ’
            return 0xB4;
        var table = jp ? G3_JP : G3_EN;
        var index = Array.IndexOf(table, chr);
        if (index == -1)
            return TerminatorByte;
        return (byte)index;
    }


    private static Int16 SetG3Char2(char chr)
    {
        if (chr == '\'') // ’
            return (byte)0xB4;
        var table = G3_CH;
        var index = Array.IndexOf(table, chr);
        if (index == -1)
            return TerminatorByte;
        return (short)index;
    }
    private static readonly char[] G3_EN =
    {
        ' ',  'À',  'Á',  'Â', 'Ç',  'È',  'É',  'Ê',  'Ë',  'Ì', 'こ', 'Î',  'Ï',  'Ò',  'Ó',  'Ô',  // 0
        'Œ',  'Ù',  'Ú',  'Û', 'Ñ',  'ß',  'à',  'á',  'ね', 'Ç',  'È', 'é',  'ê',  'ë',  'ì',  'í',  // 1
        'î',  'ï',  'ò',  'ó', 'ô',  'œ',  'ù',  'ú',  'û',  'ñ',  'º', 'ª',  '⒅', '&',  '+',  'あ', // 2
        'ぃ', 'ぅ', 'ぇ', 'ぉ', 'ゃ', '=',  'ょ', 'が', 'ぎ', 'ぐ', 'げ', 'ご', 'ざ', 'じ', 'ず', 'ぜ', // 3
        'ぞ', 'だ', 'ぢ', 'づ', 'で', 'ど', 'ば', 'び', 'ぶ', 'べ', 'ぼ', 'ぱ', 'ぴ', 'ぷ', 'ぺ', 'ぽ',  // 4
        'っ', '¿',  '¡',  '⒆', '⒇', 'オ', 'カ', 'キ', 'ク', 'ケ', 'Í',  'コ', 'サ', 'ス', 'セ', 'ソ', // 5
        'タ', 'チ', 'ツ', 'テ', 'ト', 'ナ', 'ニ', 'ヌ', 'â',  'ノ', 'ハ', 'ヒ', 'フ', 'ヘ', 'ホ', 'í',  // 6
        'ミ', 'ム', 'メ', 'モ', 'ヤ', 'ユ', 'ヨ', 'ラ', 'リ', 'ル', 'レ', 'ロ', 'ワ', 'ヲ', 'ン', 'ァ', // 7
        'ィ', 'ゥ', 'ェ', 'ォ', 'ャ', 'ュ', 'ョ', 'ガ', 'ギ', 'グ', 'ゲ', 'ゴ', 'ザ', 'ジ', 'ズ', 'ゼ', // 8
        'ゾ', 'ダ', 'ヂ', 'ヅ', 'デ', 'ド', 'バ', 'ビ', 'ブ', 'ベ', 'ボ', 'パ', 'ピ', 'プ', 'ペ', 'ポ', // 9
        'ッ', '0',  '1',  '2', '3',  '4',  '5',  '6',  '7',  '8',  '9',  '!', '?',  '.',  '-',  '・',// A
        '⑬',  '“',  '”',  '‘', '’',  '♂',  '♀',  '$',  ',',  '⑧',  '/',  'A', 'B',  'C',  'D',  'E', // B
        'F',  'G',  'H',  'I', 'J',  'K',  'L',  'M',  'N',  'O',  'P',  'Q', 'R',  'S',  'T',  'U', // C
        'V',  'W',  'X',  'Y', 'Z',  'a',  'b',  'c',  'd',  'e',  'f',  'g', 'h',  'i',  'j',  'k', // D
        'l',  'm',  'n',  'o', 'p',  'q',  'r',  's',  't',  'u',  'v',  'w', 'x',  'y',  'z',  '0', // E
        ':',  'Ä',  'Ö',  'Ü', 'ä',  'ö',  'ü',                                                      // F

        // Make the total length 256 so that any byte access is always within the array
        Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
    };

    private static readonly char[] G3_JP =
    {
        '　', 'あ', 'い', 'う', 'え', 'お', 'か', 'き', 'く', 'け', 'こ', 'さ', 'し', 'す', 'せ', 'そ', // 0
        'た', 'ち', 'つ', 'て', 'と', 'な', 'に', 'ぬ', 'ね', 'の', 'は', 'ひ', 'ふ', 'へ', 'ほ', 'ま', // 1
        'み', 'む', 'め', 'も', 'や', 'ゆ', 'よ', 'ら', 'り', 'る', 'れ', 'ろ', 'わ', 'を', 'ん', 'ぁ', // 2
        'ぃ', 'ぅ', 'ぇ', 'ぉ', 'ゃ', 'ゅ', 'ょ', 'が', 'ぎ', 'ぐ', 'げ', 'ご', 'ざ', 'じ', 'ず', 'ぜ', // 3
        'ぞ', 'だ', 'ぢ', 'づ', 'で', 'ど', 'ば', 'び', 'ぶ', 'べ', 'ぼ', 'ぱ', 'ぴ', 'ぷ', 'ぺ', 'ぽ', // 4
        'っ', 'ア', 'イ', 'ウ', 'エ', 'オ', 'カ', 'キ', 'ク', 'ケ', 'コ', 'サ', 'シ', 'ス', 'セ', 'ソ', // 5
        'タ', 'チ', 'ツ', 'テ', 'ト', 'ナ', 'ニ', 'ヌ', 'ネ', 'ノ', 'ハ', 'ヒ', 'フ', 'ヘ', 'ホ', 'マ', // 6
        'ミ', 'ム', 'メ', 'モ', 'ヤ', 'ユ', 'ヨ', 'ラ', 'リ', 'ル', 'レ', 'ロ', 'ワ', 'ヲ', 'ン', 'ァ', // 7
        'ィ', 'ゥ', 'ェ', 'ォ', 'ャ', 'ュ', 'ョ', 'ガ', 'ギ', 'グ', 'ゲ', 'ゴ', 'ザ', 'ジ', 'ズ', 'ゼ', // 8
        'ゾ', 'ダ', 'ヂ', 'ヅ', 'デ', 'ド', 'バ', 'ビ', 'ブ', 'ベ', 'ボ', 'パ', 'ピ', 'プ', 'ペ', 'ポ', // 9
        'ッ', '０', '１', '２', '３', '４', '５', '６', '７', '８', '９', '！', '？', '。', 'ー', '・', // A
        '⋯',  '『', '』', '「', '」', '♂',  '♀',  '$',  '.', '⑧',  '/',  'Ａ', 'Ｂ', 'Ｃ', 'Ｄ', 'Ｅ', // B
        'Ｆ', 'Ｇ', 'Ｈ', 'Ｉ', 'Ｊ', 'Ｋ', 'Ｌ', 'Ｍ', 'Ｎ', 'Ｏ', 'Ｐ', 'Ｑ', 'Ｒ', 'Ｓ', 'Ｔ', 'Ｕ', // C
        'Ｖ', 'Ｗ', 'Ｘ', 'Ｙ', 'Ｚ', 'ａ', 'ｂ', 'ｃ', 'ｄ', 'ｅ', 'ｆ', 'ｇ', 'ｈ', 'ｉ', 'ｊ', 'ｋ', // D
        'ｌ', 'ｍ', 'ｎ', 'ｏ', 'ｐ', 'ｑ', 'ｒ', 'ｓ', 'ｔ', 'ｕ', 'ｖ', 'ｗ', 'ｘ', 'ｙ', 'ｚ', '0',  // E
        ':',  'Ä',  'Ö',  'Ü',  'ä',  'ö', 'ü',                                                      // F

        // Make the total length 256 so that any byte access is always within the array
        Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
    };

    private static readonly char[] G3_CH = new char[7936]
    {   //0x00**
        ' ', Terminator, Terminator, Terminator, Terminator, Terminator, 'É', Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, // 0（01-05、07-0F为中文编码识别区）
         Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, 'é',Terminator, Terminator, Terminator, 'í',  // 1（10-1A、1C-1E为中文编码识别区）
        'î',  'ï',  'ò',  'ó', 'ô',  'œ',  'ù',  'ú',  'û',  'ñ',  'º', 'ª',  '⒅', '&',  '+',  'あ', // 2
        'ぃ', 'ぅ', 'ぇ', 'ぉ', 'ゃ', '=',  'ょ', 'が', 'ぎ', 'ぐ', 'げ', 'ご', 'ざ', 'じ', 'ず', 'ぜ', // 3
        'ぞ', 'だ', 'ぢ', 'づ', 'で', 'ど', 'ば', 'び', 'ぶ', 'べ', 'ぼ', 'ぱ', 'ぴ', 'ぷ', 'ぺ', 'ぽ',  // 4
        'っ', '¿',  '¡',  '⒆', '⒇', 'オ', 'カ', 'キ', 'ク', 'ケ', 'Í',  'コ', 'サ', 'ス', 'セ', 'ソ', // 5
        'タ', 'チ', 'ツ', 'テ', 'ト', 'ナ', 'ニ', 'ヌ', 'â',  'ノ', 'ハ', 'ヒ', 'フ', 'ヘ', 'ホ', 'í',  // 6
        'ミ', 'ム', 'メ', 'モ', 'ヤ', 'ユ', 'ヨ', 'ラ', 'リ', 'ル', 'レ', 'ロ', 'ワ', 'ヲ', 'ン', 'ァ', // 7
        'ィ', 'ゥ', 'ェ', 'ォ', 'ャ', 'ュ', 'ョ', 'ガ', 'ギ', 'グ', 'ゲ', 'ゴ', 'ザ', 'ジ', 'ズ', 'ゼ', // 8
        'ゾ', 'ダ', 'ヂ', 'ヅ', 'デ', 'ド', 'バ', 'ビ', 'ブ', 'ベ', 'ボ', 'パ', 'ピ', 'プ', 'ペ', 'ポ', // 9
        'ッ', '0',  '1',  '2', '3',  '4',  '5',  '6',  '7',  '8',  '9',  '!', '?',  '.',  '-',  '・',// A
        '⑬',  '“',  '”',  '‘', '’',  '♂',  '♀',  '$',  ',',  '⑧',  '/',  'A', 'B',  'C',  'D',  'E', // B
        'F',  'G',  'H',  'I', 'J',  'K',  'L',  'M',  'N',  'O',  'P',  'Q', 'R',  'S',  'T',  'U', // C
        'V',  'W',  'X',  'Y', 'Z',  'a',  'b',  'c',  'd',  'e',  'f',  'g', 'h',  'i',  'j',  'k', // D
        'l',  'm',  'n',  'o', 'p',  'q',  'r',  's',  't',  'u',  'v',  'w', 'x',  'y',  'z',  '0', // E
        ':',  'Ä',  'Ö',  'Ü', 'ä',  'ö',  'ü',                                                      // F

        // Make the total length 256 so that any byte access is always within the array
        Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,

        //0x01**
        '啊',    '阿',    '埃',    '挨',    '哎',    '唉',    '哀',    '皑',    '癌',    '蔼',    '矮',    '艾',    '碍',    '爱',    '隘',    '鞍',
        '氨',    '安',    '俺',    '按',    '暗',    '岸',    '胺',    '案',    '肮',    '昂',    '盎',    '凹',    '敖',    '熬',    '翱',    '袄',
        '傲',    '奥',    '懊',    '澳',    '芭',    '捌',    '扒',    '叭',    '吧',    '笆',    '八',    '疤',    '巴',    '拔',    '跋',    '靶',
        '把',    '耙',    '坝',    '霸',    '罢',    '爸',    '白',    '柏',    '百',    '摆',    '佰',    '败',    '拜',    '稗',    '斑',    '班',
        '搬',    '扳',    '般',    '颁',    '板',    '版',    '扮',    '拌',    '伴',    '瓣',    '半',    '办',    '绊',    '邦',    '帮',    '梆',
        '榜',    '膀',    '绑',    '棒',    '磅',    '蚌',    '镑',    '傍',    '谤',    '苞',    '胞',    '包',    '褒',    '剥',    '薄',    '雹',
        '保',    '堡',    '饱',    '宝',    '抱',    '报',    '暴',    '豹',    '鲍',    '爆',    '杯',    '碑',    '悲',    '卑',    '北',    '辈',
        '背',    '贝',    '钡',    '倍',    '狈',    '备',    '惫',    '焙',    '被',    '奔',    '苯',    '本',    '笨',    '崩',    '绷',    '甭',
        '泵',    '蹦',    '迸',    '逼',    '鼻',    '比',    '鄙',    '笔',    '彼',    '碧',    '蓖',    '蔽',    '毕',    '毙',    '毖',    '币',
        '庇',    '痹',    '闭',    '敝',    '弊',    '必',    '辟',    '壁',    '臂',    '避',    '陛',    '鞭',    '边',    '编',    '贬',    '扁',
        '便',    '变',    '卞',    '辨',    '辩',    '辫',    '遍',    '标',    '彪',    '膘',    '表',    '鳖',    '憋',    '别',    '瘪',    '彬',
        '斌',    '濒',    '滨',    '宾',    '摈',    '兵',    '冰',    '柄',    '丙',    '秉',    '饼',    '炳',    '病',    '并',    '玻',    '菠',
        '播',    '拨',    '钵',    '波',    '博',    '勃',    '搏',    '铂',    '箔',    '伯',    '帛',    '舶',    '脖',    '膊',    '渤',    '泊',
        '驳',    '捕',    '卜',    '哺',    '补',    '埠',    '不',    '布',    '步',    '簿',    '部',    '怖',    '擦',    '猜',    '裁',    '材',
        '才',    '财',    '睬',    '踩',    '采',    '彩',    '菜',    '蔡',    '餐',    '参',    '蚕',    '残',    '惭',    '惨',    '灿',    '苍',
        '舱',    '仓',    '沧',    '藏',    '操',    '糙',    '槽',    Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,

        //0x02**
        '曹',    '草',    '厕',    '策',    '侧',    '册',    '测',    '层',    '蹭',    '插',    '叉',    '茬',    '茶',    '查',    '碴',    '搽',
        '察',    '岔',    '差',    '诧',    '拆',    '柴',    '豺',    '搀',    '掺',    '蝉',    '馋',    '谗',    '缠',    '铲',    '产',    '阐',
        '颤',    '昌',    '猖',    '场',    '尝',    '常',    '长',    '偿',    '肠',    '厂',    '敞',    '畅',    '唱',    '倡',    '超',    '抄',
        '钞',    '朝',    '嘲',    '潮',    '巢',    '吵',    '炒',    '车',    '扯',    '撤',    '掣',    '彻',    '澈',    '郴',    '臣',    '辰',
        '尘',    '晨',    '忱',    '沉',    '陈',    '趁',    '衬',    '撑',    '称',    '城',    '橙',    '成',    '呈',    '乘',    '程',    '惩',
        '澄',    '诚',    '承',    '逞',    '骋',    '秤',    '吃',    '痴',    '持',    '匙',    '池',    '迟',    '弛',    '驰',    '耻',    '齿',
        '侈',    '尺',    '赤',    '翅',    '斥',    '炽',    '充',    '冲',    '虫',    '崇',    '宠',    '抽',    '酬',    '畴',    '踌',    '稠',
        '愁',    '筹',    '仇',    '绸',    '瞅',    '丑',    '臭',    '初',    '出',    '橱',    '厨',    '躇',    '锄',    '雏',    '滁',    '除',
        '楚',    '础',    '储',    '矗',    '搐',    '触',    '处',    '揣',    '川',    '穿',    '椽',    '传',    '船',    '喘',    '串',    '疮',
        '窗',    '幢',    '床',    '闯',    '创',    '吹',    '炊',    '捶',    '锤',    '垂',    '春',    '椿',    '醇',    '唇',    '淳',    '纯',
        '蠢',    '戳',    '绰',    '疵',    '茨',    '磁',    '雌',    '辞',    '慈',    '瓷',    '词',    '此',    '刺',    '赐',    '次',    '聪',
        '葱',    '囱',    '匆',    '从',    '丛',    '凑',    '粗',    '醋',    '簇',    '促',    '蹿',    '篡',    '窜',    '摧',    '崔',    '催',
        '脆',    '瘁',    '粹',    '淬',    '翠',    '村',    '存',    '寸',    '磋',    '撮',    '搓',    '措',    '挫',    '错',    '搭',    '达',
        '答',    '瘩',    '打',    '大',    '呆',    '歹',    '傣',    '戴',    '带',    '殆',    '代',    '贷',    '袋',    '待',    '逮',    '怠',
        '耽',    '担',    '丹',    '单',    '郸',    '掸',    '胆',    '旦',    '氮',    '但',    '惮',    '淡',    '诞',    '弹',    '蛋',    '当',
        '挡',    '党',    '荡',    '档',    '刀',    '捣',    '蹈',    Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,

        //0x03**
        '倒',    '岛',    '祷',    '导',    '到',    '稻',    '悼',    '道',    '盗',    '德',    '得',    '的',    '蹬',    '灯',    '登',    '等',
        '瞪',    '凳',    '邓',    '堤',    '低',    '滴',    '迪',    '敌',    '笛',    '狄',    '涤',    '翟',    '嫡',    '抵',    '底',    '地',
        '蒂',    '第',    '帝',    '弟',    '递',    '缔',    '颠',    '掂',    '滇',    '碘',    '点',    '典',    '靛',    '垫',    '电',    '佃',
        '甸',    '店',    '惦',    '奠',    '淀',    '殿',    '碉',    '叼',    '雕',    '凋',    '刁',    '掉',    '吊',    '钓',    '调',    '跌',
        '爹',    '碟',    '蝶',    '迭',    '谍',    '叠',    '丁',    '盯',    '叮',    '钉',    '顶',    '鼎',    '锭',    '定',    '订',    '丢',
        '东',    '冬',    '董',    '懂',    '动',    '栋',    '侗',    '恫',    '冻',    '洞',    '兜',    '抖',    '斗',    '陡',    '豆',    '逗',
        '痘',    '都',    '督',    '毒',    '犊',    '独',    '读',    '堵',    '睹',    '赌',    '杜',    '镀',    '肚',    '度',    '渡',    '妒',
        '端',    '短',    '锻',    '段',    '断',    '缎',    '堆',    '兑',    '队',    '对',    '墩',    '吨',    '蹲',    '敦',    '顿',    '囤',
        '钝',    '盾',    '遁',    '掇',    '哆',    '多',    '夺',    '垛',    '躲',    '朵',    '跺',    '舵',    '剁',    '惰',    '堕',    '蛾',
        '峨',    '鹅',    '俄',    '额',    '讹',    '娥',    '恶',    '厄',    '扼',    '遏',    '鄂',    '饿',    '恩',    '而',    '儿',    '耳',
        '尔',    '饵',    '洱',    '二',    '贰',    '发',    '罚',    '筏',    '伐',    '乏',    '阀',    '法',    '珐',    '藩',    '帆',    '番',
        '翻',    '樊',    '矾',    '钒',    '繁',    '凡',    '烦',    '反',    '返',    '范',    '贩',    '犯',    '饭',    '泛',    '坊',    '芳',
        '方',    '肪',    '房',    '防',    '妨',    '仿',    '访',    '纺',    '放',    '菲',    '非',    '啡',    '飞',    '肥',    '匪',    '诽',
        '吠',    '肺',    '废',    '沸',    '费',    '芬',    '酚',    '吩',    '氛',    '分',    '纷',    '坟',    '焚',    '汾',    '粉',    '奋',
        '份',    '忿',    '愤',    '粪',    '丰',    '封',    '枫',    '蜂',    '峰',    '锋',    '风',    '疯',    '烽',    '逢',    '冯',    '缝',
        '讽',    '奉',    '凤',    '佛',    '否',    '夫',    '敷',    Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        
        //0x04**
        '肤',    '孵',    '扶',    '拂',    '辐',    '幅',    '氟',    '符',    '伏',    '俘',    '服',    '浮',    '涪',    '福',    '袱',    '弗',
        '甫',    '抚',    '辅',    '俯',    '釜',    '斧',    '脯',    '腑',    '府',    '腐',    '赴',    '副',    '覆',    '赋',    '复',    '傅',
        '付',    '阜',    '父',    '腹',    '负',    '富',    '讣',    '附',    '妇',    '缚',    '咐',    '噶',    '嘎',    '该',    '改',    '概',
        '钙',    '盖',    '溉',    '干',    '甘',    '杆',    '柑',    '竿',    '肝',    '赶',    '感',    '秆',    '敢',    '赣',    '冈',    '刚',
        '钢',    '缸',    '肛',    '纲',    '岗',    '港',    '杠',    '篙',    '皋',    '高',    '膏',    '羔',    '糕',    '搞',    '镐',    '稿',
        '告',    '哥',    '歌',    '搁',    '戈',    '鸽',    '胳',    '疙',    '割',    '革',    '葛',    '格',    '蛤',    '阁',    '隔',    '铬',
        '个',    '各',    '给',    '根',    '跟',    '耕',    '更',    '庚',    '羹',    '埂',    '耿',    '梗',    '工',    '攻',    '功',    '恭',
        '龚',    '供',    '躬',    '公',    '宫',    '弓',    '巩',    '汞',    '拱',    '贡',    '共',    '钩',    '勾',    '沟',    '苟',    '狗',
        '垢',    '构',    '购',    '够',    '辜',    '菇',    '咕',    '箍',    '估',    '沽',    '孤',    '姑',    '鼓',    '古',    '蛊',    '骨',
        '谷',    '股',    '故',    '顾',    '固',    '雇',    '刮',    '瓜',    '剐',    '寡',    '挂',    '褂',    '乖',    '拐',    '怪',    '棺',
        '关',    '官',    '冠',    '观',    '管',    '馆',    '罐',    '惯',    '灌',    '贯',    '光',    '广',    '逛',    '瑰',    '规',    '圭',
        '硅',    '归',    '龟',    '闺',    '轨',    '鬼',    '诡',    '癸',    '桂',    '柜',    '跪',    '贵',    '刽',    '辊',    '滚',    '棍',
        '锅',    '郭',    '国',    '果',    '裹',    '过',    '哈',    '骸',    '孩',    '海',    '氦',    '亥',    '害',    '骇',    '酣',    '憨',
        '邯',    '韩',    '含',    '涵',    '寒',    '函',    '喊',    '罕',    '翰',    '撼',    '捍',    '旱',    '憾',    '悍',    '焊',    '汗',
        '汉',    '夯',    '杭',    '航',    '壕',    '嚎',    '豪',    '毫',    '郝',    '好',    '耗',    '号',    '浩',    '呵',    '喝',    '荷',
        '菏',    '核',    '禾',    '和',    '何',    '合',    '盒',    Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        
        //0x05**
        '貉',    '阂',    '河',    '涸',    '赫',    '褐',    '鹤',    '贺',    '嘿',    '黑',    '痕',    '很',    '狠',    '恨',    '哼',    '亨',
        '横',    '衡',    '恒',    '轰',    '哄',    '烘',    '虹',    '鸿',    '洪',    '宏',    '弘',    '红',    '喉',    '侯',    '猴',    '吼',
        '厚',    '候',    '后',    '呼',    '乎',    '忽',    '瑚',    '壶',    '葫',    '胡',    '蝴',    '狐',    '糊',    '湖',    '弧',    '虎',
        '唬',    '护',    '互',    '沪',    '户',    '花',    '哗',    '华',    '猾',    '滑',    '画',    '划',    '化',    '话',    '槐',    '徊',
        '怀',    '淮',    '坏',    '欢',    '环',    '桓',    '还',    '缓',    '换',    '患',    '唤',    '痪',    '豢',    '焕',    '涣',    '宦',
        '幻',    '荒',    '慌',    '黄',    '磺',    '蝗',    '簧',    '皇',    '凰',    '惶',    '煌',    '晃',    '幌',    '恍',    '谎',    '灰',
        '挥',    '辉',    '徽',    '恢',    '蛔',    '回',    '毁',    '悔',    '慧',    '卉',    '惠',    '晦',    '贿',    '秽',    '会',    '烩',
        '汇',    '讳',    '诲',    '绘',    '荤',    '昏',    '婚',    '魂',    '浑',    '混',    '豁',    '活',    '伙',    '火',    '获',    '或',
        '惑',    '霍',    '货',    '祸',    '击',    '圾',    '基',    '机',    '畸',    '稽',    '积',    '箕',    '肌',    '饥',    '迹',    '激',
        '讥',    '鸡',    '姬',    '绩',    '缉',    '吉',    '极',    '棘',    '辑',    '籍',    '集',    '及',    '急',    '疾',    '汲',    '即',
        '嫉',    '级',    '挤',    '几',    '脊',    '己',    '蓟',    '技',    '冀',    '季',    '伎',    '祭',    '剂',    '悸',    '济',    '寄',
        '寂',    '计',    '记',    '既',    '忌',    '际',    '妓',    '继',    '纪',    '嘉',    '枷',    '夹',    '佳',    '家',    '加',    '荚',
        '颊',    '贾',    '甲',    '钾',    '假',    '稼',    '价',    '架',    '驾',    '嫁',    '歼',    '监',    '坚',    '尖',    '笺',    '间',
        '煎',    '兼',    '肩',    '艰',    '奸',    '缄',    '茧',    '检',    '柬',    '碱',    '硷',    '拣',    '捡',    '简',    '俭',    '剪',
        '减',    '荐',    '槛',    '鉴',    '践',    '贱',    '见',    '键',    '箭',    '件',    '健',    '舰',    '剑',    '饯',    '渐',    '溅',
        '涧',    '建',    '僵',    '姜',    '将',    '浆',    '江',    Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        
        //0x06**
        Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        
        //0x07**
        '疆',    '蒋',    '桨',    '奖',    '讲',    '匠',    '酱',    '降',    '蕉',    '椒',    '礁',    '焦',    '胶',    '交',    '郊',    '浇',
        '骄',    '娇',    '嚼',    '搅',    '铰',    '矫',    '侥',    '脚',    '狡',    '角',    '饺',    '缴',    '绞',    '剿',    '教',    '酵',
        '轿',    '较',    '叫',    '窖',    '揭',    '接',    '皆',    '秸',    '街',    '阶',    '截',    '劫',    '节',    '桔',    '杰',    '捷',
        '睫',    '竭',    '洁',    '结',    '解',    '姐',    '戒',    '藉',    '芥',    '界',    '借',    '介',    '疥',    '诫',    '届',    '巾',
        '筋',    '斤',    '金',    '今',    '津',    '襟',    '紧',    '锦',    '仅',    '谨',    '进',    '靳',    '晋',    '禁',    '近',    '烬',
        '浸',    '尽',    '劲',    '荆',    '兢',    '茎',    '睛',    '晶',    '鲸',    '京',    '惊',    '精',    '粳',    '经',    '井',    '警',
        '景',    '颈',    '静',    '境',    '敬',    '镜',    '径',    '痉',    '靖',    '竟',    '竞',    '净',    '炯',    '窘',    '揪',    '究',
        '纠',    '玖',    '韭',    '久',    '灸',    '九',    '酒',    '厩',    '救',    '旧',    '臼',    '舅',    '咎',    '就',    '疚',    '鞠',
        '拘',    '狙',    '疽',    '居',    '驹',    '菊',    '局',    '咀',    '矩',    '举',    '沮',    '聚',    '拒',    '据',    '巨',    '具',
        '距',    '踞',    '锯',    '俱',    '句',    '惧',    '炬',    '剧',    '捐',    '鹃',    '娟',    '倦',    '眷',    '卷',    '绢',    '撅',
        '攫',    '抉',    '掘',    '倔',    '爵',    '觉',    '决',    '诀',    '绝',    '均',    '菌',    '钧',    '军',    '君',    '峻',    '俊',
        '竣',    '浚',    '郡',    '骏',    '喀',    '咖',    '卡',    '咯',    '开',    '揩',    '楷',    '凯',    '慨',    '刊',    '堪',    '勘',
        '坎',    '砍',    '看',    '康',    '慷',    '糠',    '扛',    '抗',    '亢',    '炕',    '考',    '拷',    '烤',    '靠',    '坷',    '苛',
        '柯',    '棵',    '磕',    '颗',    '科',    '壳',    '咳',    '可',    '渴',    '克',    '刻',    '客',    '课',    '肯',    '啃',    '垦',
        '恳',    '坑',    '吭',    '空',    '恐',    '孔',    '控',    '抠',    '口',    '扣',    '寇',    '枯',    '哭',    '窟',    '苦',    '酷',
        '库',    '裤',    '夸',    '垮',    '挎',    '跨',    '胯',    Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        
        //0x08**
        '块',    '筷',    '侩',    '快',    '宽',    '款',    '匡',    '筐',    '狂',    '框',    '矿',    '眶',    '旷',    '况',    '亏',    '盔',
        '岿',    '窥',    '葵',    '奎',    '魁',    '傀',    '馈',    '愧',    '溃',    '坤',    '昆',    '捆',    '困',    '括',    '扩',    '廓',
        '阔',    '垃',    '拉',    '喇',    '蜡',    '腊',    '辣',    '啦',    '莱',    '来',    '赖',    '蓝',    '婪',    '栏',    '拦',    '篮',
        '阑',    '兰',    '澜',    '谰',    '揽',    '览',    '懒',    '缆',    '烂',    '滥',    '琅',    '榔',    '狼',    '廊',    '郎',    '朗',
        '浪',    '捞',    '劳',    '牢',    '老',    '佬',    '姥',    '酪',    '烙',    '涝',    '勒',    '乐',    '雷',    '镭',    '蕾',    '磊',
        '累',    '儡',    '垒',    '擂',    '肋',    '类',    '泪',    '棱',    '楞',    '冷',    '厘',    '梨',    '犁',    '黎',    '篱',    '狸',
        '离',    '漓',    '理',    '李',    '里',    '鲤',    '礼',    '莉',    '荔',    '吏',    '栗',    '丽',    '厉',    '励',    '砾',    '历',
        '利',    '傈',    '例',    '俐',    '痢',    '立',    '粒',    '沥',    '隶',    '力',    '璃',    '哩',    '俩',    '联',    '莲',    '连',
        '镰',    '廉',    '怜',    '涟',    '帘',    '敛',    '脸',    '链',    '恋',    '炼',    '练',    '粮',    '凉',    '梁',    '粱',    '良',
        '两',    '辆',    '量',    '晾',    '亮',    '谅',    '撩',    '聊',    '僚',    '疗',    '燎',    '寥',    '辽',    '潦',    '了',    '撂',
        '镣',    '廖',    '料',    '列',    '裂',    '烈',    '劣',    '猎',    '琳',    '林',    '磷',    '霖',    '临',    '邻',    '鳞',    '淋',
        '凛',    '赁',    '吝',    '拎',    '玲',    '菱',    '零',    '龄',    '铃',    '伶',    '羚',    '凌',    '灵',    '陵',    '岭',    '领',
        '另',    '令',    '溜',    '琉',    '榴',    '硫',    '馏',    '留',    '刘',    '瘤',    '流',    '柳',    '六',    '龙',    '聋',    '咙',
        '笼',    '窿',    '隆',    '垄',    '拢',    '陇',    '楼',    '娄',    '搂',    '篓',    '漏',    '陋',    '芦',    '卢',    '颅',    '庐',
        '炉',    '掳',    '卤',    '虏',    '鲁',    '麓',    '碌',    '露',    '路',    '赂',    '鹿',    '潞',    '禄',    '录',    '陆',    '戮',
        '驴',    '吕',    '铝',    '侣',    '旅',    '履',    '屡',    Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        
        //0x09**
        '缕',    '虑',    '氯',    '律',    '率',    '滤',    '绿',    '峦',    '挛',    '孪',    '滦',    '卵',    '乱',    '掠',    '略',    '抡',
        '轮',    '伦',    '仑',    '沦',    '纶',    '论',    '萝',    '螺',    '罗',    '逻',    '锣',    '箩',    '骡',    '裸',    '落',    '洛',
        '骆',    '络',    '妈',    '麻',    '玛',    '码',    '蚂',    '马',    '骂',    '嘛',    '吗',    '埋',    '买',    '麦',    '卖',    '迈',
        '脉',    '瞒',    '馒',    '蛮',    '满',    '蔓',    '曼',    '慢',    '漫',    '谩',    '芒',    '茫',    '盲',    '氓',    '忙',    '莽',
        '猫',    '茅',    '锚',    '毛',    '矛',    '铆',    '卯',    '茂',    '冒',    '帽',    '貌',    '贸',    '么',    '玫',    '枚',    '梅',
        '酶',    '霉',    '煤',    '没',    '眉',    '媒',    '镁',    '每',    '美',    '昧',    '寐',    '妹',    '媚',    '门',    '闷',    '们',
        '萌',    '蒙',    '檬',    '盟',    '锰',    '猛',    '梦',    '孟',    '眯',    '醚',    '靡',    '糜',    '迷',    '谜',    '弥',    '米',
        '秘',    '觅',    '泌',    '蜜',    '密',    '幂',    '棉',    '眠',    '绵',    '冕',    '免',    '勉',    '娩',    '缅',    '面',    '苗',
        '描',    '瞄',    '藐',    '秒',    '渺',    '庙',    '妙',    '蔑',    '灭',    '民',    '抿',    '皿',    '敏',    '悯',    '闽',    '明',
        '螟',    '鸣',    '铭',    '名',    '命',    '谬',    '摸',    '摹',    '蘑',    '模',    '膜',    '磨',    '摩',    '魔',    '抹',    '末',
        '莫',    '墨',    '默',    '沫',    '漠',    '寞',    '陌',    '谋',    '牟',    '某',    '拇',    '牡',    '亩',    '姆',    '母',    '墓',
        '暮',    '幕',    '募',    '慕',    '木',    '目',    '睦',    '牧',    '穆',    '拿',    '哪',    '呐',    '钠',    '那',    '娜',    '纳',
        '氖',    '乃',    '奶',    '耐',    '奈',    '南',    '男',    '难',    '囊',    '挠',    '脑',    '恼',    '闹',    '淖',    '呢',    '馁',
        '内',    '嫩',    '能',    '妮',    '霓',    '倪',    '泥',    '尼',    '拟',    '你',    '匿',    '腻',    '逆',    '溺',    '蔫',    '拈',
        '年',    '碾',    '撵',    '捻',    '念',    '娘',    '酿',    '鸟',    '尿',    '捏',    '聂',    '孽',    '啮',    '镊',    '镍',    '涅',
        '您',    '柠',    '狞',    '凝',    '宁',    '拧',    '泞',    Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        
        //0x0A**
        '牛',    '扭',    '钮',    '纽',    '脓',    '浓',    '农',    '弄',    '奴',    '努',    '怒',    '女',    '暖',    '虐',    '疟',    '挪',
        '懦',    '糯',    '诺',    '哦',    '欧',    '鸥',    '殴',    '藕',    '呕',    '偶',    '沤',    '啪',    '趴',    '爬',    '帕',    '怕',
        '琶',    '拍',    '排',    '牌',    '徘',    '湃',    '派',    '攀',    '潘',    '盘',    '磐',    '盼',    '畔',    '判',    '叛',    '乓',
        '庞',    '旁',    '耪',    '胖',    '抛',    '咆',    '刨',    '炮',    '袍',    '跑',    '泡',    '呸',    '胚',    '培',    '裴',    '赔',
        '陪',    '配',    '佩',    '沛',    '喷',    '盆',    '砰',    '抨',    '烹',    '澎',    '彭',    '蓬',    '棚',    '硼',    '篷',    '膨',
        '朋',    '鹏',    '捧',    '碰',    '坯',    '砒',    '霹',    '批',    '披',    '劈',    '琵',    '毗',    '啤',    '脾',    '疲',    '皮',
        '匹',    '痞',    '僻',    '屁',    '譬',    '篇',    '偏',    '片',    '骗',    '飘',    '漂',    '瓢',    '票',    '撇',    '瞥',    '拼',
        '频',    '贫',    '品',    '聘',    '乒',    '坪',    '苹',    '萍',    '平',    '凭',    '瓶',    '评',    '屏',    '坡',    '泼',    '颇',
        '婆',    '破',    '魄',    '迫',    '粕',    '剖',    '扑',    '铺',    '仆',    '莆',    '葡',    '菩',    '蒲',    '埔',    '朴',    '圃',
        '普',    '浦',    '谱',    '曝',    '瀑',    '期',    '欺',    '栖',    '戚',    '妻',    '七',    '凄',    '漆',    '柒',    '沏',    '其',
        '棋',    '奇',    '歧',    '畦',    '崎',    '脐',    '齐',    '旗',    '祈',    '祁',    '骑',    '起',    '岂',    '乞',    '企',    '启',
        '契',    '砌',    '器',    '气',    '迄',    '弃',    '汽',    '泣',    '讫',    '掐',    '恰',    '洽',    '牵',    '扦',    '钎',    '铅',
        '千',    '迁',    '签',    '仟',    '谦',    '乾',    '黔',    '钱',    '钳',    '前',    '潜',    '遣',    '浅',    '谴',    '堑',    '嵌',
        '欠',    '歉',    '枪',    '呛',    '腔',    '羌',    '墙',    '蔷',    '强',    '抢',    '橇',    '锹',    '敲',    '悄',    '桥',    '瞧',
        '乔',    '侨',    '巧',    '鞘',    '撬',    '翘',    '峭',    '俏',    '窍',    '切',    '茄',    '且',    '怯',    '窃',    '钦',    '侵',
        '亲',    '秦',    '琴',    '勤',    '芹',    '擒',    '禽',    Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        
        //0x0B**
        '寝',    '沁',    '青',    '轻',    '氢',    '倾',    '卿',    '清',    '擎',    '晴',    '氰',    '情',    '顷',    '请',    '庆',    '琼',
        '穷',    '秋',    '丘',    '邱',    '球',    '求',    '囚',    '酋',    '泅',    '趋',    '区',    '蛆',    '曲',    '躯',    '屈',    '驱',
        '渠',    '取',    '娶',    '龋',    '趣',    '去',    '圈',    '颧',    '权',    '醛',    '泉',    '全',    '痊',    '拳',    '犬',    '券',
        '劝',    '缺',    '炔',    '瘸',    '却',    '鹊',    '榷',    '确',    '雀',    '裙',    '群',    '然',    '燃',    '冉',    '染',    '瓤',
        '壤',    '攘',    '嚷',    '让',    '饶',    '扰',    '绕',    '惹',    '热',    '壬',    '仁',    '人',    '忍',    '韧',    '任',    '认',
        '刃',    '妊',    '纫',    '扔',    '仍',    '日',    '戎',    '茸',    '蓉',    '荣',    '融',    '熔',    '溶',    '容',    '绒',    '冗',
        '揉',    '柔',    '肉',    '茹',    '蠕',    '儒',    '孺',    '如',    '辱',    '乳',    '汝',    '入',    '褥',    '软',    '阮',    '蕊',
        '瑞',    '锐',    '闰',    '润',    '若',    '弱',    '撒',    '洒',    '萨',    '腮',    '鳃',    '塞',    '赛',    '三',    '叁',    '伞',
        '散',    '桑',    '嗓',    '丧',    '搔',    '骚',    '扫',    '嫂',    '瑟',    '色',    '涩',    '森',    '僧',    '莎',    '砂',    '杀',
        '刹',    '沙',    '纱',    '傻',    '啥',    '煞',    '筛',    '晒',    '珊',    '苫',    '杉',    '山',    '删',    '煽',    '衫',    '闪',
        '陕',    '擅',    '赡',    '膳',    '善',    '汕',    '扇',    '缮',    '墒',    '伤',    '商',    '赏',    '晌',    '上',    '尚',    '裳',
        '梢',    '捎',    '稍',    '烧',    '芍',    '勺',    '韶',    '少',    '哨',    '邵',    '绍',    '奢',    '赊',    '蛇',    '舌',    '舍',
        '赦',    '摄',    '射',    '慑',    '涉',    '社',    '设',    '砷',    '申',    '呻',    '伸',    '身',    '深',    '娠',    '绅',    '神',
        '沈',    '审',    '婶',    '甚',    '肾',    '慎',    '渗',    '声',    '生',    '甥',    '牲',    '升',    '绳',    '省',    '盛',    '剩',
        '胜',    '圣',    '师',    '失',    '狮',    '施',    '湿',    '诗',    '尸',    '虱',    '十',    '石',    '拾',    '时',    '什',    '食',
        '蚀',    '实',    '识',    '史',    '矢',    '使',    '屎',    Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        
        //0x0C**
        '驶',    '始',    '式',    '示',    '士',    '世',    '柿',    '事',    '拭',    '誓',    '逝',    '势',    '是',    '嗜',    '噬',    '适',
        '仕',    '侍',    '释',    '饰',    '氏',    '市',    '恃',    '室',    '视',    '试',    '收',    '手',    '首',    '守',    '寿',    '授',
        '售',    '受',    '瘦',    '兽',    '蔬',    '枢',    '梳',    '殊',    '抒',    '输',    '叔',    '舒',    '淑',    '疏',    '书',    '赎',
        '孰',    '熟',    '薯',    '暑',    '曙',    '署',    '蜀',    '黍',    '鼠',    '属',    '术',    '述',    '树',    '束',    '戍',    '竖',
        '墅',    '庶',    '数',    '漱',    '恕',    '刷',    '耍',    '摔',    '衰',    '甩',    '帅',    '栓',    '拴',    '霜',    '双',    '爽',
        '谁',    '水',    '睡',    '税',    '吮',    '瞬',    '顺',    '舜',    '说',    '硕',    '朔',    '烁',    '斯',    '撕',    '嘶',    '思',
        '私',    '司',    '丝',    '死',    '肆',    '寺',    '嗣',    '四',    '伺',    '似',    '饲',    '巳',    '松',    '耸',    '怂',    '颂',
        '送',    '宋',    '讼',    '诵',    '搜',    '艘',    '擞',    '嗽',    '苏',    '酥',    '俗',    '素',    '速',    '粟',    '僳',    '塑',
        '溯',    '宿',    '诉',    '肃',    '酸',    '蒜',    '算',    '虽',    '隋',    '随',    '绥',    '髓',    '碎',    '岁',    '穗',    '遂',
        '隧',    '祟',    '孙',    '损',    '笋',    '蓑',    '梭',    '唆',    '缩',    '琐',    '索',    '锁',    '所',    '塌',    '他',    '它',
        '她',    '塔',    '獭',    '挞',    '蹋',    '踏',    '胎',    '苔',    '抬',    '台',    '泰',    '酞',    '太',    '态',    '汰',    '坍',
        '摊',    '贪',    '瘫',    '滩',    '坛',    '檀',    '痰',    '潭',    '谭',    '谈',    '坦',    '毯',    '袒',    '碳',    '探',    '叹',
        '炭',    '汤',    '塘',    '搪',    '堂',    '棠',    '膛',    '唐',    '糖',    '倘',    '躺',    '淌',    '趟',    '烫',    '掏',    '涛',
        '滔',    '绦',    '萄',    '桃',    '逃',    '淘',    '陶',    '讨',    '套',    '特',    '藤',    '腾',    '疼',    '誊',    '梯',    '剔',
        '踢',    '锑',    '提',    '题',    '蹄',    '啼',    '体',    '替',    '嚏',    '惕',    '涕',    '剃',    '屉',    '天',    '添',    '填',
        '田',    '甜',    '恬',    '舔',    '腆',    '挑',    '条',    Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        
        //0x0D**
        '迢',    '眺',    '跳',    '贴',    '铁',    '帖',    '厅',    '听',    '烃',    '汀',    '廷',    '停',    '亭',    '庭',    '挺',    '艇',
        '通',    '桐',    '酮',    '瞳',    '同',    '铜',    '彤',    '童',    '桶',    '捅',    '筒',    '统',    '痛',    '偷',    '投',    '头',
        '透',    '凸',    '秃',    '突',    '图',    '徒',    '途',    '涂',    '屠',    '土',    '吐',    '兔',    '湍',    '团',    '推',    '颓',
        '腿',    '蜕',    '褪',    '退',    '吞',    '屯',    '臀',    '拖',    '托',    '脱',    '鸵',    '陀',    '驮',    '驼',    '椭',    '妥',
        '拓',    '唾',    '挖',    '哇',    '蛙',    '洼',    '娃',    '瓦',    '袜',    '歪',    '外',    '豌',    '弯',    '湾',    '玩',    '顽',
        '丸',    '烷',    '完',    '碗',    '挽',    '晚',    '皖',    '惋',    '宛',    '婉',    '万',    '腕',    '汪',    '王',    '亡',    '枉',
        '网',    '往',    '旺',    '望',    '忘',    '妄',    '威',    '巍',    '微',    '危',    '韦',    '违',    '桅',    '围',    '唯',    '惟',
        '为',    '潍',    '维',    '苇',    '萎',    '委',    '伟',    '伪',    '尾',    '纬',    '未',    '蔚',    '味',    '畏',    '胃',    '喂',
        '魏',    '位',    '渭',    '谓',    '尉',    '慰',    '卫',    '瘟',    '温',    '蚊',    '文',    '闻',    '纹',    '吻',    '稳',    '紊',
        '问',    '嗡',    '翁',    '瓮',    '挝',    '蜗',    '涡',    '窝',    '我',    '斡',    '卧',    '握',    '沃',    '巫',    '呜',    '钨',
        '乌',    '污',    '诬',    '屋',    '无',    '芜',    '梧',    '吾',    '吴',    '毋',    '武',    '五',    '捂',    '午',    '舞',    '伍',
        '侮',    '坞',    '戊',    '雾',    '晤',    '物',    '勿',    '务',    '悟',    '误',    '昔',    '熙',    '析',    '西',    '硒',    '矽',
        '晰',    '嘻',    '吸',    '锡',    '牺',    '稀',    '息',    '希',    '悉',    '膝',    '夕',    '惜',    '熄',    '烯',    '溪',    '汐',
        '犀',    '檄',    '袭',    '席',    '习',    '媳',    '喜',    '铣',    '洗',    '系',    '隙',    '戏',    '细',    '瞎',    '虾',    '匣',
        '霞',    '辖',    '暇',    '峡',    '侠',    '狭',    '下',    '厦',    '夏',    '吓',    '掀',    '锨',    '先',    '仙',    '鲜',    '纤',
        '咸',    '贤',    '衔',    '舷',    '闲',    '涎',    '弦',    Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        
        //0x0E**
        '嫌',    '显',    '险',    '现',    '献',    '县',    '腺',    '馅',    '羡',    '宪',    '陷',    '限',    '线',    '相',    '厢',    '镶',
        '香',    '箱',    '襄',    '湘',    '乡',    '翔',    '祥',    '详',    '想',    '响',    '享',    '项',    '巷',    '橡',    '像',    '向',
        '象',    '萧',    '硝',    '霄',    '削',    '哮',    '嚣',    '销',    '消',    '宵',    '淆',    '晓',    '小',    '孝',    '校',    '肖',
        '啸',    '笑',    '效',    '楔',    '些',    '歇',    '蝎',    '鞋',    '协',    '挟',    '携',    '邪',    '斜',    '胁',    '谐',    '写',
        '械',    '卸',    '蟹',    '懈',    '泄',    '泻',    '谢',    '屑',    '薪',    '芯',    '锌',    '欣',    '辛',    '新',    '忻',    '心',
        '信',    '衅',    '星',    '腥',    '猩',    '惺',    '兴',    '刑',    '型',    '形',    '邢',    '行',    '醒',    '幸',    '杏',    '性',
        '姓',    '兄',    '凶',    '胸',    '匈',    '汹',    '雄',    '熊',    '休',    '修',    '羞',    '朽',    '嗅',    '锈',    '秀',    '袖',
        '绣',    '墟',    '戌',    '需',    '虚',    '嘘',    '须',    '徐',    '许',    '蓄',    '酗',    '叙',    '旭',    '序',    '畜',    '恤',
        '絮',    '婿',    '绪',    '续',    '轩',    '喧',    '宣',    '悬',    '旋',    '玄',    '选',    '癣',    '眩',    '绚',    '靴',    '薛',
        '学',    '穴',    '雪',    '血',    '勋',    '熏',    '循',    '旬',    '询',    '寻',    '驯',    '巡',    '殉',    '汛',    '训',    '讯',
        '逊',    '迅',    '压',    '押',    '鸦',    '鸭',    '呀',    '丫',    '芽',    '牙',    '蚜',    '崖',    '衙',    '涯',    '雅',    '哑',
        '亚',    '讶',    '焉',    '咽',    '阉',    '烟',    '淹',    '盐',    '严',    '研',    '蜒',    '岩',    '延',    '言',    '颜',    '阎',
        '炎',    '沿',    '奄',    '掩',    '眼',    '衍',    '演',    '艳',    '堰',    '燕',    '厌',    '砚',    '雁',    '唁',    '彦',    '焰',
        '宴',    '谚',    '验',    '殃',    '央',    '鸯',    '秧',    '杨',    '扬',    '佯',    '疡',    '羊',    '洋',    '阳',    '氧',    '仰',
        '痒',    '养',    '样',    '漾',    '邀',    '腰',    '妖',    '瑶',    '摇',    '尧',    '遥',    '窑',    '谣',    '姚',    '咬',    '舀',
        '药',    '要',    '耀',    '椰',    '噎',    '耶',    '爷',    Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        
        //0x0F**
        '野',    '冶',    '也',    '页',    '掖',    '业',    '叶',    '曳',    '腋',    '夜',    '液',    '一',    '壹',    '医',    '揖',    '铱',
        '依',    '伊',    '衣',    '颐',    '夷',    '遗',    '移',    '仪',    '胰',    '疑',    '沂',    '宜',    '姨',    '彝',    '椅',    '蚁',
        '倚',    '已',    '乙',    '矣',    '以',    '艺',    '抑',    '易',    '邑',    '屹',    '亿',    '役',    '臆',    '逸',    '肄',    '疫',
        '亦',    '裔',    '意',    '毅',    '忆',    '义',    '益',    '溢',    '诣',    '议',    '谊',    '译',    '异',    '翼',    '翌',    '绎',
        '茵',    '荫',    '因',    '殷',    '音',    '阴',    '姻',    '吟',    '银',    '淫',    '寅',    '饮',    '尹',    '引',    '隐',    '印',
        '英',    '樱',    '婴',    '鹰',    '应',    '缨',    '莹',    '萤',    '营',    '荧',    '蝇',    '迎',    '赢',    '盈',    '影',    '颖',
        '硬',    '映',    '哟',    '拥',    '佣',    '臃',    '痈',    '庸',    '雍',    '踊',    '蛹',    '咏',    '泳',    '涌',    '永',    '恿',
        '勇',    '用',    '幽',    '优',    '悠',    '忧',    '尤',    '由',    '邮',    '铀',    '犹',    '油',    '游',    '酉',    '有',    '友',
        '右',    '佑',    '釉',    '诱',    '又',    '幼',    '迂',    '淤',    '于',    '盂',    '榆',    '虞',    '愚',    '舆',    '余',    '俞',
        '逾',    '鱼',    '愉',    '渝',    '渔',    '隅',    '予',    '娱',    '雨',    '与',    '屿',    '禹',    '宇',    '语',    '羽',    '玉',
        '域',    '芋',    '郁',    '吁',    '遇',    '喻',    '峪',    '御',    '愈',    '欲',    '狱',    '育',    '誉',    '浴',    '寓',    '裕',
        '预',    '豫',    '驭',    '鸳',    '渊',    '冤',    '元',    '垣',    '袁',    '原',    '援',    '辕',    '园',    '员',    '圆',    '猿',
        '源',    '缘',    '远',    '苑',    '愿',    '怨',    '院',    '曰',    '约',    '越',    '跃',    '钥',    '岳',    '粤',    '月',    '悦',
        '阅',    '耘',    '云',    '郧',    '匀',    '陨',    '允',    '运',    '蕴',    '酝',    '晕',    '韵',    '孕',    '匝',    '砸',    '杂',
        '栽',    '哉',    '灾',    '宰',    '载',    '再',    '在',    '咱',    '攒',    '暂',    '赞',    '赃',    '脏',    '葬',    '遭',    '糟',
        '凿',    '藻',    '枣',    '早',    '澡',    '蚤',    '躁',    Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        
        //0x10**
        '噪',    '造',    '皂',    '灶',    '燥',    '责',    '择',    '则',    '泽',    '贼',    '怎',    '增',    '憎',    '曾',    '赠',    '扎',
        '喳',    '渣',    '札',    '轧',    '铡',    '闸',    '眨',    '栅',    '榨',    '咋',    '乍',    '炸',    '诈',    '摘',    '斋',    '宅',
        '窄',    '债',    '寨',    '瞻',    '毡',    '詹',    '粘',    '沾',    '盏',    '斩',    '辗',    '崭',    '展',    '蘸',    '栈',    '占',
        '战',    '站',    '湛',    '绽',    '樟',    '章',    '彰',    '漳',    '张',    '掌',    '涨',    '杖',    '丈',    '帐',    '账',    '仗',
        '胀',    '瘴',    '障',    '招',    '昭',    '找',    '沼',    '赵',    '照',    '罩',    '兆',    '肇',    '召',    '遮',    '折',    '哲',
        '蛰',    '辙',    '者',    '锗',    '蔗',    '这',    '浙',    '珍',    '斟',    '真',    '甄',    '砧',    '臻',    '贞',    '针',    '侦',
        '枕',    '疹',    '诊',    '震',    '振',    '镇',    '阵',    '蒸',    '挣',    '睁',    '征',    '狰',    '争',    '怔',    '整',    '拯',
        '正',    '政',    '帧',    '症',    '郑',    '证',    '芝',    '枝',    '支',    '吱',    '蜘',    '知',    '肢',    '脂',    '汁',    '之',
        '织',    '职',    '直',    '植',    '殖',    '执',    '值',    '侄',    '址',    '指',    '止',    '趾',    '只',    '旨',    '纸',    '志',
        '挚',    '掷',    '至',    '致',    '置',    '帜',    '峙',    '制',    '智',    '秩',    '稚',    '质',    '炙',    '痔',    '滞',    '治',
        '窒',    '中',    '盅',    '忠',    '钟',    '衷',    '终',    '种',    '肿',    '重',    '仲',    '众',    '舟',    '周',    '州',    '洲',
        '诌',    '粥',    '轴',    '肘',    '帚',    '咒',    '皱',    '宙',    '昼',    '骤',    '珠',    '株',    '蛛',    '朱',    '猪',    '诸',
        '诛',    '逐',    '竹',    '烛',    '煮',    '拄',    '瞩',    '嘱',    '主',    '著',    '柱',    '助',    '蛀',    '贮',    '铸',    '筑',
        '住',    '注',    '祝',    '驻',    '抓',    '爪',    '拽',    '专',    '砖',    '转',    '撰',    '赚',    '篆',    '桩',    '庄',    '装',
        '妆',    '撞',    '壮',    '状',    '椎',    '锥',    '追',    '赘',    '坠',    '缀',    '谆',    '准',    '捉',    '拙',    '卓',    '桌',
        '琢',    '茁',    '酌',    '啄',    '着',    '灼',    '浊',    Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        
        //0x11**
        '兹',    '咨',    '资',    '姿',    '滋',    '淄',    '孜',    '紫',    '仔',    '籽',    '滓',    '子',    '自',    '渍',    '字',    '鬃',
        '棕',    '踪',    '宗',    '综',    '总',    '纵',    '邹',    '走',    '奏',    '揍',    '租',    '足',    '卒',    '族',    '祖',    '诅',
        '阻',    '组',    '钻',    '纂',    '嘴',    '醉',    '最',    '罪',    '尊',    '遵',    '昨',    '左',    '佐',    '柞',    '做',    '作',
        '坐',    '座',    '亍',    '丌',    '兀',    '丐',    '廿',    '卅',    '丕',    '亘',    '丞',    '鬲',    '孬',    '噩',    '丨',    '禺',
        '丿',    '匕',    '乇',    '夭',    '爻',    '卮',    '氐',    '囟',    '胤',    '馗',    '毓',    '睾',    '鼗',    '丶',    '亟',    '鼐',
        '乜',    '乩',    '亓',    '芈',    '孛',    '啬',    '嘏',    '仄',    '厍',    '厝',    '厣',    '厥',    '厮',    '靥',    '赝',    '匚',
        '叵',    '匦',    '匮',    '匾',    '赜',    '卦',    '卣',    '刂',    '刈',    '刎',    '刭',    '刳',    '刿',    '剀',    '剌',    '剞',
        '剡',    '剜',    '蒯',    '剽',    '劂',    '劁',    '劐',    '劓',    '冂',    '罔',    '亻',    '仃',    '仉',    '仂',    '仨',    '仡',
        '仫',    '仞',    '伛',    '仳',    '伢',    '佤',    '仵',    '伥',    '伧',    '伉',    '伫',    '佞',    '佧',    '攸',    '佚',    '佝',
        '佟',    '佗',    '伲',    '伽',    '佶',    '佴',    '侑',    '侉',    '侃',    '侏',    '佾',    '佻',    '侪',    '佼',    '侬',    '侔',
        '俦',    '俨',    '俪',    '俅',    '俚',    '俣',    '俜',    '俑',    '俟',    '俸',    '倩',    '偌',    '俳',    '倬',    '倏',    '倮',
        '倭',    '俾',    '倜',    '倌',    '倥',    '倨',    '偾',    '偃',    '偕',    '偈',    '偎',    '偬',    '偻',    '傥',    '傧',    '傩',
        '傺',    '僖',    '儆',    '僭',    '僬',    '僦',    '僮',    '儇',    '儋',    '仝',    '氽',    '佘',    '佥',    '俎',    '龠',    '汆',
        '籴',    '兮',    '巽',    '黉',    '馘',    '冁',    '夔',    '勹',    '匍',    '訇',    '匐',    '凫',    '夙',    '兕',    '亠',    '兖',
        '亳',    '衮',    '袤',    '亵',    '脔',    '裒',    '禀',    '嬴',    '蠃',    '羸',    '冫',    '冱',    '冽',    '冼',    '凇',    '冖',
        '冢',    '冥',    '讠',    '讦',    '讧',    '讪',    '讴',    Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        
        //0x12**
        '讵',    '讷',    '诂',    '诃',    '诋',    '诏',    '诎',    '诒',    '诓',    '诔',    '诖',    '诘',    '诙',    '诜',    '诟',    '诠',
        '诤',    '诨',    '诩',    '诮',    '诰',    '诳',    '诶',    '诹',    '诼',    '诿',    '谀',    '谂',    '谄',    '谇',    '谌',    '谏',
        '谑',    '谒',    '谔',    '谕',    '谖',    '谙',    '谛',    '谘',    '谝',    '谟',    '谠',    '谡',    '谥',    '谧',    '谪',    '谫',
        '谮',    '谯',    '谲',    '谳',    '谵',    '谶',    '卩',    '卺',    '阝',    '阢',    '阡',    '阱',    '阪',    '阽',    '阼',    '陂',
        '陉',    '陔',    '陟',    '陧',    '陬',    '陲',    '陴',    '隈',    '隍',    '隗',    '隰',    '邗',    '邛',    '邝',    '邙',    '邬',
        '邡',    '邴',    '邳',    '邶',    '邺',    '邸',    '邰',    '郏',    '郅',    '邾',    '郐',    '郄',    '郇',    '郓',    '郦',    '郢',
        '郜',    '郗',    '郛',    '郫',    '郯',    '郾',    '鄄',    '鄢',    '鄞',    '鄣',    '鄱',    '鄯',    '鄹',    '酃',    '酆',    '刍',
        '奂',    '劢',    '劬',    '劭',    '劾',    '哿',    '勐',    '勖',    '勰',    '叟',    '燮',    '矍',    '廴',    '凵',    '凼',    '鬯',
        '厶',    '弁',    '畚',    '巯',    '坌',    '垩',    '垡',    '塾',    '墼',    '壅',    '壑',    '圩',    '圬',    '圪',    '圳',    '圹',
        '圮',    '圯',    '坜',    '圻',    '坂',    '坩',    '垅',    '坫',    '垆',    '坼',    '坻',    '坨',    '坭',    '坶',    '坳',    '垭',
        '垤',    '垌',    '垲',    '埏',    '垧',    '垴',    '垓',    '垠',    '埕',    '埘',    '埚',    '埙',    '埒',    '垸',    '埴',    '埯',
        '埸',    '埤',    '埝',    '堋',    '堍',    '埽',    '埭',    '堀',    '堞',    '堙',    '塄',    '堠',    '塥',    '塬',    '墁',    '墉',
        '墚',    '墀',    '馨',    '鼙',    '懿',    '艹',    '艽',    '艿',    '芏',    '芊',    '芨',    '芄',    '芎',    '芑',    '芗',    '芙',
        '芫',    '芸',    '芾',    '芰',    '苈',    '苊',    '苣',    '芘',    '芷',    '芮',    '苋',    '苌',    '苁',    '芩',    '芴',    '芡',
        '芪',    '芟',    '苄',    '苎',    '芤',    '苡',    '茉',    '苷',    '苤',    '茏',    '茇',    '苜',    '苴',    '苒',    '苘',    '茌',
        '苻',    '苓',    '茑',    '茚',    '茆',    '茔',    '茕',    Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        
        //0x13**
        '苠',    '苕',    '茜',    '荑',    '荛',    '荜',    '茈',    '莒',    '茼',    '茴',    '茱',    '莛',    '荞',    '茯',    '荏',    '荇',
        '荃',    '荟',    '荀',    '茗',    '荠',    '茭',    '茺',    '茳',    '荦',    '荥',    '荨',    '茛',    '荩',    '荬',    '荪',    '荭',
        '荮',    '莰',    '荸',    '莳',    '莴',    '莠',    '莪',    '莓',    '莜',    '莅',    '荼',    '莶',    '莩',    '荽',    '莸',    '荻',
        '莘',    '莞',    '莨',    '莺',    '莼',    '菁',    '萁',    '菥',    '菘',    '堇',    '萘',    '萋',    '菝',    '菽',    '菖',    '萜',
        '萸',    '萑',    '萆',    '菔',    '菟',    '萏',    '萃',    '菸',    '菹',    '菪',    '菅',    '菀',    '萦',    '菰',    '菡',    '葜',
        '葑',    '葚',    '葙',    '葳',    '蒇',    '蒈',    '葺',    '蒉',    '葸',    '萼',    '葆',    '葩',    '葶',    '蒌',    '蒎',    '萱',
        '葭',    '蓁',    '蓍',    '蓐',    '蓦',    '蒽',    '蓓',    '蓊',    '蒿',    '蒺',    '蓠',    '蒡',    '蒹',    '蒴',    '蒗',    '蓥',
        '蓣',    '蔌',    '甍',    '蔸',    '蓰',    '蔹',    '蔟',    '蔺',    '蕖',    '蔻',    '蓿',    '蓼',    '蕙',    '蕈',    '蕨',    '蕤',
        '蕞',    '蕺',    '瞢',    '蕃',    '蕲',    '蕻',    '薤',    '薨',    '薇',    '薏',    '蕹',    '薮',    '薜',    '薅',    '薹',    '薷',
        '薰',    '藓',    '藁',    '藜',    '藿',    '蘧',    '蘅',    '蘩',    '蘖',    '蘼',    '廾',    '弈',    '夼',    '奁',    '耷',    '奕',
        '奚',    '奘',    '匏',    '尢',    '尥',    '尬',    '尴',    '扌',    '扪',    '抟',    '抻',    '拊',    '拚',    '拗',    '拮',    '挢',
        '拶',    '挹',    '捋',    '捃',    '掭',    '揶',    '捱',    '捺',    '掎',    '掴',    '捭',    '掬',    '掊',    '捩',    '掮',    '掼',
        '揲',    '揸',    '揠',    '揿',    '揄',    '揞',    '揎',    '摒',    '揆',    '掾',    '摅',    '摁',    '搋',    '搛',    '搠',    '搌',
        '搦',    '搡',    '摞',    '撄',    '摭',    '撖',    '摺',    '撷',    '撸',    '撙',    '撺',    '擀',    '擐',    '擗',    '擤',    '擢',
        '攉',    '攥',    '攮',    '弋',    '忒',    '甙',    '弑',    '卟',    '叱',    '叽',    '叩',    '叨',    '叻',    '吒',    '吖',    '吆',
        '呋',    '呒',    '呓',    '呔',    '呖',    '呃',    '吡',    Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        
        //0x14**
        '呗',    '呙',    '吣',    '吲',    '咂',    '咔',    '呷',    '呱',    '呤',    '咚',    '咛',    '咄',    '呶',    '呦',    '咝',    '哐',
        '咭',    '哂',    '咴',    '哒',    '咧',    '咦',    '哓',    '哔',    '呲',    '咣',    '哕',    '咻',    '咿',    '哌',    '哙',    '哚',
        '哜',    '咩',    '咪',    '咤',    '哝',    '哏',    '哞',    '唛',    '哧',    '唠',    '哽',    '唔',    '哳',    '唢',    '唣',    '唏',
        '唑',    '唧',    '唪',    '啧',    '喏',    '喵',    '啉',    '啭',    '啁',    '啕',    '唿',    '啐',    '唼',    '唷',    '啖',    '啵',
        '啶',    '啷',    '唳',    '唰',    '啜',    '喋',    '嗒',    '喃',    '喱',    '喹',    '喈',    '喁',    '喟',    '啾',    '嗖',    '喑',
        '啻',    '嗟',    '喽',    '喾',    '喔',    '喙',    '嗪',    '嗷',    '嗉',    '嘟',    '嗑',    '嗫',    '嗬',    '嗔',    '嗦',    '嗝',
        '嗄',    '嗯',    '嗥',    '嗲',    '嗳',    '嗌',    '嗍',    '嗨',    '嗵',    '嗤',    '辔',    '嘞',    '嘈',    '嘌',    '嘁',    '嘤',
        '嘣',    '嗾',    '嘀',    '嘧',    '嘭',    '噘',    '嘹',    '噗',    '嘬',    '噍',    '噢',    '噙',    '噜',    '噌',    '噔',    '嚆',
        '噤',    '噱',    '噫',    '噻',    '噼',    '嚅',    '嚓',    '嚯',    '囔',    '囗',    '囝',    '囡',    '囵',    '囫',    '囹',    '囿',
        '圄',    '圊',    '圉',    '圜',    '帏',    '帙',    '帔',    '帑',    '帱',    '帻',    '帼',    '帷',    '幄',    '幔',    '幛',    '幞',
        '幡',    '岌',    '屺',    '岍',    '岐',    '岖',    '岈',    '岘',    '岙',    '岑',    '岚',    '岜',    '岵',    '岢',    '岽',    '岬',
        '岫',    '岱',    '岣',    '峁',    '岷',    '峄',    '峒',    '峤',    '峋',    '峥',    '崂',    '崃',    '崧',    '崦',    '崮',    '崤',
        '崞',    '崆',    '崛',    '嵘',    '崾',    '崴',    '崽',    '嵬',    '嵛',    '嵯',    '嵝',    '嵫',    '嵋',    '嵊',    '嵩',    '嵴',
        '嶂',    '嶙',    '嶝',    '豳',    '嶷',    '巅',    '彳',    '彷',    '徂',    '徇',    '徉',    '後',    '徕',    '徙',    '徜',    '徨',
        '徭',    '徵',    '徼',    '衢',    '彡',    '犭',    '犰',    '犴',    '犷',    '犸',    '狃',    '狁',    '狎',    '狍',    '狒',    '狨',
        '狯',    '狩',    '狲',    '狴',    '狷',    '猁',    '狳',    Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        
        //0x15**
        '猃',    '狺',    '狻',    '猗',    '猓',    '猡',    '猊',    '猞',    '猝',    '猕',    '猢',    '猹',    '猥',    '猬',    '猸',    '猱',
        '獐',    '獍',    '獗',    '獠',    '獬',    '獯',    '獾',    '舛',    '夥',    '飧',    '夤',    '夂',    '饣',    '饧',    '饨',    '饩',
        '饪',    '饫',    '饬',    '饴',    '饷',    '饽',    '馀',    '馄',    '馇',    '馊',    '馍',    '馐',    '馑',    '馓',    '馔',    '馕',
        '庀',    '庑',    '庋',    '庖',    '庥',    '庠',    '庹',    '庵',    '庾',    '庳',    '赓',    '廒',    '廑',    '廛',    '廨',    '廪',
        '膺',    '忄',    '忉',    '忖',    '忏',    '怃',    '忮',    '怄',    '忡',    '忤',    '忾',    '怅',    '怆',    '忪',    '忭',    '忸',
        '怙',    '怵',    '怦',    '怛',    '怏',    '怍',    '怩',    '怫',    '怊',    '怿',    '怡',    '恸',    '恹',    '恻',    '恺',    '恂',
        '恪',    '恽',    '悖',    '悚',    '悭',    '悝',    '悃',    '悒',    '悌',    '悛',    '惬',    '悻',    '悱',    '惝',    '惘',    '惆',
        '惚',    '悴',    '愠',    '愦',    '愕',    '愣',    '惴',    '愀',    '愎',    '愫',    '慊',    '慵',    '憬',    '憔',    '憧',    '憷',
        '懔',    '懵',    '忝',    '隳',    '闩',    '闫',    '闱',    '闳',    '闵',    '闶',    '闼',    '闾',    '阃',    '阄',    '阆',    '阈',
        '阊',    '阋',    '阌',    '阍',    '阏',    '阒',    '阕',    '阖',    '阗',    '阙',    '阚',    '丬',    '爿',    '戕',    '氵',    '汔',
        '汜',    '汊',    '沣',    '沅',    '沐',    '沔',    '沌',    '汨',    '汩',    '汴',    '汶',    '沆',    '沩',    '泐',    '泔',    '沭',
        '泷',    '泸',    '泱',    '泗',    '沲',    '泠',    '泖',    '泺',    '泫',    '泮',    '沱',    '泓',    '泯',    '泾',    '洹',    '洧',
        '洌',    '浃',    '浈',    '洇',    '洄',    '洙',    '洎',    '洫',    '浍',    '洮',    '洵',    '洚',    '浏',    '浒',    '浔',    '洳',
        '涑',    '浯',    '涞',    '涠',    '浞',    '涓',    '涔',    '浜',    '浠',    '浼',    '浣',    '渚',    '淇',    '淅',    '淞',    '渎',
        '涿',    '淠',    '渑',    '淦',    '淝',    '淙',    '渖',    '涫',    '渌',    '涮',    '渫',    '湮',    '湎',    '湫',    '溲',    '湟',
        '溆',    '湓',    '湔',    '渲',    '渥',    '湄',    '滟',    Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        
        //0x16**
        '溱',    '溘',    '滠',    '漭',    '滢',    '溥',    '溧',    '溽',    '溻',    '溷',    '滗',    '溴',    '滏',    '溏',    '滂',    '溟',
        '潢',    '潆',    '潇',    '漤',    '漕',    '滹',    '漯',    '漶',    '潋',    '潴',    '漪',    '漉',    '漩',    '澉',    '澍',    '澌',
        '潸',    '潲',    '潼',    '潺',    '濑',    '濉',    '澧',    '澹',    '澶',    '濂',    '濡',    '濮',    '濞',    '濠',    '濯',    '瀚',
        '瀣',    '瀛',    '瀹',    '瀵',    '灏',    '灞',    '宀',    '宄',    '宕',    '宓',    '宥',    '宸',    '甯',    '骞',    '搴',    '寤',
        '寮',    '褰',    '寰',    '蹇',    '謇',    '辶',    '迓',    '迕',    '迥',    '迮',    '迤',    '迩',    '迦',    '迳',    '迨',    '逅',
        '逄',    '逋',    '逦',    '逑',    '逍',    '逖',    '逡',    '逵',    '逶',    '逭',    '逯',    '遄',    '遑',    '遒',    '遐',    '遨',
        '遘',    '遢',    '遛',    '暹',    '遴',    '遽',    '邂',    '邈',    '邃',    '邋',    '彐',    '彗',    '彖',    '彘',    '尻',    '咫',
        '屐',    '屙',    '孱',    '屣',    '屦',    '羼',    '弪',    '弩',    '弭',    '艴',    '弼',    '鬻',    '屮',    '妁',    '妃',    '妍',
        '妩',    '妪',    '妣',    '妗',    '姊',    '妫',    '妞',    '妤',    '姒',    '妲',    '妯',    '姗',    '妾',    '娅',    '娆',    '姝',
        '娈',    '姣',    '姘',    '姹',    '娌',    '娉',    '娲',    '娴',    '娑',    '娣',    '娓',    '婀',    '婧',    '婊',    '婕',    '娼',
        '婢',    '婵',    '胬',    '媪',    '媛',    '婷',    '婺',    '媾',    '嫫',    '媲',    '嫒',    '嫔',    '媸',    '嫠',    '嫣',    '嫱',
        '嫖',    '嫦',    '嫘',    '嫜',    '嬉',    '嬗',    '嬖',    '嬲',    '嬷',    '孀',    '尕',    '尜',    '孚',    '孥',    '孳',    '孑',
        '孓',    '孢',    '驵',    '驷',    '驸',    '驺',    '驿',    '驽',    '骀',    '骁',    '骅',    '骈',    '骊',    '骐',    '骒',    '骓',
        '骖',    '骘',    '骛',    '骜',    '骝',    '骟',    '骠',    '骢',    '骣',    '骥',    '骧',    '纟',    '纡',    '纣',    '纥',    '纨',
        '纩',    '纭',    '纰',    '纾',    '绀',    '绁',    '绂',    '绉',    '绋',    '绌',    '绐',    '绔',    '绗',    '绛',    '绠',    '绡',
        '绨',    '绫',    '绮',    '绯',    '绱',    '绲',    '缍',    Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        
        //0x17**
        '绶',    '绺',    '绻',    '绾',    '缁',    '缂',    '缃',    '缇',    '缈',    '缋',    '缌',    '缏',    '缑',    '缒',    '缗',    '缙',
        '缜',    '缛',    '缟',    '缡',    '缢',    '缣',    '缤',    '缥',    '缦',    '缧',    '缪',    '缫',    '缬',    '缭',    '缯',    '缰',
        '缱',    '缲',    '缳',    '缵',    '幺',    '畿',    '巛',    '甾',    '邕',    '玎',    '玑',    '玮',    '玢',    '玟',    '珏',    '珂',
        '珑',    '玷',    '玳',    '珀',    '珉',    '珈',    '珥',    '珙',    '顼',    '琊',    '珩',    '珧',    '珞',    '玺',    '珲',    '琏',
        '琪',    '瑛',    '琦',    '琥',    '琨',    '琰',    '琮',    '琬',    '琛',    '琚',    '瑁',    '瑜',    '瑗',    '瑕',    '瑙',    '瑷',
        '瑭',    '瑾',    '璜',    '璎',    '璀',    '璁',    '璇',    '璋',    '璞',    '璨',    '璩',    '璐',    '璧',    '瓒',    '璺',    '韪',
        '韫',    '韬',    '杌',    '杓',    '杞',    '杈',    '杩',    '枥',    '枇',    '杪',    '杳',    '枘',    '枧',    '杵',    '枨',    '枞',
        '枭',    '枋',    '杷',    '杼',    '柰',    '栉',    '柘',    '栊',    '柩',    '枰',    '栌',    '柙',    '枵',    '柚',    '枳',    '柝',
        '栀',    '柃',    '枸',    '柢',    '栎',    '柁',    '柽',    '栲',    '栳',    '桠',    '桡',    '桎',    '桢',    '桄',    '桤',    '梃',
        '栝',    '桕',    '桦',    '桁',    '桧',    '桀',    '栾',    '桊',    '桉',    '栩',    '梵',    '梏',    '桴',    '桷',    '梓',    '桫',
        '棂',    '楮',    '棼',    '椟',    '椠',    '棹',    '椤',    '棰',    '椋',    '椁',    '楗',    '棣',    '椐',    '楱',    '椹',    '楠',
        '楂',    '楝',    '榄',    '楫',    '榀',    '榘',    '楸',    '椴',    '槌',    '榇',    '榈',    '槎',    '榉',    '楦',    '楣',    '楹',
        '榛',    '榧',    '榻',    '榫',    '榭',    '槔',    '榱',    '槁',    '槊',    '槟',    '榕',    '槠',    '榍',    '槿',    '樯',    '槭',
        '樗',    '樘',    '橥',    '槲',    '橄',    '樾',    '檠',    '橐',    '橛',    '樵',    '檎',    '橹',    '樽',    '樨',    '橘',    '橼',
        '檑',    '檐',    '檩',    '檗',    '檫',    '猷',    '獒',    '殁',    '殂',    '殇',    '殄',    '殒',    '殓',    '殍',    '殚',    '殛',
        '殡',    '殪',    '轫',    '轭',    '轱',    '轲',    '轳',    Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        
        //0x18**
        '轵',    '轶',    '轸',    '轷',    '轹',    '轺',    '轼',    '轾',    '辁',    '辂',    '辄',    '辇',    '辋',    '辍',    '辎',    '辏',
        '辘',    '辚',    '軎',    '戋',    '戗',    '戛',    '戟',    '戢',    '戡',    '戥',    '戤',    '戬',    '臧',    '瓯',    '瓴',    '瓿',
        '甏',    '甑',    '甓',    '攴',    '旮',    '旯',    '旰',    '昊',    '昙',    '杲',    '昃',    '昕',    '昀',    '炅',    '曷',    '昝',
        '昴',    '昱',    '昶',    '昵',    '耆',    '晟',    '晔',    '晁',    '晏',    '晖',    '晡',    '晗',    '晷',    '暄',    '暌',    '暧',
        '暝',    '暾',    '曛',    '曜',    '曦',    '曩',    '贲',    '贳',    '贶',    '贻',    '贽',    '赀',    '赅',    '赆',    '赈',    '赉',
        '赇',    '赍',    '赕',    '赙',    '觇',    '觊',    '觋',    '觌',    '觎',    '觏',    '觐',    '觑',    '牮',    '犟',    '牝',    '牦',
        '牯',    '牾',    '牿',    '犄',    '犋',    '犍',    '犏',    '犒',    '挈',    '挲',    '掰',    '搿',    '擘',    '耄',    '毪',    '毳',
        '毽',    '毵',    '毹',    '氅',    '氇',    '氆',    '氍',    '氕',    '氘',    '氙',    '氚',    '氡',    '氩',    '氤',    '氪',    '氲',
        '攵',    '敕',    '敫',    '牍',    '牒',    '牖',    '爰',    '虢',    '刖',    '肟',    '肜',    '肓',    '肼',    '朊',    '肽',    '肱',
        '肫',    '肭',    '肴',    '肷',    '胧',    '胨',    '胩',    '胪',    '胛',    '胂',    '胄',    '胙',    '胍',    '胗',    '朐',    '胝',
        '胫',    '胱',    '胴',    '胭',    '脍',    '脎',    '胲',    '胼',    '朕',    '脒',    '豚',    '脶',    '脞',    '脬',    '脘',    '脲',
        '腈',    '腌',    '腓',    '腴',    '腙',    '腚',    '腱',    '腠',    '腩',    '腼',    '腽',    '腭',    '腧',    '塍',    '媵',    '膈',
        '膂',    '膑',    '滕',    '膣',    '膪',    '臌',    '朦',    '臊',    '膻',    '臁',    '膦',    '欤',    '欷',    '欹',    '歃',    '歆',
        '歙',    '飑',    '飒',    '飓',    '飕',    '飙',    '飚',    '殳',    '彀',    '毂',    '觳',    '斐',    '齑',    '斓',    '於',    '旆',
        '旄',    '旃',    '旌',    '旎',    '旒',    '旖',    '炀',    '炜',    '炖',    '炝',    '炻',    '烀',    '炷',    '炫',    '炱',    '烨',
        '烊',    '焐',    '焓',    '焖',    '焯',    '焱',    '煳',    Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        
        //0x19**
        '煜',    '煨',    '煅',    '煲',    '煊',    '煸',    '煺',    '熘',    '熳',    '熵',    '熨',    '熠',    '燠',    '燔',    '燧',    '燹',
        '爝',    '爨',    '灬',    '焘',    '煦',    '熹',    '戾',    '戽',    '扃',    '扈',    '扉',    '礻',    '祀',    '祆',    '祉',    '祛',
        '祜',    '祓',    '祚',    '祢',    '祗',    '祠',    '祯',    '祧',    '祺',    '禅',    '禊',    '禚',    '禧',    '禳',    '忑',    '忐',
        '怼',    '恝',    '恚',    '恧',    '恁',    '恙',    '恣',    '悫',    '愆',    '愍',    '慝',    '憩',    '憝',    '懋',    '懑',    '戆',
        '肀',    '聿',    '沓',    '泶',    '淼',    '矶',    '矸',    '砀',    '砉',    '砗',    '砘',    '砑',    '斫',    '砭',    '砜',    '砝',
        '砹',    '砺',    '砻',    '砟',    '砼',    '砥',    '砬',    '砣',    '砩',    '硎',    '硭',    '硖',    '硗',    '砦',    '硐',    '硇',
        '硌',    '硪',    '碛',    '碓',    '碚',    '碇',    '碜',    '碡',    '碣',    '碲',    '碹',    '碥',    '磔',    '磙',    '磉',    '磬',
        '磲',    '礅',    '磴',    '礓',    '礤',    '礞',    '礴',    '龛',    '黹',    '黻',    '黼',    '盱',    '眄',    '眍',    '盹',    '眇',
        '眈',    '眚',    '眢',    '眙',    '眭',    '眦',    '眵',    '眸',    '睐',    '睑',    '睇',    '睃',    '睚',    '睨',    '睢',    '睥',
        '睿',    '瞍',    '睽',    '瞀',    '瞌',    '瞑',    '瞟',    '瞠',    '瞰',    '瞵',    '瞽',    '町',    '畀',    '畎',    '畋',    '畈',
        '畛',    '畲',    '畹',    '疃',    '罘',    '罡',    '罟',    '詈',    '罨',    '罴',    '罱',    '罹',    '羁',    '罾',    '盍',    '盥',
        '蠲',    '钅',    '钆',    '钇',    '钋',    '钊',    '钌',    '钍',    '钏',    '钐',    '钔',    '钗',    '钕',    '钚',    '钛',    '钜',
        '钣',    '钤',    '钫',    '钪',    '钭',    '钬',    '钯',    '钰',    '钲',    '钴',    '钶',    '钷',    '钸',    '钹',    '钺',    '钼',
        '钽',    '钿',    '铄',    '铈',    '铉',    '铊',    '铋',    '铌',    '铍',    '铎',    '铐',    '铑',    '铒',    '铕',    '铖',    '铗',
        '铙',    '铘',    '铛',    '铞',    '铟',    '铠',    '铢',    '铤',    '铥',    '铧',    '铨',    '铪',    '铩',    '铫',    '铮',    '铯',
        '铳',    '铴',    '铵',    '铷',    '铹',    '铼',    '铽',    Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        
        //0x1A**
        '铿',    '锃',    '锂',    '锆',    '锇',    '锉',    '锊',    '锍',    '锎',    '锏',    '锒',    '锓',    '锔',    '锕',    '锖',    '锘',
        '锛',    '锝',    '锞',    '锟',    '锢',    '锪',    '锫',    '锩',    '锬',    '锱',    '锲',    '锴',    '锶',    '锷',    '锸',    '锼',
        '锾',    '锿',    '镂',    '锵',    '镄',    '镅',    '镆',    '镉',    '镌',    '镎',    '镏',    '镒',    '镓',    '镔',    '镖',    '镗',
        '镘',    '镙',    '镛',    '镞',    '镟',    '镝',    '镡',    '镢',    '镤',    '镥',    '镦',    '镧',    '镨',    '镩',    '镪',    '镫',
        '镬',    '镯',    '镱',    '镲',    '镳',    '锺',    '矧',    '矬',    '雉',    '秕',    '秭',    '秣',    '秫',    '稆',    '嵇',    '稃',
        '稂',    '稞',    '稔',    '稹',    '稷',    '穑',    '黏',    '馥',    '穰',    '皈',    '皎',    '皓',    '皙',    '皤',    '瓞',    '瓠',
        '甬',    '鸠',    '鸢',    '鸨',    '鸩',    '鸪',    '鸫',    '鸬',    '鸲',    '鸱',    '鸶',    '鸸',    '鸷',    '鸹',    '鸺',    '鸾',
        '鹁',    '鹂',    '鹄',    '鹆',    '鹇',    '鹈',    '鹉',    '鹋',    '鹌',    '鹎',    '鹑',    '鹕',    '鹗',    '鹚',    '鹛',    '鹜',
        '鹞',    '鹣',    '鹦',    '鹧',    '鹨',    '鹩',    '鹪',    '鹫',    '鹬',    '鹱',    '鹭',    '鹳',    '疒',    '疔',    '疖',    '疠',
        '疝',    '疬',    '疣',    '疳',    '疴',    '疸',    '痄',    '疱',    '疰',    '痃',    '痂',    '痖',    '痍',    '痣',    '痨',    '痦',
        '痤',    '痫',    '痧',    '瘃',    '痱',    '痼',    '痿',    '瘐',    '瘀',    '瘅',    '瘌',    '瘗',    '瘊',    '瘥',    '瘘',    '瘕',
        '瘙',    '瘛',    '瘼',    '瘢',    '瘠',    '癀',    '瘭',    '瘰',    '瘿',    '瘵',    '癃',    '瘾',    '瘳',    '癍',    '癞',    '癔',
        '癜',    '癖',    '癫',    '癯',    '翊',    '竦',    '穸',    '穹',    '窀',    '窆',    '窈',    '窕',    '窦',    '窠',    '窬',    '窨',
        '窭',    '窳',    '衤',    '衩',    '衲',    '衽',    '衿',    '袂',    '袢',    '裆',    '袷',    '袼',    '裉',    '裢',    '裎',    '裣',
        '裥',    '裱',    '褚',    '裼',    '裨',    '裾',    '裰',    '褡',    '褙',    '褓',    '褛',    '褊',    '褴',    '褫',    '褶',    '襁',
        '襦',    '襻',    '疋',    '胥',    '皲',    '皴',    '矜',    Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        
        //0x1B**
        Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        
        //0x1C**
        '耒',    '耔',    '耖',    '耜',    '耠',    '耢',    '耥',    '耦',    '耧',    '耩',    '耨',    '耱',    '耋',    '耵',    '聃',    '聆',
        '聍',    '聒',    '聩',    '聱',    '覃',    '顸',    '颀',    '颃',    '颉',    '颌',    '颍',    '颏',    '颔',    '颚',    '颛',    '颞',
        '颟',    '颡',    '颢',    '颥',    '颦',    '虍',    '虔',    '虬',    '虮',    '虿',    '虺',    '虼',    '虻',    '蚨',    '蚍',    '蚋',
        '蚬',    '蚝',    '蚧',    '蚣',    '蚪',    '蚓',    '蚩',    '蚶',    '蛄',    '蚵',    '蛎',    '蚰',    '蚺',    '蚱',    '蚯',    '蛉',
        '蛏',    '蚴',    '蛩',    '蛱',    '蛲',    '蛭',    '蛳',    '蛐',    '蜓',    '蛞',    '蛴',    '蛟',    '蛘',    '蛑',    '蜃',    '蜇',
        '蛸',    '蜈',    '蜊',    '蜍',    '蜉',    '蜣',    '蜻',    '蜞',    '蜥',    '蜮',    '蜚',    '蜾',    '蝈',    '蜴',    '蜱',    '蜩',
        '蜷',    '蜿',    '螂',    '蜢',    '蝽',    '蝾',    '蝻',    '蝠',    '蝰',    '蝌',    '蝮',    '螋',    '蝓',    '蝣',    '蝼',    '蝤',
        '蝙',    '蝥',    '螓',    '螯',    '螨',    '蟒',    '蟆',    '螈',    '螅',    '螭',    '螗',    '螃',    '螫',    '蟥',    '螬',    '螵',
        '螳',    '蟋',    '蟓',    '螽',    '蟑',    '蟀',    '蟊',    '蟛',    '蟪',    '蟠',    '蟮',    '蠖',    '蠓',    '蟾',    '蠊',    '蠛',
        '蠡',    '蠹',    '蠼',    '缶',    '罂',    '罄',    '罅',    '舐',    '竺',    '竽',    '笈',    '笃',    '笄',    '笕',    '笊',    '笫',
        '笏',    '筇',    '笸',    '笪',    '笙',    '笮',    '笱',    '笠',    '笥',    '笤',    '笳',    '笾',    '笞',    '筘',    '筚',    '筅',
        '筵',    '筌',    '筝',    '筠',    '筮',    '筻',    '筢',    '筲',    '筱',    '箐',    '箦',    '箧',    '箸',    '箬',    '箝',    '箨',
        '箅',    '箪',    '箜',    '箢',    '箫',    '箴',    '篑',    '篁',    '篌',    '篝',    '篚',    '篥',    '篦',    '篪',    '簌',    '篾',
        '篼',    '簏',    '簖',    '簋',    '簟',    '簪',    '簦',    '簸',    '籁',    '籀',    '臾',    '舁',    '舂',    '舄',    '臬',    '衄',
        '舡',    '舢',    '舣',    '舭',    '舯',    '舨',    '舫',    '舸',    '舻',    '舳',    '舴',    '舾',    '艄',    '艉',    '艋',    '艏',
        '艚',    '艟',    '艨',    '衾',    '袅',    '袈',    '裘',    Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        
        //0x1D**
        '裟',    '襞',    '羝',    '羟',    '羧',    '羯',    '羰',    '羲',    '籼',    '敉',    '粑',    '粝',    '粜',    '粞',    '粢',    '粲',
        '粼',    '粽',    '糁',    '糇',    '糌',    '糍',    '糈',    '糅',    '糗',    '糨',    '艮',    '暨',    '羿',    '翎',    '翕',    '翥',
        '翡',    '翦',    '翩',    '翮',    '翳',    '糸',    '絷',    '綦',    '綮',    '繇',    '纛',    '麸',    '麴',    '赳',    '趄',    '趔',
        '趑',    '趱',    '赧',    '赭',    '豇',    '豉',    '酊',    '酐',    '酎',    '酏',    '酤',    '酢',    '酡',    '酰',    '酩',    '酯',
        '酽',    '酾',    '酲',    '酴',    '酹',    '醌',    '醅',    '醐',    '醍',    '醑',    '醢',    '醣',    '醪',    '醭',    '醮',    '醯',
        '醵',    '醴',    '醺',    '豕',    '鹾',    '趸',    '跫',    '踅',    '蹙',    '蹩',    '趵',    '趿',    '趼',    '趺',    '跄',    '跖',
        '跗',    '跚',    '跞',    '跎',    '跏',    '跛',    '跆',    '跬',    '跷',    '跸',    '跣',    '跹',    '跻',    '跤',    '踉',    '跽',
        '踔',    '踝',    '踟',    '踬',    '踮',    '踣',    '踯',    '踺',    '蹀',    '踹',    '踵',    '踽',    '踱',    '蹉',    '蹁',    '蹂',
        '蹑',    '蹒',    '蹊',    '蹰',    '蹶',    '蹼',    '蹯',    '蹴',    '躅',    '躏',    '躔',    '躐',    '躜',    '躞',    '豸',    '貂',
        '貊',    '貅',    '貘',    '貔',    '斛',    '觖',    '觞',    '觚',    '觜',    '觥',    '觫',    '觯',    '訾',    '謦',    '靓',    '雩',
        '雳',    '雯',    '霆',    '霁',    '霈',    '霏',    '霎',    '霪',    '霭',    '霰',    '霾',    '龀',    '龃',    '龅',    '龆',    '龇',
        '龈',    '龉',    '龊',    '龌',    '黾',    '鼋',    '鼍',    '隹',    '隼',    '隽',    '雎',    '雒',    '瞿',    '雠',    '銎',    '銮',
        '鋈',    '錾',    '鍪',    '鏊',    '鎏',    '鐾',    '鑫',    '鱿',    '鲂',    '鲅',    '鲆',    '鲇',    '鲈',    '稣',    '鲋',    '鲎',
        '鲐',    '鲑',    '鲒',    '鲔',    '鲕',    '鲚',    '鲛',    '鲞',    '鲟',    '鲠',    '鲡',    '鲢',    '鲣',    '鲥',    '鲦',    '鲧',
        '鲨',    '鲩',    '鲫',    '鲭',    '鲮',    '鲰',    '鲱',    '鲲',    '鲳',    '鲴',    '鲵',    '鲶',    '鲷',    '鲺',    '鲻',    '鲼',
        '鲽',    '鳄',    '鳅',    '鳆',    '鳇',    '鳊',    '鳋',    Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        
        //0x1E**
        '鳌',    '鳍',    '鳎',    '鳏',    '鳐',    '鳓',    '鳔',    '鳕',    '鳗',    '鳘',    '鳙',    '鳜',    '鳝',    '鳟',    '鳢',    '靼',
        '鞅',    '鞑',    '鞒',    '鞔',    '鞯',    '鞫',    '鞣',    '鞲',    '鞴',    '骱',    '骰',    '骷',    '鹘',    '骶',    '骺',    '骼',
        '髁',    '髀',    '髅',    '髂',    '髋',    '髌',    '髑',    '魅',    '魃',    '魇',    '魉',    '魈',    '魍',    '魑',    '飨',    '餍',
        '餮',    '饕',    '饔',    '髟',    '髡',    '髦',    '髯',    '髫',    '髻',    '髭',    '髹',    '鬈',    '鬏',    '鬓',    '鬟',    '鬣',
        '麽',    '麾',    '縻',    '麂',    '麇',    '麈',    '麋',    '麒',    '鏖',    '麝',    '麟',    '黛',    '黜',    '黝',    '黠',    '黟',
        '黢',    '黩',    '黧',    '黥',    '黪',    '黯',    '鼢',    '鼬',    '鼯',    '鼹',    '鼷',    '鼽',    '鼾',    '齄',    '祐',    '咲',
        '冴',    '広',    Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        'Ａ',    'Ｂ',    'Ｃ',    'Ｄ',    'Ｅ',    'Ｆ',    'Ｇ',    'Ｈ',    'Ｉ',    'Ｊ',    'Ｋ',    'Ｌ',    'Ｍ',    'Ｎ',    'Ｏ',    'Ｐ',
        'Ｑ',    'Ｒ',    'Ｓ',    'Ｔ',    'Ｕ',    'Ｖ',    'Ｗ',    'Ｘ',    'Ｙ',    'Ｚ',    'ａ',    'ｂ',    'ｃ',    'ｄ',    'ｅ',    'ｆ',
        'ｇ',    'ｈ',    'ｉ',    'ｊ',    'ｋ',    'ｌ',    'ｍ',    'ｎ',    'ｏ',    'ｐ',    'ｑ',    'ｒ',    'ｓ',    'ｔ',    'ｕ',    'ｖ',
        'ｗ',    'ｘ',    'ｙ',    'ｚ',    '０',    '１',    '２',    '３',    '４',    '５',    '６',    '７',    '８',    '９',    'Ⅰ',    'Ⅱ',
        'Ⅲ',    'Ⅳ',    'Ⅴ',    'Ⅵ',    'Ⅶ',    'Ⅷ',    'Ⅸ',    'Ⅹ',    'Ⅺ',    'Ⅻ',    '，',    '、',    '：',    '；',    '。',    '！',
        '？',    '•',    '～',    '—', Terminator, Terminator, '（',    '）',    '【',    '】',    '《',    '》', Terminator, Terminator, Terminator, Terminator,
        Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator, Terminator,
        //祐、咲、冴、広，及全角英文大小写、罗马数字、部分中文符号为新版官译汉化增加部分，不适用于12版汉化
    };

}
