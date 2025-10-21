using UnityEngine;
using System.Collections.Generic;
using System.IO;
/// <summary>
/// Simple but robust OBJ file exporter
/// </summary>
public static class OBJExporter
{
    public static void Export(GameObject root, string path)
    {
        var objContent = new System.Text.StringBuilder();
        var mtlContent = new System.Text.StringBuilder();

        string mtlFileName = System.IO.Path.GetFileNameWithoutExtension(path) + ".mtl";
        string mtlPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path), mtlFileName);

        // Header
        objContent.AppendLine("# Exported from Unity - HyparHangers");
        objContent.AppendLine($"# Created: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        objContent.AppendLine($"# Total Objects: {root.transform.childCount}");
        objContent.AppendLine($"mtllib {mtlFileName}");
        objContent.AppendLine();

        mtlContent.AppendLine("# Material Library");
        mtlContent.AppendLine($"# Created: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        mtlContent.AppendLine();

        // Collect all meshes
        var meshFilters = root.GetComponentsInChildren<MeshFilter>();
        var materialDict = new Dictionary<Material, string>();
        int vertexOffset = 1;
        int normalOffset = 1;
        int uvOffset = 1;
        int materialIndex = 0;

        foreach (var mf in meshFilters)
        {
            if (mf.sharedMesh == null) continue;

            Mesh mesh = mf.sharedMesh;
            Transform trans = mf.transform;
            var renderer = mf.GetComponent<MeshRenderer>();

            // Object name
            objContent.AppendLine($"o {SanitizeName(mf.gameObject.name)}");
            objContent.AppendLine($"g {SanitizeName(mf.gameObject.name)}");

            // Write vertices (world space)
            Vector3[] vertices = mesh.vertices;
            foreach (Vector3 v in vertices)
            {
                Vector3 worldV = trans.TransformPoint(v);
                objContent.AppendLine($"v {worldV.x:F6} {worldV.y:F6} {worldV.z:F6}");
            }

            // Write normals
            Vector3[] normals = mesh.normals;
            if (normals.Length > 0)
            {
                foreach (Vector3 n in normals)
                {
                    Vector3 worldN = trans.TransformDirection(n).normalized;
                    objContent.AppendLine($"vn {worldN.x:F6} {worldN.y:F6} {worldN.z:F6}");
                }
            }

            // Write UVs
            Vector2[] uvs = mesh.uv;
            if (uvs.Length > 0)
            {
                foreach (Vector2 uv in uvs)
                {
                    objContent.AppendLine($"vt {uv.x:F6} {uv.y:F6}");
                }
            }

            // Material
            Material material = renderer?.sharedMaterial;
            if (material != null)
            {
                if (!materialDict.ContainsKey(material))
                {
                    string matName = $"mat_{materialIndex++}_{SanitizeName(material.name)}";
                    materialDict[material] = matName;
                    WriteMaterial(mtlContent, material, matName);
                }
                objContent.AppendLine($"usemtl {materialDict[material]}");
            }

            // Write faces
            int[] triangles = mesh.triangles;
            bool hasNormals = normals.Length > 0;
            bool hasUVs = uvs.Length > 0;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                int v1 = triangles[i] + vertexOffset;
                int v2 = triangles[i + 1] + vertexOffset;
                int v3 = triangles[i + 2] + vertexOffset;

                if (hasUVs && hasNormals)
                {
                    int vt1 = triangles[i] + uvOffset;
                    int vt2 = triangles[i + 1] + uvOffset;
                    int vt3 = triangles[i + 2] + uvOffset;
                    int vn1 = triangles[i] + normalOffset;
                    int vn2 = triangles[i + 1] + normalOffset;
                    int vn3 = triangles[i + 2] + normalOffset;
                    objContent.AppendLine($"f {v1}/{vt1}/{vn1} {v2}/{vt2}/{vn2} {v3}/{vt3}/{vn3}");
                }
                else if (hasNormals)
                {
                    int vn1 = triangles[i] + normalOffset;
                    int vn2 = triangles[i + 1] + normalOffset;
                    int vn3 = triangles[i + 2] + normalOffset;
                    objContent.AppendLine($"f {v1}//{vn1} {v2}//{vn2} {v3}//{vn3}");
                }
                else
                {
                    objContent.AppendLine($"f {v1} {v2} {v3}");
                }
            }

            objContent.AppendLine();

            // Update offsets
            vertexOffset += vertices.Length;
            normalOffset += normals.Length;
            uvOffset += uvs.Length;
        }

        // Write files
        System.IO.File.WriteAllText(path, objContent.ToString());
        System.IO.File.WriteAllText(mtlPath, mtlContent.ToString());
    }

    private static void WriteMaterial(System.Text.StringBuilder sb, Material mat, string name)
    {
        sb.AppendLine($"newmtl {name}");

        // Diffuse color
        if (mat.HasProperty("_Color"))
        {
            Color c = mat.color;
            sb.AppendLine($"Kd {c.r:F6} {c.g:F6} {c.b:F6}");
        }
        else
        {
            sb.AppendLine("Kd 0.8 0.8 0.8");
        }

        // Specular
        if (mat.HasProperty("_SpecColor"))
        {
            Color spec = mat.GetColor("_SpecColor");
            sb.AppendLine($"Ks {spec.r:F6} {spec.g:F6} {spec.b:F6}");
        }
        else
        {
            sb.AppendLine("Ks 0.5 0.5 0.5");
        }

        // Shininess
        if (mat.HasProperty("_Glossiness"))
        {
            float gloss = mat.GetFloat("_Glossiness");
            sb.AppendLine($"Ns {gloss * 1000:F2}");
        }
        else
        {
            sb.AppendLine("Ns 100.0");
        }

        // Transparency
        if (mat.HasProperty("_Color"))
        {
            float alpha = mat.color.a;
            sb.AppendLine($"d {alpha:F6}");
            if (alpha < 1.0f)
                sb.AppendLine("Tr 0.0");
        }

        // Illumination model (2 = highlight on)
        sb.AppendLine("illum 2");
        sb.AppendLine();
    }

    private static string SanitizeName(string name)
    {
        // Remove invalid characters for OBJ format
        return name.Replace(" ", "_")
                   .Replace("(", "")
                   .Replace(")", "")
                   .Replace("#", "")
                   .Replace(".", "_");
    }
}
