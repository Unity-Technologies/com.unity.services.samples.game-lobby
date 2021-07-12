# Unity Transport Design Rules

## All features are optional
Unity transport is conceptually a thin layer on UDP adding a connection concept. All additional features on top of UDP + connection are optional, when not used they have zero performance or complexity overhead. If possible features are implemented as pipeline stages.

Features that have a limited audience are implemented outside the package - either in game code or other packages.

## Full control over processing time and when packets are sent/received
UTP is optimized for making games. It can be used without creating any additional threads - only using the JobSystem. The layer on top has full control over when the transport schedules jobs. The layer on top also has full control over when packets are sent on the wire. There are no internal buffers delaying messages (except possibly in pipelines).

There is generally no need to continuously poll for messages since incoming data needs to be read right before simulation starts, and we cannot start using new data in the middle of the simulation

## Written in HPC#
All code is jobified and burst compiled, there is no garbage collection. The transport does not spend any processing time outside setup on the main thread, and it allows the layer on top to not sync on the main thread.

## Follows the DOTS principles, is usable in DOTS Runtime and always compatible with the latest versions of the DOTS packages
There should always be a version compatible with the latest verions of the DOTS dependencies such as Unity Collections.

## The protocol is well defined and documented
Other implementations can communicate with games written with Unity Transport, without reverse engineering or reading the transport source code