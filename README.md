# Beat Saber Twitch Integration Plugin
For all you streamers out there who want song requests in your chat!

## Installation
1. Download the [latest release](https://github.com/Soliel/Beat-Saber-Twitch-Integration/releases) and extract to the root of your Beat Saber installation directory.
2. Edit the config (located in `Plugins/Config/TwitchIntegration.xml`)  
Add your Twitch username, an OAuth token that you can get here: https://twitchapps.com/tmi **(this does require the `oauth:` prefix)**  
Lastly, add the channel to monitor. (This doesn't have to be the same as your twitch login, ie if you're using a bot to do requests)

## Usage

### Mod Commands

| Command | Description |
| --- | --- |
| !next | Jumps the queue ahead to the next song. |
| !clearall | Clears the queue. |
| !remove | Allows you to remove a song from the queue. Used like !remove \<BeatsaverID\> |
| !block | Allows you to ban a song from ever getting requested again. Used like !block \<BeatsaverID\> |


### Basic Commands

| Command | Description |
| --- | --- |
| !bsr | Allows you to request a song, can be used with song name or Beatsaver ID. | 
| !add | Another version of !bsr but without the song name option. |
| !queue | Shows all songs in queue. |
| !blist | Shows all banned songs. |
| !qhelp | Shows the commands. |

### Config Options

| Option | Description |
| --- | --- |
| Username | The username the bot will post under, this needs to match the name of the account you generated your oauth token under. |
| Oauth | The Oauth token for the bot to login to twitch with. Gained from [here](https://twitchapps.com/tmi). |
| Channel | The twitch channel the bot should listen to, in most cases this will be the same as Username. |
| Moderator Only | Sets whether or not to make requests mod only. Can only be true or false. |
| Subscriber Only | Same as above but for subs too. |
| ViewerRequestLimit | The maximum amount of songs a view can have in queue at any given time. |
| SubscriberLimitOverride | A higher limit than the above for subs to have in the queue at any given time. |
