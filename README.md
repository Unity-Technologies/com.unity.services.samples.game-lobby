# Lobby Rooms
  **com.unity.services.samples.lobby-rooms**

A Unity Project Sample showing how to integrate Rooms and Relay into a typical Lobby experience use case.

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

# Service Setup
** Create an organization.

	- Follow the attached guide to set up your cloud organization:
	[url=https://support.unity.com/hc/en-us/articles/208592876-How-do-I-create-a-new-Organization-]Organization Tutorial[/url]
	
	- In the project, navigate to **Edit => Project Settings => Services**
		![Services Editor](Documentation/Images/services1.PNG?raw=true "Service in Editor")
	
	
	- Select your organization from the drop-down, and push **Create Project ID**
		![Services Editor Complete](Documentation/Images/services2.PNG?raw=true "Service in Editor set up.")

# Lobby:
	**[url=http://documentation.cloud.unity3d.com/en/articles/5371715-unity-lobby-service]Lobby Overview[/url] **


# Relay Setup:
	**[url=http://documentation.cloud.unity3d.com/en/articles/5371723-relay-overview]Relay Overview[/url] **
	
	- Navigate to https://dashboard.unity3d.com/landing

	- Select Relay from the drop-down list
		![Relay](Documentation/Images/dashboard1_beta.PNG?raw=true "Relay location.")

	- Select your project
	
	- In the bottom left, select "Get Started"
	
	- Follow the steps until you hit "Relay On"
		(For this project, you can skip downloading the Transport)
		

# Solo Testing

Create a new Unity Build of the project in the OS of your choice.
Because the Authentication service creates a unique ID for builds, you will need to host a lobby in Build and join in Editor or vice versa.

**1. Start the game, and hit start to enter the Room List, It Queries the rooms service for available Lobbies, there wont be any right now.

![Join Menu](Documentation/Images/tutorial_1_lobbyList.PNG?raw=true "Join Menu")

**2 The Create Menu Lets you make a new Lobby**

![Create Menu](Documentation/Images/tutorial_2_createMenu.PNG?raw=true "Create Menu")

**3 This is the Lobby, It has a Room code for you to share with your friends to allow them to join.
For demonstration purposes we also show the Relay Code, which will be passed to all users in the Lobby**

![Lobby View](Documentation/Images/tutorial_3_HostGame.PNG?raw=true "Lobby View")


**4 Open the second game instance in Editor or in Build, you should now see your Lobby in the list.

![Populated Join View](Documentation/Images/tutorial_4_newLobby.PNG?raw=true "Populated Join View")


**5 The Lobby holds up to 4 players and will pass the Relay code once all the players are ready.**

![Relay Ready!](Documentation/Images/tutorial_5_editorCow.PNG?raw=true "Create Menu Name")


**6 The countdown will start after the rooms data synch has completed. (It is a little slow due to our refresh rate being low at the moment)**

![Countdown!](Documentation/Images/tutorial_6_countDown.PNG?raw=true "Countdown")


**7 The relay service IP gets passed to all users in the lobby, and this is where you would connect to a server, if you had one.**

![InGame!](Documentation/Images/tutorial_7_ingame.PNG?raw=true "InGame")
