# Install, recovery, and removal

## Portable install

1. Install the x64 .NET 8 Desktop Runtime.
2. Verify `StrataShell-0.1.0-win-x64.zip` against the adjacent SHA-256 file.
3. Extract all files to a user-writable directory and run `StrataShell.exe`.
4. Configure the panel and test it before opting into the custom taskbar.
5. Enable **Run when I sign in** only after the host build passes that test.

The app writes settings and diagnostics under `%LOCALAPPDATA%\StrataShell` and
uses only the current-user `Run` registry key. It requires no administrator
rights and does not replace the configured Windows shell.

## Recovery

Normal exit restores the Explorer taskbar. An independent watchdog does the
same after an abnormal exit. If necessary, press Ctrl+Shift+Esc, end
StrataShell, and start `explorer.exe` from Task Manager's **Run new task**.

## Removal

Disable sign-in startup, exit from the tray menu, and delete the extracted
directory. Delete `%LOCALAPPDATA%\StrataShell` only if settings and diagnostics
are no longer wanted.
