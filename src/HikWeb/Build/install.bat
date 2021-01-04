sc create HikWeb binPath = D:\Video\v4\HikWeb.exe
sc failure HikWeb actions= restart/60000/restart/60000/""/60000 reset= 86400
sc start HikWeb
sc config HikWeb start=auto