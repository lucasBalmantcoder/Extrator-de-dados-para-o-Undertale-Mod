using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModLib.Util;

EnsureDataLoaded();

ThreadLocal<GlobalDecompileContext> DECOMPILE_CONTEXT =
    new(() => new GlobalDecompileContext(Data, false));

string baseDir = GetFolder(FilePath);
string exportDir = Path.Combine(baseDir, "Export_Code");

if (Directory.Exists(exportDir))
{
    ScriptError("Uma exportação de código já existe. Por favor, remova-a.", "Erro");
    return;
}

Directory.CreateDirectory(exportDir);

bool exportFromCache = GMLCacheEnabled && Data.GMLCache != null
    && ScriptQuestion("Exportar do cache?");

Dictionary<string, List<(string codeName, string content)>> objetosOrganizados = new();

// Calcular total de arquivos relevantes
int totalRelevante = 0;
if (exportFromCache)
{
    totalRelevante = Data.GMLCache.Count(entry => !DeveIgnorar(entry.Key));
}
else
{
    totalRelevante = Data.Code.Count(code => 
        code.ParentEntry == null && !DeveIgnorar(code.Name.Content));
}

SetProgressBar(null, "Exportando e Organizando Código", 0, totalRelevante);
StartProgressBarUpdater();

if (exportFromCache)
{
    await Task.Run(() =>
    {
        Parallel.ForEach(Data.GMLCache, entry =>
        {
            if (DeveIgnorar(entry.Key))
            {
                IncrementProgressParallel();
                return;
            }
            
            ProcessarCodigo(entry.Key, entry.Value);
            IncrementProgressParallel();
        });
    });
}
else
{
    await Task.Run(() =>
    {
        Parallel.ForEach(Data.Code, code =>
        {
            if (code.ParentEntry != null)
            {
                IncrementProgressParallel();
                return;
            }
            
            string codeName = code.Name.Content;
            if (DeveIgnorar(codeName))
            {
                IncrementProgressParallel();
                return;
            }
            
            try
            {
                string output = Decompiler.Decompile(code, DECOMPILE_CONTEXT.Value);
                ProcessarCodigo(codeName, output);
            }
            catch (Exception e)
            {
                ProcessarCodigo(codeName,
                    $"/*\nDECOMPILADOR FALHOU\n\n{e}\n*/");
            }
            
            IncrementProgressParallel();
        });
    });
}

// Agora escreve todos os arquivos organizados em suas pastas
await Task.Run(() =>
{
    foreach (var grupo in objetosOrganizados)
    {
        string nomePasta = grupo.Key;
        
        // Se for um script, vai para a pasta Scripts
        if (nomePasta.StartsWith("Script_"))
        {
            nomePasta = "Scripts";
        }
        
        string pastaDestino = Path.Combine(exportDir, nomePasta);
        Directory.CreateDirectory(pastaDestino);
        
        foreach (var arquivo in grupo.Value.OrderBy(a => a.codeName))
        {
            string nomeArquivoFinal = arquivo.codeName + ".gml";
            string caminhoArquivo = Path.Combine(pastaDestino, nomeArquivoFinal);
            File.WriteAllText(caminhoArquivo, arquivo.content);
        }
    }
});

await StopProgressBarUpdater();
HideProgressBar();

// Mostrar resumo da exportação
string resumo = "Exportação concluída com sucesso!\n\n";
resumo += $"Diretório: {exportDir}\n\n";
resumo += "Estrutura criada:\n";

// Agrupar por categoria para o relatório
var objetosPorCategoria = objetosOrganizados
    .Select(kvp => new
    {
        Categoria = ExtrairCategoria(kvp.Key),
        Nome = kvp.Key,
        Quantidade = kvp.Value.Count,
        Arquivos = kvp.Value.Select(v => v.codeName).ToList()
    })
    .GroupBy(x => x.Categoria)
    .OrderBy(g => g.Key);

foreach (var categoria in objetosPorCategoria)
{
    resumo += $"\n{categoria.Key}:\n";
    foreach (var item in categoria.OrderBy(x => x.Nome))
    {
        resumo += $"  {item.Nome}/ ({item.Quantidade} arquivos)\n";
    }
}

// Mostrar estatísticas
int totalIgnorado = 0;
if (exportFromCache)
{
    totalIgnorado = Data.GMLCache.Count(entry => DeveIgnorar(entry.Key));
}
else
{
    totalIgnorado = Data.Code.Count(code => 
        code.ParentEntry == null && DeveIgnorar(code.Name.Content));
}

resumo += $"\n\n📊 Estatísticas:\n";
resumo += $"• Arquivos exportados: {totalRelevante}\n";
resumo += $"• Arquivos ignorados: {totalIgnorado}\n";
resumo += $"• Pastas criadas: {objetosOrganizados.Count}\n";

ScriptMessage(resumo);

// ================= FUNÇÕES AUXILIARES =================

bool DeveIgnorar(string codeName)
{
    // Ignorar Rooms com padrão RoomCC_ (irrelevantes)
    if (codeName.StartsWith("gml_RoomCC_"))
    {
        return true;
    }
    
    // Opcional: também ignorar outros tipos irrelevantes
    // if (codeName.StartsWith("gml_Object_obj_irrelevante_"))
    // {
    //     return true;
    // }
    
    return false;
}

void ProcessarCodigo(string codeName, string content)
{
    string nomeArquivo = ExtrairNomeArquivo(codeName);
    
    lock (objetosOrganizados)
    {
        if (!objetosOrganizados.ContainsKey(nomeArquivo))
        {
            objetosOrganizados[nomeArquivo] = new List<(string, string)>();
        }
        
        objetosOrganizados[nomeArquivo].Add((codeName, content));
    }
}

string ExtrairNomeArquivo(string codeName)
{
    if (codeName.StartsWith("gml_Object_"))
    {
        if (codeName.StartsWith("gml_Object_PreCreate_") || 
            codeName.StartsWith("gml_Object_CleanUp_"))
        {
            return "objetos";
        }

        string[] partes = codeName.Split('_');

        if (partes.Length >= 3)
        {
            string nomeObjeto = partes[2];

            for (int i = 3; i < partes.Length; i++)
            {
                // Se for número (_0, _1, etc), para
                if (int.TryParse(partes[i], out _))
                    break;

                // Lista de eventos do GameMaker
                if (
                    partes[i] == "Create" ||
                    partes[i] == "Destroy" ||
                    partes[i] == "Step" ||
                    partes[i] == "Draw" ||
                    partes[i] == "Alarm" ||
                    partes[i] == "Collision" ||
                    partes[i] == "Other" ||
                    partes[i] == "Keyboard" ||
                    partes[i] == "KeyPress" ||
                    partes[i] == "KeyRelease" ||
                    partes[i] == "Mouse" ||
                    partes[i] == "Gesture" ||
                    partes[i] == "Async" ||
                    partes[i] == "PreCreate" ||
                    partes[i] == "CleanUp"
                )
                {
                    break;
                }

                nomeObjeto += "_" + partes[i];
            }

            return nomeObjeto;
        }
    }

    else if (codeName.StartsWith("gml_GlobalScript_"))
    {
        return "Scripts";
    }
    else if (codeName.StartsWith("gml_Script_"))
    {
        return "Scripts";
    }
    else if (codeName.StartsWith("gml_Room_"))
    {
        string nomeRoom = codeName.Substring("gml_Room_".Length);
        if (nomeRoom.EndsWith("_Create"))
            nomeRoom = nomeRoom.Replace("_Create", "");
        return $"Room_{nomeRoom}";
    }

    return "Outros";
}


string ExtrairCategoria(string nomeArquivo)
{
    if (nomeArquivo.StartsWith("obj_"))
        return "Objetos";
    else if (nomeArquivo.StartsWith("Script_"))
        return "Scripts";
    else if (nomeArquivo.StartsWith("Room_"))
        return "Rooms";
    else if (nomeArquivo.StartsWith("Background_"))
        return "Backgrounds";
    else if (nomeArquivo.StartsWith("Timeline_"))
        return "Timelines";
    else if (nomeArquivo.StartsWith("Path_"))
        return "Paths";
    else if (nomeArquivo.StartsWith("Font_"))
        return "Fonts";
    else if (nomeArquivo == "objetos")
        return "Objetos (eventos globais)";
    else
        return "Outros";
}

string GetFolder(string path)
{
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}