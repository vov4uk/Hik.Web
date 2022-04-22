| What | Badge|
| ---- | ---- |
| Build | [![Build Status](https://dev.azure.com/khmelovskyi/HikConsole/_apis/build/status/vov4uk.HikConsole?branchName=master)](https://dev.azure.com/khmelovskyi/HikConsole/_build/latest?definitionId=1&branchName=master)|
| Sonar | [![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=vov4uk_HikConsole&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=vov4uk_HikConsole)|

# Hik.Client
* Wrapper over Hik.Api.
* HikPhotoClient - Download photo files from Hikvision IP cameras.
* HikVideoClient - Download video files from Hikvision IP cameras.
* YiClient - Download files from Xiaomi YI cameras. Connetct YI camera over FTP, and download files.

# HikWeb
* Download photo/video files from Hikvision IP cameras.
* Download files from FTP
* Migrate files from one folder to another. Move and rename.
* Remove old files if low of space.
* Store job results to SQLite DB.
* Running as Windows Service
* Simple Web UI to view status and statistics.
* Job execution based on cron string.

![Alt text](HikConsole.png?raw=true "Output example")
