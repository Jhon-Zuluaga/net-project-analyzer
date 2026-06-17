using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.Json;

class Program
{
    public static string CurrentProjectPath { get; private set; } = string.Empty;

    static async Task Main(string[] args)
    {
        if (args.Length > 0)
        {
            CurrentProjectPath = args[0];
        }

        // Use pure UTF8 without invisible BOM characters
        using var reader = new StreamReader(Console.OpenStandardInput(), Encoding.UTF8);

        // Explicitly specify buffer size (4096) and leave the stream open (true)
        using var writer = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false), 4096, leaveOpen: true);

        while (await reader.ReadLineAsync() is string line)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;

                if (root.TryGetProperty("method", out var methodProp))
                {
                    string method = methodProp.GetString() ?? "";

                    if (method == "initialize")
                    {
                        var id = root.GetProperty("id").GetInt64();
                        var initResponse = new
                        {
                            jsonrpc = "2.0",
                            id = id,
                            result = new
                            {
                                protocolVersion = "2025-11-25",
                                capabilities = new
                                {
                                    tools = new { listChanged = false }
                                },
                                serverInfo = new { name = "CentralAnalyzer", version = "1.0.0" }
                            }
                        };

                        string jsonOut = JsonSerializer.Serialize(initResponse) + "\n";
                        await writer.WriteAsync(jsonOut);
                        await writer.FlushAsync();
                    }
                    else if (method == "tools/list")
                    {
                        var id = root.GetProperty("id").GetInt64();
                        var toolsResponse = new
                        {
                            jsonrpc = "2.0",
                            id = id,
                            result = new
                            {
                                tools = new[]
                                {
                                    new {
                                        name = "analizar_con_skill_global",
                                        description = "Analyzes the open project using the global skills folder rules.",
                                        inputSchema = new {
                                            type = "object",
                                            properties = new {
                                                skillName = new { type = "string", description = "The exact name of the skill folder (e.g., 'dotnet-best-practices')" }
                                            },
                                            required = new[] { "skillName" }
                                        }
                                    }
                                }
                            }
                        };
                        string jsonOut = JsonSerializer.Serialize(toolsResponse) + "\n";
                        await writer.WriteAsync(jsonOut);
                        await writer.FlushAsync();
                    }
                    else if (method == "tools/call")
                    {
                        var id = root.GetProperty("id").GetInt64();
                        var paramsEl = root.GetProperty("params");
                        var argumentsEl = paramsEl.GetProperty("arguments");
                        string skillName = argumentsEl.GetProperty("skillName").GetString() ?? "";

                        string mcpResponse = await ProcessAnalysisAsync(skillName);

                        var callResponse = new
                        {
                            jsonrpc = "2.0",
                            id = id,
                            result = new
                            {
                                content = new[] { new { type = "text", text = mcpResponse } }
                            }
                        };
                        string jsonOut = JsonSerializer.Serialize(callResponse) + "\n";
                        await writer.WriteAsync(jsonOut);
                        await writer.FlushAsync();
                    }
                }
            }
            catch
            {
                // Ignore parsing errors from corrupt streams
            }
        }
    }

    private static async Task<string> ProcessAnalysisAsync(string skillName)
    {
        string globalSkillsPath = @"C:\agents\.agents\skills";
        string currentProjectPath = CurrentProjectPath;

        if (string.IsNullOrEmpty(currentProjectPath) || !Directory.Exists(currentProjectPath))
        {
            return $"Error: Provided path is invalid or inaccessible: '{currentProjectPath}'";
        }

        string skillFolder = Path.Combine(globalSkillsPath, skillName);
        string rulesPath = Path.Combine(skillFolder, "rules.md");
        if (!File.Exists(rulesPath)) rulesPath = Path.Combine(skillFolder, "prompt.txt");
        if (!File.Exists(rulesPath)) rulesPath = Path.Combine(skillFolder, "SKILL.md");

        if (!File.Exists(rulesPath))
        {
            return $"No rules found in the global folder for the skill: '{skillName}'";
        }

        string globalRules = await File.ReadAllTextAsync(rulesPath);

        // MULTI-FILE MODIFICATION: Read all files from root directory and subdirectories
        var allFiles = Directory.GetFiles(currentProjectPath, "*.*", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") &&
                        !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") &&
                        !f.Contains($"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}") &&
                        !f.Contains($"{Path.DirectorySeparatorChar}.vs{Path.DirectorySeparatorChar}"))
            .ToList();

        // Filter allowed extensions or key file names (C#, Dockerfile, YAML, JSON, Markdown)
        var allowedExtensions = new HashSet<string> { ".cs", ".yml", ".yaml", ".json", ".md" };
        var filteredFiles = allFiles
            .Where(f => allowedExtensions.Contains(Path.GetExtension(f).ToLower()) ||
                        Path.GetFileName(f).Equals("Dockerfile", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (filteredFiles.Count == 0)
        {
            return $"No valid files found to analyze in: {currentProjectPath}";
        }

        var projectContext = new StringBuilder();

        // Take a maximum of 20 relevant files to avoid saturating the AI context window
        foreach (var file in filteredFiles.Take(20))
        {
            projectContext.AppendLine($"\n--- FILE: {Path.GetRelativePath(currentProjectPath, file)} ---");
            projectContext.AppendLine(await File.ReadAllTextAsync(file));
        }

        return $"Analyze the project based on the global skill rules.\n\n" +
               $"[GLOBAL RULES - SKILL: {skillName.ToUpper()}]\n{globalRules}\n\n" +
               $"[CURRENT PROJECT CODE AND CONFIGURATION]:\n{projectContext}\n\n" +
               $"Tell me if it complies with the skill rules in a concise manner.";
    }
}