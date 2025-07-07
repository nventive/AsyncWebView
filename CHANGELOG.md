# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## 0.7.3 (2025-07-07)
- Fix the navigate to blank page replacing error state by ready state.

## 0.7.2 (2025-06-20)
- Fix infinite loading when setting a source that is the same Uri with a new anchor.

## 0.7.1 (2025-06-19)
- Fix blank page after cancelling navigation via `OnNavigationStarting`.

## 0.7.0 (2025-06-18)
- Target .NET 8.
- Added overridable method `OnNewWindowRequested` to support cancelling external navigations.
- Added overridable method `OnHistoryChanged` to support observing local navigations.

## 0.6.0 (2023-01-22)
### Added
* Support for .NET 7

### Changed
* Updated Uno.WinUI to 5.0.19

### Removed
* Dropped support for Xamarin and UWP
* Dropped support for NetStandard2.0
- Removed Xamarin samples 

## 0.5.0 (2023-05-24)

### Added
* Support for NET 6
* Support for WebView2 in AsyncWebView.Uno.WinUI with Uno.WinUI version 4.9.0-dev.1113

## 0.4.0

### Added
* Build with VS2022
* Add support for uap10.0.19041
* Add support for Android 12
* Support for Android 11 (March, 2021)

### Changed
* [#23] Change target Uno.UI version to 4.0.7.

### Removed
* Dropped support for uap10.0.18362
* Dropped support for MonoAndroid10 target