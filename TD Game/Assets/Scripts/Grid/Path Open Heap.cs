using System.Collections.Generic;

public sealed class PathOpenHeap
{
    private struct Entry
    {
        public GridTile tile;
        public int score;
        public long order;
    }

    private readonly List<Entry> entries = new List<Entry>();
    private readonly Dictionary<GridTile, int> indices = new Dictionary<GridTile, int>();
    private long nextOrder;
    public int Count => entries.Count;

    public void Clear()
    {
        entries.Clear();
        indices.Clear();
        nextOrder = 0;
    }

    public void AddOrUpdate(GridTile tile, int score)
    {
        if (indices.TryGetValue(tile, out int index))
        {
            Entry existing = entries[index];
            existing.score = score;
            entries[index] = existing;
        }
        else
        {
            index = entries.Count;
            entries.Add(new Entry { tile = tile, score = score, order = nextOrder++ });
            indices[tile] = index;
        }
        while (index > 0)
        {
            int parent = (index - 1) / 2;
            if (!Before(entries[index], entries[parent])) break;
            Swap(index, parent);
            index = parent;
        }
    }

    public GridTile Pop()
    {
        GridTile result = entries[0].tile;
        int last = entries.Count - 1;
        Swap(0, last);
        entries.RemoveAt(last);
        indices.Remove(result);
        int index = 0;
        while (index * 2 + 1 < entries.Count)
        {
            int child = index * 2 + 1;
            if (child + 1 < entries.Count && Before(entries[child + 1], entries[child])) child++;
            if (!Before(entries[child], entries[index])) break;
            Swap(index, child);
            index = child;
        }
        return result;
    }

    private static bool Before(Entry left, Entry right) =>
        left.score < right.score || (left.score == right.score && left.order < right.order);

    private void Swap(int first, int second)
    {
        Entry temporary = entries[first];
        entries[first] = entries[second];
        entries[second] = temporary;
        indices[entries[first].tile] = first;
        indices[entries[second].tile] = second;
    }
}
