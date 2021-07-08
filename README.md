
### Closed Beta - 7/14/21
Lobby and Relay are **only** available in closed beta at the moment, to use these services you will need to have signed up here for the services to show in your Organization: https://create.unity3d.com/relay-lobby-beta-signup

# Game Lobby Sample
## *Unity 2021.2 0b1*

This is a Unity Project Sample showing how to integrate Lobby and Relay into a typical Game Lobby experience.

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
** Create an organization**

Follow the attached guide to set up your cloud organization:

[Organization Tutorial](https://support.unity.com/hc/en-us/articles/208592876-How-do-I-create-a-new-Organization-)



## Lobby & Relay

We use the lobby service to create a space that our users can join and share data through.

[Lobby Overview](http://documentation.cloud.unity3d.com/en/articles/5371715-unity-lobby-service)

Lobby Dashboard: https://dashboard.unity3d.com/lobby

We use the Relay service to obfuscate the Hosts' IP, while still allowing them to locally host strangers.

[Relay Overview](http://documentation.cloud.unity3d.com/en/articles/5371723-relay-overview)

Relay Dashboard: https://dashboard.unity3d.com/relay


### Setup 
For either one, select "Get Started"

** Closed Beta Only **

Follow the steps, downloading your packaged folders to the Sample Project Pac.

Follow the steps until you hit "Lobby/Relay On"


## Solo Testing

Create a new Unity Build of the project in the OS of your choice.
Because the Authentication service creates a unique ID for builds, you will need to host a lobby in Build and join in Editor or vice versa.

1. Start the game, and hit start to enter the Room List. This Queries the rooms service for available Lobbies, there wont be any right now.

![Join Menu](~Documentation/Images/tutorial_1_lobbyList.png?raw=true "Join Menu")

2. The Create Menu Lets you make a new Lobby.

![Create Menu](~Documentation/Images/tutorial_2_createMenu.png?raw=true)

3. This is the Lobby, It has a Room code for you to share with your friends to allow them to join.
For demonstration purposes we also show the Relay Code, which will be passed to all users in the Lobby.

![Lobby View](~Documentation/Images/tutorial_3_HostGame.png?raw=true)


4. Open the second game instance in Editor or in Build, you should now see your Lobby in the list.

![Populated Join View](~Documentation/Images/tutorial_4_newLobby.png?raw=true)


5. The Lobby holds up to 4 players and will pass the Relay code once all the players are ready.

![Relay Ready!](~Documentation/Images/tutorial_5_editorCow.png?raw=true)


6. The countdown will start after the rooms data synch has completed. (It is a little slow due to our refresh rate being low at the moment)

![Countdown!](~Documentation/Images/tutorial_6_countDown.png?raw=true)


7. The relay service IP gets passed to all users in the lobby, and this is where you would connect to a server, if you had one.

![InGame!](~Documentation/Images/tutorial_7_ingame.png?raw=true)
