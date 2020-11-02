using ReactiveUI;
using Splat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Observatory.Core.Virtualization
{
    /// <summary>
    /// Represents a block of items for an <see cref="IndexRange"/>.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    public class VirtualizingCacheBlock<TSource, TTarget> : IDisposable
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        /// <summary>
        /// Gets the item at a given index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public TTarget this[int index] => Items[index - Range.FirstIndex];

        /// <summary>
        /// Gets the range of the block.
        /// </summary>
        public IndexRange Range { get; }

        /// <summary>
        /// Gets the array of items the block is holding.
        /// </summary>
        public TTarget[] Items { get; }

        /// <summary>
        /// Gets the requests of new items.
        /// </summary>
        public Queue<VirtualizingCacheBlockRequest<TSource, TTarget>> Requests { get; }

        /// <summary>
        /// Constructs an instance of <see cref="VirtualizingCacheBlock{TSource, TTarget}"/>.
        /// </summary>
        /// <param name="range">The range of the block.</param>
        /// <param name="items">The items the block holds.</param>
        /// <param name="requests">The requests for new items.</param>
        /// <param name="observer">The observer.</param>
        public VirtualizingCacheBlock(IndexRange range, TTarget[] items,
            Queue<VirtualizingCacheBlockRequest<TSource, TTarget>> requests,
            IObserver<VirtualizingCacheBlockLoadedEvent<TSource, TTarget>> observer)
        {
            Range = range;
            Items = items;
            Requests = requests;

            foreach (var r in requests)
            {
                r.Items.Subscribe(items =>
                {
                    var sourceIndex = r.EffectiveRange.FirstIndex - r.FullRange.FirstIndex;
                    var destinationIndex = r.EffectiveRange.FirstIndex - Range.FirstIndex;
                    var length = r.EffectiveRange.Length;
                    Array.Copy(items, sourceIndex, Items, destinationIndex, length);
                    r.IsReceived = true;

                    observer.OnNext(new VirtualizingCacheBlockLoadedEvent<TSource, TTarget>(r.EffectiveRange, this));
                })
                .DisposeWith(_disposables);
            }
        }

        /// <summary>
        /// Determines whether a given index is within the block.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool ContainsIndex(int index) => Range.Contains(index);

        /// <summary>
        /// Disposes the block, stop observing for new items when they are loaded from source.
        /// </summary>
        public void Dispose()
        {
            _disposables.Dispose();
            if (typeof(IDisposable).IsAssignableFrom(typeof(TTarget)))
            {
                foreach (var i in Items.Cast<IDisposable>().Where(i => i != null))
                {
                    i.Dispose();
                }
            }
        }
    }
}
