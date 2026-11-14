using System;
using System.Collections.Generic;

// Four-way navigation prevents corner cutting. Geometry supplies the blocked cells.
public sealed class UXGridPathfinder
{
    private readonly int width;
    private readonly int height;
    private readonly bool[] blocked;
    public UXGridPathfinder(int width, int height, bool[] blocked)
    {
        if (width <= 0 || height <= 0 || blocked == null || blocked.Length != width * height)
            throw new ArgumentException("Invalid navigation grid.");
        this.width = width; this.height = height; this.blocked = blocked;
    }

    public int NearestOpen(int x, int y)
    {
        int best = -1, distance = int.MaxValue;
        for (int i = 0; i < blocked.Length; i++)
        {
            if (blocked[i]) continue;
            int dx = i % width - x, dy = i / width - y;
            int d = dx * dx + dy * dy;
            if (d < distance) { best = i; distance = d; }
        }
        return best;
    }

    public List<int> Find(int start, int goal)
    {
        var result = new List<int>();
        if (start < 0 || goal < 0 || start >= blocked.Length || goal >= blocked.Length ||
            blocked[start] || blocked[goal]) return result;
        int[] previous = new int[blocked.Length];
        for (int i = 0; i < previous.Length; i++) previous[i] = -1;
        var queue = new Queue<int>();
        queue.Enqueue(start); previous[start] = start;
        while (queue.Count > 0 && previous[goal] < 0)
        {
            int cell = queue.Dequeue(), x = cell % width, y = cell / width;
            if (x > 0) Visit(cell - 1, cell, previous, queue);
            if (x + 1 < width) Visit(cell + 1, cell, previous, queue);
            if (y > 0) Visit(cell - width, cell, previous, queue);
            if (y + 1 < height) Visit(cell + width, cell, previous, queue);
        }
        if (previous[goal] < 0) return result;
        for (int at = goal; at != start; at = previous[at]) result.Add(at);
        result.Add(start); result.Reverse();
        return result;
    }

    private void Visit(int next, int from, int[] previous, Queue<int> queue)
    {
        if (blocked[next] || previous[next] >= 0) return;
        previous[next] = from; queue.Enqueue(next);
    }
}
