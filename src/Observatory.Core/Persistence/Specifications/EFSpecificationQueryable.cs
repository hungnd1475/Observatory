﻿using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Observatory.Core.Persistence.Specifications
{
    /// <summary>
    /// Represents a type of <see cref="ISpecificationQueryable{T}"/> that works with Entity Framework Core.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EFSpecificationQueryable<T> : ISpecificationQueryable<T>
        where T : class
    {
        private readonly DbSet<T> _source;

        /// <summary>
        /// Constructs an instance of <see cref="EFSpecificationQueryable{T}"/> that works on a <see cref="DbSet{TEntity}"/>.
        /// </summary>
        /// <param name="source"></param>
        public EFSpecificationQueryable(DbSet<T> source)
        {
            _source = source;
        }

        public int Count<TResult>(ISpecification<T, TResult> specification)
        {
            var query = specification.Apply(_source);
            return query.Count();
        }

        public TResult FirstOrDefault<TResult>(ISpecification<T, TResult> specification)
        {
            var query = specification.Apply(_source);
            return query.FirstOrDefault();
        }

        public TResult[] ToArray<TResult>(ISpecification<T, TResult> specification)
        {
            var query = specification.Apply(_source);
            return query.ToArray();
        }

        public List<TResult> ToList<TResult>(ISpecification<T, TResult> specification)
        {
            var query = specification.Apply(_source);
            return query.ToList();
        }
    }
}
