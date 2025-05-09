﻿using System;
using System.Collections;
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

    public CustomBgm() : base("Custom Background Music")
    {
        _dir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, _folder);

        if (!Directory.Exists(_dir))
        {
            Directory.CreateDirectory(_dir);
        }

        _dirToUse = _dir;

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
        DebugLog("Initializing");

        DebugLog("Initialized");
    }

    private void InitCallbacks()
    {
        // Hooks
        On.AudioManager.BeginApplyMusicCue += OnAudioManagerBeginApplyMusicCue;

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

    private AudioClip GetAudioClip(string origName)
    {
        if (File.Exists($"{_dirToUse}/{origName}.wav"))
        {
            DebugLog($"Using audio file \"{origName}.wav\"");
            FileStream stream = File.OpenRead($"{_dirToUse}/{origName}.wav");
            WavData.Inspect(stream, DebugLog);
            WavData wavData = new WavData();
            wavData.Parse(stream, DebugLog);
            stream.Close();

            DebugLog($"{origName} - AudioFormat: {wavData.FormatChunk.AudioFormat}");
            DebugLog($"{origName} - NumChannels: {wavData.FormatChunk.NumChannels}");
            DebugLog($"{origName} - SampleRate: {wavData.FormatChunk.SampleRate}");
            DebugLog($"{origName} - ByteRate: {wavData.FormatChunk.ByteRate}");
            DebugLog($"{origName} - BlockAlign: {wavData.FormatChunk.BlockAlign}");
            DebugLog($"{origName} - BitsPerSample: {wavData.FormatChunk.BitsPerSample}");

            float[] wavSoundData = wavData.GetSamples();
            AudioClip audioClip = AudioClip.Create(origName, wavSoundData.Length / wavData.FormatChunk.NumChannels, wavData.FormatChunk.NumChannels, (int) wavData.FormatChunk.SampleRate, false);
            audioClip.SetData(wavSoundData, 0);
            return audioClip;
            //return WavUtility.ToAudioClip($"{_dirToUse}/{origName}.wav");
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