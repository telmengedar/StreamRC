using NightlyCode.Modules;
using NUnit.Framework;
using StreamRC.Core.Scripts;

namespace Core.Tests {

    [TestFixture]
    public class ScriptTests {

        [Test]
        public void ExecuteMethod() {
            ModuleContext context = new ModuleContext();
            context.AddModule<ScriptModule>();

            string[] result = context.GetModule<ScriptModule>().Execute("scripts.listmodules()") as string[];
            Assert.NotNull(result);
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("scripts", result[0]);
        }
    }
}