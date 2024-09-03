# 各版本修改内容：
## For_Gen1_CHN(en)_Y:
### \PKHeX.Core\PKM\Shared\GBPKML.cs
函数 protected override void GetNonNickname(int language, Span<byte> data)
-       if (Korean)
+       //if (Korean)
此程序会将0xF2转码为0xE8，由于ckn版皮卡丘美版汉化编码不兼容原编码，故直接跳过该判定。
### \PKHeX.Core\PKM\Strings\StringConverter12.cs
汉字识别及输入的主程序部分

## For_Gen1+2+3_CHN(en)_RGB_C_RSEFRLG:
### \PKHeX.Core\PKM\Shared\GBPKML.cs
函数 protected override void GetNonNickname(int language, Span<byte> data)
-       foreach (ref var c in data)
-       {
-           if (c == 0xF2)
-               c = 0xE8;
-       }

+       for (int i = 0; i < data.Length; i++)
+       {
+           var c = data[i];
+           if(!Japanese && c >= 0x01 && c <= 0x2E && c != 0x14 && c != 0x15 && c != 0x16 && c != 0x17 && c != 0x20 && c != 0x21 && c != 0x22 && c != 0x23 && c != 0x24 && c != 0x25 && c != 0x26 && c != 0x27)
+           {
+               i = i + 2;
+               c = data[i];
+           }
+           if (c == 0xF2)
+               c = 0xE8;
+       }
新1~2代美版汉化编码兼容原编码，在保留原转码程序基础上，添加对汉字编码的识别跳过。
### \PKHeX.Core\PKM\PK2.cs
-   public override bool Korean => !Japanese && OT_Trash[0] <= 0xB;
+   public override bool Korean => false;
新1~2代美版汉化编码与韩版编码存在重叠，会被识别为韩版，直接禁用韩版识别。
### \PKHeX.Core\PKM\Strings\StringConverter12.cs
汉字识别及输入的主程序部分
### \PKHeX.Core\PKM\Strings\StringConverter3.cs
汉字识别及输入的主程序部分

## For_Gen2_CHN(ko)_GS:
### \PKHeX.Core\PKM\PK2.cs
-   public override bool Korean => !Japanese && OT_Trash[0] <= 0xB;
+   public override bool Korean => !Japanese;
正常情况韩版无法出现同时存在单双混合字节的昵称，会对来源语言判定存在混乱（海外及韩版），为方便测试输入，强制改为始终为韩版（海外与韩版判定）。
### \PKHeX.Core\PKM\Strings\StringConverter2KOR.cs
汉字识别及输入的主程序部分

## For_Gen4_CHN(jp)_DP:
### \PKM\Strings\StringConverter4Util.cs
汉字识别及输入的主程序部分


# 通用更改：
## \PKHeX.Core\PKM\Strings\StringConverter.cs
函数 public static bool HasEastAsianScriptCharacters(ReadOnlySpan<char> str)
            if (c is >= '\u4E00' and <= '\u9FFF')
-               return true;
+               return false;
用于检测是否是CJK中日韩汉字字符，非官中世代无法输入中文。回避该判定，以便输入中文。

## \PKHeX.Core\Resources\text\other\
en\text_Species_en.txt
ja\text_Species_ja.txt
ko\text_Species_ko.txt
默认的宝可梦名（用于判别是否是原名）
en为1~3代美版汉化使用
jp为4代yyjoy的dp汉化使用
ko为2代金银韩版汉化使用

## \PKHeX.Core\Util\UpdateUtil.cs
函数 public static Version? GetLatestPKHeXVersion()
-       const string apiEndpoint = "https://api.github.com/repos/kwsch/pkhex/releases/latest";
+       const string apiEndpoint = "https://api.github.com/repos/wokann/pkhex/releases/latest";
用于检测版本是否更新的地址重定向

## \PKHeX.WinForms\MainWindow\Main.cs
-     private const string ThreadPath = "https://projectpokemon.org/pkhex/";
+     private const string ThreadPath = "https://projectpokemon.org/pkhex/";
跳转新版本发布的地址重定向，博客页面待建设中