[![Build Status](https://dev.azure.com/khmelovskyi/HikConsole/_apis/build/status/vov4uk.HikConsole?branchName=master)](https://dev.azure.com/khmelovskyi/HikConsole/_build/latest?definitionId=1&branchName=master)

# HikConsole
* Download photo/video files from Hikvision IP cameras.
# HikConsole.DataBaseSaver
* Download photo/video files from Hikvision IP cameras. 
* Save results to SQLite DB.
# HikWeb
* Download photo/video files from Hikvision IP cameras. 
* Save results to SQLite DB.
* Running as Windows Service
* Simple Web UI to view results.
* Job execution based on cron string.
* Deleting old files.

App works in two modes:
* "Fire-and-forget" - one time execution, just download videos from processing period, and exit 
* "Recurring" - regularly check for new videos, and download them, until 'q' kye pressed

## configuration.json example
  * "ProcessingPeriodHours": 6 - _App looking for videos for last N hours._
  * "Mode": "Recurring" - _Fire-and-forget OR Recurring._
  * "IntervalMinutes": 120 - _For Recurring mode, how often check for updates._
  _____
  * "Cameras"
     * "Alias": "Camera1" - _User friendly name_
    * "IPAddress": "192.168.1.64" - _IP address of camera._
    * "PortNumber": 8000 - _Camera port, by default 8000._
    * "UserName": "user" - _Credentials to camera._
    * "Password": "pass" - _Credentails to camera._
    * "DestinationFolder": "D:\\Video" - _Folder for saving video files._
    * "ShowProgress": false - _Show progress bar._
    * "DownloadPhotos": false - _Download photos  from camera._
  ______
  * "EmailSettings": 
    * "UserName": "user@gmail.com",
    * "Password": "pass",
    * "Server": "smtp.gmail.com",
    * "Port": 587,
    * "Receiver": "receiver@gmail.com"

![Alt text](HikConsole.png?raw=true "Output example")
