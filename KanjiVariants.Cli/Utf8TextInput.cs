// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

using System.Text;

namespace KanjiVariants.Cli;

internal static class Utf8TextInput
{
    internal static string Read(string file)
    {
        byte[] bytes = File.ReadAllBytes(file);
        // UTF-8 BOMだけを除去し、他の文字コードへの自動切り替えや改行変換を行いません。
        int start = bytes.AsSpan().StartsWith(new byte[] { 0xEF, 0xBB, 0xBF }) ? 3 : 0;
        return new UTF8Encoding(false, true).GetString(bytes, start, bytes.Length - start);
    }
}
