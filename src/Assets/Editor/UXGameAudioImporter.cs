#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Restrict import settings to our own small, pre-rendered audio bank.
public sealed class UXGameAudioImporter : AssetPostprocessor
{
    public override uint GetVersion() => 1;
    private void OnPreprocessAudio()
    {
        if(!assetPath.StartsWith("Assets/Resources/UXAudio/",System.StringComparison.Ordinal))return;
        var importer=(AudioImporter)assetImporter;
        importer.forceToMono=false;
        importer.loadInBackground=false;
        AudioImporterSampleSettings settings=importer.defaultSampleSettings;
        settings.loadType=AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat=AudioCompressionFormat.PCM;
        settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;
        settings.quality=1f;
        importer.defaultSampleSettings=settings;
    }
}
#endif
