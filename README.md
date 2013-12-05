#SunokoLibrary.Web.GooglePlus 2.0

##Unofficial Google+API Lib for .NET

このライブラリはGoogle+の非公式APIを.NETアプリから使用するためのライブラリです。PortableClassLibraryとして作られており、.NET4.5/WP8/Windows Store Appで動かせるように作っています。  

G+非公式APIとの通信をC#上から扱いやすい形で行うライブラリを作ってみるという方針のもと、作者が興味を持ったライブラリなどを活用しながら作っています。ソースコードはVS2012/C#5.0で作っています。  

------------------------
 ライセンス
------------------------
このライブラリは修正BSDライセンスの下にあります。  
Copyright (C) 2012 namoshika  

## この成果物は以下のものが使用されています。
* [Reactive Extensions](http://nuget.org/packages/Rx-Main/)  
* [Json.NET](http://nuget.org/packages/Newtonsoft.Json/)  
Copyright (c) 2007 James Newton-King  
licensed under MIT License (MIT)  

## Unofficial Google+APIの呼び出しで一部拝借しています。
* [GooglePlusLibrary.NET](https://github.com/gplusnasite/GooglePlusLibrary.NET)  
Copyright (c) 2011-2013 Mori Satoshi, All Rights Reserved.

------------------------
 ビルド方法
------------------------
NuGetのインストール済みを想定。  

1. (ライブラリの.csprojを初めて開く際に ".\packages\Microsoft.Bcl.Build.1.0.7\tools\Microsoft.Bcl.Build.targets" が見つからずエラーが発生する事を確認。その場合は以下の手順で必要パッケージのダウンロードを行なってください)  
2. NuGetパッケージの復元を有効化  
3. 追加されたファイルを使用し、コンソール上で参照パッケージをインストール  
> SolutionFolder>.\.nuget\NuGet.exe install -o packages SnkLib_GooglePlus\packages.config
4. プロジェクトをソリューションエクスプローラー上で再読み込みする  

------------------------
 使い方
------------------------
各クラスの役割

* PlatformClient: 最上層。ログイン情報を保持。  
* PlatformClient.Relation: サークル情報\(CircleInfo\)を提供  
* PlatformClient.Activity: ストリーム上の投稿データ\(ActivityInfo\)を提供  

使用例  
>var generator = await PlatformClient.Factory.ImportFromChrome();
>var client = await generator[0].Build();
>
>await client.Relation.UpdateCirclesAndBlockAsync(false, CircleUpdateLevel.Loaded);  
>var circles = client.Relation.Circles;  
>var activities = await client.Relation.YourCircle.GetActivities().TakeAsync(15);  

------------------------
 注意
------------------------

>Portable libraries built using Rx do not include the System.Reactive.PlatformServices assembly
containing "platform enlightenments" that depend on platform-specific functionality. (Doing so
would prevent the library to be portable due to the dependencies on a specific platform.)  
>When including the resulting portable library in another project, please include the platform's
System.Reactive.PlatformServices assembly in order to get the best performance. To include this
assembly, use any of the following options:  

>1. Select the System.Reactive.PlatformServices assembly from the Visual Studio "Add Reference"
   dialog. This option works for Windows Store apps, .NET 4.5, and Windows Phone 8 projects.
>2. For Windows Store apps and Windows Phone 8 projects, use the Reactive Extensions Extension SDK
   which can be found in the "Add Reference" dialog.
>3. Use NuGet to include the Rx-Main package (or any package that depends on Rx-Main, such as
   Rx-Xaml) which will automatically include the Rx-PlatformServices enlightenment package.
