# Welcome

Welcome to the Unity Transport repository!

The new Unity Transport Package which will replace the UNet low-level API.
The preview of the transport package supports establishing connections and sending messages to a
remote host. It also contains utilities for serializing data streams to send
over the network.

## Transport CI summary
[![](https://badge-proxy.cds.internal.unity3d.com/c59df3b8-7f64-4158-9ef7-4c99748185cb)](https://badges.cds.internal.unity3d.com/packages/com.unity.transport/build-info?branch=master) [![](https://badge-proxy.cds.internal.unity3d.com/65a2af76-0337-4ec3-a20c-5f9a09ed62eb)](https://badges.cds.internal.unity3d.com/packages/com.unity.transport/dependencies-info?branch=master) [![](https://badge-proxy.cds.internal.unity3d.com/5cd5fb42-a61f-4502-b75a-b8d80deb41f2)](https://badges.cds.internal.unity3d.com/packages/com.unity.transport/dependants-info) [![](https://badge-proxy.cds.internal.unity3d.com/cad278d5-2dba-4434-aac2-1466a4bd2ea6)](https://badges.cds.internal.unity3d.com/packages/com.unity.transport/warnings-info?branch=master) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/f2096d78-45e6-4402-978b-0058b1e3277c) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/fb5e4d88-0b2f-4883-ad0d-1b69b33e7861)

## Documentation

For more information about the Transport package, please see the [Unity Transport Documentation](https://docs-multiplayer.unity3d.com/transport/introduction). The site includes guides, API reference, and release notes.

A [changelog](CHANGELOG.md) is also available in the package.

## Connect

See the [Multiplayer forum](https://forum.unity.com/forums/multiplayer.26/) to ask questions and connect with Transport.

# Samples

All samples are in */TransportSamples~*.

## Ping
The ping sample is a good starting point for learning about all the parts included
in the transport package. The ping client establishes a connection to the ping server,
sends a ping message and receives a pong reply. Once pong is received the client
will disconnect.

It is a simple example showing you how to use the new Unity Transport Package.
Ping consists of multiple scenes, all found in `sampleproject/Assets/Scenes/`.

- `PingMainThread.unity` - A main-thread only implementation of ping.
- `Ping.unity` - A fully jobified version of the ping client and server.
- `PingClient.unity` - The same jobified client code as `Ping.unity`, but without the server.
- `PingServer.unity` - The dedicated server version of the jobified ping. A headless (or Server Build in 2019.1) Linux 64 bit build of this scene is what should be deployed to Multiplay.
- `PingECS.unity` - An ECS version of the jobified ping sample.

## Soaker
A stress test which will create a set number of clients and a server in the same process. Each client will send messages at the specified rate with the specified size and measure statistics.

## Pipeline
An example of the pipelines feature that offers layers of functionality on top of the default socket implementation behaviour.
