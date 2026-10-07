// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

namespace KanjiVariants;

/// <summary>文字列中の漢字が常用漢字表の本表字そのものかを診断します。</summary>
public static class JoyoKanjiDiagnostics
{
    /// <summary>入力をUTF-16位置付きの論理単位に分け、常用漢字表の本表字かを判定します。</summary>
    /// <param name="text">診断する元の文字列。正規化や文字置換を行いません。</param>
    /// <returns>入力全体の項目と、非常用漢字だけの読み取り専用一覧を含む結果。</returns>
    /// <remarks>旧字体・異体字・互換漢字を自動変換しません。漢字と直後のVSは登録有無によらず一項目で、基底文字へフォールバックせずNotJoyoKanjiとします。論理単位は書記素クラスタではありません。漢字以外は違反扱いせず、CRとLFは別項目です。不正UTF-16は1コード単位ずつ位置付きで保持し、文章全体を正常とは扱いません。</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="text"/>がnullの場合。</exception>
    public static JoyoKanjiDiagnosticResult AnalyzeText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Length == 0)
        {
            var empty = Array.Empty<JoyoKanjiDiagnosticItem>();
            return new JoyoKanjiDiagnosticResult(text, empty, empty, false);
        }

        var items = new List<JoyoKanjiDiagnosticItem>();
        List<JoyoKanjiDiagnosticItem>? nonJoyo = null;
        bool hasInvalidUtf16 = false;
        int offset = 0;
        while (offset < text.Length)
        {
            int start = offset;
            bool valid = KanjiText.TryReadUnit(text, ref offset, out int baseCp, out int? selectorCp);
            JoyoKanjiDiagnosticReason reason;
            if (!valid)
            {
                reason = JoyoKanjiDiagnosticReason.InvalidUtf16;
                hasInvalidUtf16 = true;
            }
            // 教育漢字診断と同じく、踊り字等は漢字の判定対象から除きます。
            else if (baseCp is 0x3005 or 0x3006 or 0x303B || !Lookup.IsRecognizedKanji(baseCp))
                reason = JoyoKanjiDiagnosticReason.NotKanji;
            else
                reason = selectorCp is null && JoyoKanji.IsJoyo(KanjiCharacter.FromScalar(baseCp))
                    ? JoyoKanjiDiagnosticReason.JoyoKanji : JoyoKanjiDiagnosticReason.NotJoyoKanji;

            var item = new JoyoKanjiDiagnosticItem(text.Substring(start, offset - start), start, reason);
            items.Add(item);
            // 非常用漢字一覧は全項目と同じインスタンスを共有し、不正UTF-16を混ぜません。
            if (reason == JoyoKanjiDiagnosticReason.NotJoyoKanji)
                (nonJoyo ??= new List<JoyoKanjiDiagnosticItem>()).Add(item);
        }
        return new JoyoKanjiDiagnosticResult(text, items.AsReadOnly(),
            nonJoyo is null ? Array.Empty<JoyoKanjiDiagnosticItem>() : nonJoyo.AsReadOnly(), hasInvalidUtf16);
    }
}
