﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Observatory.Core.Virtualization
{
    /// <summary>
    /// Represents a range of indices.
    /// </summary>
    public struct IndexRange : IEquatable<IndexRange>
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
            if (other.FirstIndex >= FirstIndex && other.FirstIndex <= LastIndex ||
                FirstIndex >= other.FirstIndex && FirstIndex <= other.LastIndex)
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
        public (IndexRange? Left, IndexRange? Right) Diff(IndexRange other)
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

        public static bool operator ==(IndexRange x, IndexRange y)
        {
            return x.Equals(y);
        }

        public static bool operator !=(IndexRange x, IndexRange y)
        {
            return !x.Equals(y);
        }
    }

    public static class IndexRangeExtensions
    {
        /// <summary>
        /// Sorts then compacts a given list of ranges.
        /// </summary>
        /// <param name="ranges">The ranges.</param>
        /// <returns></returns>
        public static IndexRange[] Normalize(this IndexRange[] ranges)
        {
            var sortedRanges = ranges.OrderBy(r => r.FirstIndex).ToList();
            var normalizedRanges = new List<IndexRange>();
            if (sortedRanges.Count > 0)
            {
                var anchor = sortedRanges[0];
                var index = 1;

                while (true)
                {
                    if (index >= sortedRanges.Count)
                    {
                        normalizedRanges.Add(anchor);
                        break;
                    }

                    var current = sortedRanges[index++];
                    if (!anchor.TryUnion(current, ref anchor))
                    {
                        normalizedRanges.Add(anchor);
                        anchor = current;
                    }
                }
            }
            return normalizedRanges.ToArray();
        }
    }
}