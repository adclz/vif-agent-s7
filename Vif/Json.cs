using System.Collections.ObjectModel;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Vif_siemens_compiler.Vif;

public enum BlockType
{
    Fb,
    Fc,
    Db,
    Udt,
    Unknown
}


public class VifFile
{
    public List<string> Folders { get; set; }
    public string FileName { get; set; }
    public BlockType Type { get; set; }
    public string Code { get; set; }
}

public static class Json
{
    public static IEnumerable<VifFile>? ParseJson(JObject json)
    {
        var files = new List<VifFile>();
        foreach (var item in json)
        {
            if (!item.Key.StartsWith("file")) continue;
            var split = item.Key.Replace("file:///", "").Split('/');
            files.Add(new VifFile
            {
                FileName = split.Last(),
                Code = string.Join("\n", Correction(item.Value!.Value<JArray>()!.Select(line => line.Value<string>()).ToList())),
                Folders = split.Take(split.Length - 1).ToList(),
                Type = item.Value[0].Value<string>().StartsWith("DATA_BLOCK") ? BlockType.Db
                    : item.Value[0].Value<string>().StartsWith("FUNCTION_BLOCK") ? BlockType.Fb 
                        : item.Value[0].Value<string>().StartsWith("FUNCTION") ? BlockType.Fc
                        : item.Value[0].Value<string>().StartsWith("UDT") ? BlockType.Udt : BlockType.Unknown
            });
        }
        return files;
    }
    
    // LF Fix
    // Not sure if i should keep this
    private static IEnumerable<string> Correction(IEnumerable<string> data)
    {
        var source = new Collection<string>();
        var cs = data as string[] ?? data.ToArray();
        foreach (var c in cs)
        {
            var newBlock = new string(c
                .Where(c1 =>
                    char.IsLetter(c1) ||
                    char.IsDigit(c1) ||
                    char.IsPunctuation(c1) ||
                    char.IsWhiteSpace(c1) ||
                    char.IsSymbol(c1)).ToArray());

            if (cs.Contains("//")) // Comment
                newBlock += Encoding.UTF8.GetString(new byte[] { 0x0D, 0x0A, 0x09 });
            source.Add(newBlock);
        }

        return source;
    }
}