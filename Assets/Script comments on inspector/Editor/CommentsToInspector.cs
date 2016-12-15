using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Quack
{
    //[CustomEditor(typeof(MonoBehaviour), true)]
    public class CommentsToInspector : Editor
    {
        const string doubleSlash = "//";
        const string tripleSlash = "///";
        const string xmlOpenTag = "<summary>";
        const string xmlCloseTag = "</summary>";

        const string returnFieldNameRegexPattern = @"(\w+)(?:\s*[;=])";
        const string isClassRegexPattern = @"(\w+){0,1}\s+class\s+\w+";

        public static void DrawInspector(SerializedObject serializedObject, FieldAndComment[] comments)
        {
            EditorGUI.BeginChangeCheck();
            serializedObject.Update();

            foreach (FieldAndComment fieldAndComment in comments.Where(comment => string.IsNullOrEmpty(comment.field)).Reverse())
                GUILayout.TextArea(fieldAndComment.comment, "Box", GUILayout.ExpandWidth(true));

            SerializedProperty iterator = serializedObject.GetIterator();
            for (var enterChildren = true; iterator.NextVisible(enterChildren); enterChildren = false)
            {
                string comment = null;
                FieldAndComment field = comments.FirstOrDefault(f => f.field == iterator.name);
                if (field != null)
                    comment = field.comment;

                bool disableScope = iterator.propertyPath == "m_Script";

                EditorGUI.BeginDisabledGroup(disableScope);
                if (string.IsNullOrEmpty(comment))
                    EditorGUILayout.PropertyField(iterator, true);
                else
                    EditorGUILayout.PropertyField(iterator,
                        new GUIContent(Preferences.fieldPrefix + iterator.displayName, comment), true);

                EditorGUI.EndDisabledGroup();
            }
            serializedObject.ApplyModifiedProperties();
            EditorGUI.EndChangeCheck();
        }


        public static IEnumerable<FieldAndComment> GetCommentsForClass(Type classType)
        {
            string className = classType.ToString().Split('.').Last();

            string path = GetClassPath(className);

            if (string.IsNullOrEmpty(path))
                yield break;

            var reader = new StreamReader(path);

            string currentLine;

            while ((currentLine = reader.ReadLine()) != null)
            {
                if (string.IsNullOrEmpty(currentLine))
                    continue;

                currentLine = currentLine.Trim();
                FieldAndComment fieldAndComment = null;

                if (currentLine.StartsWith(tripleSlash) && currentLine.Contains(xmlOpenTag))
                {
                    fieldAndComment = ReadXmlComment(reader);
                    if (!Preferences.useXml)
                        fieldAndComment = null;
                }
                else if (currentLine.StartsWith(doubleSlash))
                {
                    fieldAndComment = ReadDoubleSlashComment(reader, currentLine);
                    if (!Preferences.useDoubleSlash)
                        fieldAndComment = null;
                }

                if (fieldAndComment != null)
                    yield return fieldAndComment;
            }

            reader.Dispose();

            if (classType.BaseType != null && classType.BaseType != typeof(Behaviour))
            {
                foreach (FieldAndComment fieldAndComment in GetCommentsForClass(classType.BaseType))
                    yield return fieldAndComment;
            }
        }

        static FieldAndComment ReadXmlComment(StreamReader reader)
        {
            var commentBuilder = new StringBuilder();
            string line;

            // Read all comments
            while ((line = reader.ReadLine().Trim()).StartsWith(tripleSlash) && !line.Contains(xmlCloseTag))
                commentBuilder.Append(ApplyUnityGuiFormatting(line.Remove(0, 3)));

            var checkIfIsClass = new Regex(isClassRegexPattern);

            while (string.IsNullOrEmpty(line = reader.ReadLine().Trim()) || line.StartsWith(tripleSlash) ||
                   line.StartsWith("[") && !line.EndsWith(";"))
                continue;

            var isClass = checkIfIsClass.IsMatch(line);
            if(isClass)
                return new FieldAndComment(string.Empty, commentBuilder.ToString());

            // If not end with ';', probaly it's a Method or Property
            if (!line.EndsWith(";"))
            {
                //Debug.LogError("This line don't ends with \";\": " + line);
                return null;
            }

            // Remove unnecessary things and return only field name

            // word that ends with ; or =, but return only the word
            var regex = new Regex(returnFieldNameRegexPattern);
            string fieldName = regex.Match(line).Groups[1].ToString();
            return new FieldAndComment(fieldName, commentBuilder.ToString());
        }

        static FieldAndComment ReadDoubleSlashComment(StreamReader reader, string currentLineData)
        {
            var commentBuilder = new StringBuilder();
            string fieldName;

            try
            {
                commentBuilder.Append(currentLineData.Remove(0, 2));

                string line;
                while ((line = reader.ReadLine().Trim()).StartsWith(doubleSlash))
                    commentBuilder.Append("\n" + line.Remove(0, 2));

                while (line.StartsWith("[") && !line.EndsWith(";"))
                    line = reader.ReadLine().Trim();

                var regex1 = new Regex(isClassRegexPattern);

                if (regex1.IsMatch(line))
                    return new FieldAndComment(string.Empty, commentBuilder.ToString());

                if (!line.EndsWith(";"))
                {
                    //Debug.Log("Double slash not end with semi-colon");
                    //Debug.Log(commentBuilder.ToString());
                    return null;
                }

                var regex = new Regex(returnFieldNameRegexPattern);
                fieldName = regex.Match(line).Groups[1].ToString();
            }
            catch (Exception)
            {
                return null;
            }
            return new FieldAndComment(fieldName, commentBuilder.ToString());
        }

        static string ApplyUnityGuiFormatting(string s)
        {
            // remove <code> * </code>
            var codeRegex = new Regex(@"(?:<code>)(.*?)(?:<\/code>)");

            // remove this: <see cref="text" /> or <seealso cref="text"/>
            var seeAlsoRegex = new Regex(@"<(?:see|seealso)\s+cref\s*=\s*""\s*(.*?)\s*""\s*\/>");

            if (codeRegex.IsMatch(s))
            {
                foreach (Match match in codeRegex.Matches(s))
                    s = s.Replace(match.Groups[0].Value, match.Groups[1].Value);
            }
            if (seeAlsoRegex.IsMatch(s))
            {
                foreach (Match match in seeAlsoRegex.Matches(s))
                    s = s.Replace(match.Groups[0].Value, match.Groups[1].Value);
            }
            return s;
        }

        [CanBeNull]
        static string GetClassPath(string className)
        {
            foreach (string assetGUID in AssetDatabase.FindAssets(string.Format("t:script {0}", className)))
            {
                string path = AssetDatabase.GUIDToAssetPath(assetGUID);
                string fileName = Path.GetFileNameWithoutExtension(path);

                if (string.Equals(fileName, className, StringComparison.InvariantCultureIgnoreCase))
                    return path;
            }

            return null;
        }
    }

    [CustomEditor(typeof(MonoBehaviour), true)]
    [CanEditMultipleObjects]
    public class CommentToMonoBehaviours : Editor
    {
        FieldAndComment[] comments;

        void OnEnable()
        {
            comments = CommentsToInspector.GetCommentsForClass(target.GetType()).ToArray();
        }

        public override void OnInspectorGUI()
        {
            CommentsToInspector.DrawInspector(serializedObject, comments);
        }
    }

    [CustomEditor(typeof(ScriptableObject), true)]
    public class CommentToScriptableObjects : Editor
    {
        FieldAndComment[] comments;

        void OnEnable()
        {
            comments = CommentsToInspector.GetCommentsForClass(target.GetType()).ToArray();
        }

        public override void OnInspectorGUI()
        {   
            CommentsToInspector.DrawInspector(serializedObject, comments);
        }
    }

    public class ClassComments
    {
        public FieldAndComment[] FieldAndComments { get; set; }
        public string classComment { get; set; }

        public ClassComments()
        {
            
        }

        public ClassComments(FieldAndComment[] fieldAndComments, string classComment)
        {
            FieldAndComments = fieldAndComments;
            this.classComment = classComment;
        }
    }

    public class FieldAndComment
    {
        public FieldAndComment(string field, string comment)
        {
            this.field = field;
            this.comment = comment;
        }

        public string field { get; set; }
        public string comment { get; set; }

        public override string ToString()
        {
            return string.Format("{0}: {1}", field, comment);
        }
    }
}