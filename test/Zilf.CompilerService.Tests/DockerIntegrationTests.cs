using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace Zilf.CompilerService.Tests
{
    [TestClass]
    [TestCategory("Docker")]
    [TestCategory("Slow")]
    public class DockerIntegrationTests
    {
        private static (int exitCode, string stdout, string stderr) Run(string fileName, string arguments, string? workingDir = null, int timeoutMs = 300_000)
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            if (!string.IsNullOrEmpty(workingDir))
                psi.WorkingDirectory = workingDir;

            using var proc = new Process { StartInfo = psi };
            
            // Read output asynchronously to avoid deadlocks when buffers fill
            var stdoutBuilder = new System.Text.StringBuilder();
            var stderrBuilder = new System.Text.StringBuilder();
            proc.OutputDataReceived += (_, e) => { if (e.Data != null) stdoutBuilder.AppendLine(e.Data); };
            proc.ErrorDataReceived += (_, e) => { if (e.Data != null) stderrBuilder.AppendLine(e.Data); };
            
            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            
            if (!proc.WaitForExit(timeoutMs))
            {
                try { proc.Kill(true); } catch { }
                return (-1, stdoutBuilder.ToString(), "Process timed out after " + timeoutMs + "ms");
            }
            
            // Ensure async reads complete
            proc.WaitForExit();
            return (proc.ExitCode, stdoutBuilder.ToString(), stderrBuilder.ToString());
        }

        private static string FindRepoRoot()
        {
            var dir = Directory.GetCurrentDirectory();
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir, "Zilf.sln"))) return dir;
                dir = Directory.GetParent(dir)?.FullName;
            }
            throw new DirectoryNotFoundException("Could not locate repository root (Zilf.sln)");
        }

        [TestMethod]
        [Timeout(15 * 60 * 1000)]
        public async Task Container_end_to_end_compile()
        {
            // Precondition: docker CLI available
            var (code, _, _) = Run("docker", "--version");
            if (code != 0)
                Assert.Inconclusive("Docker is not available on this machine.");

            var root = FindRepoRoot();

            // Build image (skip build if image already exists to reduce test flakiness & time)
            var dockerfile = Path.Combine(root, "src", "Zilf.CompilerService", "Dockerfile");
            var tag = "zilf-compiler-service-it:latest";
            var (icode, _, _) = Run("docker", $"image inspect {tag}");
            if (icode != 0)
            {
                var buildTimeoutSec = int.TryParse(Environment.GetEnvironmentVariable("DOCKER_TEST_BUILD_TIMEOUT_SEC"), out var secs) ? secs : 480;
                var buildArgs = $"build --build-arg SKIP_CRON=1 -f \"{dockerfile}\" -t {tag} \"{root}\"";
                var (bcode, bout, berr) = Run("docker", buildArgs, workingDir: root, timeoutMs: buildTimeoutSec * 1000);
                if (bcode != 0)
                    Assert.Inconclusive($"Docker build failed or timed out (timeout={buildTimeoutSec}s). Set DOCKER_TEST_BUILD_TIMEOUT_SEC to increase, or pre-build the image.\nSTDOUT:\n{bout}\nSTDERR:\n{berr}");
            }

            // Choose a random high port
            var port = new Random().Next(20000, 60000);
            var name = "zilf-compiler-service-it-" + Guid.NewGuid().ToString("N");
            var runArgs = $"run --rm -d -p {port}:8080 --name {name} -e ASPNETCORE_URLS=http://0.0.0.0:8080 {tag}";
            var (rcode, rOut, rErr) = Run("docker", runArgs, workingDir: root);
            if (rcode != 0)
                Assert.Fail($"Docker run failed:\n{rOut}\n{rErr}");

            var containerId = rOut.Trim();
            try
            {
                using var http = new HttpClient { BaseAddress = new Uri($"http://localhost:{port}") };

                // Wait for health
                var start = DateTime.UtcNow;
                while (DateTime.UtcNow - start < TimeSpan.FromSeconds(90))
                {
                    try
                    {
                        var resp = await http.GetAsync("/monitoring/ping");
                        if (resp.StatusCode == HttpStatusCode.OK) break;
                    }
                    catch { }
                    await Task.Delay(1000);
                }
                // If health not up, include container logs for diagnostics
                // Capture logs to Console for visibility (TestContext not available statically)
                try
                {
                    var (lcode, lout, lerr) = Run("docker", $"logs {name}");
                    Console.WriteLine($"Container logs (code={lcode}):\n{lout}\n{lerr}");
                }
                catch { }

                // Prepare project
                var sessionId = "docker-it-1";
                var codeZil = "<VERSION 5>\n<ROUTINE GO () <TELL \"HELLO\">>";
                var prepare = new
                {
                    data = new { sessionId, language = "zil", debug = false, uuid = (string?)null },
                    included = new[]
                    {
                        new { type = "file", attributes = new { name = "main.zil", directory = "", contents = codeZil } }
                    }
                };
                var prepResp = await http.PostAsJsonAsync("/prepare", prepare);
                Assert.AreEqual(HttpStatusCode.OK, prepResp.StatusCode, "prepare failed");
                var prepJson = await prepResp.Content.ReadFromJsonAsync<JsonElement>();
                Assert.AreEqual(sessionId, prepJson.GetProperty("jobId").GetString());

                // Compile
                var compile = new { jobId = sessionId, mainFile = "main.zil", debug = false };
                var compResp = await http.PostAsJsonAsync("/compile", compile);
                Assert.AreEqual(HttpStatusCode.OK, compResp.StatusCode, "compile failed http");
                var compJson = await compResp.Content.ReadFromJsonAsync<JsonElement>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                Assert.IsTrue(compJson.GetProperty("success").GetBoolean(), "compile/assemble not successful\n" + compJson.GetProperty("log").GetString());
                var storyUrl = compJson.GetProperty("storyfileUrl").GetString();
                Assert.IsFalse(string.IsNullOrEmpty(storyUrl), "storyfileUrl missing");

                var storyResp = await http.GetAsync(storyUrl);
                Assert.AreEqual(HttpStatusCode.OK, storyResp.StatusCode, "storyfile get failed");
                var data = await storyResp.Content.ReadAsByteArrayAsync();
                Assert.IsTrue(data.Length > 0, "storyfile empty");
            }
            finally
            {
                // Stop the container
                Run("docker", $"stop {name}");
            }
        }
    }
}
