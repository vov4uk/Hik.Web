# HikConsole
Download video files from Hikvision IP cameras, only x64 platform supported.

App works in two modes:
* "Fire-and-forget" - one time execution, just download videos from processing period, and exit 
* "Recurring" - regularly check for new videos, and download them, until 'q' kye pressed

## configuration.json example
  * "IPAddress": "127.0.0.1" - _IP address of camera._
  * "PortNumber": 8000 - _Camera port, by default 8000._
  * "UserName": "user" - _Credentials to camera._
  * "Password": "pass" - _Credentails to camera._
  * "ProcessingPeriodHours": 6 - _App looking for videos for last N hours._
  * "DestinationFolder": "D:\\Video" - _Folder for saving video files._
  * "Mode": "Recurring" - _Avaliable values : Fire-and-forget and Recurring. _
  * "IntervalMinutes": 120 - _For Recurring mode, how often check for updates._

![Alt text](HikConsole.jpg?raw=true "Output example")
