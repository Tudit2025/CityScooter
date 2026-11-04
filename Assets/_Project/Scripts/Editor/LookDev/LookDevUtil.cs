using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CityScooter.EditorTools.LookDev
{
    static class LookDevUtil
    {
        public const string LogPrefix = "[CityScooter] LookDev: ";

        public static void Log(string message) => Debug.Log(LogPrefix + message);

        public static void Warn(string message) => Debug.LogWarning(LogPrefix + message);

        /// <summary>Absolute path of the folder that contains Assets/.</summary>
        public static string ProjectRoot => Path.GetDirectoryName(Application.dataPath);

        public static string ToFullPath(string assetPath) => Path.Combine(ProjectRoot, assetPath);

        /// <summary>Creates every missing folder of an asset path like "Assets/A/B".</summary>
        public static void EnsureFolder(string folder)
        {
            folder = folder.TrimEnd('/');
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        /// <summary>Value after a command line flag, e.g. -lookdevOut C:/x. Null when absent.</summary>
        public static string GetArg(string flag)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], flag, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
            }
            return null;
        }

        public static bool HasArg(string flag) => Array.Exists(Environment.GetCommandLineArgs(),
            a => string.Equals(a, flag, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Loads an asset or creates it. Existing assets are only modified when <paramref name="overwrite"/> is set,
        /// so tuning done by the tech artist survives "build missing" runs. GUIDs are always kept.
        /// Returns true when <paramref name="configure"/> ran.
        /// </summary>
        public static bool GetOrCreate<T>(string path, bool overwrite, Func<T> create, Action<T> configure, out T asset)
            where T : Object
        {
            asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
                asset = create();
                configure(asset);
                AssetDatabase.CreateAsset(asset, path);
                return true;
            }
            if (!overwrite) return false;
            configure(asset);
            EditorUtility.SetDirty(asset);
            return true;
        }

        public static Shader FindShader(string name)
        {
            Shader shader = Shader.Find(name);
            if (shader == null) throw new InvalidOperationException("Shader not found: " + name);
            return shader;
        }

        public static Shader LoadShader(string path)
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            if (shader == null) throw new InvalidOperationException("Shader asset missing: " + path);
            return shader;
        }

        public static Color Hex(string html)
        {
            if (!ColorUtility.TryParseHtmlString(html, out Color c)) throw new ArgumentException("Bad colour " + html);
            return c;
        }
    }
}
