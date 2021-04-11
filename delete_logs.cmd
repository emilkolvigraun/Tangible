@ECHO OFF
SET amount=%1

IF [%1] == [] GOTO MISSINGPARAMETER

CALL :DELETEFILES
GOTO EXIT

:DELETEFILES
    FOR /L %%X in (0, 1, %amount%) DO (
        IF %%X==%amount% (
            GOTO EXIT
        )
        del /f "TcpNode%%X.txt"
        ECHO Deleted log "TcpNode%%X.txt"
    )

:MISSINGPARAMETER
    ECHO How many log files shoudl be deleted? INT
    GOTO EXIT

:EXIT