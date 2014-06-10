# FileName Rules

このディレクトリにはApiWrapperのスタブ化用にG+の各APIのレスポンスを入れておく。ファイル名は以下のルールとする。

1.URL全体をUri.EscapeDataString()
2.エスケープされたURLをReplace("_", "__").Replace("%", "_") + ".txt"
3.以上の処置をしたファイル名でURLのレスポンスを置く