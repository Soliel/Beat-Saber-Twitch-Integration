# Beat Saber Twitch Integration Plugin
For all you streamers out there who want song requests in your chat!

## Installation
1. Download the [latest release](https://github.com/Soliel/Beat-Saber-Twitch-Integration/releases) and extract to the root of your Beat Saber installation directory.
2. Edit the config (located in `Plugins/Config/TwitchIntegration.xml`)  
Add your Twitch username, an OAuth token that you can get here: https://twitchapps.com/tmi **(this does require the `oauth:` prefix)**  
Lastly, add the channel to monitor. (This doesn't have to be the same as your twitch login, ie if you're using a bot to do requests)

## Usage
* Enable the plugin from in-game on the main menu.
* Users can type `!bsr <songname>` in chat, then after you finish a song it will take you to the queue screen to download or start a song.
