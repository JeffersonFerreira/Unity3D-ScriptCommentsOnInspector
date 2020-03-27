using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

namespace Quack
{
    public class Preferences : SettingsProvider
    {
        const string xmlKey = "comments: use-xml";
        const string doubleSlashKey = "comments: use-//";
        const string fieldPrefixKey = "comments: field-prefix";

        public static bool useXml
        {
            get { return EditorPrefs.GetBool(xmlKey, true); }
            set { EditorPrefs.SetBool(xmlKey, value); }
        }

        public static bool useDoubleSlash
        {
            get { return EditorPrefs.GetBool(doubleSlashKey, true); }
            set { EditorPrefs.SetBool(doubleSlashKey, value); }
        }

        public static string fieldPrefix
        {
            get { return EditorPrefs.GetString(fieldPrefixKey, "(i) "); }
            set { EditorPrefs.SetString(fieldPrefixKey, value); }
        }


        [SettingsProvider]
        public override void OnGUI(string searchContext)
        {
            base.OnGUI(searchContext);
            fieldPrefix = EditorGUILayout.DelayedTextField("Field prefix", fieldPrefix);
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal("Box");
            GUILayout.Label("Read comment format:");
            useXml = GUILayout.Toggle(useXml, "XML");
            useDoubleSlash = GUILayout.Toggle(useDoubleSlash, "//");
            EditorGUILayout.EndHorizontal();
        }

        [SettingsProvider]
        static SettingsProvider MyNewPrefCode()
        {
            return new Preferences("Preferences/Comments");
        }

        public Preferences(string path, SettingsScope scopes = SettingsScope.User, IEnumerable<string> keywords = null) : base(path, scopes, keywords)
        {
        }
    }
}
