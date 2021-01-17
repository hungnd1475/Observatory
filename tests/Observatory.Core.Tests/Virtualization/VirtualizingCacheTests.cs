using Observatory.Core.Services.ChangeTracking;
using Observatory.Core.Virtualization;
using Observatory.Core.Virtualization.Internals;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using Xunit;

namespace Observatory.Core.Tests.Virtualization
{
    public class VirtualizingCacheTests
    {
        [Theory]
        [MemberData(nameof(VirtualizingCacheTestCases.ApplyChangesToSelection), MemberType = typeof(VirtualizingCacheTestCases))]
        public void ApplyChangesToSelection(IndexRange[] oldSelection, NotifyCollectionChangedEventArgs e, IndexRange[] expectedNewSelection)
        {
            var newSelection = e.ApplyToSelection(oldSelection);
            Assert.Equal(expectedNewSelection, newSelection);
        }
    }

    public static class VirtualizingCacheTestCases
    {
        public static NotifyCollectionChangedEventArgs ToSpecialized<T>(this LogicalChange<T> logicalChange)
            where T : class
        {
            switch (logicalChange.State)
            {
                case DeltaState.Add:
                    return new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Add,
                        logicalChange.CurrentEntity,
                        logicalChange.CurrentIndex.Value);
                case DeltaState.Remove:
                    return new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Remove,
                        logicalChange.PreviousEntity,
                        logicalChange.PreviousIndex.Value);
                case DeltaState.Update:
                    if (logicalChange.PreviousIndex.Value != logicalChange.CurrentIndex.Value)
                    {
                        return new NotifyCollectionChangedEventArgs(
                            NotifyCollectionChangedAction.Move,
                            logicalChange.CurrentEntity,
                            logicalChange.CurrentIndex.Value,
                            logicalChange.PreviousIndex.Value);
                    }
                    else
                    {
                        return new NotifyCollectionChangedEventArgs(
                            NotifyCollectionChangedAction.Replace,
                            logicalChange.PreviousEntity,
                            logicalChange.CurrentEntity,
                            logicalChange.CurrentIndex.Value);
                    }
                default:
                    throw new NotSupportedException();
            }
        }

        public static IEnumerable<object[]> ApplyChangesToSelection => new object[][]
        {
            new object[]
            {
                new IndexRange[] { new IndexRange(0, 2) },
                LogicalChange.Addition("", 3).ToSpecialized(),
                new IndexRange[] { new IndexRange(0, 2) },
            },
            new object[] // add before selection
            {
                new IndexRange[] { new IndexRange(0, 2) },
                LogicalChange.Addition("", 0).ToSpecialized(),
                new IndexRange[] { new IndexRange(1, 3) }
            },
            new object[] // add before selection
            {
                new IndexRange[] { new IndexRange(1, 3) },
                LogicalChange.Addition("", 0).ToSpecialized(),
                new IndexRange[] { new IndexRange(2, 4) }
            },
            new object[] // add within selection
            {
                new IndexRange[] { new IndexRange(0, 2) },
                LogicalChange.Addition("", 1).ToSpecialized(),
                new IndexRange[] { new IndexRange(0, 0), new IndexRange(2, 3) },
            },
            new object[]
            {
                new IndexRange[] { new IndexRange(0, 2), new IndexRange(4, 5) },
                LogicalChange.Addition("", 0).ToSpecialized(),
                new IndexRange[] { new IndexRange(1, 3), new IndexRange(5, 6) },
            },
            new object[]
            {
                new IndexRange[] { new IndexRange(0, 2), new IndexRange(4, 5) },
                LogicalChange.Addition("", 3).ToSpecialized(),
                new IndexRange[] { new IndexRange(0, 2), new IndexRange(5, 6) },
            },
            new object[]
            {
                new IndexRange[] { new IndexRange(0, 2), new IndexRange(4, 5) },
                LogicalChange.Addition("", 5).ToSpecialized(),
                new IndexRange[] { new IndexRange(0, 2), new IndexRange(4, 4), new IndexRange(6, 6) },
            },
            new object[]
            {
                new IndexRange[] { new IndexRange(0, 2), new IndexRange(4, 5), new IndexRange(7, 8) },
                LogicalChange.Addition("", 5).ToSpecialized(),
                new IndexRange[] { new IndexRange(0, 2), new IndexRange(4, 4), new IndexRange(6, 6), new IndexRange(8, 9) },
            },
            new object[]
            {
                new IndexRange[] { new IndexRange(2, 4) },
                LogicalChange.Removal("", 0).ToSpecialized(),
                new IndexRange[] { new IndexRange(1, 3) },
            },
            new object[]
            {
                new IndexRange[] { new IndexRange(2, 4) },
                LogicalChange.Removal("", 5).ToSpecialized(),
                new IndexRange[] { new IndexRange(2, 4) },
            },
            new object[]
            {
                new IndexRange[] { new IndexRange(2, 4) },
                LogicalChange.Removal("", 2).ToSpecialized(),
                new IndexRange[] { new IndexRange(2, 3) },
            },
            new object[]
            {
                new IndexRange[] { new IndexRange(2, 4), new IndexRange(6, 7) },
                LogicalChange.Removal("", 4).ToSpecialized(),
                new IndexRange[] { new IndexRange(2, 3), new IndexRange(5, 6) },
            },
            new object[]
            {
                new IndexRange[] { new IndexRange(2, 4), new IndexRange(6, 7) },
                LogicalChange.Removal("", 5).ToSpecialized(),
                new IndexRange[] { new IndexRange(2, 6) },
            },
            new object[]
            {
                new IndexRange[] { new IndexRange(2, 4), new IndexRange(6, 9) },
                LogicalChange.Update("", 2, "", 2).ToSpecialized(),
                new IndexRange[] { new IndexRange(2, 4), new IndexRange(6, 9) },
            },
            new object[]
            {
                new IndexRange[] { new IndexRange(2, 4), new IndexRange(6, 8) },
                LogicalChange.Update("", 1, "", 5).ToSpecialized(),
                new IndexRange[] { new IndexRange(3, 8) },
            },
            new object[]
            {
                new IndexRange[] { new IndexRange(2, 4), new IndexRange(6, 8) },
                LogicalChange.Update("", 6, "", 5).ToSpecialized(),
                new IndexRange[] { new IndexRange(2, 5), new IndexRange(7, 8), },
            },
            new object[]
            {
                new IndexRange[] { new IndexRange(2, 4), new IndexRange(6, 8) },
                LogicalChange.Update("", 6, "", 2).ToSpecialized(),
                new IndexRange[] { new IndexRange(2, 3), new IndexRange(5, 8), },
            },
            new object[]
            {
                new IndexRange[] { new IndexRange(2, 4), new IndexRange(6, 8) },
                LogicalChange.Update("", 4, "", 2).ToSpecialized(),
                new IndexRange[] { new IndexRange(2, 4), new IndexRange(6, 8), },
            },
            new object[]
            {
                new IndexRange[] { new IndexRange(2, 4), new IndexRange(6, 8) },
                LogicalChange.Update("", 5, "", 2).ToSpecialized(),
                new IndexRange[] { new IndexRange(2, 3), new IndexRange(5, 8), },
            },
            new object[]
            {
                new IndexRange[] { new IndexRange(2, 4), new IndexRange(7, 8) },
                LogicalChange.Update("", 6, "", 8).ToSpecialized(),
                new IndexRange[] { new IndexRange(2, 4), new IndexRange(6, 6), new IndexRange(8, 8) },
            },
        };
    }
}
