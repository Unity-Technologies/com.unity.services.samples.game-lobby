# Lobby Rooms

  
**WIP -  6/17/2021 -  This project is being shared for internal review, currently there are known issues are around how many requests we post, which is being worked on. We believe this can still serve as a good example of integrating Rooms+Relay together for**

**If you use this for hackweek, please give us feedback @jacob.lorentzen or @nathaniel.buck**

A Unity Project Sample showing how to integrate Rooms and Relay into a typical Lobby experience use case.

Features Covered:
- Lobby Creation
- Lobby Query
- Lobby Data Sync
  - Emotes
  - Player Names
  - Player Ready Check State  
- Lobby Joining
- Relay Service Creation
- Relay Code Generation
- Relay Service Joining

# Service Setup
**Create an organization.**

- Follow the attached guide to set up your cloud organization:
[Organization Tutorial](https://support.unity.com/hc/en-us/articles/208592876-How-do-I-create-a-new-Organization-)

- In the project, navigate to **Edit => Project Settings => Services**
	![Services Editor](~Documentation/Images/services1.PNG?raw=false "Service in Editor" )


- Select your organization from the drop-down, and push **Create Project ID**
	![Services Editor Complete](~Documentation/Images/services2.PNG?raw=false "Service in Editor set up.")

# Rooms:
**COMING SOON, ROOMS API URL**


# Relay Setup:
**COMING SOON, RELAY API URL**

- Navigate to https://dashboard.unity3d.com/landing

- Select Relay from the drop-down list

![Relay](~Documentation/Images/dashboard1.PNG?raw=true "Relay location.")


- Select your project

![Project Select](~Documentation/Images/dashboard2.PNG?raw=true "Project Select")


- In the bottom left, select "Get Started"

![Get Started Location is Bottom Right](~Documentation/Images/dashboard3.PNG?raw=true "Bottom right for Getting Started")


- Follow the steps until you hit "Relay On"
(For this project, you can skip downloading the Transport)

![Relay: On!](~Documentation/Images/dashboard4.PNG?raw=true "Hit Relay On")



# Solo Testing

**1. Press Start to enter the Lobby Menu.**

![Lobby Menu](~Documentation/Images/mainMenu1.PNG?raw=true "Lobby Menu")


**2. The Join menu Queries the rooms service for available Lobbies, there wont be any right now.**

![Join Menu](~Documentation/Images/joinMenu2.PNG?raw=true "Join Menu")


**3. The Create Menu Lets you make a new Lobby**

![Create Menu](~Documentation/Images/createMenu3.PNG?raw=true "Create Menu")


**4. Enter a Lobby Name of your preference and go!**

![Create Menu Name](~Documentation/Images/createMenuName4.PNG?raw=true "Create Menu Name")


**5. This is the Lobby, It has a room code for you to share with your friends to allow them to join.**
For demonstration purposes we also show the Relay Code, which will be passed to all users in the Lobby

![Lobby View](~Documentation/Images/lobbyView5.PNG?raw=true "Lobby View")


**6. TheLobby holds up to 4 players and will pass the Relay code once all the players are ready.**

![Relay Ready!](~Documentation/Images/lobbyViewIP6.PNG?raw=true "Create Menu Name")
