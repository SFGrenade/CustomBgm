using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using Modding;
using UnityEngine;
using WavLib;

namespace CustomBgm;

public class CustomBgm : Mod
{
    private readonly string _dir;

    private readonly string _folder = "CustomBgm";

    private string _dirToUse;

    private Dictionary<string, AudioClip> _audioClipCache = new Dictionary<string, AudioClip>();

    public CustomBgm() : base("Custom Background Music")
    {
        _dir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, _folder);

        if (!Directory.Exists(_dir))
        {
            Directory.CreateDirectory(_dir);
        }

        _dirToUse = _dir;
        CacheAudioFiles();

        InitCallbacks();
    }

    private void CacheAudioFiles()
    {
        _audioClipCache.Clear();

        DirectoryInfo directoryInfo = new DirectoryInfo(_dirToUse);
        foreach (FileInfo fileInfo in directoryInfo.GetFiles())
        {
            if (fileInfo.Extension == ".wav")
            {
                string fullFilename = fileInfo.Name;
                string mainFilename = Path.GetFileNameWithoutExtension(fullFilename);
                DebugLog($"Caching audio file \"{fullFilename}\"");
                FileStream stream = File.OpenRead(fileInfo.FullName);
                WavData.Inspect(stream, DebugLog);
                WavData wavData = new WavData();
                wavData.Parse(stream, DebugLog);
                stream.Close();

                DebugLog($"{mainFilename} - AudioFormat: {wavData.FormatChunk.AudioFormat}");
                DebugLog($"{mainFilename} - NumChannels: {wavData.FormatChunk.NumChannels}");
                DebugLog($"{mainFilename} - SampleRate: {wavData.FormatChunk.SampleRate}");
                DebugLog($"{mainFilename} - ByteRate: {wavData.FormatChunk.ByteRate}");
                DebugLog($"{mainFilename} - BlockAlign: {wavData.FormatChunk.BlockAlign}");
                DebugLog($"{mainFilename} - BitsPerSample: {wavData.FormatChunk.BitsPerSample}");

                float[] wavSoundData = wavData.GetSamples();
                AudioClip audioClip = AudioClip.Create(mainFilename, wavSoundData.Length / wavData.FormatChunk.NumChannels, wavData.FormatChunk.NumChannels, (int) wavData.FormatChunk.SampleRate, false);
                audioClip.SetData(wavSoundData, 0);
                GameObject.DontDestroyOnLoad(audioClip);
                _audioClipCache.Add(mainFilename.ToUpper(), audioClip);
            }
        }
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
        DebugLog("Initializing");

        DebugLog("Initialized");
    }

    private void InitCallbacks()
    {
        // Hooks
        On.AudioManager.BeginApplyMusicCue += OnAudioManagerBeginApplyMusicCue;
        // colosseum bgm special case
        On.HutongGames.PlayMaker.Actions.AudioPlaySimple.OnEnter += OnAudioPlaySimpleOnEnter;

        // CustomKnight hook
        if (ModHooks.GetMod("CustomKnight") is Mod)
        {
            AddCustomKnightHandle();
        }
    }

    private void AddCustomKnightHandle()
    {
        CustomKnight.SkinManager.OnSetSkin += ResetAudio;
    }

    private void ResetAudio(object sender, EventArgs e)
    {
        string currentSkinPath = CustomKnight.SkinManager.GetCurrentSkin().getSwapperPath();
        // refer to `CustomKnight.SkinManager.DEFAULT_SKIN`
        if (CustomKnight.SkinManager.GetCurrentSkin().GetId() == "Default")
        {
            // when default skin, use default folder lol
            currentSkinPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        }
        if (Directory.Exists(Path.Combine(currentSkinPath, _folder)))
        {
            _dirToUse = Path.Combine(currentSkinPath, _folder);
            CacheAudioFiles();
        }
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

    private void OnAudioPlaySimpleOnEnter(On.HutongGames.PlayMaker.Actions.AudioPlaySimple.orig_OnEnter orig, HutongGames.PlayMaker.Actions.AudioPlaySimple self)
    {
        if (self.oneShotClip != null && self.oneShotClip.Value != null)
        {
            AudioClip possibleReplace = GetAudioClip(self.oneShotClip.Value.name);
            if (possibleReplace != null)
            {
                self.oneShotClip.Value = possibleReplace;
            }
        }
        else
        {
            // otherwise try existing audioclip on gameobjects audio
            GameObject owner = self.Fsm.GetOwnerDefaultTarget(self.gameObject);
            if (owner != null)
            {
                AudioSource src = owner.GetComponent<AudioSource>();
                if (src != null && src.clip != null)
                {
                    AudioClip possibleReplace = GetAudioClip(src.clip.name);
                    if (possibleReplace != null)
                    {
                        src.clip = possibleReplace;
                    }
                }
            }
        }
        orig(self);
    }

    private AudioClip GetAudioClip(string origName)
    {
        if (_audioClipCache.ContainsKey(origName.ToUpper()))
        {
            // audioclip is in cache
            return _audioClipCache[origName.ToUpper()];
        }
        DebugLog($"Using original for \"{origName}\"");
        return null;
    }

    private void DebugLog(string msg)
    {
        Log(msg);
        Debug.Log("[CustomBgm] - " + msg);
    }

    private void DebugLog(object msg)
    {
        DebugLog($"{msg}");
    }
}