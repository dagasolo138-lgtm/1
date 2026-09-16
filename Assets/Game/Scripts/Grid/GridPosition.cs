using System;

namespace ShanMen.Grid
{
    [Serializable]
    public struct GridPosition : IEquatable<GridPosition>
    {
        public int x;
        public int y;

        public GridPosition(int x, int y) { this.x = x; this.y = y; }
        public bool Equals(GridPosition other) => x == other.x && y == other.y;
        public override bool Equals(object obj) => obj is GridPosition other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(x, y);
        public override string ToString() => $"({x},{y})";
        public static bool operator ==(GridPosition a, GridPosition b) => a.Equals(b);
        public static bool operator !=(GridPosition a, GridPosition b) => !a.Equals(b);
    }
}
