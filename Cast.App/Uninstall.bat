@ECHO OFF

ECHO -------------------------------------------
ECHO ---    HomeKast Service Uninstaller     ---
ECHO -------------------------------------------


ECHO Attempting to delete service: HomeKast
sc.exe delete HomeKast

PAUSE