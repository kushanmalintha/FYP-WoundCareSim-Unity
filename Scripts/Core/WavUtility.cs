using System;
using UnityEngine;

public static class WavUtility
{
    public static AudioClip ToAudioClip(byte[] wavFile, string name = "TTSAudio")
    {
        // Parse channels from byte index 22
        int channels = BitConverter.ToInt16(wavFile, 22);
        
        // Parse sample rate from byte index 24
        int sampleRate = BitConverter.ToInt32(wavFile, 24);
        
        // Extract data starting at byte index 44
        int dataIndex = 44;
        int bytesCount = wavFile.Length - dataIndex;
        // Assume PCM 16-bit audio (2 bytes per sample)
        int samplesCount = bytesCount / 2;
        
        // Convert 16-bit samples to float (-1f to 1f)
        float[] audioData = new float[samplesCount];
        for (int i = 0; i < samplesCount; i++)
        {
            short sample = BitConverter.ToInt16(wavFile, dataIndex + i * 2);
            audioData[i] = sample / 32768f;
        }
        
        // Create AudioClip. Samples count is per channel
        AudioClip clip = AudioClip.Create(name, samplesCount / channels, channels, sampleRate, false);
        clip.SetData(audioData, 0);
        return clip;
    }
}
