@ECHO OFF
SET stateLog=%1
SET size=%2
SET tofile=%3

IF [%1] == [] GOTO MISSINGPARAMETER
IF [%2] == [] GOTO MISSINGPARAMETER

CALL :GENERATE
GOTO EXIT

:GENERATE
    python ./settings-generator.py %size%
    CALL :TANGIBLECLUSTER
    GOTO EXIT

:TANGIBLECLUSTER
    FOR /L %%X in (0, 1, %size%) DO (
        IF %%X==%size% (
            CALL :CLEAN
            GOTO EXIT
        )
        ECHO Deploying TcpNode%%X
        CALL start "TcpNode%%X" call TangibleNode\bin\Debug\net5.0\TangibleNode.exe TcpNode%%X.json %stateLog% TcpNode%%X
    )

:CLEAN
    TIMEOUT 5
    FOR /L %%X in (0, 1, %size%) DO (
        IF %%X==%size% (
            CALL :TESTRECEIVER
            GOTO EXIT
        )
        del /f "TcpNode%%X.json"
        ECHO Deleted settings-file "TcpNode%%X.json"
    )
    CALL :TESTRECEIVER
    GOTO EXIT

:MISSINGPARAMETER
    ECHO Please define whether to run with STATE_LOG enabled, i.e., "$> ...cmd <true or false>"
    ECHO Please define cluster size, INT
    GOTO EXIT

:TESTRECEIVER
    IF "%tofile%" == "true" (
        ECHO Starting receiver [piping to file]
        test\TestReceiver\bin\Debug\net5.0\TestReceiver.exe >> receiver_log.txt
    ) ELSE (
        ECHO Starting receiver [in cmd]
        test\TestReceiver\bin\Debug\net5.0\TestReceiver.exe
    )
    GOTO EXIT

:EXIT