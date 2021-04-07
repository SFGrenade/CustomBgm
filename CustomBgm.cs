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
        internal static CustomBgm Instance;
        private readonly string DIR;

        private readonly string FOLDER = "CustomBgm";

        //private int[] memoryHugger1;
        //private int[] memoryHugger2;
        //private int[] memoryHugger3;
        //private int[] memoryHugger4;
        //private int[] memoryHugger5;
        //private int[] memoryHugger6;
        //private int[] memoryHugger7;
        //private int[] memoryHugger8;
        //private int[] memoryHugger9;
        //private int[] memoryHugger10;

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

            if (!Directory.Exists(DIR)) Directory.CreateDirectory(DIR);

            InitCallbacks();
        }

        public override string GetVersion()
        {
            var asm = Assembly.GetExecutingAssembly();
            var ver = asm.GetName().Version.ToString();
            var sha1 = SHA1.Create();
            var stream = File.OpenRead(asm.Location);
            var hashBytes = sha1.ComputeHash(stream);
            var hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            stream.Close();
            sha1.Clear();
            return $"{ver}-{hash.Substring(0, 6)}";
        }

        public override void Initialize()
        {
            Log("Initializing");
            Instance = this;

            //Log("Loading Hugger 1");
            //memoryHugger1 = new int[536870912];
            //Log("Loading Hugger 2");
            //memoryHugger2 = new int[536870912];
            //Log("Loading Hugger 3");
            //memoryHugger3 = new int[536870912];
            //Log("Loading Hugger 4");
            //memoryHugger4 = new int[536870912];
            //Log("Loading Hugger 5");
            //memoryHugger5 = new int[536870912];
            //Log("Loading Hugger 6");
            //memoryHugger6 = new int[536870912];
            //Log("Loading Hugger 7");
            //memoryHugger7 = new int[536870912];
            //Log("Loading Hugger 8");
            //memoryHugger8 = new int[536870912];
            //Log("Loading Hugger 9");
            //memoryHugger9 = new int[536870912];
            //Log("Loading Hugger 10");
            //memoryHugger10 = new int[536870912];

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
            var changed = false;
            var infosFieldInfo = musicCue.GetType()
                .GetField("channelInfos", BindingFlags.NonPublic | BindingFlags.Instance);
            var infos = (MusicCue.MusicChannelInfo[]) infosFieldInfo.GetValue(musicCue);

            foreach (var info in infos)
            {
                var audioFieldInfo = info.GetType().GetField("clip", BindingFlags.NonPublic | BindingFlags.Instance);
                var origAudio = (AudioClip) audioFieldInfo.GetValue(info);

                if (origAudio != null)
                {
                    var possibleReplace = GetAudioClip(origAudio.name);
                    if (possibleReplace != null)
                    {
                        // Change Audio Clip
                        audioFieldInfo.SetValue(info, possibleReplace);
                        changed = true;
                    }
                }
            }

            if (changed) infosFieldInfo.SetValue(musicCue, infos);

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