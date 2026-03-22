using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using OpenFFBoardPlugin.Utils;

namespace OpenFFBoardPlugin.DTO
{
    internal class ProfileHolder
    {
        public int Release { get; set; }
        public GlobalSettings Global { get; set; }
        [JsonConverter(typeof(FlexibleListConverter<Profile>))]
        public List<Profile> Profiles { get; set; }

        public static ProfileHolder LoadFromJson(string profilePath)
        {
            return JsonHandler.LoadFromJsonFile<ProfileHolder>(profilePath);
        }

        public void SaveToJson(string profilePath)
        {
            JsonHandler.SaveToJsonFile(profilePath, this);
        }

        /// <summary>
        /// Returns the profile for the given game name. If none exists, clones the "default"
        /// profile (or creates a blank one), adds it to the list, and returns it.
        /// The caller is responsible for saving back to disk.
        /// </summary>
        public Profile GetOrCreateProfileForGame(string gameName)
        {
            if (Profiles == null)
                Profiles = new List<Profile>();

            var existing = Profiles.Find(p => p.Name.Equals(gameName, StringComparison.InvariantCultureIgnoreCase));
            if (existing != null)
                return existing;

            var defaultProfile = Profiles.Find(p => p.Name.Equals("default", StringComparison.InvariantCultureIgnoreCase));

            Profile newProfile = defaultProfile != null
                ? JsonHandler.Clone(defaultProfile)
                : new Profile { Data = new List<ProfileData>() };

            newProfile.Name = gameName;
            Profiles.Add(newProfile);
            return newProfile;
        }
    }

    internal class GlobalSettings
        {
        public bool DonotnotifyUpdates { get; set; }
        public string Language { get; set; }
    }

    internal class Profile
    {
        public string Name { get; set; }
        [JsonConverter(typeof(FlexibleListConverter<ProfileData>))]
        public List<ProfileData> Data { get; set; }
    }

    internal class ProfileData
    {
        public string Fullname { get; set; }
        public string Cls { get; set; }
        public int Instance { get; set; }
        public string Cmd { get; set; }
        public int Value { get; set; }
    }
}
