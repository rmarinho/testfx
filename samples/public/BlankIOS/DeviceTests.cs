// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BlankIOS.Tests;

[TestClass]
public class DeviceTests
{
    private const string TAG = "DeviceTests";

    [TestMethod]
    public void SimpleTest_ShouldPass()
    {
        // Arrange
        int a = 2;
        int b = 3;

        // Act
        int result = a + b;

        // Assert
        Assert.AreEqual(5, result);
        Console.WriteLine($"[{TAG}] SimpleTest_ShouldPass completed");
    }

    [TestMethod]
    public void IOSPlatformTest()
    {
        // Verify we're running on iOS
        Assert.IsTrue(OperatingSystem.IsIOS(), "Should be running on iOS");
        Console.WriteLine($"[{TAG}] IOSPlatformTest completed");
    }

    [TestMethod]
    public void StringTest_ShouldPass()
    {
        // Test string operations
        string hello = "Hello";
        string world = "World";

        string result = $"{hello}, {world}!";

        Assert.AreEqual("Hello, World!", result);
        Console.WriteLine($"[{TAG}] StringTest_ShouldPass completed");
    }

    [TestMethod]
    public async Task LongRunningTest_30Seconds()
    {
        // This test takes approximately 30 seconds to complete
        // Used to verify that the app properly waits for test completion
        Console.WriteLine($"[{TAG}] LongRunningTest_30Seconds started - will take 30 seconds");

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Simulate work over 30 seconds with periodic logging
        for (int i = 1; i <= 30; i++)
        {
            await Task.Delay(1000);

            if (i % 5 == 0)
            {
                Console.WriteLine($"[{TAG}] LongRunningTest_30Seconds progress: {i}/30 seconds");
            }
        }

        stopwatch.Stop();

        Console.WriteLine($"[{TAG}] LongRunningTest_30Seconds completed in {stopwatch.Elapsed.TotalSeconds:F1} seconds");

        // Verify the test actually took approximately 30 seconds
        Assert.IsTrue(stopwatch.Elapsed.TotalSeconds >= 29, "Test should take at least 29 seconds");
    }
}
