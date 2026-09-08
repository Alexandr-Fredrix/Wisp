using Modding;
using System.Reflection;

namespace Wisp
{
    public sealed class WispMod : Mod
    {
        public WispMod() : base("Wisp") { }

        public override string GetVersion() =>
            typeof(WispMod).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion ?? "development";

        public override void Initialize()
        {
            Log("Wisp foundation loaded. Guide features are not implemented yet.");
        }
    }
}
