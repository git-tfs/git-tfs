setlocal
set GIT_PATH=%1
if not exist "%GIT_PATH%" set GIT_PATH=git
%GIT_PATH% show --stat HEAD
