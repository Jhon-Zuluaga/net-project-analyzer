# .NET Project Analyzer (MCP Server)

A Model Context Protocol (MCP) server built natively in C# that acts as an automated code auditor. It reads local project source files and evaluates them against global architectural rules and best practices.

## Components

### Tools

* **`analizar_con_skill_global`**
  * Analyzes the active project code against the guidelines defined in a given skill folder.
  * **Input**: `skillName` (string): The exact name of the skill folder (e.g., `"dotnet-best-practices"`).

## Installation & File Setup

To make this server work correctly on your local machine, you must place the repository files and the skills folder in their specific system paths.

### Step 1: Set Up the Skills Directory

The analyzer looks for rulesets inside a specific global directory on your `C:\` drive.

1. Locate the `agents` folder (or `.zip`) provided in this repository.
2. Move or extract it directly into the root of your Local Disk so that the path matches exactly:

   ```text
   C:\agents\.agents\skills\
   ```

3. Inside that folder, ensure your skills are organized like this:
   * `C:\agents\.agents\skills\dotnet-best-practices\SKILL.md`
   * `C:\agents\.agents\skills\containerize-aspnetcore\SKILL.md`

### Step 2: Set Up the MCP Server Project

1. Clone or move this repository's source code folder directly into your `C:\` drive.
2. Rename the folder to:

   ```text
   C:\mpc-projects-csharp
   ```

3. Open a terminal in `C:\mpc-projects-csharp` and compile the executable by running:

   ```bash
   dotnet build
   ```

   This will generate the final native binary at:
   `C:\mpc-projects-csharp\bin\Debug\net10.0\mpc-projects-csharp.exe`

## Configuration

### Usage with Claude Desktop

Add the following configuration block to the `mcpServers` section of your local `claude_desktop_config.json` file:

```json
{
  "mcpServers": {
    "net-project-analyzer": {
      "command": "C:\\mpc-projects-csharp\\bin\\Debug\\net10.0\\mpc-projects-csharp.exe",
      "args": ["C:\\Users\\YOUR_WINDOWS_USER\\YourTargetProject"]
    }
  }
}
```

**Configuration Details:**

* `command`: Points directly to the compiled executable in the required path.
* `args`: Change `C:\\Users\\YOUR_WINDOWS_USER\\YourTargetProject` to the absolute path of the specific .NET API or project you want Claude to audit.

## How to Use

Once Claude Desktop is restarted and the server is connected, you can trigger the analysis directly from the chat window.

Open Claude and ask it to analyze your project by specifying the exact name of the skill folder you want to apply.

**Example Prompts:**

* To audit C# code quality:
  `"nalyze my project using the skill 'dotnet-best-practices'"`
* To audit Docker infrastructure:
  `"Analyze the container files in my project using the skill 'containerize-aspnetcore'"`

Claude will automatically execute the tool, load up to 20 relevant files from your target project path, compare them against your global `C:\agents` rules, and return a concise evaluation report.
