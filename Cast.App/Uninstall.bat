@ECHO OFF

ECHO -------------------------------------------
ECHO ---    HomeKast Service Uninstaller     ---
ECHO -------------------------------------------


ECHO Administrative permissions required
ECHO:

net session >nul 2>&1
if %errorLevel% == 0 (
	
    ECHO Success: Administrative permissions confirmed
	ECHO Deleting service: HomeKast

	sc.exe stop HomeKast
	ECHO Attempting deletion in 3 seconds...

	timeout /t 3 /nobreak
	sc.exe delete HomeKast
	
	ECHO:
	ECHO Process: completed

) else (
    ECHO Failure: Current permissions inadequate!
)
    
PAUSE