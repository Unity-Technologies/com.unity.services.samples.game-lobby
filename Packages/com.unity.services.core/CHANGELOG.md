# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.1.0-pre.2] - 2021-06-14
### Added
- `IProjectConfiguration` component to access services settings at runtime.
- `IConfigurationProvider` to provide configuration values that need to be available at runtime.
- `InitializationOptions` to enable services initialization customization through code.
  `UnityServices` API has been changed accordingly.

### Changed
- Moves all class meant for internal use from _Unity.Service.Core_ to _Unity.Service.Core.Internal_
- Make `AsyncOperation` and related classes internal

## [1.1.0-pre.1] - 2021-05-31
### Changed
- BREAKING CHANGES:
  - `IInitializablePackage.Initialize` now returns a `System.Threading.Tasks.Task` instead of `IAsyncOperation`
  - `UnityServices.Initialize` now returns a `System.Threading.Tasks.Task` instead of `IAsyncOperation`

### Removed
- Removed Moq dependency.

## [0.3.0-preview] - 2021-05-04
### Added
- Installation Identifier component.
- Service Activation popup.

### Changed 
- Review of the Editor API to rename the following:
  - `OperateService` to `EditorGameService`
    - Members: 
      - `ServiceName` to `Name`
      - `OperateServiceEnabler` to `Enabler`
  - `IServiceIdentifier` to `IEditorGameServiceIdentifier`
  - `OperateDashboardHelper` to `EditorGameServiceDashboardHelper`
  - `ServiceFlagEnabler` to `EditorGameServiceFlagEnabler`
    - Members: 
      - `ServiceFlagName` to `FlagName`
  - `IOperateServiceEnabler` to `IEditorGameServiceEnabler`
  - `OperateServiceRegistry` to `EditorGameServiceRegistry`
    - Methods:
       - `GetService` to `GetEditorGameService`
  - `IOperateServiceRegistry` to `IEditorGameServiceRegistry`
    - Methods: 
      - `GetService` to `GetEditorGameService`
  - `OperateRemoteConfiguration` to `EditorGameServiceRemoteConfiguration`
  - `OperateServiceSettingsProvider` to `EditorGameServiceSettingsProvider`
    - Members: 
      - `OperateService` to `EditorGameService`
  - `OperateSettingsCommonHeaderUiHelper` to `SettingsCommonHeaderUiHelper`
  - GlobalDefine: 
    - `ENABLE_OPERATE_SERVICES` to `ENABLE_EDITOR_GAME_SERVICES`
    

## [0.2.0-preview] - 2021-04-14
### Added
- DevEx integration into the editor.

### Changed
- `IAsyncOperation` to extend `IEnumerator` so they can be yielded in routines.

### Removed
- Removed all API under the `Unity.Services.Core.Networking` namespace because it wasn't ready for use yet.

## [0.1.0-preview] - 2021-03-12

### This is the first release of *com.unity.services.core*.
