using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Wisp.UI
{
    // Render existing meshes without activating or cloning game scripts.
    public sealed class AreaMap : IDisposable
    {
        public RenderTexture Texture { get; private set; }
        public string Message { get; private set; } = "";
        private readonly List<Material> materials = new List<Material>();
        private sealed class Piece { public Mesh Mesh; public Matrix4x4 Matrix; public Material Material; }

        public void Load(string chapter, bool spoilers)
        {
            Dispose();
            Message = "";
            var field = FieldFor(chapter);
            if (field == null) { Message = "У этого раздела нет одной карты области. Выбери раздел локации."; return; }
            GameObject area = null;
            foreach (var map in Resources.FindObjectsOfTypeAll<GameMap>())
            {
                var info = typeof(GameMap).GetField(field, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (info != null) area = info.GetValue(map) as GameObject;
                if (area != null) break;
            }
            if (area == null) { Message = "Карта игры пока не загружена. Открой обычную карту Hollow Knight, затем вернись в Wisp."; return; }
            var mappedInfo = typeof(PlayerData).GetField("scenesMapped");
            var mapped = mappedInfo == null || PlayerData.instance == null ? null : mappedInfo.GetValue(PlayerData.instance) as List<string>;
            var pieces = new List<Piece>();
            Bounds bounds = new Bounds();
            bool first = true;
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) { Message = "Не удалось подготовить изображение карты."; return; }
            foreach (var filter in area.GetComponentsInChildren<MeshFilter>(true))
            {
                var renderer = filter.GetComponent<MeshRenderer>();
                if (filter.sharedMesh == null || renderer == null || renderer.sharedMaterial == null) continue;
                if (!spoilers && !MappedRoom(filter.transform, area.transform, mapped)) continue;
                var material = new Material(shader) { mainTexture = renderer.sharedMaterial.mainTexture };
                materials.Add(material);
                var matrix = area.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                var meshBounds = filter.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var corner = meshBounds.center + Vector3.Scale(meshBounds.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var point = matrix.MultiplyPoint3x4(corner);
                    if (first) { bounds = new Bounds(point, Vector3.zero); first = false; } else bounds.Encapsulate(point);
                }
                pieces.Add(new Piece { Mesh = filter.sharedMesh, Matrix = matrix, Material = material });
            }
            if (pieces.Count == 0) { Message = "Нет нанесённых на карту комнат. Обнови карту на скамейке или включи спойлеры для полной области."; return; }
            float w = Mathf.Max(bounds.size.x * 1.08f, 1f), h = Mathf.Max(bounds.size.y * 1.08f, 1f);
            int textureW = w >= h ? 1600 : Mathf.Max(256, Mathf.RoundToInt(1600 * w / h));
            int textureH = h >= w ? 1600 : Mathf.Max(256, Mathf.RoundToInt(1600 * h / w));
            Texture = new RenderTexture(textureW, textureH, 0, RenderTextureFormat.ARGB32);
            Texture.Create();
            var previous = RenderTexture.active;
            bool previousSrgb = GL.sRGBWrite;
            GL.PushMatrix();
            try
            {
                RenderTexture.active = Texture;
                GL.sRGBWrite = false;
                GL.Clear(true, true, new Color(0.035f, 0.05f, 0.075f, 1));
                GL.LoadProjectionMatrix(Matrix4x4.Ortho(bounds.center.x - w / 2, bounds.center.x + w / 2, bounds.center.y - h / 2, bounds.center.y + h / 2, -1000, 1000));
                GL.modelview = Matrix4x4.identity;
                foreach (var piece in pieces)
                {
                    if (!piece.Material.SetPass(0)) continue;
                    for (int submesh = 0; submesh < piece.Mesh.subMeshCount; submesh++) Graphics.DrawMeshNow(piece.Mesh, piece.Matrix, submesh);
                }
            }
            finally { GL.PopMatrix(); RenderTexture.active = previous; GL.sRGBWrite = previousSrgb; }
        }

        private static bool MappedRoom(Transform node, Transform root, List<string> mapped)
        {
            if (mapped == null) return false;
            for (var current = node; current != null && current != root; current = current.parent)
                if (mapped.Contains(current.name)) return true;
            return false;
        }

        private static string FieldFor(string chapter)
        {
            switch (chapter)
            {
                case "kings-pass": return "areaCliffs";
                case "abyss": return "areaAncientBasin";
                case "dirtmouth": return "areaDirtmouth";
                case "crossroads": return "areaCrossroads";
                case "greenpath": return "areaGreenpath";
                case "fungal": return "areaFungalWastes";
                case "city": return "areaCity";
                case "crystal": return "areaCrystalPeak";
                case "resting": return "areaRestingGrounds";
                case "waterways": return "areaWaterways";
                case "basin": return "areaAncientBasin";
                case "deepnest": return "areaDeepnest";
                case "edge": return "areaKingdomsEdge";
                default: return null;
            }
        }

        public void Dispose()
        {
            if (Texture != null) { Texture.Release(); UnityEngine.Object.Destroy(Texture); Texture = null; }
            foreach (var material in materials) UnityEngine.Object.Destroy(material);
            materials.Clear();
        }
    }
}
