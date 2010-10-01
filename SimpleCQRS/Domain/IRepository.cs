﻿using System;

namespace SimpleCQRS.Domain
{
    public interface IRepository<T> where T : AggregateRoot
    {
        void Save(AggregateRoot aggregate, int expectedVersion);
        T GetById(Guid id);
    }
}