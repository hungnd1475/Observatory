using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Observatory.Core.Persistence
{
    public class ProfilePersistenceConfiguration
    {
        public string ProfileRegistryPath { get; }
        public string ProfileDataDirectory { get; }

        public ProfilePersistenceConfiguration(string profileRegistryPath,
            string profileDataDirectory)
        {
            ProfileRegistryPath = profileRegistryPath;
            ProfileDataDirectory = profileDataDirectory;
        }
    }
}
