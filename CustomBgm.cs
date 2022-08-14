using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using Modding;
using UnityEngine;

namespace CustomBgm
{
    public class CustomBgm : Mod
    {
        private readonly string _dir;

        private readonly string _folder = "CustomBgm";

        public CustomBgm() : base("Custom Background Music")
        {
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
            
            Log("Initialized");
        }

        private void InitCallbacks()
        {
            // Hooks
            On.AudioManager.BeginApplyMusicCue += OnAudioManagerBeginApplyMusicCue;
        }

        private IEnumerator OnAudioManagerBeginApplyMusicCue(On.AudioManager.orig_BeginApplyMusicCue orig, AudioManager self, MusicCue musicCue, float delayTime, float transitionTime, bool applySnapshot)
        {
            bool changed = false;
            MusicCue.MusicChannelInfo[] infos = ReflectionHelper.GetField<MusicCue, MusicCue.MusicChannelInfo[]>(musicCue, "channelInfos");

            foreach (MusicCue.MusicChannelInfo info in infos)
            {
                AudioClip origAudio = ReflectionHelper.GetField<MusicCue.MusicChannelInfo, AudioClip>(info, "clip");

                if (origAudio != null)
                {
                    AudioClip possibleReplace = GetAudioClip(origAudio.name);
                    if (possibleReplace != null)
                    {
                        // Change Audio Clip
                        ReflectionHelper.SetField<MusicCue.MusicChannelInfo, AudioClip>(info, "clip", possibleReplace);
                        changed = true;
                    }
                }
            }

            if (changed) ReflectionHelper.SetField<MusicCue, MusicCue.MusicChannelInfo[]>(musicCue, "channelInfos", infos);
            
            yield return orig(self, musicCue, delayTime, transitionTime, applySnapshot);
        }

        private AudioClip GetAudioClip(string origName)
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