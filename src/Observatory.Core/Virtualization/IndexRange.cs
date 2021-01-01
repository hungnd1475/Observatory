using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
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
        /// <returns>True if union happened, otherwise false.</returns>
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

    public static class IndexRangeExtensions
    {
        /// <summary>
        /// Sorts then compacts a given array of ranges.
        /// </summary>
        /// <param name="ranges">The ranges.</param>
        /// <returns>A new array of ranges that are sorted and compacted.</returns>
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

        /// <summary>
        /// Searches an array for the range containing a given index. This method will try to 
        /// find the nearest range in case the given index is not covered within the ranges and
        /// the value of <paramref name="approximation"/> is not <see cref="IndexRangeSearchApproximation.Exact"/>.
        /// </summary>
        /// <param name="ranges">The sorted ranges to search from.</param>
        /// <param name="index">The index.</param>
        /// <returns>
        /// The index of the range if found, otherwise the index of the nearest left or right range depending
        /// on the value of <paramref name="approximation"/> or -1 if it is <see cref="IndexRangeSearchApproximation.Exact"/>.
        /// </returns>
        public static int Search(this IndexRange[] ranges, int index,
            IndexRangeSearchApproximation approximation = IndexRangeSearchApproximation.Exact)
        {
            var startIndex = 0;
            var endIndex = ranges.Length - 1;
            var approximationResult = -1;

            while (startIndex <= endIndex)
            {
                var midIndex = (startIndex + endIndex) / 2;
                var midRange = ranges[midIndex];

                if (midRange.Contains(index))
                {
                    return midIndex;
                }
                else if (midRange.FirstIndex < index)
                {
                    startIndex = midIndex + 1;
                    if (approximation == IndexRangeSearchApproximation.NearestLeft)
                    {
                        approximationResult = midIndex;
                    }
                }
                else
                {
                    endIndex = midIndex - 1;
                    if (approximation == IndexRangeSearchApproximation.NearestRight)
                    {
                        approximationResult = midIndex;
                    }
                }
            }

            return approximation == IndexRangeSearchApproximation.Exact ? -1 : approximationResult;
        }

        public static IndexRange[] Merge(this IndexRange[] ranges, IndexRange otherRange)
        {
            var result = new List<IndexRange>(ranges) { otherRange };
            return result.ToArray().Normalize();
        }

        public static IndexRange[] Subtract(this IndexRange[] ranges, IndexRange otherRange)
        {
            if (ranges.Length == 0)
                return ranges;

            var result = new List<IndexRange>();
            IndexRange? subtractingRange = otherRange;

            foreach (var currentRange in ranges)
            {
                if (subtractingRange.HasValue)
                {
                    var (leftRemain, rightRemain) = subtractingRange.Value.Difference(currentRange);
                    if (leftRemain.HasValue)
                    {
                        result.Add(leftRemain.Value);
                    }
                    if (rightRemain.HasValue)
                    {
                        result.Add(rightRemain.Value);
                    }
                    subtractingRange = currentRange.Difference(subtractingRange.Value).Right;
                }
                else
                {
                    result.Add(currentRange);
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// Determines if a given <paramref name="index"/> is contained within the <paramref name="ranges"/>.
        /// </summary>
        /// <param name="ranges">The ranges.</param>
        /// <param name="index">The index.</param>
        /// <returns>True if <paramref name="index"/> is within the <paramref name="ranges"/>, otherwise false.</returns>
        public static bool Contains(this IndexRange[] ranges, int index)
        {
            var startIndex = 0;
            var endIndex = ranges.Length - 1;

            while (startIndex <= endIndex)
            {
                var midIndex = (startIndex + endIndex) / 2;
                var midRange = ranges[midIndex];

                if (midRange.Contains(index))
                {
                    return true;
                }
                else if (midRange.FirstIndex < index)
                {
                    startIndex = midIndex + 1;
                }
                else
                {
                    endIndex = midIndex - 1;
                }
            }
            return false;
        }

        /// <summary>
        /// Enumerates all the indices in the ranges.
        /// </summary>
        /// <param name="ranges"></param>
        /// <returns></returns>
        public static IEnumerable<int> EnumerateIndex(this IndexRange[] ranges)
        {
            foreach (var r in ranges)
            {
                foreach (var i in r)
                {
                    yield return i;
                }
            }
        }
    }

    /// <summary>
    /// Indicates what the search algorithm should return in case no containing range is found.
    /// </summary>
    public enum IndexRangeSearchApproximation
    {
        /// <summary>
        /// Return -1 if no containing range is found.
        /// </summary>
        Exact,
        /// <summary>
        /// Return the nearest range to the left if no containing range is found. 
        /// If there is no nearest left range, return -1.
        /// </summary>
        NearestLeft,
        /// <summary>
        /// Return the nearest range to the left if no containing range is found.
        /// If there is no nearest left range, return the nearest range to the right.
        /// </summary>
        NearestLeftFlexible,
        /// <summary>
        /// Return the nearest range to the right if no containing range is found.
        /// If there is no nearest right range, return -1.
        /// </summary>
        NearestRight,
        /// <summary>
        /// Return the nearest range to the right if no containing range is found.
        /// If there is no nearest right range, return the nearest range to the left.
        /// </summary>
        NearestRightFlexible,
    }
}
