SonarScanner.MSBuild.exe begin /k:"HikConsole" /d:sonar.host.url="http://localhost:9000" /d:sonar.login=%1 /d:sonar.cs.opencover.reportsPaths=".\src\HikConsole.Tests\TestResults\opencover*.xml" /d:sonar.log.level=error

"%LOCALAPPDATA%\Apps\OpenCover\OpenCover.Console.exe" -excludebyattribute:*.ExcludeFromCodeCoverageAttribute -oldStyle -output:".\src\HikConsole.Tests\TestResults\opencover.xml" -register:user -target:"C:\Program Files\dotnet\dotnet.exe" -targetargs:"test" -register:Administrator

SonarScanner.MSBuild.exe end /d:sonar.login=%1