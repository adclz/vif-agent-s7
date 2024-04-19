using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Xml;
using Siemens.Engineering;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Blocks;
using Vif_siemens_compiler.Vif;
using BlockType = Vif_siemens_compiler.Vif.BlockType;

namespace Vif_siemens_compiler.Siemens;

public class Sw
{
    /// <summary>
    ///  When creating blocks with external sources, you can't specify a folder structure.
    /// 
    ///  And the PlcBlockGroup object does not provide a way to compile an external source.
    ///
    ///  To overcome this limitation, we create an empty block using Tia Portal XML imports in the right directory.
    ///
    ///  Then we compile the external source, if the source has the same name as our empty block, TIA Portal will override it in the right location. 
    ///
    ///  Had a great time coding this thing. 
    /// </summary>
    /// <param name="tia"></param>
    /// <param name="name"></param>
    /// <param name="type"></param>
    /// <param name="target"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static void CreateBlockWithXml(TiaPortal tia, string name, BlockType type, PlcBlockGroup target)
    {
        var editedPath = new FileInfo($"{new DirectoryInfo(Path.GetTempPath())}\\PlcBlock.xml");
        if (editedPath.Exists)
            editedPath.Delete();

        var header = GetDocumentTemplate(tia);

        IXmlBlock block = type switch
        {
            BlockType.Fb => new Fb(name),
            BlockType.Fc => new Fc(name),
            BlockType.Db => new Db(name),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        header.AddContent(block.ToString());

        var document = header.ToXml();

        File.WriteAllText(editedPath.FullName, document.OuterXml);

        target.Blocks.Import(new FileInfo(editedPath.FullName), ImportOptions.Override,
            SWImportOptions.IgnoreUnitAttributes |
            SWImportOptions.IgnoreStructuralChanges |
            SWImportOptions.IgnoreMissingReferencedObjects);
    }

    private static DocumentTemplate GetDocumentTemplate(TiaPortal tia)
    {
        Packages(tia, out var version, out var products, out var optionPackages);
        return new DocumentTemplate(version, "WithDefaults, WithReadOnly", products, optionPackages);
    }

    private static void Packages(TiaPortal tia, out string version, out Collection<Product> products,
        out Collection<OptionPackage> optionPackages)
    {
        products = new Collection<Product>();
        optionPackages = new Collection<OptionPackage>();
        version = "";

        var process = Process.GetCurrentProcess();
        var fullPath = process.MainModule!.FileName;

        foreach (var session in tia.GetCurrentProcess().AttachedSessions)
        {
            if (session.ProcessPath.FullName == fullPath)
                version = session.Version;
        }

        foreach (var product in tia.GetCurrentProcess().InstalledSoftware)
        {
            products.Add(new Product(product.Name, product.Version));
            if (product.Options.Count <= 0) continue;
            foreach (var option in product.Options)
                optionPackages.Add(new OptionPackage(option.Name, option.Version));
        }
    }
}

public readonly struct Product
{
    private string DisplayName { get; }
    private string DisplayVersion { get; }

    public Product(string displayName, string displayVersion)
    {
        DisplayName = displayName;
        DisplayVersion = displayVersion;
    }

    public override string ToString() =>
        $"<Product><DisplayName>{DisplayName}</DisplayName><DisplayVersion>{DisplayVersion}</DisplayVersion></Product>";
}

public readonly struct OptionPackage
{
    private string DisplayName { get; }
    private string DisplayVersion { get; }

    public OptionPackage(string displayName, string displayVersion)
    {
        DisplayName = displayName;
        DisplayVersion = displayVersion;
    }

    public override string ToString() =>
        $"<OptionPackage><DisplayName>{DisplayName}</DisplayName><DisplayVersion>{DisplayVersion}</DisplayVersion></OptionPackage>";
}

public class DocumentTemplate
{
    private string Version { get; }
    private string ExportsSettings { get; }
    private IEnumerable<Product> Products { get; }
    private IEnumerable<OptionPackage> OptionPackages { get; }
    private string Content { get; set; }

    public DocumentTemplate(string version, string exportsSettings, IEnumerable<Product> products,
        IEnumerable<OptionPackage> optionPackages)
    {
        Version = version;
        ExportsSettings = exportsSettings;
        Products = products;
        OptionPackages = optionPackages;
        Content = "";
    }

    public void AddContent(string content) => Content += content;
    public void AddContent(XmlNode content) => Content += content.InnerText;

    public override string ToString() =>
        $"<?xml version=\"1.0\" encoding=\"utf-8\"?><Document><Engineering version=\"{Version}\"/><DocumentInfo><Created>1997-27-03T00:53:41.8511582Z</Created><ExportSetting>{ExportsSettings}</ExportSetting> <InstalledProducts>{string.Join("", Products.Select(attr => attr.ToString()))}{string.Join("", OptionPackages.Select(attr => attr.ToString()))}</InstalledProducts></DocumentInfo>{Content}</Document>";

    public XmlDocument ToXml()
    {
        var document = new XmlDocument();
        document.LoadXml(ToString());
        return document;
    }
}

public interface IXmlBlock
{
    string ToString();
}

public class Fb : IXmlBlock
{
    private string BlockName { get; }

    public Fb(string blockName)
    {
        BlockName = blockName;
    }

    public override string ToString() =>
        $"<SW.Blocks.FB ID=\"0\"><AttributeList><AutoNumber>true</AutoNumber><HeaderAuthor /><HeaderFamily /><HeaderName /><HeaderVersion>0.1</HeaderVersion><Interface><Sections xmlns=\"http://www.siemens.com/automation/Openness/SW/Interface/v4\"><Section Name=\"Input\" /><Section Name=\"Output\" /><Section Name=\"InOut\" /><Section Name=\"Static\" /><Section Name=\"Temp\" /><Section Name=\"Constant\" /></Sections></Interface><Name>{BlockName}</Name><ProgrammingLanguage>SCL</ProgrammingLanguage></AttributeList><ObjectList><SW.Blocks.CompileUnit ID=\"3\" CompositionName=\"CompileUnits\"><AttributeList><NetworkSource><StructuredText xmlns=\"http://www.siemens.com/automation/Openness/SW/NetworkSource/StructuredText/v3\" /></NetworkSource><ProgrammingLanguage>SCL</ProgrammingLanguage></AttributeList><ObjectList></ObjectList></SW.Blocks.CompileUnit></ObjectList></SW.Blocks.FB>";
}

public class Fc : IXmlBlock
{
    private string BlockName { get; }

    public Fc(string blockName)
    {
        BlockName = blockName;
    }

    public override string ToString() =>
        $"<SW.Blocks.FC ID=\"0\"><AttributeList><AutoNumber>true</AutoNumber><HeaderAuthor /><HeaderFamily /><HeaderName /><HeaderVersion>0.1</HeaderVersion><Interface><Sections xmlns=\"http://www.siemens.com/automation/Openness/SW/Interface/v4\"><Section Name=\"Input\" /><Section Name=\"Output\" /><Section Name=\"InOut\" /><Section Name=\"Temp\" /><Section Name=\"Constant\" /><Section Name=\"Return\"><Member Name=\"Ret_Val\" Datatype=\"Void\" Accessibility=\"Public\" /></Section></Sections></Interface><Name>{BlockName}</Name></AttributeList><ObjectList><SW.Blocks.CompileUnit ID=\"3\" CompositionName=\"CompileUnits\"><AttributeList><NetworkSource><StructuredText xmlns=\"http://www.siemens.com/automation/Openness/SW/NetworkSource/StructuredText/v3\" /></NetworkSource><ProgrammingLanguage>SCL</ProgrammingLanguage></AttributeList><ObjectList></ObjectList></SW.Blocks.CompileUnit></ObjectList></SW.Blocks.FC>";
}

public class Db : IXmlBlock
{
    private string BlockName { get; }

    public Db(string blockName)
    {
        BlockName = blockName;
    }

    public override string ToString() =>
        $"<SW.Blocks.GlobalDB ID=\"0\"><AttributeList><AutoNumber>true</AutoNumber><HeaderAuthor /><HeaderFamily /><HeaderName /><HeaderVersion>0.1</HeaderVersion><Interface><Sections xmlns=\"http://www.siemens.com/automation/Openness/SW/Interface/v4\"><Section Name=\"Static\" /></Sections></Interface><Name>{BlockName}</Name><ProgrammingLanguage>DB</ProgrammingLanguage></AttributeList><ObjectList></ObjectList></SW.Blocks.GlobalDB>";
}