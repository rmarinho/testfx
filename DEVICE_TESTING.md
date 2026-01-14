# Device Testing Implementation Status

## Goal
Make `dotnet test` work exactly like `dotnet run` for device projects:

```bash
# Current dotnet run (works in .NET 11):
dotnet run --project MyTests.csproj -f net10.0-android --device emulator-5554
dotnet run --project MyTests.csproj -f net10.0-ios --device "iPhone Xs"

# Goal for dotnet test:
dotnet test --project MyTests.csproj -f net10.0-android --device emulator-5554
dotnet test --project MyTests.csproj -f net10.0-ios --device "iPhone Xs"
```

## Current Status: ✅ Working for Android and iOS

The implementation supports both **Android** and **iOS** device testing with Microsoft.Testing.Platform.

### Android Test Execution

#### Mode 1: Activity Mode (Default) - via `dotnet run --device`

```bash
dotnet test BlankAndroid.csproj -f net10.0-android \
  -p:DeviceId=emulator-5554 \
  -p:DotnetDevicePath=/path/to/dotnet11
```

#### Mode 2: Instrumentation Mode - via `adb instrument`

```bash
dotnet test BlankAndroid.csproj -f net10.0-android \
  -p:DeviceId=emulator-5554 \
  -p:DotnetDevicePath=/path/to/dotnet11 \
  -p:UseInstrumentation=true
```

### iOS Test Execution

```bash
dotnet test BlankIOS.csproj -f net10.0-ios \
  -p:DeviceId="iPhone Xs" \
  -p:DotnetDevicePath=/path/to/dotnet11
```

### Test Output
```
# ✓ Passed:  SimpleTest_ShouldPass
# ✓ Passed:  IOSPlatformTest / AndroidPlatformTest
# ✓ Passed:  StringTest_ShouldPass
# ✓ Passed:  LongRunningTest_30Seconds
#
# Test run summary: Passed!
#   total: 4
#   failed: 0
#   succeeded: 4
#   skipped: 0
#   duration: 30s 095ms
```

## What Works ✅

| Feature | Android | iOS | Implementation |
|---------|---------|-----|----------------|
| Build device test project | ✅ | ✅ | Standard MSBuild |
| Deploy to device/emulator | ✅ | ✅ | Via `dotnet run --device` |
| Execute tests on device | ✅ | ✅ | MainActivity/AppDelegate |
| **Long-running tests** | ✅ | ✅ | App runs until tests complete, then exits |
| Test results to console/logcat | ✅ | ✅ | `IDataConsumer` MTP extension |
| Session start/end events | ✅ | ✅ | `ITestSessionLifetimeHandler` |
| Pass/Fail/Error output | ✅ | ✅ | Streamed via logcat/console |
| Exit code propagation | ✅ | ✅ | Via `Environment.Exit()` |
| **TRX file generation** | ✅ | ✅ | MTP TrxReport extension |
| **TRX file collection** | ✅ | 🔄 | `adb shell run-as` / xcrun simctl |
| **Logcat collection** | ✅ | N/A | `adb logcat -d` saved to TestResults |
| Android Instrumentation mode | ✅ | N/A | `adb instrument -w` |

## What's Missing ❌

| Feature | Status | Blocker |
|---------|--------|---------|
| `--device` CLI argument | ❌ | Needs SDK change to `dotnet test` |
| `--project` CLI argument | ❌ | Needs SDK change to `dotnet test` |
| `--list-devices` argument | ❌ | Needs SDK change (already in `dotnet run`) |
| .NET 11 iOS support | ❌ | Missing `Microsoft.NET.Runtime.MonoTargets.Sdk` for .NET 11 |

## Architecture

### MSBuild Integration

Device testing targets are designed to be split across SDKs:

1. **Platform-specific targets** (Android/iOS) - will live in the respective SDK repos (dotnet/android, dotnet/maui)
2. **Common MTP targets** - remain in `Microsoft.Testing.Platform.MSBuild` package

Currently for development:
- Android targets: `samples/public/BlankAndroid/Sdk.DeviceTesting.Android.targets`
- iOS targets: `samples/public/BlankIOS/Sdk.DeviceTesting.iOS.targets`

### MSBuild Properties

| Property | Description | Default |
|----------|-------------|---------|
| `DeviceId` | Device/emulator ID | `$(DEVICE_ID)` env var |
| `DotnetDevicePath` | Path to .NET 11+ SDK with device support | `$(DOTNET_HOST_PATH)` or `dotnet` |
| `UseInstrumentation` | Use Android Instrumentation mode (Android only) | `false` |
| `AndroidInstrumentationName` | Instrumentation class name (Android only) | `$(RootNamespace.ToLower()).TestInstrumentation` |

### iOS-Specific Notes

iOS requires the following test arguments to avoid `PlatformNotSupportedException`:

```csharp
// In AppDelegate.cs
var args = new[]
{
    "--results-directory", testResultsDir,
    "--report-trx",
    "--no-ansi",      // Required: Console.BufferWidth not supported on iOS
    "--no-progress"   // Required: Avoids terminal progress rendering
};
```

## Key Files

### Android Device Testing (→ dotnet/android SDK)

| File | Purpose | Future Location |
|------|---------|-----------------|
| `samples/public/BlankAndroid/Sdk.DeviceTesting.Android.targets` | All Android device testing MSBuild logic | `dotnet/android` SDK |

### iOS Device Testing (→ dotnet/maui or dotnet/ios SDK)

| File | Purpose | Future Location |
|------|---------|-----------------|
| `samples/public/BlankIOS/Sdk.DeviceTesting.iOS.targets` | All iOS device testing MSBuild logic | `dotnet/maui` or `dotnet/ios` SDK |

### Sample Projects

#### samples/public/BlankAndroid/

| File | Purpose |
|------|---------|
| `BlankAndroid.csproj` | Simple test project with `IsTestProject=true` |
| `Sdk.DeviceTesting.Android.targets` | Android device testing targets |
| `MainActivity.cs` | Activity mode entry point |
| `TestInstrumentation.cs` | Instrumentation mode entry point |
| `DeviceTestReporter.cs` | MTP extensions for logcat output |
| `DeviceTests.cs` | Sample MSTest tests |

#### samples/public/BlankIOS/

| File | Purpose |
|------|---------|
| `BlankIOS.csproj` | Simple test project with `IsTestProject=true` |
| `Sdk.DeviceTesting.iOS.targets` | iOS device testing targets |
| `AppDelegate.cs` | iOS entry point that runs tests |
| `DeviceTestReporter.cs` | MTP extensions for console output |
| `DeviceTests.cs` | Sample MSTest tests |

## Usage

### Android

#### Activity Mode (Default)
```bash
dotnet test BlankAndroid.csproj -f net10.0-android \
  -p:DeviceId=emulator-5554 \
  -p:DotnetDevicePath=/path/to/dotnet11
```

#### Instrumentation Mode
```bash
dotnet test BlankAndroid.csproj -f net10.0-android \
  -p:DeviceId=emulator-5554 \
  -p:DotnetDevicePath=/path/to/dotnet11 \
  -p:UseInstrumentation=true
```

### iOS

```bash
dotnet test BlankIOS.csproj -f net10.0-ios \
  -p:DeviceId="iPhone Xs" \
  -p:DotnetDevicePath=/path/to/dotnet11
```

### Goal (CLI arguments - requires SDK changes)
```bash
dotnet test --project MyTests.csproj -f net10.0-android --device emulator-5554
dotnet test --project MyTests.csproj -f net10.0-ios --device "iPhone 17 Pro"
```

## Path to Success

### ✅ Phase 1: COMPLETE - Working Prototype

- [x] MSBuild targets for Android device testing
- [x] MSBuild targets for iOS device testing
- [x] Auto-detection of device TFMs (android/ios)
- [x] Activity/AppDelegate mode via `dotnet run --device`
- [x] Android Instrumentation mode via `adb instrument`
- [x] MTP test execution on device
- [x] Test result reporting via logcat/console
- [x] TRX file generation with `--report-trx`
- [x] TRX file collection from Android device
- [x] Logcat collection for debugging (Android)

### 🔄 Phase 2: IN PROGRESS - CLI Parity with `dotnet run`

**Required:** Add `--device` and `--project` flags to `dotnet test` CLI

```bash
# dotnet run (works today in .NET 11):
dotnet run --project X.csproj -f net10.0-android --device emulator-5554

# dotnet test (goal):
dotnet test --project X.csproj -f net10.0-android --device emulator-5554
```

### 📋 Phase 3: Future Enhancements

- [ ] `--list-devices` support (already in `dotnet run`)
- [ ] TRX collection from iOS simulator/device
- [ ] Code coverage collection from device
- [ ] MacCatalyst support

## References

- [MAUI Device Testing Spec](https://github.com/dotnet/maui/pull/33117)
- [Microsoft.Testing.Platform](https://aka.ms/mtp-overview)
- [dotnet run --device (.NET 11)](https://github.com/dotnet/sdk)
- [Android Instrumentation](https://developer.android.com/reference/android/app/Instrumentation)

---
**Last Updated:** 2026-01-14  
**Status:** ✅ Working prototype with Android and iOS device testing
