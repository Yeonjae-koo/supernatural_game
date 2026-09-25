using System.Collections.Generic;
using UnityEngine;
namespace BeforeJericho.UI
{
    /// <summary>Blueprint-style floor plan (HTML map colours) rasterized once per scene from the greybox colliders:
    /// tall blocks filled with an outline, floors as faint outlines. No hand-authored per-chapter maps needed.</summary>
    public static class MapPlan
    {
        static readonly Color Fill = UIStyle.Hex(0x2a2c2e), Stroke = UIStyle.Hex(0x9a968c), Floor = UIStyle.Hex(0x55544f, .55f);
        public static Texture2D Build(Vector3 focus, out Rect world, int size = 768)
        {
            var boxes = new List<BoxCollider>();
            foreach(var b in Object.FindObjectsByType<BoxCollider>(FindObjectsInactive.Exclude))
                if(!b.isTrigger && b.gameObject.layer == 0 && !b.GetComponentInParent<HuntInteractable>()) boxes.Add(b);
            var bounds = new Bounds(focus, Vector3.one * 10);
            foreach(var b in boxes) if(b.bounds.size.x < 120 && b.bounds.size.z < 120) bounds.Encapsulate(b.bounds);
            float span = Mathf.Max(bounds.size.x, bounds.size.z) + 4;
            world = new Rect(bounds.center.x - span / 2, bounds.center.z - span / 2, span, span);
            var pixels = new Color32[size * size];
            // Floors first (faint), then walls/buildings on top.
            foreach(var b in boxes) if(b.bounds.size.y < 1.2f) Draw(pixels, size, world, b, false);
            foreach(var b in boxes) if(b.bounds.size.y >= 1.2f) Draw(pixels, size, world, b, true);
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, name = "Map plan" };
            tex.SetPixels32(pixels); tex.Apply(false, true);
            return tex;
        }

        static void Draw(Color32[] px, int size, Rect world, BoxCollider box, bool solid)
        {
            // Oriented footprint: test pixels against the box's local XZ extent.
            var t = box.transform; Vector3 c = t.TransformPoint(box.center);
            Vector3 ax = t.TransformVector(new Vector3(box.size.x, 0, 0)), az = t.TransformVector(new Vector3(0, 0, box.size.z));
            ax.y = 0; az.y = 0; float hx = ax.magnitude / 2, hz = az.magnitude / 2;
            if(hx < .05f || hz < .05f) return;
            Vector2 ux = new Vector2(ax.x, ax.z).normalized, uz = new Vector2(az.x, az.z).normalized, center = new(c.x, c.z);
            Bounds b = box.bounds; float scale = size / world.width, edge = 1.5f / scale;
            int x0 = Mathf.Max(0, (int)((b.min.x - world.x) * scale)), x1 = Mathf.Min(size - 1, (int)((b.max.x - world.x) * scale) + 1);
            int y0 = Mathf.Max(0, (int)((b.min.z - world.y) * scale)), y1 = Mathf.Min(size - 1, (int)((b.max.z - world.y) * scale) + 1);
            for(int y = y0; y <= y1; y++)
                for(int x = x0; x <= x1; x++)
                {
                    Vector2 d = new Vector2(world.x + (x + .5f) / scale, world.y + (y + .5f) / scale) - center;
                    float lx = Mathf.Abs(Vector2.Dot(d, ux)), lz = Mathf.Abs(Vector2.Dot(d, uz));
                    if(lx > hx || lz > hz) continue;
                    bool border = lx > hx - edge || lz > hz - edge;
                    if(solid) px[y * size + x] = border ? Stroke : Fill;
                    else if(border && px[y * size + x].a == 0) px[y * size + x] = Floor;
                }
        }

        public static Vector2 ToMap(Vector3 p, Rect world) => new((p.x - world.x) / world.width, (p.z - world.y) / world.height);
    }
}
