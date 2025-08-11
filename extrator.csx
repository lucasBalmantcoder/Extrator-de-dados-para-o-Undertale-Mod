// Adapted from original script by Grossley
using System.Linq;
using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;
using UndertaleModLib;
using UndertaleModLib.Models;

EnsureDataLoaded();

if (Data.IsYYC())
{
    ScriptError("You cannot do a code dump of a YYC game! There is no code to dump!");
    return;
}

ThreadLocal<GlobalDecompileContext> DECOMPILE_CONTEXT = new ThreadLocal<GlobalDecompileContext>(() => new GlobalDecompileContext(Data, false));

int failed = 0;

string codeFolder = PromptChooseDirectory();
if (codeFolder == null)
    throw new ScriptException("The export folder was not set.");

Directory.CreateDirectory(Path.Combine(codeFolder, "Code"));
codeFolder = Path.Combine(codeFolder, "Code");

List<string> codeToDump = new();
List<string> gameObjectCandidates = new();
List<string> splitStringsList = new();
string InputtedText = "";

InputtedText = SimpleTextInput("Menu", "Enter object, script, or code entry names", InputtedText, true);
string[] IndividualLineArray = InputtedText.Split('\n', StringSplitOptions.RemoveEmptyEntries);

foreach (var OneLine in IndividualLineArray)
{
    splitStringsList.Add(OneLine.Trim());
}

// Match inputs to scripts, codes, and objects
foreach (var item in splitStringsList)
{
    string lowered = item.ToLower();

    foreach (UndertaleGameObject obj in Data.GameObjects)
    {
        if (obj.Name.Content.ToLower() == lowered)
        {
            gameObjectCandidates.Add(obj.Name.Content);
        }
    }

    foreach (UndertaleScript scr in Data.Scripts)
    {
        if (scr.Code != null && scr.Name.Content.ToLower() == lowered)
        {
            codeToDump.Add(scr.Code.Name.Content);
        }
    }

    foreach (UndertaleGlobalInit globalInit in Data.GlobalInitScripts)
    {
        if (globalInit.Code != null && globalInit.Code.Name.Content.ToLower() == lowered)
        {
            codeToDump.Add(globalInit.Code.Name.Content);
        }
    }

    foreach (UndertaleCode code in Data.Code)
    {
        if (code.Name.Content.ToLower() == lowered)
        {
            codeToDump.Add(code.Name.Content);
        }
    }
}

// Adiciona códigos de eventos dos objetos informados
foreach (var objName in gameObjectCandidates)
{
     try
    {
        UndertaleGameObject obj = Data.GameObjects.ByName(objName);

        foreach (var eventList in obj.Events)
        {
            foreach (var evnt in eventList)
            {
                if (evnt == null) continue;

                foreach (var action in evnt.Actions)
                {
                    if (action?.CodeId?.Name?.Content != null)
                    {
                        codeToDump.Add(action.CodeId.Name.Content);
                    }
                }
            }
        }
    }
    catch
    {
        // Silent catch: object might be malformed or missing data
    }
}

SetProgressBar(null, "Exporting Code", 0, codeToDump.Count);
StartProgressBarUpdater();

// Exportação dos códigos decompilados
await Task.Run(() =>
{
    foreach (var codeName in codeToDump.Distinct())
    {
        UndertaleCode code = Data.Code.FirstOrDefault(c => c.Name.Content == codeName);
        if (code != null)
            DumpCode(code);
        else
            failed++;
    }
});

await StopProgressBarUpdater();

void DumpCode(UndertaleCode code)
{
    string safeName = SanitizeFileName(code.Name.Content);
    string path = Path.Combine(codeFolder, safeName + ".gml");

    try
    {
        string output = Decompiler.Decompile(code, DECOMPILE_CONTEXT.Value);

        if (code.ParentEntry != null)
        {
            string dupFolder = Path.Combine(codeFolder, "Duplicates");
            Directory.CreateDirectory(dupFolder);
            path = Path.Combine(dupFolder, safeName + ".gml");
            output = output.Replace("@@This@@()", "self/*@@This@@()*/");
        }

        File.WriteAllText(path, output);
    }
    catch (Exception e)
    {
        string failedDir = Path.Combine(codeFolder, "Failed");
        Directory.CreateDirectory(failedDir);
        path = Path.Combine(failedDir, safeName + ".gml");
        File.WriteAllText(path, $"/*\nDECOMPILER FAILED!\n\n{e}\n*/");
        failed++;
    }

    IncrementProgress();
}

string SanitizeFileName(string name)
{
    foreach (char c in Path.GetInvalidFileNameChars())
        name = name.Replace(c, '_');
    return name;
}
