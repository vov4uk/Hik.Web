| What | Badge|
| ---- | ---- |
| Latest | [![GitHub (pre-)release](https://img.shields.io/github/v/release/vov4uk/HikConsole?include_prereleases)](https://github.com/vov4uk/HikConsole/releases)|
| Total| [![Github Releases](https://img.shields.io/github/downloads/vov4uk/HikConsole/total)](https://github.com/vov4uk/HikConsole/releases)|

# HikWeb - key features
* Download photo/video files from Hikvision & Dahua IP cameras.
* Download files from FTP/Yi camera
* Migrate files from one folder to another. Move and rename.
* Remove old files if low of space.
* Store job results to SQLite DB.
* Running as Windows Service
* Web UI to view status and statistics.
* Simple search and video playback (h264 supported)
* Job execution based on cron string.
* Logging options :
  * SEQ
  * Text file
  * Console

# Implemented Job Types
* PhotoDownloaderJob
  * Only HikVision IP cameras supported
* VideoDownloaderJob
  * HikVision IP Camera
  * Dahua IP Camera
  * FTP Server (YI Home camera with [custom firmware](https://github.com/TheCrypt0/yi-hack-v4))
* FilesCollectorJob
  * Move files from one folder to another
* GarbageCollectorJob
  * Remove files older than n days
  * Remove oldest files if less than n% of free space
________________________________________________________________________

* Download Hik.Web_xxx.zip [latets releases](https://github.com/vov4uk/HikConsole/releases/latest)
* Unzip Hik.Web_xxx.zip to Hik.Web folder
* Start from command line
```
cd Hik.Web
Hik.Web.exe --console
```

* start as windows server -> [click here](https://github.com/vov4uk/HikConsole/blob/master/src/Hik.Web/Build/install.bat)

![Alt text](HikConsole.png?raw=true "Output example")
