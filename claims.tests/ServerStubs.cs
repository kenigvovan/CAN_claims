using System.Reflection;
using System.Runtime.CompilerServices;
using claims.src;
using claims.src.database;
using Moq;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace claims.tests
{
    /// <summary>
    /// The mod's statics as code past the early exits expects them, without a server: a loose
    /// database handler mock behind claims.getModInstance(), a sapi whose world finds no online
    /// player (so personal messages go nowhere), and Lang returning keys. Restores what it replaced.
    /// </summary>
    public sealed class ServerStubs : IDisposable
    {
        private static readonly FieldInfo DatabaseHandlerField =
            typeof(claims.src.claims).GetField("databaseHandler", BindingFlags.NonPublic | BindingFlags.Static)!;

        private readonly ICoreServerAPI previousSapi = claims.src.claims.sapi;
        private readonly claims.src.claims previousInstance = claims.src.claims.modInstance;
        private readonly object? previousHandler = DatabaseHandlerField.GetValue(null);
        private readonly Config previousConfig = claims.src.claims.config;
        private readonly DataStorage previousStorage = claims.src.claims.dataStorage;

        public readonly Mock<DatabaseHandler> Db = new();
        public readonly DataStorage Storage = new(false);

        public ServerStubs(Config? config = null)
        {
            var world = new Mock<IServerWorldAccessor> { DefaultValue = DefaultValue.Mock };
            var sapi = new Mock<ICoreServerAPI> { DefaultValue = DefaultValue.Mock };
            sapi.Setup(a => a.World).Returns(world.Object);
            claims.src.claims.sapi = sapi.Object;

            claims.src.claims.modInstance = (claims.src.claims)RuntimeHelpers.GetUninitializedObject(typeof(claims.src.claims));
            DatabaseHandlerField.SetValue(null, Db.Object);

            claims.src.claims.config = config ?? new Config();
            claims.src.claims.dataStorage = Storage;

            if (!Lang.AvailableLanguages.ContainsKey("en"))
            {
                var langMock = new Mock<ITranslationService>();
                langMock.Setup(t => t.Get(It.IsAny<string>(), It.IsAny<object[]>()))
                    .Returns<string, object[]>((key, _) => key);
                langMock.Setup(t => t.HasTranslation(It.IsAny<string>(), It.IsAny<bool>()))
                    .Returns(false);
                langMock.Setup(t => t.HasTranslation(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
                    .Returns(false);
                Lang.AvailableLanguages["en"] = langMock.Object;
            }
            Lang.ChangeLanguage("en");
        }

        public void Dispose()
        {
            claims.src.claims.sapi = previousSapi;
            claims.src.claims.modInstance = previousInstance;
            DatabaseHandlerField.SetValue(null, previousHandler);
            claims.src.claims.config = previousConfig;
            claims.src.claims.dataStorage = previousStorage;
        }
    }
}
