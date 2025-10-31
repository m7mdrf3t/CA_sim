#if UNITY_EDITOR
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

public class CGPlusHashToolsWindow : EditorWindow
{
    private string inputCode = "";
    private string sha256Hex = "";

    [MenuItem("CGPlus/Hash Tools/SHA-256 Window")]
    public static void ShowWindow()
    {
        var win = GetWindow<CGPlusHashToolsWindow>("SHA-256 Hasher");
        win.minSize = new Vector2(420, 160);
        win.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("Admin Code → SHA-256 (hex)", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        inputCode = EditorGUILayout.TextField("Input Code", inputCode);

        if (GUILayout.Button("Compute SHA-256"))
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(inputCode ?? ""));
                var sb = new StringBuilder(bytes.Length * 2);
                foreach (var b in bytes) sb.Append(b.ToString("x2"));
                sha256Hex = sb.ToString();
                EditorGUIUtility.systemCopyBuffer = sha256Hex;
            }
        }

        EditorGUILayout.LabelField("Hash (copied to clipboard):");
        EditorGUILayout.SelectableLabel(sha256Hex, GUILayout.Height(40));
    }
}
#endif