using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Observatory.Core.Virtualization
{
    /// <summary>
    /// Represents a range of indices.
    /// </summary>
    public struct IndexRange : IEquatable<IndexRange>, IEnumerable<int>
    {
        /// <summary>
        /// Gets the first index of this range.
        /// </summary>
        public int FirstIndex { get; }

        /// <summary>
        /// Gets the last index of this range.
        /// </summary>
        public int LastIndex { get; }

        /// <summary>
        /// Gets the length of this range.
        /// </summary>
        public int Length => LastIndex - FirstIndex + 1;

        /// <summary>
        /// Constructs an instance of <see cref="IndexRange"/> with first and last indices.
        /// </summary>
        /// <param name="firstIndex">The first index.</param>
        /// <param name="lastIndex">The last index.</param>
        public IndexRange(int firstIndex, int lastIndex)
        {
            FirstIndex = firstIndex;
            LastIndex = lastIndex;
        }

        /// <summary>
        /// Tries union with another range.
        /// </summary>
        /// <param name="other">The other range.</param>
        /// <param name="result">The result.</param>
        /// <returns></returns>
        public bool TryUnion(IndexRange other, ref IndexRange result)
        {
            if (other.FirstIndex >= FirstIndex && other.FirstIndex <= LastIndex + 1 ||
                FirstIndex >= other.FirstIndex && FirstIndex <= other.LastIndex + 1)
            {
                var firstIndex = Math.Min(FirstIndex, other.FirstIndex);
                var lastIndex = Math.Max(LastIndex, other.LastIndex);
                result = new IndexRange(firstIndex, lastIndex);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns the range(s) that are in other but not in this.
        /// </summary>
        /// <param name="other">The other range.</param>
        /// <returns></returns>
        public (IndexRange? Left, IndexRange? Right) Difference(IndexRange other)
        {
            IndexRange? left = null, right = null;
            if (FirstIndex > other.FirstIndex)
            {
                var firstIndex = other.FirstIndex;
                var lastIndex = Math.Min(FirstIndex - 1, other.LastIndex);
                left = new IndexRange(firstIndex, lastIndex);
            }
            if (LastIndex < other.LastIndex)
            {
                var firstIndex = Math.Max(LastIndex + 1, other.FirstIndex);
                var lastIndex = other.LastIndex;
                right = new IndexRange(firstIndex, lastIndex);
            }
            return (Left: left, Right: right);
        }

        /// <summary>
        /// Returns the intersection of this range and another range.
        /// </summary>
        /// <param name="other">The other range.</param>
        /// <returns>An instance of <see cref="IndexRange"/> describing the intersection if any, otherwise null.</returns>
        public IndexRange? Intersect(IndexRange other)
        {
            var firstIndex = Math.Max(FirstIndex, other.FirstIndex);
            var lastIndex = Math.Min(LastIndex, other.LastIndex);
            return firstIndex <= lastIndex
                ? new IndexRange(firstIndex, lastIndex)
                : (IndexRange?)null;
        }

        /// <summary>
        /// Determines whether this range covers another range.
        /// </summary>
        /// <param name="other">The other range to check against.</param>
        /// <returns></returns>
        public bool Covers(IndexRange other)
        {
            return FirstIndex <= other.FirstIndex && LastIndex >= other.LastIndex;
        }

        /// <summary>
        /// Determines if this range and a given range are disjoint.
        /// </summary>
        /// <param name="other">The other range.</param>
        /// <returns></returns>
        public bool IsDisjoint(IndexRange other)
        {
            return FirstIndex > other.LastIndex || LastIndex < other.FirstIndex;
        }

        /// <summary>
        /// Determines whether a given index is within this range.
        /// </summary>
        /// <param name="index">The index to be checked.</param>
        /// <returns></returns>
        public bool Contains(int index)
        {
            return index >= FirstIndex && index <= LastIndex;
        }

        /// <summary>
        /// Slices an array described by the range based on a given subrange.
        /// </summary>
        /// <typeparam name="T">The type of items in the array.</typeparam>
        /// <param name="array">The array to be sliced.</param>
        /// <param name="subrange">The subrange.</param>
        /// <returns>A <see cref="Span{T}"/> holding the slice.</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public Span<T> Slice<T>(T[] array, IndexRange subrange)
        {
            if (!Covers(subrange)) throw new ArgumentOutOfRangeException(nameof(subrange));
            if (array.Length != Length) throw new ArgumentException("The given array does not have a matched length.");

            return array.AsSpan(subrange.FirstIndex - FirstIndex, subrange.Length);
        }

        public override string ToString()
        {
            return $"{FirstIndex}->{LastIndex}";
        }

        public bool Equals(IndexRange other)
        {
            return FirstIndex == other.FirstIndex &&
                LastIndex == other.LastIndex;
        }

        public override bool Equals(object obj)
        {
            if (obj is IndexRange other)
            {
                return Equals(other);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(FirstIndex, LastIndex);
        }

        public IEnumerator<int> GetEnumerator()
        {
            for (var i = FirstIndex; i <= LastIndex; i++)
            {
                yield return i;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public static bool operator ==(IndexRange x, IndexRange y)
        {
            return x.Equals(y);
        }

        public static bool operator !=(IndexRange x, IndexRange y)
        {
            return !x.Equals(y);
        }
    }
}
