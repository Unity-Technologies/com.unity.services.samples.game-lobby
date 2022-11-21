# 1.1 (2022-11-18)
## Highlights:
This updates the sample to support the latest Lobby-Wire integration, which introduces a websocket callback workflow for listening to Lobby changes.
This update also addresses multiple rounds of user feedback from discord, forums and github, where we heard that the sample had too much uneccesary infrastructure for it's size.
As such we've taken steps to simplify it.

### **Lobby with Wire Integration**
 Switch from polling model to a websocket-callback architecture for listening to Lobby changes.
### **Sample Simplification Refactor**
We heard feedback from users that there simply was too much in this sample. We took the opportunity that came with the incoming Wire changes to Lobby to take a hard look at the rest of the project and replace the generic infrastructure with much simpler coupled code.
### **Tasks over Callbacks** 
We refactored to a largerly Task-based workflow, as it makes reading asynchronous service code much easier.

## Features:
* **Observer** class removed, replaced with **CallbackValue<T>** which is responsible for notifying UI when a value has been changed, and hooks neatly into the Lobby API callback workflow.
* **Messenger and Locator** patterns retired. Replaced with the more tightly coupled, but more readable **Unity Singleton Pattern** on the GameManager and InGameRunner
* **LobbySynchronizer** retired, we now subscribe to Lobby change callbacks in **LobbyManager.BindLocalLobbyToRemote** 
* **RelayUTP** classes retired, the lobby change callbacks are so snappy we no longer need to use the Relay and Transport combo to create a decent Lobby experience.


