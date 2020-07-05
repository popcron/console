using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Popcron.Console
{
    public class ConsoleSettingsProvider : SettingsProvider
    {
        private SerializedObject settings;

        public ConsoleSettingsProvider(string path, SettingsScope scope = SettingsScope.Project) : base(path, scope)
        {

        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            settings = new SerializedObject(Settings.Current);
        }

        public override void OnGUI(string searchContext)
        {
            float x = 7;
            float y = 10;
            Rect rect = new Rect(x, y, Screen.width - x, Screen.height - y);
            GUILayout.BeginArea(rect);
            SettingsInspector.Show(settings);
            GUILayout.EndArea();
        }

        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            ConsoleSettingsProvider provider = new ConsoleSettingsProvider("Project/Console", SettingsScope.Project)
            {
                keywords = new string[] { "console" }
            };

            return provider;
        }
    }
}