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
    public static byte[] GetWavBytes(float[] samples, int sampleRate, int channels)
    {
        using (var stream = new System.IO.MemoryStream())
        {
            using (var writer = new System.IO.BinaryWriter(stream))
            {
                writer.Write(new char[4] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + samples.Length * 2);
                writer.Write(new char[4] { 'W', 'A', 'V', 'E' });
                writer.Write(new char[4] { 'f', 'm', 't', ' ' });
                writer.Write(16);
                writer.Write((short)1); // PCM
                writer.Write((short)channels);
                writer.Write(sampleRate);
                writer.Write(sampleRate * channels * 2);
                writer.Write((short)(channels * 2));
                writer.Write((short)16);
                writer.Write(new char[4] { 'd', 'a', 't', 'a' });
                writer.Write(samples.Length * 2);

                for (int i = 0; i < samples.Length; i++)
                {
                    writer.Write((short)(samples[i] * 32767f));
                }
            }
            return stream.ToArray();
        }
    }
}
