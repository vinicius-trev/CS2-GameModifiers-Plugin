@echo off

call ..\ThirdParty\Binaries\premake\premake5.exe --file=Build.lua vs2022 && (
  echo Generated Project Files Succesfully...
) || (
  echo Generated Project Files Failed, see error above...
)

@pause