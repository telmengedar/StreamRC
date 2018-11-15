using System.Reflection;
using NightlyCode.Core.ComponentModel;

namespace StreamRC.Streaming.Cache {
    public class ResourceImage : IImageSource {
        readonly Assembly assembly;
        readonly string path;

        public ResourceImage(Assembly assembly, string path) {
            this.assembly = assembly;
            this.path = path;
        }

        public string Key => $"res://{path}";

        public System.IO.Stream Data => ResourceAccessor.GetResource<System.IO.Stream>(assembly, path);
    }
}