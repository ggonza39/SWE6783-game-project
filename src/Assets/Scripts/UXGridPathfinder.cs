using System;
using System.Collections.Generic;

// Four-way navigation prevents corner cutting. Geometry supplies the blocked cells.
public sealed class UXGridPathfinder
{
    private readonly int width;
    private readonly int height;
    private readonly bool[] blocked;
    private readonly int[] previous,visited,queue;
    private int stamp,tail;
    public UXGridPathfinder(int width, int height, bool[] blocked)
    {
        if (width <= 0 || height <= 0 || blocked == null || blocked.Length != width * height)
            throw new ArgumentException("Invalid navigation grid.");
        this.width = width; this.height = height; this.blocked = blocked;
        previous=new int[blocked.Length];visited=new int[blocked.Length];queue=new int[blocked.Length];
    }

    public int NearestOpen(int x, int y)
    {
        if(x>=0 && x<width && y>=0 && y<height && !blocked[y*width+x])return y*width+x;
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
        if(++stamp==int.MaxValue){Array.Clear(visited,0,visited.Length);stamp=1;}
        int head=0;tail=1;queue[0]=start;previous[start]=start;visited[start]=stamp;
        while (head<tail && visited[goal]!=stamp)
        {
            int cell = queue[head++], x = cell % width, y = cell / width;
            if (x > 0) Visit(cell - 1, cell);
            if (x + 1 < width) Visit(cell + 1, cell);
            if (y > 0) Visit(cell - width, cell);
            if (y + 1 <height) Visit(cell + width, cell);
        }
        if (visited[goal]!=stamp) return result;
        for (int at = goal; at != start; at = previous[at]) result.Add(at);
        result.Add(start); result.Reverse();
        return result;
    }

    private void Visit(int next, int from)
    {
        if (blocked[next] || visited[next]==stamp) return;
        previous[next] = from;visited[next]=stamp;queue[tail++]=next;
    }
}
