using System.CommandLine;
using System.Diagnostics;
using System.Diagnostics.Metrics;
var bundleCommand = new Command("bundle", "Bundle code files to a single file");
var bundleOutput = new Option<FileInfo>("--output", "File path and name") { IsRequired = true };
bundleOutput.AddAlias("-o");
var bundleLanguage = new Option<string>("--language", "List of programming languages") { IsRequired = true };
bundleLanguage.AddAlias("-l");
var bundleNote = new Option<bool>("--note", "Do List the source code as a comment");
bundleNote.AddAlias("-n");
var bundleSort = new Option<string>("--sort", "sort by name or code type");
bundleSort.AddAlias("-s");
bundleSort.SetDefaultValue("ABC");
var bundleRemove = new Option<bool>("--remove-empty-lines", "Remove empty lines from code");
bundleRemove.AddAlias("-r");
var bundleAuthor = new Option<string>("--author", "the creator`s name of the file will appear");
bundleAuthor.AddAlias("-a");
var createRspCommand = new Command("create-rsp", "Create a response file with command options");
bundleCommand.AddOption(bundleOutput);
bundleCommand.AddOption(bundleLanguage);
bundleCommand.AddOption(bundleNote);
bundleCommand.AddOption(bundleSort);
bundleCommand.AddOption(bundleRemove);
bundleCommand.AddOption(bundleAuthor);
string currentPath = Directory.GetCurrentDirectory();
List<string> files = Directory.GetFiles(currentPath, "", SearchOption.AllDirectories).Where(file => !file.Contains("bin") && !file.Contains("Debug") && !file.Contains("node_modules") && !file.Contains(".git") && !file.Contains(".vscode")).ToList();
string[] allLanguages = { "c", "c++", "c#", "java", "pyton", "javascript", "html", "SQL" };
string[] allExtentions = { ".c", ".cpp", ".cs", ".java", ".py", ".js", ".html", ".sql" };
bundleCommand.SetHandler(async (output, language, note, sort, remove, author) =>
{
    if (output.Exists)
    {
        Console.WriteLine(" file name already exist. Please choose a differente name.");
        return;
    }
    if (!language.Equals("all"))
    {
        List<string> selectedLanguages = SelectLanguages(allLanguages, allExtentions, language);
        files = files.Where(f => selectedLanguages.Contains(Path.GetExtension(f))).ToList();
    }
    if (sort == "ABC")
        files = files.OrderBy(f => Path.GetFileName(f)).ToList();
    else files = files.OrderBy(f => Path.GetExtension(f)).ThenBy(f => Path.GetFileName(f)).ToList();
    await WriteToFile(output, files, note, remove, author);
}, bundleOutput, bundleLanguage, bundleNote, bundleSort, bundleRemove, bundleAuthor);
createRspCommand.SetHandler(async () =>
{
    var output = PromptForString("Enter output file path: ");
    while (output.Length == 0)
    {
        Console.WriteLine("This field is required! enter again");
        output = Console.ReadLine();
    }
    var language = PromptForString("Enter programming languages (all for all languages): ");
    while (language.Length == 0)
    {
        Console.WriteLine("This field is required! enter again");
        language = Console.ReadLine();
    }
    var note = PromptForBool("Do you want to list the source code as a comment? (true/false): ");
    var sort = PromptForString("Do you want to sort by name or code type? (ABC/Type): ");
    while (sort != "ABC" && sort != "Type")
    {
        Console.WriteLine("Invalid input.Please enter 'ABC' or 'Type'.");
        sort = Console.ReadLine();
    }
    var remove = PromptForBool("Do you want to remove empty lines from code? (true/false): ");
    var author = PromptForString("Enter the creator's name of the file: ");
    // Create the response file content
    var rspContent = $@"bundle --output {output} --language {language} --note {note} --sort {sort} --remove-empty-lines {remove} ";
    if (author.Length > 0)
        rspContent += $"--author {author}";
    // Save the content to the response file
    var rspFilePath = "rspFile.rsp";  // You can customize the file name and path
    File.WriteAllText(rspFilePath, rspContent);
    Console.WriteLine($"Response file created: {rspFilePath}");
});
var rootCommand = new RootCommand("Root command for File Bundle CLI");
rootCommand.AddCommand(bundleCommand);
rootCommand.AddCommand(createRspCommand);
await rootCommand.InvokeAsync(args);
static List<string> SelectLanguages(string[] allLanguages, string[] allExtentions, string language)
{
    List<string> selectedLanguages = new List<string>();
    for (int i = 0; i < allLanguages.Length; i++)
    {
        if (language.Contains(allLanguages[i]))
        {
            selectedLanguages.Add(allExtentions[i]);
        }
    }
    return selectedLanguages;
}
static async Task WriteToFile(FileInfo path, List<string> files, bool note, bool remove, string author)
{
    try
    {
        using (StreamWriter file = new StreamWriter(path.FullName, true))
        {
            if (author != null)
            {
                string s = $"//name: {author}";
                await file.WriteLineAsync(s);
            }
            foreach (var f in files)
            {
                await file.WriteLineAsync($"-----------------{Path.GetFileName(f)}---------------");
                if (note)
                {
                    string p = Path.GetRelativePath(path.FullName, f);
                    string sourceInfo = $"//source: {p}";
                    await file.WriteLineAsync(sourceInfo);
                }
                if (remove)
                {
                    string[] lines = File.ReadAllLines(f);
                    lines = lines.Where(line => !string.IsNullOrEmpty(line)).ToArray();
                    File.WriteAllLines(f, lines);
                }
                using (var codeFile = new StreamReader(f))
                {
                    await file.WriteAsync(await codeFile.ReadToEndAsync());
                }
            }
        }
        Console.WriteLine("File was created");
    }
    catch (DirectoryNotFoundException ex)
    {
        Console.WriteLine("Error");
    }
}
static string PromptForString(string prompt)
{
    Console.Write(prompt);
    return Console.ReadLine();
}
static bool PromptForBool(string prompt)
{
    Console.Write(prompt);
    bool result;
    while (!bool.TryParse(Console.ReadLine(), out result))
    {
        Console.WriteLine("Invalid input. Please enter 'true' or 'false'.");
        Console.Write(prompt);
    }
    return result;
}
