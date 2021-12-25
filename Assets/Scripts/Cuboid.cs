using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

public enum Axis { X, Y, Z }

public struct Cuboid : IEquatable<Cuboid>
{
    public int MinX;
    public int MinY;
    public int MinZ;
    public int MaxX;
    public int MaxY;
    public int MaxZ;

    public (int X, int Y, int Z) Extents => ((MaxX - MinX), (MaxY - MinY), (MaxZ - MinZ));
    public (float X, float Y, float Z) Center => ((MaxX + MinX) / 2f, (MaxY + MinY) / 2f, (MaxZ + MinZ) / 2f);

    public static Cuboid Default => new Cuboid(int.MaxValue, int.MaxValue, int.MaxValue, int.MinValue, int.MinValue, int.MinValue);

    public ulong Size
    {
        get
        {
            if(MaxZ - (MinZ - 1) < 0) return 0;
            if(MaxY - (MinY - 1) < 0) return 0;
            if(MaxX - (MinX - 1) < 0) return 0;
            return (ulong)(MaxZ - (MinZ - 1)) * (ulong)(MaxY - (MinY - 1)) * (ulong)(MaxX - (MinX - 1));
        }
    }

    public Cuboid (int MinX, int MinY, int MinZ, int MaxX, int MaxY, int MaxZ)
    {
        this.MinX = MinX;
        this.MinY = MinY;
        this.MinZ = MinZ;
        this.MaxX = MaxX;
        this.MaxY = MaxY;
        this.MaxZ = MaxZ;
    }

    public bool IsWithinBounds(Cuboid bounds)
    {
        return this.MinX >= bounds.MinX &&
            this.MinY >= bounds.MinY &&
            this.MinZ >= bounds.MinZ &&
            this.MaxX <= bounds.MaxX &&
            this.MaxY <= bounds.MaxY &&
            this.MaxZ <= bounds.MaxZ;
    }

    public bool IsOverlapping(Cuboid other)
    {
        return
            IsAxisOverlapping(this.MinX, this.MaxX, other.MinX, other.MaxX) &&
            IsAxisOverlapping(this.MinY, this.MaxY, other.MinY, other.MaxY) &&
            IsAxisOverlapping(this.MinZ, this.MaxZ, other.MinZ, other.MaxZ)
            ;
    }

    private bool IsAxisOverlapping(int minA, int maxA, int minB, int maxB)
    {
        return Math.Sign(minA - maxB) != Math.Sign(maxA - minB); // !(minA >= maxB || maxA <= minB);
    }

    public List<Cuboid> Subtract(Cuboid other)
    {
        if(!this.IsOverlapping(other)) return new List<Cuboid> { this };

        Cuboid overlappingSegment = this;

        List<Cuboid> resultingSegments = new List<Cuboid>();

        List<Cuboid> newParts;
        (newParts, overlappingSegment) = CalculateOverlapResults(overlappingSegment, other, Axis.Z);
        resultingSegments.AddRange(newParts);

        (newParts, overlappingSegment) = CalculateOverlapResults(overlappingSegment, other, Axis.Y);
        resultingSegments.AddRange(newParts);

        (newParts, overlappingSegment) = CalculateOverlapResults(overlappingSegment, other, Axis.X);
        resultingSegments.AddRange(newParts);


        return resultingSegments;
    }

    private (List<Cuboid> clearedParts, Cuboid overlappingSegment)
        CalculateOverlapResults(Cuboid overlappingSegment, Cuboid other, Axis axis)
    {
        Func<Cuboid, int> minSelector = axis switch
        {
            Axis.X => (Cuboid x) => x.MinX,
            Axis.Y => (Cuboid x) => x.MinY,
            Axis.Z => (Cuboid x) => x.MinZ,
        };
        Func<Cuboid, int> maxSelector = axis switch
        {
            Axis.X => (Cuboid x) => x.MaxX,
            Axis.Y => (Cuboid x) => x.MaxY,
            Axis.Z => (Cuboid x) => x.MaxZ,
        };

        if(
            other.Equals(Cuboid.Default) ||
            overlappingSegment.Equals(Cuboid.Default) ||
            !IsAxisOverlapping(
                minSelector(overlappingSegment),
                maxSelector(overlappingSegment),
                minSelector(other),
                maxSelector(other)
            )
        )
        {
            return (new List<Cuboid>(), overlappingSegment);
        }

        List<Cuboid> cutResults = new List<Cuboid>() {
            new Cuboid(
                overlappingSegment.MinX,
                overlappingSegment.MinY,
                overlappingSegment.MinZ,
                axis == Axis.X ? minSelector(other) - 1 : overlappingSegment.MaxX,
                axis == Axis.Y ? minSelector(other) - 1 : overlappingSegment.MaxY,
                axis == Axis.Z ? minSelector(other) - 1 : overlappingSegment.MaxZ
            ),
            new Cuboid(
                axis == Axis.X ? Math.Max(minSelector(overlappingSegment), minSelector(other)) : overlappingSegment.MinX,
                axis == Axis.Y ? Math.Max(minSelector(overlappingSegment), minSelector(other)) : overlappingSegment.MinY,
                axis == Axis.Z ? Math.Max(minSelector(overlappingSegment), minSelector(other)) : overlappingSegment.MinZ,
                axis == Axis.X ? Math.Min(maxSelector(overlappingSegment), maxSelector(other)) : overlappingSegment.MaxX,
                axis == Axis.Y ? Math.Min(maxSelector(overlappingSegment), maxSelector(other)) : overlappingSegment.MaxY,
                axis == Axis.Z ? Math.Min(maxSelector(overlappingSegment), maxSelector(other)) : overlappingSegment.MaxZ
            ),
            new Cuboid(
                axis == Axis.X ? maxSelector(other) + 1 : overlappingSegment.MinX,
                axis == Axis.Y ? maxSelector(other) + 1 : overlappingSegment.MinY,
                axis == Axis.Z ? maxSelector(other) + 1 : overlappingSegment.MinZ,
                overlappingSegment.MaxX,
                overlappingSegment.MaxY,
                overlappingSegment.MaxZ
            ),
        };

        var options = cutResults.Where(x => x.Size > 0 && x.IsOverlapping(other)).ToArray();
        var newOverlappingSegment = options.Any() ? options.Single() : Cuboid.Default;
        var clearedParts = cutResults.Where(x => x.Size > 0 && !x.IsOverlapping(other)).ToList();
        return (clearedParts, newOverlappingSegment);
    }

    private bool PrintMembers(StringBuilder stringBuilder)
    {
        stringBuilder.Append($"Center = {Center}, ");
        stringBuilder.Append($"Extents = {Extents}");
        return true;
    }

    public bool Equals(Cuboid other)
    {
        return MinX == other.MinX && MinY == other.MinY && MinZ == other.MinZ && MaxX == other.MaxX && MaxY == other.MaxY && MaxZ == other.MaxZ;
    }

    public override bool Equals(object obj)
    {
        return obj is Cuboid other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = MinX;
            hashCode = (hashCode * 397) ^ MinY;
            hashCode = (hashCode * 397) ^ MinZ;
            hashCode = (hashCode * 397) ^ MaxX;
            hashCode = (hashCode * 397) ^ MaxY;
            hashCode = (hashCode * 397) ^ MaxZ;
            return hashCode;
        }
    }
}