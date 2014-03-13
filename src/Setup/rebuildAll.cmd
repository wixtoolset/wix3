setlocal
set _B=
set _D=
set _S=

:Parse
if ""=="%1" goto Go
if /I "dutil"=="%1" set _D=build
if /I "burn"=="%1" set _B=build
if /I "ship"=="%1" set _S=-D:flavor=ship
shift
goto Parse

:Go
if ""=="%_D%" goto SkipDutil

pushd ..\dutil
nant dutil.build %_S%
popd

:SkipDutil
if ""=="%_B%" goto SkipBurn

pushd ..\burn
nant burn.build %_S%
popd
pushd ..\ext\BalExtension
nant balextension.build %_S%
popd

:SkipBurn
pushd WixBA
nant wixba.build %_S%
popd
pushd Bundle
nant bundle.build %_S%
popd
endlocal