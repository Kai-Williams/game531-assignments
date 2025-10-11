using System;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using GameAssignment; 


namespace OpenTKFpsCamera
{
    internal static class Program
    {
        // Example 1
        private static void Main()
        {
            var nativeSettings = new NativeWindowSettings
            {
                Title = "OpenTK FPS Camera",
                Size = new OpenTK.Mathematics.Vector2i(1280, 720),
                APIVersion = new Version(4, 6),
            };


            using var game = new Game(GameWindowSettings.Default, nativeSettings);
            game.Run();
        }
    }
}