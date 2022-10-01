@ECHO OFF

ECHO -------------------------------------------
ECHO ---     HomeKast Service Installer      ---
ECHO -------------------------------------------

Set binPath=%cd%\bin\Debug\net6.0\Cast.App.exe

ECHO Creating new service: HomeKast
ECHO Binary path: %binPath% 

sc.exe create HomeKast binpath= %binPath% type= own start= delayed-auto displayname= HomeKast
sc.exe description HomeKast "Locally hosted website to support Chromecast media conversion and streaming"
sc.exe start HomeKast

PAUSE