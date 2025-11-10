using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace Zilf.CompilerService.Tests
{
    [TestClass]
    public class CompilerServiceIntegrationTests
    {
        private sealed class TestAppFactory : WebApplicationFactory<Zilf.CompilerService.Program>
        {
            protected override void ConfigureClient(HttpClient client)
            {
                client.Timeout = TimeSpan.FromSeconds(60);
                base.ConfigureClient(client);
            }
        }

        record PrepareData(string sessionId, string? language = "zil", bool? debug = false, string? uuid = null);
        record FileAttributes(string name, string directory, string contents);
        record ProjectFile(string type, FileAttributes attributes);
        record PrepareRequest(PrepareData data, ProjectFile[] included);
        record PrepareResponse(string jobId);
        record CompileRequest(string jobId, string? mainFile, bool? debug);
        record CompileResponse(bool success, string jobId, string? storyfileUrl, string log, object[] messages);

        [TestMethod]
        public async Task Compile_WithMainFile_AndVersionDirective_ProducesStoryfile()
        {
            using var factory = new TestAppFactory();

            // Isolate service work root to temp directory
            var temp = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "zilf-work-" + Guid.NewGuid().ToString("N"))).FullName;
            Environment.SetEnvironmentVariable("ZILF_WORK_ROOT", temp);
            try
            {
                using var client = factory.CreateClient();

                var sessionId = "svc-it-1";
                var code = "<VERSION 5>\n<ROUTINE GO () <TELL \"HELLO\">>";
                var prepare = new PrepareRequest(
                    new PrepareData(sessionId),
                    new[]
                    {
                        new ProjectFile("file", new FileAttributes("main.zil", "", code))
                    }
                );

                var prepResp = await client.PostAsJsonAsync("/prepare", prepare);
                Assert.AreEqual(HttpStatusCode.OK, prepResp.StatusCode, "prepare failed");
                var prepJson = await prepResp.Content.ReadFromJsonAsync<PrepareResponse>();
                Assert.IsNotNull(prepJson);
                Assert.AreEqual(sessionId, prepJson!.jobId);

                var compile = new CompileRequest(sessionId, "main.zil", false);
                var compResp = await client.PostAsJsonAsync("/compile", compile);
                Assert.AreEqual(HttpStatusCode.OK, compResp.StatusCode, "compile failed http");
                var compJson = await compResp.Content.ReadFromJsonAsync<CompileResponse>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                Assert.IsNotNull(compJson, "compile response null");
                Assert.IsTrue(compJson!.success, "compile/assemble not successful. Log:\n" + compJson.log);
                Assert.IsFalse(string.IsNullOrEmpty(compJson.storyfileUrl), "storyfileUrl missing");

                // Download storyfile
                var storyResp = await client.GetAsync(compJson.storyfileUrl);
                Assert.AreEqual(HttpStatusCode.OK, storyResp.StatusCode, "storyfile get failed");
                var data = await storyResp.Content.ReadAsByteArrayAsync();
                Assert.IsTrue(data.Length > 0, "storyfile empty");
            }
            finally
            {
                // Cleanup temp dir
                try { Directory.Delete(temp, recursive: true); } catch { }
                Environment.SetEnvironmentVariable("ZILF_WORK_ROOT", null);
            }
        }
    }
}
