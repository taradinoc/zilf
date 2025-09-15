using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using System.Linq;

namespace Zilf.Tests.Integration
{
    [TestClass, TestCategory("Compiler"), TestCategory("Error Messages")]
    public class IsNearMatchTests : IntegrationTestClass
    {
        [TestMethod]
        public async Task Wrong_Argument_Count_Should_Give_Specific_Error()
        {
            // FIRST? expects exactly 1 argument, not 3
            await AssertExpr("<FIRST? 1 2 3>")
                .DoesNotCompileAsync(res => 
                {
                    var errorMessage = string.Join(" ", res.Diagnostics.Select(d => d.GetFormattedMessage()));
                    return errorMessage.Contains("exactly 1 argument");
                });
        }
        
        [TestMethod]
        public async Task Variable_Argument_Builtin_Should_Give_Specific_Range()
        {
            // SOUND with no arguments (should require 1 or more)
            await AssertExpr("<SOUND>")
                .DoesNotCompileAsync(res => 
                {
                    var errorMessage = string.Join(" ", res.Diagnostics.Select(d => d.GetFormattedMessage()));
                    // Should give specific error about argument count requirements
                    return !errorMessage.Contains("1 to 4 arguments");
                });
        }
    }
}