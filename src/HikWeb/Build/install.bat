﻿sc create HikWeb binPath= %~dp0HikWeb.exe
sc failure HikWeb actions= restart/60000/restart/60000/""/60000 reset= 86400
sc start HikWeb
sc config HikWeb start=auto