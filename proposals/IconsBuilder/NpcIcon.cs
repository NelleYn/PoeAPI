// EXPERIMENTAL candidate ported from exApiTools/IconsBuilder — see proposals/IconsBuilder/README.md. Not part of the build.

using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Abstract;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;

namespace ExileCore.IconsBuilder;

/// <summary>Icon for friendly NPCs (town/league vendors etc.).</summary>
public class NpcIcon : BaseIcon
{
    /// <summary>Builds an NPC icon, falling back to the generic NPC sprite when the game supplies no minimap icon.</summary>
    public NpcIcon(Entity entity, GameController gameController, IconsBuilderSettings settings) : base(entity, settings)
    {
        if (!_HasIngameIcon) MainTexture = new HudTexture("Icons.png");

        MainTexture.Size = settings.SizeNpcIcon;
        var component = entity.GetComponent<Render>();
        Text = component?.Name?.Split(',')[0];
        Show = () => entity.IsValid;
        if (_HasIngameIcon) return;

        if (entity.Path.StartsWith("Metadata/NPC/League/Cadiro"))
            MainTexture.UV = SpriteHelper.GetUV(MapIconsIndex.QuestObject);
        else if (entity.Path.StartsWith("Metadata/Monsters/LeagueBetrayal/MasterNinjaCop"))
            MainTexture.UV = SpriteHelper.GetUV(MapIconsIndex.BetrayalSymbolDjinn);
        else
            MainTexture.UV = SpriteHelper.GetUV(MapIconsIndex.NPC);
    }
}
