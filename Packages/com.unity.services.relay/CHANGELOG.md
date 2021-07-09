# Changelog

## [0.0.1-preview.9] - 2021-07-07

* Update documentation, points to dashboard

## [0.0.1-preview.8] - 2021-07-07

* Update documentation folder

## [0.0.1-preview.7] - 2021-07-05

* Improve README

## [0.0.1-preview.6] - 2021-06-23

* Update `JoinAllocation` response object to include `404s`.

## [0.0.1-preview.5] - 2021-06-17

* Use generator version `v0.2.0`
    * uses `com.unity.services.core` `v1.1.0`
    * allows compatibility with `com.unity.services.authentication` `v0.5.0`
* Sample project updated to use `com.unity.services.authentication` `v0.5.0`
* No breaking changes on Relay SDK

## [0.0.1-preview.4] - 2021-06-16

* Use generator version `v0.1.0`
    * Fixed obsolete warning in Unity 2020 with `HttpClientResponse` by making use
    of `UnityWebRequest`
    * BREAKING CHANGE: use `RelayService.Configuration.BasePath` instead of `Configuration.BasePath`
* Update to closed beta version of the allocations HTTP API
    * Adds `AllocationIdBytes` to `JoinAllocation` and `Allocation` response objects
    * Adds `ServerEndpoints` to `JoinAllocation` and `Allocation` response objects
    * `RelayServer` (used in `JoinAllocation` and `Allocation` response objects) is now deprecated. Instead `ServerEndpoints` should be used.


## [0.0.1-preview.3] - 2021-05-12

* Update to newer allocations API, error type changed.

## [0.0.1-preview.2] - 2021-04-30

* Use generator version `v0.0.25-preview`
* Obtain Access Token from Core SDK
* Use production gateway URL by default
* BREAKING CHANGE: wraps API response body in a `Response<T>` object
* Cleanup markdown documents
* Inline documentation

## [0.0.1-preview.1] - 2021-04-23

* Removed `Samples~` folder.
* No functional changes.

## [0.0.1-preview] - 2021-04-19

First internal release
