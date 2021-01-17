using Observatory.Core.Virtualization;
using System;
using System.Collections.Generic;
using Xunit;

namespace Observatory.Core.Tests.Virtualization
{
    public class IndexRangeTests
    {
        [Fact]
        public void MergeWithSingleResult()
        {
            var ranges = new IndexRange[]
            {
                new IndexRange(0, 6),
            };
            var otherRange = new IndexRange(4, 7);
            var mergedRanges = ranges.Merge(otherRange);

            Assert.Single(mergedRanges);
            Assert.Equal(new IndexRange(0, 7), mergedRanges[0]);
        }

        [Fact]
        public void MergeWithManyResult()
        {
            var ranges = new IndexRange[]
            {
                new IndexRange(0, 4),
                new IndexRange(9, 10),
            };
            var otherRange = new IndexRange(7, 13);
            var mergedRanges = ranges.Merge(otherRange);

            Assert.Collection(mergedRanges,
                x => Assert.Equal(ranges[0], x),
                x => Assert.Equal(new IndexRange(7, 13), x));
        }

        [Fact]
        public void SubtractAffectingTwoRanges()
        {
            var ranges = new IndexRange[]
            {
                new IndexRange(0, 6),
                new IndexRange(8, 10),
            };
            var otherRange = new IndexRange(4, 9);
            var subtractedRanges = ranges.Subtract(otherRange);

            Assert.Collection(subtractedRanges,
                x => Assert.Equal(new IndexRange(0, 3), x),
                x => Assert.Equal(new IndexRange(10, 10), x));
        }

        [Fact]
        public void SubtractDividivingOneRange()
        {
            var ranges = new IndexRange[]
            {
                new IndexRange(1, 3),
                new IndexRange(5, 10),
            };
            var otherRange = new IndexRange(6, 7);
            var subtractedRanges = ranges.Subtract(otherRange);

            Assert.Collection(subtractedRanges,
                x => Assert.Equal(ranges[0], x),
                x => Assert.Equal(new IndexRange(5, 5), x),
                x => Assert.Equal(new IndexRange(8, 10), x));
        }

        [Theory]
        [MemberData(nameof(IndexRangeTestCases.Contains), MemberType = typeof(IndexRangeTestCases))]
        public void Contains(IndexRange[] ranges, int index, bool expected)
        {
            Assert.Equal(expected, ranges.Contains(index));
        }

        [Theory]
        [MemberData(nameof(IndexRangeTestCases.Search), MemberType = typeof(IndexRangeTestCases))]
        public void Search(IndexRange[] ranges, int index,
            IndexRangeSearchApproximation approximation, IndexRangeSearchResult expectedResult)
        {
            var searchedIndex = ranges.Search(index, approximation);
            Assert.Equal(searchedIndex, expectedResult);
        }
    }

    public static class IndexRangeTestCases
    {
        public static IEnumerable<object[]> Contains => new object[][]
        {
            new object[] { new IndexRange[] { new IndexRange(0, 1) }, 2, false },
            new object[] { new IndexRange[] { new IndexRange(3, 4) }, 2, false },
            new object[] { new IndexRange[] { new IndexRange(0, 0), new IndexRange(2, 3) }, 1, false },
            new object[] { new IndexRange[] { new IndexRange(0, 0), new IndexRange(2, 3), new IndexRange(6, 9) }, 1, false },
            new object[] { new IndexRange[] { new IndexRange(0, 1) }, 1, true },
            new object[] { new IndexRange[] { new IndexRange(0, 1), new IndexRange(3, 5) }, 4, true },
        };

        public static IEnumerable<object[]> Search => new object[][]
        {
            new object[]
            {
                new IndexRange[] { new IndexRange(0, 3) },
                1, IndexRangeSearchApproximation.Exact, new IndexRangeSearchResult(true, 0),
            },
            new object[]
            {
                new IndexRange[] { new IndexRange(0, 3), new IndexRange(5, 9) },
                7, IndexRangeSearchApproximation.Exact, new IndexRangeSearchResult(true, 1)
            },
            new object[]
            {
                new IndexRange[] { new IndexRange(0, 4), new IndexRange(6, 6), new IndexRange(9, 10) },
                4, IndexRangeSearchApproximation.Exact, new IndexRangeSearchResult(true, 0)
            },
            new object[]
            {
                new IndexRange[] { new IndexRange(0, 4), new IndexRange(6, 6), new IndexRange(9, 10) },
                14, IndexRangeSearchApproximation.Exact, new IndexRangeSearchResult(false, -1)
            },
            new object[]
            {
                new IndexRange[0],
                5, IndexRangeSearchApproximation.Exact, new IndexRangeSearchResult(false, -1)
            },
            new object[]
            {
                new IndexRange[] { new IndexRange(0, 4) },
                7, IndexRangeSearchApproximation.NearestLeft, new IndexRangeSearchResult(false, 0)
            },
            new object[]
            {
                new IndexRange[] { new IndexRange(6, 9) },
                4, IndexRangeSearchApproximation.NearestLeft, new IndexRangeSearchResult(false, -1)
            },
            new object[]
            {
                new IndexRange[] { new IndexRange(0, 4), new IndexRange(6, 6), new IndexRange(9, 10) },
                7, IndexRangeSearchApproximation.NearestLeft, new IndexRangeSearchResult(false, 1)
            },
            new object[]
            {
                new IndexRange[] { new IndexRange(0, 4), new IndexRange(6, 6), new IndexRange(9, 10) },
                6, IndexRangeSearchApproximation.NearestLeft, new IndexRangeSearchResult(true, 1)
            },
            new object[]
            {
                new IndexRange[] { new IndexRange(0, 4), new IndexRange(6, 6), new IndexRange(9, 10) },
                11, IndexRangeSearchApproximation.NearestLeft, new IndexRangeSearchResult(false, 2)
            },
            new object[]
            {
                new IndexRange[] { new IndexRange(0, 4) },
                7, IndexRangeSearchApproximation.NearestRight, new IndexRangeSearchResult(false, -1)
            },
            new object[]
            {
                new IndexRange[] { new IndexRange(6, 9) },
                4, IndexRangeSearchApproximation.NearestRight, new IndexRangeSearchResult(false, 0)
            },
            new object[]
            {
                new IndexRange[] { new IndexRange(0, 4), new IndexRange(6, 6), new IndexRange(9, 10) },
                7, IndexRangeSearchApproximation.NearestRight, new IndexRangeSearchResult(false, 2)
            },
            new object[]
            {
                new IndexRange[] { new IndexRange(0, 4), new IndexRange(6, 6), new IndexRange(9, 10) },
                6, IndexRangeSearchApproximation.NearestRight, new IndexRangeSearchResult(true, 1)
            },
            new object[]
            {
                new IndexRange[] { new IndexRange(0, 4), new IndexRange(6, 6), new IndexRange(9, 10) },
                11, IndexRangeSearchApproximation.NearestRight, new IndexRangeSearchResult(false, -1)
            },
            new object[]
            {
                new IndexRange[] { new IndexRange(2, 4), new IndexRange(6, 6), new IndexRange(9, 10) },
                0, IndexRangeSearchApproximation.NearestRight, new IndexRangeSearchResult(false, 0)
            },
        };
    }
}
