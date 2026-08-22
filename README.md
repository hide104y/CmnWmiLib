# CmnWmiLib

## 事前作業
1. .NET SDKがインストールされていない場合はインストール：winget install -e --id Microsoft.DotNet.SDK.10
1. Github CLIがインストールされていない場合はインストール：winget install -e --id GitHub.cli
1. Powershellプロンプトを開く

## リポジトリ作成（未作成の場合）
```shell
# サインイン状態の確認
gh auth status
# 初回サインインしていない場合はサインイン
gh auth login
# 削除権限付与
gh auth refresh -h github.com -s delete_repo
# 作成
gh repo create CmnWmiLib --private
# 確認
gh repo list | Select-String CmnWmiLib
```

## リモートリポジトリ（mainブランチ）の取得
```shell
# CD
cd D:\Github\Projects
# フォルダが存在する場合は削除
if (Test-Path -Path .\CmnWmiLib){rmdir .\CmnWmiLib}
# クローン実行
git clone https://github.com/hide104y/CmnWmiLib.git
```

## リモートリポジトリ（mainブランチ）にREADME.mdが存在しない場合
```shell
# CD
cd D:\Github\Projects\CmnWmiLib
# ファイル作成
ruby -e "File.write('README.md', '# CmnWmiLib', encoding: 'UTF-8')"
# コミット
git add README.md
git commit -m "add README.md"
# プッシュ
git push -u origin main
# ブランチの一覧表示
git branch -a
```

## ブランチの作成
```shell
# ブランチをmainに切り替え・復元
git checkout main
# ブランチ作成
git checkout -b dotnet10
# 作成したブランチをリモートにプッシュ
git push -u origin dotnet10
```

## プロジェクトの作成
```shell
# クラスライブラリ：.net 10.0
cd D:\Github\Projects\CmnWmiLib
dotnet new classlib --framework net10.0 -o CmnWmiLib
```

## ソリューションファイルの作成
.\CmnWmiLib\CmnWmiLib.slnx
```xml
<Solution>
  <Project Path="CmnWmiLib/CmnWmiLib.csproj" />
  <Project Path="TestProject1/TestProject1.csproj" />
</Solution>
```

## プロジェクトファイルの修正
.\CmnWmiLib\CmnWmiLib\CmnWmiLib.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <InvariantGlobalization>false</InvariantGlobalization>
    <AssemblyVersion>1.0.0.0</AssemblyVersion>
    <FileVersion>1.0.0.0</FileVersion>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\CmnClsLib\CmnClsLib\CmnClsLib.csproj" />
    <PackageReference Include="System.Management" Version="10.0.11" />
    <FluentValidationExcludedCultures Include="ar;el;he;hi;no;ro;sk;be;cs;cs-CZ;da;de;es;fa;fi;fr;it;ko;mk;nl;pl;pt;ru;sv;tr;uk;zh-CN;zh-CHS;zh-CHT;zh;zh-Hans;zh-Hant;pt-BR;">
      <InProject>false</InProject>
    </FluentValidationExcludedCultures>
  </ItemGroup>

  <Target Name="RemoveTranslationsAfterBuild" AfterTargets="AfterBuild">
    <RemoveDir Directories="@(FluentValidationExcludedCultures->'$(OutputPath)%(Filename)')" />
  </Target>

</Project>
```

## 依存パッケージ
```shell
# CD
cd D:\Github\Projects
# 依存プロジェクト参照の追加
dotnet add .\FsMkShortCut\FsMkShortCut\FsMkShortCut.csproj reference .\CmnClsLib\CmnClsLib\CmnClsLib.csproj
# 依存パッケージのインストール
dotnet add .\FsMkShortCut\FsMkShortCut\FsMkShortCut.csproj package System.Management --version 10.0.11
```

## コーディング
(省略)

## AIレビュー
```shell
# CD
cd D:\Github\Projects
agy
.\CmnWmiLib\CmnWmiLib\Class\ClsIniFile.csに対して、スキル「source-review」を実行して
/clear
.\CmnWmiLib\CmnWmiLib\Class\ClsEventLog.csに対して、スキル「source-review」を実行して
/exit
```

## ビルド
```shell
# CD
cd D:\Github\Projects
# ビルド
dotnet build .\CmnWmiLib\CmnWmiLib.slnx -c Release -p:InvariantGlobalization=false
dotnet build .\CmnWmiLib\TestProject1\TestProject1.csproj
# 単体テスト
dotnet test .\CmnWmiLib\TestProject1\TestProject1.csproj
```

## リポジトリにコミット
```shell
cd D:\Github\Projects\CmnWmiLib
git switch dotnet10
git add .
git commit -m "README.mdの修正"
git push -u origin dotnet10
```

## デプロイ
```shell
dotnet publish .\CmnWmiLib\CmnWmiLib\CmnWmiLib.csproj -c Release -o D:\Github\bin.n10 -r win-x64 --self-contained=false -p:PublishSingleFile=false -p:PublishReadyToRun=false -p:PublishTrimmed=false -p:PublishAot=false -p:InvariantGlobalization=false
```

## リモートリポジトリの確認
- https://github.com/hide104y/CmnWmiLib/tree/dotnet10
<br>※GitHubの画面で「Compare & pull request」が表示されるが放置

## リモートリポジトリ（dotnet10ブランチ）の取得
```shell
# CD
cd D:\Github\Projects
# フォルダが存在する場合は削除
if (Test-Path -Path .\CmnWmiLib){rmdir .\CmnWmiLib}
# クローン実行
git clone -b dotnet10 https://github.com/hide104y/CmnWmiLib.git
```

## License
- These codes are licensed under CC0.
- http://creativecommons.org/publicdomain/zero/1.0/deed.ja
