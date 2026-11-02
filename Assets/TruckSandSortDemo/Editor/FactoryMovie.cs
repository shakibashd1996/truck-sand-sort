using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Process = System.Diagnostics.Process;

namespace TruckSandSortDemo.Editor
{
    [InitializeOnLoad]
    public static class FactoryMovie
    {
        const string Request = "Temp/RecordFactory.request";
        static FactoryMovie() { EditorApplication.update += CheckRequest; }

        [MenuItem("Tools/Truck Sand Sort/Record 60 second MP4")]
        static void RequestMovie() { File.WriteAllText(Request, ""); }

        static void CheckRequest()
        {
            if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (!EditorApplication.isPlaying)
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode) EditorApplication.isPlaying = true;
                return;
            }
            var factory = UnityEngine.Object.FindFirstObjectByType<FactoryPortrait>();
            var camera = Camera.main;
            if (!factory || !camera) return;
            File.Delete(Request);
            try { Record(factory, camera); }
            catch (Exception error) { Debug.LogException(error); }
            finally { EditorApplication.isPlaying = false; }
        }

        static void Record(FactoryPortrait factory, Camera camera)
        {
            string folder = Path.GetFullPath("Captures");
            Directory.CreateDirectory(folder);
            string output = Path.Combine(folder, "Truck-Sand-Sort-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".mp4");
            string encoder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"KMPlayer 64X\LAVFilters64\ffmpeg.exe");
            var settings = new System.Diagnostics.ProcessStartInfo {
                FileName = File.Exists(encoder) ? encoder : "ffmpeg",
                Arguments = "-hide_banner -loglevel error -f rawvideo -pix_fmt rgb24 -s 1080x1920 -r 30 -i pipe:0 -vf vflip -an -c:v libx264 -preset veryfast -crf 17 -pix_fmt yuv420p -movflags +faststart \"" + output + "\"",
                UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true
            };
            var originalTarget = camera.targetTexture;
            var originalActive = RenderTexture.active;
            var target = RenderTexture.GetTemporary(1080, 1920, 24, RenderTextureFormat.ARGB32);
            var pixels = new Texture2D(1080, 1920, TextureFormat.RGB24, false);
            var bytes = new byte[1080 * 1920 * 3];
            try
            {
                using (var process = Process.Start(settings))
                {
                    camera.targetTexture = target;
                    using (var stream = process.StandardInput.BaseStream)
                    {
                        for (int frame = 0; frame < 1800; frame++)
                        {
                            factory.PreviewTime(frame / 30f);
                            Canvas.ForceUpdateCanvases();
                            camera.Render();
                            RenderTexture.active = target;
                            pixels.ReadPixels(new Rect(0, 0, 1080, 1920), 0, 0, false);
                            pixels.GetRawTextureData<byte>().CopyTo(bytes);
                            stream.Write(bytes, 0, bytes.Length);
                            if (frame % 150 == 0) Debug.Log("[FactoryMovie] frame " + frame);
                        }
                    }
                    process.WaitForExit();
                    if (process.ExitCode != 0) throw new IOException("Video encoder exit code " + process.ExitCode);
                }
                Debug.Log("[FactoryMovie] Saved " + output);
            }
            finally
            {
                camera.targetTexture = originalTarget;
                RenderTexture.active = originalActive;
                RenderTexture.ReleaseTemporary(target);
                UnityEngine.Object.DestroyImmediate(pixels);
            }
        }
    }
}
