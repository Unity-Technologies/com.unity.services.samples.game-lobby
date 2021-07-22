
### Closed Beta - 7/14/21
Lobby and Relay are **only** available in closed beta at the moment. To use these services, you will need to have signed up here for the services to show in your organization: https://create.unity3d.com/relay-lobby-beta-signup

# Game Lobby Sample
## *Unity 2021.2 0b1*

This is a Unity project sample showing how to integrate Lobby and Relay into a typical game lobby experience.

	Features Covered:
	- Lobby Creation
	- Lobby Query
	- Lobby Data Sync
	  - Emotes
	  - Player Names
	  - Player Ready Check State  
	- Lobby Join
	- Relay Server Creation
	- Relay Code Generation
	- Relay Server Join

## Service Organization Setup
**Create an organization**

Follow the guide to set up your cloud organization:

[Organization Tutorial](https://support.unity.com/hc/en-us/articles/208592876-How-do-I-create-a-new-Organization-)

Then, in the Unity Editor, open Services > General Settings to create a cloud project ID (or link to an existing one) to associate the Unity project with your organization.


## Lobby & Relay

We use the Lobby service to create a space that our users can join and share data through.

[Lobby Overview](http://documentation.cloud.unity3d.com/en/articles/5371715-unity-lobby-service)

[Lobby Dashboard](https://dashboard.unity3d.com/lobby)



We use the Relay service to obfuscate the hosts' IP, while still allowing them to locally host strangers.

[Relay Overview](http://documentation.cloud.unity3d.com/en/articles/5371723-relay-overview)

[Relay Dashboard]( https://dashboard.unity3d.com/relay)


### Setup 
For either one, select "About & Support => Get Started."

**Closed Beta Only**

	Follow the steps, downloading your packaged folders to the Sample Project Package\Packages

	*If you open the project and you get the "Enter Safe Mode" dialogue, it means you are missing your packages.*

	*If you still cannot find the package namespaces, ensure the Assets/Scripts/LobbyRelaySample.asmdef is referencing the packages.*

Follow the steps until you reach "Lobby/Relay On."


## Solo Testing

Create a build of the project in the OS of your choice.
The Authentication service creates a unique ID for builds, so you may run a build and the Editor at the same time to represent two users.

1. Enter Play mode, and select Start to open the lobby list. This queries the Lobby service for available lobbies, but there are currently none.

![Join Menu](~Documentation/Images/tutorial_1_lobbyList.png?raw=true "Join Menu")

2. The Create menu lets you host a new lobby.

![Create Menu](~Documentation/Images/tutorial_2_createMenu.png?raw=true)

3. This is the lobby. It has a shareable lobby code to allow other users to join directly.
For demonstration purposes, we also show the Relay code, which will be passed to all users in the lobby.

![Lobby View](~Documentation/Images/tutorial_3_HostGame.png?raw=true)


4. Run your build, and as this second user, you should now see your lobby in the list. 

![Populated Join View](~Documentation/Images/tutorial_4_newLobby.png?raw=true)


5. The lobby holds up to 4 users and will pass the Relay code once all the users are ready. Changes to a user's name or emote will appear for other users after a couple seconds.

![Relay Ready!](~Documentation/Images/tutorial_5_editorCow.png?raw=true)


6. Once the lobby host has received a ready signal from all users, it will send out a countdown, and all users will enter a simultaneous countdown before connecting to Relay.

![Countdown!](~Documentation/Images/tutorial_6_countDown.png?raw=true)


7. An anonymous IP from the Relay service is passed to all users in the lobby, at which point your game logic could connect them to a server and begin transmitting realtime data.

![InGame!](~Documentation/Images/tutorial_7_ingame.png?raw=true)
