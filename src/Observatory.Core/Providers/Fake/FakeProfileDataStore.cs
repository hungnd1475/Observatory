using Microsoft.Extensions.Logging;
using Observatory.Core.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Core.Providers.Fake.Persistence
{
    public class FakeProfileDataStore : ProfileDataStore
    {
        public delegate FakeProfileDataStore Factory(string path);

        public FakeProfileDataStore(string path, ILoggerFactory loggerFactory) 
            : base(path, loggerFactory)
        {
        }
    }
}
