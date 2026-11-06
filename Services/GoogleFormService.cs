namespace Regen.Services;

using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;

public class GoogleFormService
{
    // const pattern = /<script[^>]*>[\s\S]*?<\/script>/g;
    private static readonly Regex ScriptPattern =
        new Regex(@"<script[^>]*>[\s\S]*?<\/script>", RegexOptions.Compiled);

    // const regex = /FB_PUBLIC_LOAD_DATA_/;
    private static readonly Regex FbRegex =
        new Regex(@"FB_PUBLIC_LOAD_DATA_", RegexOptions.Compiled);

    private const string VarName = "let FB_PUBLIC_LOAD_DATA_ = ";

    public string HtmlToJson(string html)
    {
        var response = html;

        // response.match(pattern) returns null when nothing matches, and the
        // JS then throws on find_match.length (TypeError). Same behavior here.
        var findMatch = ScriptPattern.Matches(response);
        if (findMatch.Count == 0)
            throw new InvalidOperationException(
                "No <script> tags found (JS original throws TypeError on null match).");

        var findScriptTags = "";

        // No break: the LAST script containing FB_PUBLIC_LOAD_DATA_ wins.
        for (int i = 0; i < findMatch.Count; i++)
        {
            if (FbRegex.IsMatch(findMatch[i].Value))
            {
                findScriptTags = findMatch[i].Value;
            }
        }

        // const $ = cheerio.load(find_script_tags);
        // $("script").text()  -> combined text of all matched script elements
        var document = new HtmlParser().ParseDocument(findScriptTags);
        var targetScript = string.Concat(
            document.QuerySelectorAll("script").Select(s => s.TextContent));

        // indexOf is ordinal in JS; returns -1 if not found
        var varNameIndex = targetScript.IndexOf(VarName, StringComparison.Ordinal);

        // slice(var_name_index + length) -> if index is -1, JS still slices from (length - 1)
        var scriptContent = JsSlice(targetScript, varNameIndex + VarName.Length);

        // script_content.slice(1, -1)
        var filteredScriptContent = JsSlice(scriptContent, 1, -1);

        return filteredScriptContent;
    }

    /// Exact equivalent of JavaScript String.prototype.slice(start, end):
    /// supports negative indexes, clamps out-of-range values, never throws.
    private static string JsSlice(string s, int start, int? end = null)
    {
        int len = s.Length;

        int from = start < 0 ? Math.Max(len + start, 0) : Math.Min(start, len);

        int to;
        if (end is null) to = len;
        else if (end < 0) to = Math.Max(len + end.Value, 0);
        else to = Math.Min(end.Value, len);

        return from >= to ? string.Empty : s.Substring(from, to - from);
    }
}