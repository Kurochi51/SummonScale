using System;
using System.Collections.Generic;

using Dalamud;
using Lumina.Excel;
using Dalamud.Memory;
using Dalamud.Plugin.Services;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Game.ClientState.Objects.Enums;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.Interop;
using PetScale.Enums;

namespace PetScale.Helpers;

public class Utilities(IDataManager _dataManager, IPluginLog _pluginLog)
{
    private readonly IDataManager dataManager = _dataManager;
    private readonly IPluginLog log = _pluginLog;

    /// <summary>
    ///     Attempt to retrieve an <see cref="ExcelSheet{T}"/>, optionally in a specific <paramref name="language"/>.
    /// </summary>
    /// <returns><see cref="ExcelSheet{T}"/> or <see langword="null"/> if <see cref="IDataManager.GetExcelSheet{T}(ClientLanguage)"/> returns an invalid sheet.</returns>
    public ExcelSheet<T>? GetSheet<T>(ClientLanguage language = ClientLanguage.English) where T : ExcelRow
    {
        try
        {
            var sheet = dataManager.GetExcelSheet<T>(language);
            if (sheet is null)
            {
                log.Fatal("Invalid lumina sheet!");
            }
            return sheet;
        }
        catch (Exception e)
        {
            log.Fatal("Retrieving lumina sheet failed!");
            log.Fatal(e.Message);
            return null;
        }
    }

    public unsafe void SetScale(BattleChara* summon, float scale)
    {
        if (summon is null)
        {
            return;
        }
        summon->Character.CharacterData.ModelScale = scale;
        summon->Character.GameObject.Scale = scale;
        var drawObject = summon->Character.GameObject.GetDrawObject();
        if (drawObject is not null)
        {
            drawObject->Object.Scale.X = scale;
            drawObject->Object.Scale.Y = scale;
            drawObject->Object.Scale.Z = scale;
        }
    }

    private static unsafe DrawState* ActorDrawState(FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject* actor)
        => (DrawState*)&actor->RenderFlags;

    public static unsafe void ToggleVisibility(FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject* actor)
    {
        if (actor is null || actor->ObjectID is 0xE0000000)
        {
            return;
        }
        *ActorDrawState(actor) ^= DrawState.Invisibility;
    }

    public static unsafe void CachePlayerList(uint playerObjectId, Queue<string> queue, Span<Pointer<BattleChara>> CharacterSpan)
    {
        foreach (var chara in CharacterSpan)
        {
            if (chara.Value is null || &chara.Value->Character is null)
            {
                continue;
            }
            if (chara.Value->Character.GameObject.ObjectKind is not (byte)ObjectKind.Player || chara.Value->Character.GameObject.ObjectID is 0xE0000000)
            {
                continue;
            }
            if (chara.Value->Character.GameObject.ObjectID != playerObjectId)
            {
                queue.Enqueue(MemoryHelper.ReadStringNullTerminated((nint)chara.Value->Character.GameObject.GetName()));
            }
        }
    }

    public static IFontHandle GetFixedFontAwesomeIconFont(IFontAtlas atlas, float sizePx)
    {
        return atlas.NewDelegateFontHandle(
            e => e.OnPreBuild(
                tk =>
                {
                    var fixedFont = tk.AddFontAwesomeIconFont(new()
                    {
                        SizePx = sizePx,
                        GlyphMinAdvanceX = sizePx,
                        GlyphMaxAdvanceX = sizePx,
                    });
                    tk.SetFontScaleMode(fixedFont, FontScaleMode.UndoGlobalScale);
                }));
    }
}