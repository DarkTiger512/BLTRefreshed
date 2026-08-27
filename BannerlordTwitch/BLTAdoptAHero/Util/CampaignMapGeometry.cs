using System;
using System.Collections.Generic;
using System.Linq;

namespace BLTAdoptAHero.Util
{
    public sealed class MapProjection
    {
        public float MinX { get; }
        public float MaxX { get; }
        public float MinY { get; }
        public float MaxY { get; }
        public float DisplayWidth { get; }
        public float DisplayHeight { get; }

        public MapProjection(float minX, float maxX, float minY, float maxY, float displayHeight = 100f)
        {
            MinX = minX;
            MaxX = maxX > minX ? maxX : minX + 1f;
            MinY = minY;
            MaxY = maxY > minY ? maxY : minY + 1f;
            DisplayHeight = displayHeight;
            DisplayWidth = displayHeight * (MaxX - MinX) / (MaxY - MinY);
        }

        public MapPoint Project(float x, float y)
        {
            float px = (x - MinX) / (MaxX - MinX) * DisplayWidth;
            float py = (1f - (y - MinY) / (MaxY - MinY)) * DisplayHeight;
            return new MapPoint(px, py);
        }
    }

    public readonly struct MapPoint
    {
        public float X { get; }
        public float Y { get; }
        public MapPoint(float x, float y) { X = x; Y = y; }
    }

    public readonly struct MapLine
    {
        public MapPoint A { get; }
        public MapPoint B { get; }
        public MapLine(MapPoint a, MapPoint b) { A = a; B = b; }
    }

    public readonly struct MapView
    {
        public float X { get; }
        public float Y { get; }
        public float Width { get; }
        public float Height { get; }
        public MapView(float x, float y, float width, float height) { X = x; Y = y; Width = width; Height = height; }
    }

    public sealed class MapMarkerInput
    {
        public string Id { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
    }

    public static class CampaignMapGeometry
    {
        public static MapView FocusView(float mapWidth, float mapHeight, float focusX, float focusY, float zoom)
        {
            zoom = Math.Max(1f, zoom);
            float width = mapWidth / zoom, height = mapHeight / zoom;
            float x = Math.Max(0, Math.Min(mapWidth - width, focusX - width / 2));
            float y = Math.Max(0, Math.Min(mapHeight - height, focusY - height / 2));
            return new MapView(x, y, width, height);
        }

        // Marching-squares at the midpoint of each cell edge. Ambiguous cells use
        // two stable segments so identical terrain always produces identical paths.
        public static List<MapLine> TraceContours(bool[,] land, MapProjection projection,
            float minX, float maxX, float minY, float maxY)
        {
            int width = land.GetLength(0);
            int height = land.GetLength(1);
            var lines = new List<MapLine>();
            if (width < 2 || height < 2) return lines;

            float dx = (maxX - minX) / (width - 1);
            float dy = (maxY - minY) / (height - 1);
            for (int x = 0; x < width - 1; x++)
            for (int y = 0; y < height - 1; y++)
            {
                int mask = (land[x, y] ? 1 : 0) |
                           (land[x + 1, y] ? 2 : 0) |
                           (land[x + 1, y + 1] ? 4 : 0) |
                           (land[x, y + 1] ? 8 : 0);
                if (mask == 0 || mask == 15) continue;

                float x0 = minX + x * dx, x1 = x0 + dx;
                float y0 = minY + y * dy, y1 = y0 + dy;
                var bottom = projection.Project((x0 + x1) * .5f, y0);
                var right = projection.Project(x1, (y0 + y1) * .5f);
                var top = projection.Project((x0 + x1) * .5f, y1);
                var left = projection.Project(x0, (y0 + y1) * .5f);

                switch (mask)
                {
                    case 1: case 14: Add(left, bottom); break;
                    case 2: case 13: Add(bottom, right); break;
                    case 3: case 12: Add(left, right); break;
                    case 4: case 11: Add(right, top); break;
                    case 5: Add(left, top); Add(bottom, right); break;
                    case 6: case 9: Add(bottom, top); break;
                    case 7: case 8: Add(left, top); break;
                    case 10: Add(left, bottom); Add(right, top); break;
                }
            }
            return lines;

            void Add(MapPoint a, MapPoint b) => lines.Add(new MapLine(a, b));
        }

        public static IReadOnlyDictionary<string, string> ClusterMarkers(
            IEnumerable<MapMarkerInput> markers, float radius)
        {
            var ordered = markers.OrderBy(m => m.Id, StringComparer.Ordinal).ToList();
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            var visited = new bool[ordered.Count];
            for (int i = 0; i < ordered.Count; i++)
            {
                if (visited[i]) continue;
                var members = new List<int> { i };
                visited[i] = true;
                for (int cursor = 0; cursor < members.Count; cursor++)
                {
                    int a = members[cursor];
                    for (int j = 0; j < ordered.Count; j++)
                    {
                        if (visited[j]) continue;
                        float dx = ordered[a].X - ordered[j].X;
                        float dy = ordered[a].Y - ordered[j].Y;
                        if (dx * dx + dy * dy <= radius * radius)
                        {
                            visited[j] = true;
                            members.Add(j);
                        }
                    }
                }
                string clusterId = string.Join("+", members.Select(index => ordered[index].Id));
                foreach (int member in members) result[ordered[member].Id] = clusterId;
            }
            return result;
        }

        public static IReadOnlyList<string> PrioritizeLabels<T>(IEnumerable<T> values,
            Func<T, bool> isHero, Func<T, bool> isTown, Func<T, string> id)
            => values.OrderByDescending(isHero)
                .ThenByDescending(isTown)
                .ThenBy(id, StringComparer.Ordinal)
                .Select(id).ToList();
    }
}
