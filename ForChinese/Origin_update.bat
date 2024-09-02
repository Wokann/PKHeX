cd /d %~dp0
rmdir /s /q ".\Origin_update"
mkdir ".\Origin_update"
xcopy "..\PKHeX.Core\PKM\PK2.cs" ".\Origin_update\PKHeX.Core\PKM\PK2.cs" /s /e /y
xcopy "..\PKHeX.Core\PKM\Shared\GBPKML.cs" ".\Origin_update\PKHeX.Core\PKM\Shared\GBPKML.cs" /s /e /y
xcopy "..\PKHeX.Core\PKM\Strings\StringConverter.cs" ".\Origin_update\PKHeX.Core\PKM\Strings\StringConverter.cs" /s /e /y
xcopy "..\PKHeX.Core\PKM\Strings\StringConverter12.cs" ".\Origin_update\PKHeX.Core\PKM\Strings\StringConverter12.cs" /s /e /y
xcopy "..\PKHeX.Core\PKM\Strings\StringConverter2KOR.cs" ".\Origin_update\PKHeX.Core\PKM\Strings\StringConverter2KOR.cs" /s /e /y
xcopy "..\PKHeX.Core\PKM\Strings\StringConverter3.cs" ".\Origin_update\PKHeX.Core\PKM\Strings\StringConverter3.cs" /s /e /y
xcopy "..\PKHeX.Core\PKM\Strings\StringConverter4Util.cs" ".\Origin_update\PKHeX.Core\PKM\Strings\StringConverter4Util.cs" /s /e /y
xcopy "..\PKHeX.Core\Resources\text\other\en\text_Species_en.txt" ".\Origin_update\PKHeX.Core\Resources\text\other\en\text_Species_en.txt" /s /e /y
xcopy "..\PKHeX.Core\Resources\text\other\ja\text_Species_ja.txt" ".\Origin_update\PKHeX.Core\Resources\text\other\ja\text_Species_ja.txt" /s /e /y
xcopy "..\PKHeX.Core\Resources\text\other\ko\text_Species_ko.txt" ".\Origin_update\PKHeX.Core\Resources\text\other\ko\text_Species_ko.txt" /s /e /y
xcopy "..\PKHeX.Core\Util\UpdateUtil.cs" ".\Origin_update\PKHeX.Core\Util\UpdateUtil.cs" /s /e /y
xcopy "..\PKHeX.WinForms\MainWindow\Main.cs" ".\Origin_update\PKHeX.WinForms\MainWindow\Main.cs" /s /e /y
pause