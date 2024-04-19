using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Sharprompt;
using Siemens.Engineering;
using Siemens.Engineering.Compiler;
using Siemens.Engineering.SW.Blocks;
using Siemens.Engineering.SW.ExternalSources;
using Vif_siemens_compiler.Siemens;
using Vif_siemens_compiler.Vif;
using static Crayon.Output;

class Program
{
    private static async Task<int> Main(string[] args)
    {
        AppDomain.CurrentDomain.AssemblyResolve += (_, eventArgs) =>
        {

            var index = eventArgs.Name.IndexOf(',');
            
            if (index == -1) return null;
            var compatibleVersion = new[] { "V19", "V18", "V17", "V16" };

            var assembly = (from version in compatibleVersion
                let name = $"{eventArgs.Name.Substring(0, index)}.dll"
                select Path.Combine($@"C:\Program Files\Siemens\Automation\Portal {version}\PublicAPI\{version}\", name)
                into path
                select Path.GetFullPath(path)
                into fullPath
                where File.Exists(fullPath)
                select Assembly.LoadFrom(fullPath)).FirstOrDefault();
            

            return assembly;
        };
        
        return await RealMain(args);
    }
    
    private static async Task<int> RealMain(string[] args)
    {
        Console.WriteLine($"[{Green("Vif Agent S7")}]");

        if (args.Length == 0)
        {
            Console.WriteLine($"{Yellow("No arguments provided.")}");
            Console.WriteLine("Usage:");
            
            Console.WriteLine("A link               https:///www.yourlink.com/program.json");
            Console.WriteLine("A file path          \"C:Path\\Folder\\program.json\"");
            Console.WriteLine("A json object        \"{ \"file://myblock\": { ... } }\"");
            Console.WriteLine("--web                Waits for post");
            
            return Exit();
        }

        var arg = args[0];
        if (args[0].StartsWith("vif-comp-s7:"))
        {
            arg = args[0].Replace("vif-comp-s7:", "");
        }
        
        if (arg.Length == 0)
        {
            Console.WriteLine($"{Yellow("No arguments provided.")}");
            Console.WriteLine("Usage:");
            
            Console.WriteLine("A link               https:///www.yourlink.com/program.json");
            Console.WriteLine("A file path          \"C:Path\\Folder\\program.json\"");
            Console.WriteLine("A json object        \"{ \"file://myblock\": { ... } }\"");
            Console.WriteLine("--web                Waits for post");
            
            return Exit();
        }

        IEnumerable<VifFile>? files;

        if (arg.StartsWith("--web"))
        {
            try
            {
                var server = new Server();
                
                var id = new Random().Next(0, 1000000);
                var token = $"{server.port}-{id}";
                
                Console.WriteLine($"Paste this token on your browser: {Cyan($"{token}")}");

                var content = await Server.ok.Task;
                files = Json.ParseJson(JObject.Parse(content));
            }
            catch (Exception e)
            {
                Console.WriteLine($"{Red($"Failed to get json object from url. {arg}")}");
                Console.WriteLine(e.ToString());
                return Exit();
            }
        }
        else if (arg.StartsWith("http"))
        {
            try
            {
                var httpClient = new HttpClient();
                var content = await httpClient.GetStringAsync(arg);
                files = Json.ParseJson(JObject.Parse(content));
            }
            catch (Exception e)
            {
                Console.WriteLine($"{Red($"Failed to get json object from url. {arg}")}");
                Console.WriteLine(e.ToString());
                return Exit();
            }
        }
        
        else if (arg.StartsWith("{"))
        {
            try
            {
                files = Json.ParseJson(JObject.Parse(arg));
            }
            catch (Exception e)
            {
                Console.WriteLine($"{Red($"Failed to parse json object.")}");
                Console.WriteLine(e.ToString());
                return Exit();
            }
        }
        else
        {
            try
            {
                var file = new FileInfo(arg);
                if (!file.Exists)
                {
                    Console.WriteLine($"{Yellow("File could not be found.")}");
                    Console.WriteLine($"{arg}");
                    return Exit();
                }

                files = Json.ParseJson(JObject.Parse(File.ReadAllText(file.FullName)));
            }
            catch (Exception e)
            {
                Console.WriteLine($"{Red($"Failed to parse file: {arg}")}");
                Console.WriteLine(e.ToString());
                return Exit();
            }
        }
        
        
        Console.WriteLine("Checking assemblies ...");
        

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!assembly.GetName().Name.Contains("Siemens")) continue;
            Console.WriteLine($"Loaded Siemens API From: ");
            Console.WriteLine(Underline(assembly.Location));
            Console.WriteLine(
                $"Current Version: {Cyan(new Regex(@"(?<=Version=)(.*)(?=, C)").Match(assembly.FullName).Value)}");
        }

        // Parse file 

        
        Console.WriteLine("Parsing Vif file...");

        Console.WriteLine("Found " + Cyan(files.Count().ToString()) + " file(s)");

        foreach (var f in files)
        {
            Console.WriteLine("File: " + Cyan(f.FileName) + " --> " + Cyan(f.Type.ToString()));
        }

        var processes = TiaPortal.GetProcesses()
            .Where(x => x.ProjectPath != null)
            .ToList();

        if (processes.Count == 0)
        {
            Console.WriteLine(Yellow("No TIA processes found"));
            Console.WriteLine("You must have an active TIA instance with a project to use vif compiler.");
            return Exit();
        }

        
        Console.WriteLine("Found " + Cyan(processes.Count.ToString()) + " processe(s):");
        foreach (var tiaPortalProcess in processes.Select((value, i) => new { i, value }))
        {
            Console.WriteLine($"\t [{Cyan(tiaPortalProcess.value.ProjectPath.Name)}]");
        }

        
        
        var projectInput = Prompt.Select("Select a Tia Instance", processes.Select(x => x.ProjectPath.Name));

        var projectExists = processes.Find(x => x.ProjectPath.Name == projectInput);
        if (projectExists == null)
        {
            Console.WriteLine(Yellow("Invalid project selected"));
            return Exit();
        }
        

        Console.WriteLine($"Attaching to {Magenta(projectExists.ProjectPath.Name)} ...");
        var process = projectExists.Attach();
        Console.WriteLine(Green("Success"));
        
        Console.WriteLine("Now pick a Plc target");

        var plcs = Hw.ListPlc(process);
        
        if (plcs.Count == 0)
        {
            Console.WriteLine(Yellow("No Plc found"));
            Console.WriteLine("Declare a Plc in your TIA Portal project.");
            return Exit();
        }
        
        foreach (var plc in plcs.Select((value, i) => new { i, value }))
        {
            Console.WriteLine($"\t [{Cyan(plc.i.ToString())}] --> {plc.value.Name}");
        }

        var plcInput = Prompt.Select("Select a Tia Instance",plcs.Select(x => x.Name));

        var targetPlc = plcs.Find(x => x.Name == plcInput);
        
        if (targetPlc == null)
        {
            Console.WriteLine(Yellow("Invalid plc name"));
            return Exit();
        }
        

        Console.WriteLine(Green("Success"));

        var rootFolder = targetPlc.BlockGroup.Groups;
        var externalSources = targetPlc.ExternalSourceGroup;

        Console.WriteLine("Vif is about to compile your blocks");
        Console.WriteLine(Cyan("TIP " +
                               "To speed up this process, make sure you have closed all opened editors in TIA Portal"));
        Console.WriteLine("When you are ready, press Enter to proceed...");
        Console.ReadLine();

        foreach (var f in files)
        {
            var targetFolder = rootFolder;
            foreach (var folder in f.Folders)
            {
                var exists = targetFolder.Find(folder);
                targetFolder = exists == null ? targetFolder.Create(folder).Groups : exists.Groups;
            }


            Console.WriteLine("Creating block " + Green(f.FileName) + "...");

            Sw.CreateBlockWithXml(process, f.FileName, f.Type, targetFolder.Parent as PlcBlockGroup);
            var externalSource = externalSources.ExternalSources.Find(f.FileName);
            externalSource?.Delete();

            var sourcePath =
                new FileInfo(
                    $@"{new DirectoryInfo(Path.GetTempPath())}\{targetPlc.Name}\{f.FileName}.scl");
            if (sourcePath.Exists)
                sourcePath.Delete();

            sourcePath.Directory.Create();

            File.WriteAllText(sourcePath.FullName, f.Code, Encoding.UTF8);

            var blockSource =
                externalSources.ExternalSources.CreateFromFile(f.FileName, sourcePath.FullName);
            blockSource.GenerateBlocksFromSource(GenerateBlockOption.KeepOnError);
        }

        Console.WriteLine("All blocks built, now compiling plc.");

        var plcCompileService = targetPlc.GetService<ICompilable>();
        var result = plcCompileService.Compile();

        FormatCompilerResult(result);

        

        return Exit();
    }

    private static string DisplayStringOrNot(string? any)
    {
        return any ?? "";
    }

    private static void SendCompilerMsg(CompilerResultMessage message)
    {
        foreach (var compilerResultMessage in message.Messages)
        {
            SendCompilerMsg(compilerResultMessage);
        }

        Console.WriteLine(
            message.State switch
            {
                CompilerResultState.Warning =>
                    $"[Tia Portal] [{Yellow("warn")}] {message.Description} {DisplayStringOrNot(message.Path)}",
                CompilerResultState.Error =>
                    $"[Tia Portal] [{Yellow("warn")}] {message.Description} {DisplayStringOrNot(message.Path)}",
                _ => $"[Tia Portal] [{Blue("info")}] {message.Description} {DisplayStringOrNot(message.Path)}",
            });
    }

    private static void FormatCompilerResult(CompilerResult result)
    {
        foreach (var compilerResultMessage in result.Messages)
        {
            SendCompilerMsg(compilerResultMessage);
        }
    }

    private static int Exit()
    {
        Console.WriteLine("Press Enter to exit...");
        Console.ReadLine();
        return 0;
    }
}