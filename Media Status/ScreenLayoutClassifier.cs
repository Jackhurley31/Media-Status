using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

public static class ScreenLayoutClassifier
{
    public class ScreenInfo
    {
        public Screen Screen { get; set; }
        public string Label { get; set; }
    }

    public static List<ScreenInfo> GetClassifiedScreens()
    {
        // Sort screens physically from top to bottom (Y axis), then left to right (X axis)
        var screens = Screen.AllScreens
                            .OrderBy(s => s.Bounds.Y)
                            .ThenBy(s => s.Bounds.X)
                            .ToArray();

        var result = new List<ScreenInfo>();
        if (screens.Length == 0) return result;

        var primary = Screen.PrimaryScreen ?? screens[0];
        int primaryCenterX = primary.Bounds.X + (primary.Bounds.Width / 2);
        int primaryCenterY = primary.Bounds.Y + (primary.Bounds.Height / 2);

        for (int i = 0; i < screens.Length; i++)
        {
            var s = screens[i];
            string posLabel;

            if (s.Equals(primary))
            {
                posLabel = "Main Display";
            }
            else
            {
                int screenCenterX = s.Bounds.X + (s.Bounds.Width / 2);
                int screenCenterY = s.Bounds.Y + (s.Bounds.Height / 2);

                int deltaX = screenCenterX - primaryCenterX;
                int deltaY = screenCenterY - primaryCenterY;

                if (Math.Abs(deltaY) > Math.Abs(deltaX))
                {
                    posLabel = deltaY < 0 ? "Top Display" : "Bottom Display";
                }
                else
                {
                    posLabel = deltaX < 0 ? "Left Display" : "Right Display";
                }
            }

            string primaryTag = s.Primary ? " [Primary]" : "";
            string label = $"{posLabel} ({s.Bounds.Width}x{s.Bounds.Height}){primaryTag}";

            result.Add(new ScreenInfo
            {
                Screen = s,
                Label = label
            });
        }

        return result;
    }
}