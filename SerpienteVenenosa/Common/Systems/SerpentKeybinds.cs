using System.Collections.Generic;
using Terraria.ModLoader;

namespace SerpienteVenenosa.Common.Systems;

public class SerpentKeybinds : ModSystem
{
	/// <summary>Escupir veneno estando transformado. Se puede reasignar en Ajustes → Controles.</summary>
	public static ModKeybind SpitVenom { get; private set; }

	public override void Load() {
		SpitVenom = KeybindLoader.RegisterKeybind(Mod, "SpitVenom", "F");
	}

	public override void Unload() {
		SpitVenom = null;
	}

	/// <summary>Texto de ayuda con la tecla asignada, para tooltips.</summary>
	public static string SpitKeyHint(Mod mod) {
		List<string> keys = SpitVenom?.GetAssignedKeys() ?? [];
		string key = keys.Count > 0 ? keys[0] : mod.GetLocalization("Common.Unbound").Value;
		return mod.GetLocalization("Common.SpitKeyHint").Format(key);
	}
}
