# Device Testing Implementation Status

## Goal
Make `dotnet test` work exactly like `dotnet run` for device projects:

```bash
# Current dotnet run (works in .NET 11):
dotnet run --project MyTests.csproj -f net10.0-android --device emulator-5554

# Goal for dotnet test:
dotnet test --project MyTests.csproj -f net10.0-android --device emulator-5554
```

## Current Status: ✅ Working with MSBuild Properties

```bash
# This works TODAY:
dotnet test BlankAndroid.csproj -f net10.0-android \
  -p:DeviceId=emulator-5554 \
  -p:DotnetDevicePath=/path/to/dotnet11

# Output:
# ╔══════════════════════════════════════════════════════════════╗
# ║               DEVICE TESTING (Microsoft.Testing.Platform)    ║
# ╠══════════════════════════════════════════════════════════════╣
# ║  Project:    BlankAndroid
# ║  Framework:  net10.0-android
# ║  Device:     emulator-5554
# ╚══════════════════════════════════════════════════════════════╝
# Deploying and running tests on Android device...
# Collecting test results from Android device...
# Test results: bin/Debug/net10.0-android/TestResults/BlankAndroid.trx
# ══════════════════════════════════════════════════════════════
# Test Results: 3 passed, 0 failed
# ══════════════════════════════════════════════════════════════
```

## What Works ✅

| Feature | Status | Implementation |
|---------|--------|----------------|
| Build device test project | ✅ | Standard MSBuild |
| Deploy to device/emulator | ✅ | Via `dotnet run --device` |
| Execute tests on device | ✅ | Microsoft.Testing.Platform |
| Test results to console | ✅ | Parsed from TRX file |
| **TRX file collection** | ✅ | `adb shell run-as ... cat` |
| **Pass/Fail reporting** | ✅ | TRX-based test counts |
| Exit code propagation | ✅ | Non-zero on failures |

## What's Missing ❌

| Feature | Status | Blocker |
|---------|--------|---------|
| `--device` CLI argument | ❌ | Needs SDK change |
| `--project` CLI argument | ❌ | Needs SDK change |
| `--list-devices` argument | ❌ | Needs SDK change |

## Architecture

```
dotnet test BlankAndroid.csproj -f net10.0-android -p:DeviceId=emulator-5554
    │
    ▼
┌─────────────────────────────────────────────────────────────┐
│  Directory.Build.targets (Common)                           │
│  - Detects device TFM (net10.0-android)                    │
│  - Overrides VSTest target                                  │
│  - Imports platform-specific targets                        │
└─────────────────────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────────────────────┐
│  Android.DeviceTest.targets (Android-specific)             │
│  - _ComputeAndroidTestRunArguments                          │
│  - _RunAndroidTests (dotnet run --device)                  │
│  - _PullAndroidTestResults (adb shell run-as cat)          │
│  - _ReportAndroidTestResults (parse TRX)                   │
└─────────────────────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────────────────────┐
│  dotnet run --device (SDK .NET 11)                         │
│  - Builds APK                                               │
│  - Deploys to device via ADB                               │
│  - Launches app                                             │
└─────────────────────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────────────────────┐
│  App on Device (MainActivity.cs)                           │
│  - Calls MicrosoftTestingPlatformEntryPoint.Main()         │
│  - MTP discovers and runs tests                            │
│  - TRX file generated via --report-trx                     │
│  - Exits with test result code                             │
└─────────────────────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────────────────────┐
│  Output                                                     │
│  - TRX: bin/.../TestResults/BlankAndroid.trx               │
│  - Console: Test Results: 3 passed, 0 failed               │
│  - Exit code: 0 (success) or non-zero (failures)           │
└─────────────────────────────────────────────────────────────┘
```

## Key Files

### samples/public/BlankAndroid/

| File | Purpose |
|------|---------|
| `BlankAndroid.csproj` | Project with MTP + TRX configuration |
| `Directory.Build.targets` | Common device test infrastructure |
| `Android.DeviceTest.targets` | Android-specific targets |
| `MainActivity.cs` | Entry point with `--report-trx` |
| `DeviceTestReporter.cs` | MTP extensions for logcat output |
| `DeviceTests.cs` | Sample MSTest tests |

## Path to Success

### ✅ Phase 1: COMPLETE - Working Prototype
- [x] MSBuild targets intercept `dotnet test` for device projects
- [x] Invoke `dotnet run --device` for deployment and execution
- [x] TRX file collection from device via ADB
- [x] Parse TRX for test results (passed/failed counts)
- [x] Proper exit code based on test results
- [x] **Separated Android-specific targets** (per PR feedback)

### 🔄 Phase 2: IN PROGRESS - CLI Parity with `dotnet run`

**Required:** Add `--device` flag to `dotnet test` CLI

The .NET SDK already supports `--device` for `dotnet run`. We need the same for `dotnet test`:

```bash
# dotnet run (works today):
dotnet run --project X.csproj -f net10.0-android --device emulator-5554

# dotnet test (goal):
dotnet test --project X.csproj -f net10.0-android --device emulator-5554
```

### 📋 Phase 3: Future Enhancements
- [ ] `--list-devices` support (provided by SDK)
- [ ] Code coverage collection from device
- [ ] iOS support (same pattern with iOS.DeviceTest.targets)

## Usage

### Current (with MSBuild properties)
```bash
dotnet test BlankAndroid.csproj -f net10.0-android \
  -p:DeviceId=emulator-5554 \
  -p:DotnetDevicePath=/path/to/dotnet11
```

### With Environment Variables
```bash
export DEVICE_ID=emulator-5554
export DOTNET_DEVICE_PATH=/path/to/dotnet11
dotnet test BlankAndroid.csproj -f net10.0-android
```

### Goal (CLI arguments)
```bash
dotnet test --project BlankAndroid.csproj -f net10.0-android --device emulator-5554
```

## TRX Collection Details

The TRX file is collected using:
1. `adb shell run-as <app-id> ls -t files/TestResults/` - Get latest TRX filename
2. `adb shell run-as <app-id> cat files/TestResults/<file.trx>` - Read file content
3. Save to `bin/Debug/net10.0-android/TestResults/<ProjectName>.trx`
4. Parse TRX to extract `passed` and `failed` counts for reporting

This works because:
- `run-as` allows accessing app's private storage without root
- `cat` outputs file content to stdout which can be redirected locally
- Works with debuggable APKs (debug builds)

## References

- [PR Feedback from @jonathanpeppers](https://github.com/dotnet/sdk/pull/52427#discussion_r2687253131)
- [MAUI Device Testing Spec](https://github.com/dotnet/maui/pull/33117)
- [Microsoft.Testing.Platform](https://aka.ms/mtp-overview)
- [dotnet run --device (.NET 11)](https://github.com/dotnet/sdk)

---
**Last Updated:** 2026-01-13  
**Status:** Working prototype with separated Android targets, TRX collection, and test result parsing
