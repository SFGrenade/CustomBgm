using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using Modding;
using UnityEngine;

namespace CustomBgm
{
    public class CustomBgm : Mod
    {
        internal static CustomBgm? Instance;
        private readonly string _dir;

        private readonly string _folder = "CustomBgm";

        public CustomBgm() : base("Custom Background Music")
        {
            Instance = this;

            _dir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new DirectoryNotFoundException("I have no idea how you did this, but good luck figuring it out."), _folder);

            if (!Directory.Exists(_dir))
            {
                Directory.CreateDirectory(_dir);
            }

            InitCallbacks();
        }

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

        private void OnAudioManagerApplyMusicCue(On.AudioManager.orig_ApplyMusicCue orig, AudioManager self,
            MusicCue musicCue, float delayTime, float transitionTime, bool applySnapshot)
        {
            bool changed = false;
            FieldInfo? infosFieldInfo = musicCue.GetType()
                .GetField("channelInfos", BindingFlags.NonPublic | BindingFlags.Instance);
            if (infosFieldInfo == null) return;
            MusicCue.MusicChannelInfo[] infos = (MusicCue.MusicChannelInfo[]) infosFieldInfo.GetValue(musicCue);

            foreach (var info in infos)
            {
                var audioFieldInfo = info.GetType().GetField("clip", BindingFlags.NonPublic | BindingFlags.Instance);
                var origAudio = (AudioClip?) audioFieldInfo?.GetValue(info);

                if (origAudio != null)
                {
                    var possibleReplace = GetAudioClip(origAudio.name);
                    if (possibleReplace != null)
                    {
                        // Change Audio Clip
                        audioFieldInfo?.SetValue(info, possibleReplace);
                        changed = true;
                    }
                }
            }

            if (changed) infosFieldInfo.SetValue(musicCue, infos);

            orig(self, musicCue, delayTime, transitionTime, applySnapshot);
        }

        private AudioClip? GetAudioClip(string origName)
        {
            if (File.Exists($"{_dir}/{origName}.wav"))
            {
                Log($"Using audio file \"{origName}.wav\"");
                return WavUtility.ToAudioClip($"{_dir}/{origName}.wav");
            }

            Log($"Using original for \"{origName}\"");
            return null;
        }
    }
}