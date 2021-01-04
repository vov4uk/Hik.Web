sc create Hik.Web binPath = D:\Video\v4\Hik.Web.exe
sc failure Hik.Web actions= restart/60000/restart/60000/""/60000 reset= 86400
sc start Hik.Web
sc config Hik.Web start=auto