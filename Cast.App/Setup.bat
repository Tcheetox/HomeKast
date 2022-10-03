@ECHO OFF

ECHO -------------------------------------------
ECHO ---    HomeKast Service Installer       ---
ECHO -------------------------------------------

ECHO Administrative permissions required
ECHO:

cd /d %~dp0
SET binPath=%cd%\Cast.App.exe

net session >nul 2>&1
if %errorLevel% == 0 (
    ECHO Creating new service: HomeKast
    ECHO Binary path: %binPath% 
    ECHO:

    sc create HomeKast binpath= "%binPath%" type= own start= delayed-auto displayname= HomeKast
    sc.exe description HomeKast "Locally hosted website to support Chromecast media conversion and streaming"

    ECHO Attempting auto-start in 3 seconds...

    timeout /t 3 /nobreak
    sc.exe start HomeKast

    ECHO:
    ECHO Process: Upon successfull termination, you can browse HomeKast at http://localhost:8081. Enjoy!

) else (
    ECHO Failure: Current permissions inadequate!
)
    
PAUSE