using System.Collections.Generic;
using System.Reflection;
using Wisp.Core;

namespace Wisp.Game
{
    // Field existence is checked explicitly: a missing field must never imply completion.
    public sealed class PlayerReader : IPlayerState
    {
        private readonly Dictionary<string, FieldInfo> fields = new Dictionary<string, FieldInfo>();
        private FieldInfo Field(string name)
        {
            FieldInfo field;
            if (!fields.TryGetValue(name, out field))
            {
                field = typeof(PlayerData).GetField(name, BindingFlags.Instance | BindingFlags.Public);
                fields[name] = field;
            }
            return field;
        }

        public bool TryBool(string name, out bool value)
        {
            value = false;
            var field = Field(name);
            if (PlayerData.instance == null || field == null || field.FieldType != typeof(bool)) return false;
            value = (bool)field.GetValue(PlayerData.instance);
            return true;
        }

        public bool TryInt(string name, out int value)
        {
            value = 0;
            var field = Field(name);
            if (PlayerData.instance == null || field == null || field.FieldType != typeof(int)) return false;
            value = (int)field.GetValue(PlayerData.instance);
            return true;
        }

        public string Zone
        {
            get
            {
                var zoneField = Field("mapZone");
                return PlayerData.instance == null || zoneField == null ? "" : System.Convert.ToString(zoneField.GetValue(PlayerData.instance));
            }
        }
    }
}
