# StreamRC

A bot for streaming services. Provides chat interaction and a html hud for obs integration.

## Dependencies

- [NightlyCode.Core](https://github.com/telmengedar/NightlyCode.Core) for data conversion
- [NightlyCode.DB](https://github.com/telmengedar/NightlyCode.DB) for database storage
- [NightlyCode.Modules](https://github.com/telmengedar/NightlyCode.Modules) for module/plugin management
- [NightlyCode.Japi](https://github.com/telmengedar/japi) for JSON deserialization
- [NightlyCode.Net](https://github.com/telmengedar/NightlyCode.Net) provides http-server for http api and html overlay
- [NightlyCode.Discord](https://github.com/telmengedar/NightlyCode.Discord) for Discord integration (optional)
- [NightlyCode.Twitch](https://github.com/telmengedar/NightlyCode.Twitch) for Twitch integration (kind of optional)

the dependencies might introduce other dependencies which are listed on the related repository sites.

## Setup

To setup connections of the bot several UIs are provided in the *Profile* menu. The settings there depend on the specific connections but should be self explanatory.

## Overlay

Overlays for OBS and other streaming apps are provided as web resources using a http server. The server serves the resources normally under **http://localhost/streamrc/**

## Chat

The chat is read by the bot which connects to the configured profiles. The chat messages are scanned for commands and if no command is found the message is displayed as such.

### Http

Chat messages for the overlay are served under **<root>/chat** (default: **http://localhost/streamrc/chat**)

## Events

Selected events like new followers or donations are registered by the bot and written to the database.
Currently supported events are:
* Hosts
* New Followers
* Subscriptions
* Donations
* Bug Reports
* Raids
* Chat Messages

### Http

Stream events are served under **<root>/events** (default: **http://localhost/streamrc/events**)