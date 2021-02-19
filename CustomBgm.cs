using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using GlobalEnums;
using Modding;
using UnityEngine;

namespace CustomBgm
{
    class CustomBgm : Mod
    {
        internal static CustomBgm Instance;

        private readonly string FOLDER = "CustomBgm";
        private readonly string DIR;

        public override string GetVersion()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            string ver = asm.GetName().Version.ToString();
            SHA1 sha1 = SHA1.Create();
            FileStream stream = File.OpenRead(asm.Location);
            byte[] hashBytes = sha1.ComputeHash(stream);
            string hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            stream.Close();
            sha1.Clear();
            return $"{ver}-{hash.Substring(0, 6)}";
        }

        public CustomBgm() : base("Custom Background Music")
        {
            Instance = this;

            switch (SystemInfo.operatingSystemFamily)
            {
                case OperatingSystemFamily.MacOSX:
                    DIR = Path.GetFullPath(Application.dataPath + "/Resources/Data/Managed/Mods/" + FOLDER);
                    break;
                default:
                    DIR = Path.GetFullPath(Application.dataPath + "/Managed/Mods/" + FOLDER);
                    break;
            }
            if (!Directory.Exists(DIR))
            {
                Directory.CreateDirectory(DIR);
            }

            InitCallbacks();
        }

        public override void Initialize()
        {
            Log("Initializing");
            Instance = this;

            Log("Initialized");
        }

        private void InitCallbacks()
        {
            // Hooks
            On.AudioManager.ApplyMusicCue += OnAudioManagerApplyMusicCue;
        }

        private void OnAudioManagerApplyMusicCue(On.AudioManager.orig_ApplyMusicCue orig, AudioManager self, MusicCue musicCue, float delayTime, float transitionTime, bool applySnapshot)
        {
            bool changed = false;
            var infosFieldInfo = musicCue.GetType().GetField("channelInfos", BindingFlags.NonPublic | BindingFlags.Instance);
            MusicCue.MusicChannelInfo[] infos = (MusicCue.MusicChannelInfo[]) infosFieldInfo.GetValue(musicCue);

            foreach (MusicCue.MusicChannelInfo info in infos)
            {
                var audioFieldInfo = info.GetType().GetField("clip", BindingFlags.NonPublic | BindingFlags.Instance);
                AudioClip origAudio = (AudioClip) audioFieldInfo.GetValue(info);

                if (origAudio != null)
                {
                    AudioClip possibleReplace = GetAudioClip(origAudio.name);
                    if (possibleReplace != null)
                    {
                        // Change Audio Clip
                        audioFieldInfo.SetValue(info, possibleReplace);
                        changed = true;
                    }
                }
            }
            if (changed)
            {
                infosFieldInfo.SetValue(musicCue, infos);
            }

            orig(self, musicCue, delayTime, transitionTime, applySnapshot);
        }

        private AudioClip GetAudioClip(string origName)
        {
            if (File.Exists($"{DIR}/{origName}.wav"))
            {
                Log($"Using audio file \"{origName}.wav\"");
                return WavUtility.ToAudioClip($"{DIR}/{origName}.wav");
            }
            Log($"Using original for \"{origName}\"");
            return null;
        }
    }
}
