using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;

namespace Bounce_Companion
{
    public struct ConfigJson
    {
        [JsonProperty("mapspath")]
        public string MapsPath { get; private set; }

        [JsonProperty("custommapspath")]
        public string CustomMapsPath { get; private set; }

        [JsonProperty("serverPlaylistPath")]
        public string ServerPlaylistPath { get; private set; }

        [JsonProperty("moveSpeed")]
        public string moveSpeed { get; private set; }

        [JsonProperty("turnSpeed")]
        public string turnSpeed { get; private set; }

        [JsonProperty("pitchSpeed")]
        public string pitchSpeed { get; private set; }

        [JsonProperty("heightSpeed")]
        public string heightSpeed { get; private set; }

        [JsonProperty("rollSpeed")]
        public string rollSpeed { get; private set; }
        [JsonProperty("globalTransitionTime")]
        public string globalTransitionTime { get; private set; }

    }
}
