using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace IntelligentAttendanceSystem.Services
{
    public class FaceService
    {
        private readonly string _pythonPath = "python"; // Assume python is in PATH
        private readonly string _scriptPath;
        private readonly string _faceDataDir;

        public FaceService(string scriptPath, string faceDataDir)
        {
            _scriptPath = scriptPath;
            _faceDataDir = faceDataDir;

            if (!Directory.Exists(_faceDataDir))
            {
                Directory.CreateDirectory(_faceDataDir);
            }
        }

        public async Task<FaceResult> RegisterFaceAsync(string imageBase64, string identifier)
        {
            string savePath = Path.Combine(_faceDataDir, identifier + ".npy");
            return await RunPythonAsync("register", savePath, imageBase64);
        }

        public async Task<FaceResult> VerifyFaceAsync(string imageBase64)
        {
            return await RunPythonAsync("verify", _faceDataDir, imageBase64);
        }

        private async Task<FaceResult> RunPythonAsync(string command, string argument, string imageBase64)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _pythonPath,
                Arguments = $"\"{_scriptPath}\" {command} \"{argument}\"",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            // Write imageBase64 to standard input
            await using (var sw = process.StandardInput)
            {
                if (sw.BaseStream.CanWrite)
                {
                    await sw.WriteAsync(imageBase64);
                }
            }

            string resultStr = await process.StandardOutput.ReadToEndAsync();
            string errorStr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (!string.IsNullOrEmpty(errorStr))
            {
                Console.WriteLine("Python Error: " + errorStr);
            }

            if (process.ExitCode != 0)
            {
                return new FaceResult
                {
                    Success = false,
                    Message = !string.IsNullOrEmpty(errorStr)
                        ? errorStr.Trim()
                        : $"Python process exited with code {process.ExitCode}."
                };
            }

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<FaceResult>(resultStr, options);
                return result ?? new FaceResult { Success = false, Message = "Python returned null." };
            }
            catch (Exception ex)
            {
                return new FaceResult { Success = false, Message = "Failed to parse Python output: " + ex.Message + "\nOutput: " + resultStr };
            }
        }
    }

    public class FaceResult
    {
        public bool Success { get; set; }
        public bool Match { get; set; }
        public string Identifier { get; set; } = string.Empty;
        public float Confidence { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
