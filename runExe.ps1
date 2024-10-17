$executablePath = ".\bin\Debug\net8.0\CellsGame.exe"

for ($i = 0; $i -lt 20; $i++) {
    # Start the process and wait for it to exit
    $process = Start-Process -FilePath $executablePath -NoNewWindow -PassThru
    $process.WaitForExit()

    # Get the exit code
    $exitCode = $process.ExitCode

    # Check the exit code
    if ($exitCode -ne 0) {
        Write-Host "Process exited with code $exitCode. Exiting loop."
        break
    } else {
        Write-Host "Process exited with code 0. Restarting process. Iteration: $i"
    }
}
