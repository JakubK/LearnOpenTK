using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace LearnOpenTK
{
    public static class Program
    {
        private static void Main()
        {
            var nativeWindowSettings = new NativeWindowSettings()
            {
                ClientSize = new Vector2i(800, 600),
                Title = "LearnOpenTK - Materials",
                Flags = ContextFlags.ForwardCompatible // This is needed to run on macos
                        | ContextFlags.Debug,
            };

            using (var window = new Window(GameWindowSettings.Default, nativeWindowSettings))
            {
                window.Run();
            }
        }
    }
}
